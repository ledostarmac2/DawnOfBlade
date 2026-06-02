# Production Backend Architecture

A staged path from today's single-process prototype to a server-authoritative, horizontally
zoned backend for Dawn of Blade. It specifies service boundaries, the Redis dirty-sync cache,
persistence schemas, atomic bank/trade transactions, zone handoff, and a rollout order. This is a
design document only — it changes no source. It deliberately reuses the existing engine-independent
domain (`src/Communication`, `src/Simulation`, `src/World/Grid`, `src/Inventory`, `src/Skills`,
`src/Combat`, `src/Quests`, `src/Shops`) so the server runs the *same* C# rules the client already
links, not a reimplementation.

## 0. Where we are today

- **Auth**: `src/Auth/AccountStore.cs` + `Session.cs` — local credential check, no network.
- **Persistence**: `src/Save/SaveService.cs` writes one JSON blob to `user://savegame.json` per
  player on a 10 s autosave + on quit. Single machine, single player, last-write-wins.
- **Simulation**: `src/Simulation/SimulationLoop` is a deterministic 600 ms tick core, currently
  driveable in-process; `src/World/Grid` provides tiles, 32×32 chunks, 3×3 relevance, A*, and LOS.
- **Messaging**: `src/Communication` is an in-process bus with transport-neutral envelopes
  (`MessageEnvelope<T>`, `CorrelationId`/`CausationId`) explicitly built to later cross a wire.

The target architecture keeps every one of these and swaps only the *edges* (transport, cache,
storage) so the domain code is unchanged.

## 1. Service boundaries

```
            ┌────────────┐     ┌──────────────────┐
 Client ───▶│  Gateway   │────▶│  Zone Server (N)  │  authoritative 600ms tick per zone
 (Godot)    │  (auth,    │     │  - SimulationLoop │
            │  routing,  │     │  - Grid + AI FSM  │
            │  relevance)│◀────│  - dirty tracking │
            └─────┬──────┘     └───────┬───────────┘
                  │                    │ dirty writes / reads
                  │             ┌──────▼───────┐    ┌──────────────────┐
                  │             │  Redis (hot) │◀──▶│ Persistence Svc   │
                  │             │  live state  │    │ (batched flush)   │
                  │             └──────────────┘    └─────────┬─────────┘
                  │                                           │
            ┌─────▼───────┐   ┌──────────────────┐     ┌──────▼──────┐
            │ Account Svc │   │ Market Svc        │     │  SQL (cold) │
            │ (login/JWT) │   │ (limit-order book)│     │  durable    │
            └─────────────┘   └──────────────────┘     └─────────────┘
```

| Service | Responsibility | Owns |
| --- | --- | --- |
| **Gateway** | TLS, session/JWT validation, packet framing, chunk relevance fan-out, zone routing | no game state |
| **Zone Server** | Authoritative 600 ms tick for one contiguous zone; movement, combat triangle, AI FSM, gathering/processing loops | live entities in its zone |
| **Account Service** | Registration, login, session tokens; the network successor to `AccountStore` | credentials, account metadata |
| **Persistence Service** | Batched read/write between Redis and SQL; the network successor to `SaveService` | durability |
| **Market Service** | Asynchronous limit-order matching + escrow (Part 5.2); independent of login state | order book, escrow ledger |
| **Redis** | Hot live state, dirty-set, cross-zone handoff payloads, distributed locks | ephemeral truth-in-flight |
| **SQL** | Durable system of record | cold truth |

Boundary rules:
1. The client talks **only** to the Gateway. Zone servers are never directly addressable.
2. A player is owned by exactly **one** zone server at a time (the one running their current zone).
3. Game services never share a database table directly; they exchange `MessageEnvelope<T>` messages
   (the same contracts as `src/Communication`, serialized) so correlation/causation tracing is uniform.

## 2. Redis dirty-sync cache

The tick loop must never block on SQL. Redis is the authoritative *live* store; SQL is written
behind it.

- **Live keys**: `player:{id}` (hash of inventory, skills XP, equipment, position, vitals),
  `zone:{id}:entities`, `market:book:{itemId}`.
- **Dirty set**: every authoritative mutation also `SADD dirty:players {id}`. The zone server writes
  to Redis synchronously within the tick (O(1) hash ops); it never writes SQL inline.
- **Flush cycle**: the Persistence Service runs an independent timer (e.g. every 5 s and on graceful
  shutdown). It `SPOP`s a bounded batch from `dirty:players`, reads each hash, and upserts SQL in one
  transaction, then clears the entry. A crash loses at most one flush interval, never a torn write.
- **Write-through invariant**: Redis is always ahead of or equal to SQL. On cache miss, the
  Persistence Service hydrates Redis from SQL, then the zone server reads Redis.
- **Locks**: cross-cutting operations (bank, trade settle, zone handoff) take a short Redis lock
  (`SET key val NX PX 3000`) keyed by player id to serialize against the owning tick.

This generalizes today's `SaveService` "serialize the whole `SaveGame` blob" into "serialize dirty
fields per player, batched" — the `SaveGame` shape becomes the SQL row shape (§3).

## 3. Persistence schemas

Cold storage mirrors the existing `SaveGame`/domain records, normalized:

```
accounts(account_id PK, username UNIQUE, pw_hash, pw_salt, created_at, last_login)
characters(character_id PK, account_id FK, name, appearance_json, zone_id, tile_x, tile_z, updated_at)
character_skills(character_id FK, skill_id, experience BIGINT, PRIMARY KEY(character_id, skill_id))
character_inventory(character_id FK, slot SMALLINT, item_id, quantity, PRIMARY KEY(character_id, slot))
character_equipment(character_id FK, slot, item_id, PRIMARY KEY(character_id, slot))
character_quests(character_id FK, quest_id, stage SMALLINT, flags_json, PRIMARY KEY(character_id, quest_id))
bank_items(character_id FK, slot, item_id, quantity, PRIMARY KEY(character_id, slot))
wallet(character_id PK, coins BIGINT)
market_orders(order_id PK, side, item_id, unit_price, qty_total, qty_remaining, owner_id, created_tick, state)
market_escrow(order_id FK, item_id, quantity, coins)   -- locked goods/gold backing open orders
ledger(txn_id PK, kind, character_id, item_id, delta, coins_delta, tick, correlation_id)  -- append-only audit
```

Notes:
- `experience` is `BIGINT` and the level is derived (never stored) from the logarithmic curve in
  `SkillProgress`, keeping XP authoritative and the 99 cap a pure function.
- `appearance_json` reuses the existing `Appearance` serialization; `flags_json` reuses the
  flag-based quest model (Part 6.1).
- `ledger` is append-only and stamped with the originating `correlation_id` from the message
  envelope, so any item/coin movement is auditable end-to-end — the primary dupe-detection tool.

## 4. Atomic bank and trade transactions

All item/coin movement obeys the blueprint's "atomic, server-only" rule (Part 5.1). Every operation
is a single logical transaction over Redis (live) with a ledger append, flushed to SQL by §2.

- **Bank deposit/withdraw**: lock `player:{id}`; validate slot bounds and quantities against the live
  hash; move between `character_inventory` and `bank_items`; append two ledger rows (out/in); unlock.
  Reject (no-op) if validation fails — the client only ever sees the resulting authoritative snapshot.
- **Limit-order place (sell)**: lock player; remove goods from inventory into `market_escrow`
  instantly; insert `market_orders(side=sell, qty_remaining=qty)`; ledger the escrow move; unlock.
- **Limit-order place (buy)**: lock player; remove `unit_price × qty` coins from `wallet` into
  escrow; insert buy order; ledger; unlock.
- **Matching engine** (Market Service, async, independent of login state — Part 5.2): on any new
  order, scan the book for `buy.price ≥ sell.price`; on a match, transfer goods to the buyer's
  collection depot and coins to the seller's depot, decrement `qty_remaining`, append ledger rows,
  and close filled orders. Runs as its own loop, not on a zone tick, so trade settles even when both
  parties are offline.
- **Idempotency**: each client-initiated transaction carries a `MessageId`; the server records
  processed ids (short TTL in Redis) so a retried packet cannot double-apply.

## 5. Zone handoff

A player crossing a zone border (the contiguous eco-zones of Part 2.2) must migrate ownership
between zone servers without duplication or a visible stall.

```
1. Source zone detects the player's next tile is in target zone's chunk range.
2. Source takes the player lock, marks the player `Migrating`, stops applying their commands.
3. Source serializes a handoff payload (live hash + in-flight command buffer slice) to
   redis key handoff:{player} and publishes ZoneHandoffRequested{player, target} to the Gateway.
4. Gateway tells the client to suspend input and rebinds its session route to the target zone.
5. Target zone loads handoff:{player}, spawns the entity at the border tile on its next tick,
   clears Migrating, deletes the handoff key, publishes ZoneHandoffCompleted.
6. Gateway resumes the client; the brief gap is covered by client-side interpolation (Part 1.1).
```

Invariants: the player is owned by exactly one zone at every instant; the handoff payload is the
single source of truth during transfer; the Redis lock prevents the source from applying late
commands after step 2; a target-load failure rolls back to the source (the player was never removed
from Redis, only marked `Migrating`).

## 6. Determinism & anti-cheat alignment

- Zone servers run the **same** `SimulationLoop` + domain assemblies as the client links, so server
  and client compute identical results from identical inputs; the client only predicts/interpolates.
- The client sends `ISimulationCommand`s (Part 1.1 tick-buffered input); the server schedules them on
  its authoritative loop and broadcasts verified results. The client never asserts state.
- Chunk relevance filtering (`src/World/Grid/ChunkInterestManager`) runs at the Gateway so a client
  only receives events within its 3×3 chunk window — bandwidth control *and* an information-leak guard.

## 7. Staged rollout

Each stage ships independently and keeps the game playable:

1. **Stage 0 — extract domain (no behavior change).** Ensure all authoritative rules live in
   engine-independent assemblies (already true for skills/combat/inventory/grid/simulation). Done
   incrementally; no servers yet.
2. **Stage 1 — local loopback server.** Run `SimulationLoop` + domain behind the in-process
   `ICommunicationService`; `GameManager` talks to it as if remote. Proves the command/event contract
   without networking. (This is the natural consumer of LQ-004.)
3. **Stage 2 — split Account Service + JWT.** Replace `AccountStore` direct calls with a network auth
   service; client stores a token in `Session`. Saves still local.
4. **Stage 3 — Persistence Service + Redis dirty-sync.** Move `SaveService` behind the cache; SQL
   schema from §3; per-field dirty writes replace whole-blob saves.
5. **Stage 4 — single authoritative Zone Server + Gateway.** One zone, real transport, server-side
   tick, client prediction + rubber-banding. Relevance filtering at the Gateway.
6. **Stage 5 — Market Service.** Asynchronous limit-order book + escrow; offline settlement.
7. **Stage 6 — multiple zones + handoff.** Add zone servers per eco-zone and the §5 migration
   protocol; scale horizontally by zone.

Risks tracked per stage: save-format migrations (version `SaveGame`), clock skew between zones
(authoritative tick is per-zone; handoff re-seeds tick-local state), and Redis/SQL divergence
(reconciled by the write-through invariant in §2 and the append-only `ledger`).

## 8. Coordination with other slices

- **LQ-004 `src/Simulation`** is the deterministic core every zone server runs (Stage 1+).
- **`src/Communication`** envelopes are the on-wire message contract (Stages 2+); no new contract.
- **`src/World/Grid`** supplies tiles, chunks, relevance, A*, LOS used by zone servers and the
  Gateway's relevance fan-out.
- **HUD** (see `docs/HUD_ARCHITECTURE.md`) consumes authoritative snapshots only; it is transport-
  agnostic and needs no change when state begins arriving from a zone server.
