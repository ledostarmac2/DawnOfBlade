# Dawn of Blade — Tick Simulation Engine Design

This document specifies the authoritative server simulation for **Dawn of Blade**, an
original classic-sandbox MMORPG. The engine adopts well-known *genre mechanics* (a fixed
discrete heartbeat, true-tile movement, an exponential level curve, probabilistic melee, and
a sink-driven economy). All **content** — world, names, lore, items, skills, and spells — is
original to Dawn of Blade. Formulas below are functional specifications, not lore.

> Scope: this is the engine-independent simulation core (`DawnOfBlade.Engine.*`, pure .NET 8,
> no Godot). The Godot client renders interpolated state; the server owns truth.

---

## Pillar 1 — The 600 ms Discrete Heartbeat

The world advances in fixed **ticks** of `600 ms`. Nothing in the simulation happens between
ticks: client inputs are *batched* during a tick window and *applied* in one deterministic
sequence at the next tick boundary.

### Tick phase ordering

All queued actions are sorted by phase, then by submission order (stable), then executed:

| Phase | Name        | Contents                                                            |
|-------|-------------|---------------------------------------------------------------------|
| 0     | Interface   | UI interactions, inventory reordering, equipment/overhead swaps     |
| 1     | Consumption | Item usage, resource consumption, health modifications              |
| 2     | Movement    | Coordinate translation + pathfinding steps                          |
| 3     | Combat      | Accuracy rolls, damage distribution, mitigation evaluation          |

Because Phase 0 runs before Phase 3, an overhead mitigation toggled in the same tick is
already applied (or cleared) by the time damage is computed.

### Action queue & "combo intake"

Phase 1 processes **all** queued consumption actions in the same tick. A consumption action
carries a flag for whether it triggers an animation delay. Queuing one animation-delay action
together with one standard action in a single 600 ms frame lets both resolve before any
consecutive-use cooldown gate engages on the following tick. This is the engine's
"combo intake" behavior, expressed entirely through same-tick Phase 1 batching.

### Mitigation timing

Overhead/environmental mitigations expose an `IsActive` predicate. Combat (Phase 3) reads that
predicate **at the instant** damage is calculated — never cached earlier in the tick — so a
mitigation flipped during Phase 0 is honored exactly.

### Implementation

`TickEngine.ProcessTick()` is synchronous and deterministic (ideal for tests and headless
servers). `TickLoop` drives it on a real cadence via `PeriodicTimer(600ms)`.

---

## Pillar 2 — True-Tile Spatial Matrix

A flat 2D integer grid. The authoritative entity position is its **True Tile**; the client
interpolates a visual model toward it but never owns it.

### Velocity profiles

- **Walking:** exactly **1 tile / tick**.
- **Running:** exactly **2 tiles / tick**.

### Running and skipped tiles

When running, an entity passes through an **intermediate** tile and **lands** on the second.
Environmental trigger flags, obstacle bounds, and area-of-effect trap nodes are evaluated
**only on landing tiles**. Intermediate tiles are skipped — a trap on an intermediate tile is
not sprung. (If only one path step remains, the entity moves one tile and that tile is a
landing tile.)

### Line of sight, pathfinding, and safespotting

- Projectiles/spells use straight-line raycasting (`LineOfSight.HasProjectilePath`) that
  passes over *low* terrain but is stopped by *solid* blockers.
- Entity AI pathfinds around *solid* corners (`GridPathfinder`, 4-connected BFS).

The asymmetry — projectiles fly over a low wall while melee AI must walk around a solid one —
is what enables line-of-sight trapping ("safespotting") as an emergent tactic.

---

## Pillar 3 — Exponential Progression

Skills are capped at level **99**. Cumulative XP required to *reach* level `L`:

```
XP(L) = floor( 0.25 * Σ_{i=1}^{L-1} floor( i + 300 * 2^(i/7) ) )
```

Consequences (verified by unit tests):

- `XP(2)   = 83`
- `XP(99)  = 13,034,431`  (total to max)
- `XP(92)  = 6,517,253`   ≈ the 50 % midpoint of the road to 99

`ExperienceTable` precomputes the table once and offers `XpForLevel(L)` and `LevelForXp(xp)`.

---

## Pillar 4 — Probabilistic Combat

Weapons resolve on per-weapon attack-tick intervals. Each swing is two phases.

### Phase 1 — Accuracy

```
MaxRoll          = EffectiveLevel * (EquipmentBonus + 64)
attackRoll       = randomInt[0 .. A]          // A = attacker MaxRoll
defenceRoll      = randomInt[0 .. D]          // D = defender MaxRoll
accurate         = attackRoll > defenceRoll
```

Closed-form hit probability (for tooltips/analysis), equivalent to the roll comparison:

```
if A > D:   P = 1 - (D + 2) / (2 * (A + 1))
else:       P = A / (2 * (D + 1))
```

### Phase 2 — Damage

If `accurate`, roll `randomInt[0 .. MaxHit]`. The engine distinguishes:

- **Accuracy miss** → forced `0` damage (`Accurate = false`).
- **Accuracy success that rolls `0`** → a landed hit dealing `0` (`Accurate = true, Damage = 0`).

`CombatFormulas` holds the pure math; `AttackResolver` performs the rolls through an
`IRandomSource` so combat is reproducible in tests.

---

## Pillar 5 — Fixed Asymmetric Economy

- **28-slot inventory grid** (`GridInventory`): a hard 28-slot ceiling where gear, supplies,
  and gathered resources all consume slots with identical friction. Stackable items share a
  slot; non-stackables each take one.
- **Alchemy floor** (`AlchemyTable`, the in-world *Aurum Rite*): converts any item id to a
  hardcoded, invariant coin amount, establishing an absolute market floor price.
- **Deflationary market sink** (`MarketSink`): a global transaction tax pools coins and
  automatically buys surplus high-tier item ids off the market pool and **permanently deletes**
  them, countering long-term asset inflation.

---

## Module map

| Namespace                         | Responsibility                                  |
|-----------------------------------|-------------------------------------------------|
| `DawnOfBlade.Engine.Tick`         | Heartbeat, phases, action queue, mitigation     |
| `DawnOfBlade.Engine.Spatial`      | True tiles, walk/run movement, LoS, pathfinding |
| `DawnOfBlade.Engine.Progression`  | Experience table & level mapping                |
| `DawnOfBlade.Engine.Combat`       | Accuracy/damage formulas & resolver             |
| `DawnOfBlade.Engine.Economy`      | Grid inventory, alchemy floor, market sink      |

All modules are pure C# and unit-tested under `tests/`.
