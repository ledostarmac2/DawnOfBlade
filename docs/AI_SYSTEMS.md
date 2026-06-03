# AI, Levels & Movement Systems

This document covers the engine-pure systems added under `DawnOfBlade.Engine.*` for actor
levels, aggression, wandering, and chase pathfinding, plus how the Godot scene drives them. All
logic is deterministic and unit-tested off-engine (`tests/AiTests.cs`); the Godot layer is a thin
adapter.

---

## 1. Level system — `Engine.Progression.CombatLevel`

A single formula is the source of truth for both the player and every monster, so "who is
stronger than whom" is symmetric everywhere:

```
combatLevel = floor( 0.25 * (Defense + Hitpoints) + 0.325 * (Attack + Strength) )
```

- Each input is floored to 1; the result is floored to 1.
- With all four melee skills at the cap (99) the maximum combat level is **113**.
- `CombatProfile.CombatLevel` now delegates here, so the HUD's "Combat Lv" and a monster's level
  come from the same place. (Verified: `CombatLevelTests`.)

A monster's level is therefore just a function of its stats — set `attackLevel/strengthLevel/
defenseLevel/maxHitpoints` in `data/world/monsters_oakhaven.json` (or on the `HostileActor`
exports) and the level follows.

## 2. Aggression — `Engine.Ai.MonsterArchetype` + `AggressionPolicy`

Whether a monster attacks you is a function of **type** and **relative level**:

| Archetype | Initiates? | Level rule |
|-----------|-----------|------------|
| `Passive` | never | — (used for villagers/critters) |
| `Defensive` | never (retaliates only) | — |
| `Aggressive` | within aggro radius | ignores you once your combat level > `2 * monsterLevel + 1` |
| `Predator` | within aggro radius | always, regardless of your level (bosses/hunters) |

`AggressionPolicy.WillEngage(archetype, selfLevel, targetLevel, distance, aggroRadius)` is the
one predicate the AI and any threat indicator should call. (Verified: `AggressionPolicyTests`.)

## 3. Behavior — `Engine.Ai.ActorBrain`

One class drives **both** monsters and NPCs; only the archetype differs. Each tick, `Tick(grid,
perception)` runs a four-state machine:

```
        ┌─────────── target in aggro range & disparity OK ──────────┐
        │                                                           ▼
  ┌──────────┐  idle timer  ┌────────────┐  arrive   ┌────────┐  target  ┌──────────┐
  │   Idle   │ ───expires──▶│  Wandering │ ────────▶ │  Idle  │          │ Chasing  │
  └──────────┘              └────────────┘           └────────┘          └──────────┘
        ▲                                                                   │  lost target /
        │                                                                   ▼  past leash
        │                          ┌────────────┐                          │
        └──────── home ────────────│  Returning │◀─────────────────────────┘
                                   └────────────┘
```

- **Wandering** picks a random reachable tile within `WanderArea.WanderRadius` of the spawn
  anchor and walks there, then idles a random number of ticks. This keeps every actor inside a
  **fixed area** — it can never drift across the map.
- **Chasing** re-paths toward the target every tick using *adjacency* pathing (it stops one tile
  away and reports `InAttackRange` so the combat layer can swing). Aggro is re-evaluated each
  tick.
- **Leashing**: if the actor strays past `LeashRadius` from its anchor, it stops chasing and
  **returns home**, ignoring the target until it is back — so it can't be kited indefinitely.
- A `Passive` actor never enters `Chasing`, so villagers get bounded strolling for free.

Determinism comes from an injected `IRandomSource`. (Verified: `ActorBrainTests`, including a
full lure-past-leash-and-return integration scenario.)

## 4. Pathfinding — `Engine.Spatial.GridPathfinder`

Two additions, both backward compatible (existing `FindPath(grid, start, goal)` is unchanged):

- **`maxExpansion` budget** (optional, default unbounded): caps tiles dequeued per search. Chasers
  re-path every tick, so an unbounded flood-fill on a large open map would be wasted work; the
  brain passes a budget sized to the actor's territory.
- **`FindPathAdjacent(grid, start, target, maxExpansion)`**: shortest path to a walkable tile
  *next to* the target rather than onto it — the correct primitive for a melee pursuer, since the
  target tile is occupied. Returns empty when already adjacent (attack, don't move).

(Verified: `PathfinderAdjacencyTests`.)

### Review note on the world-grid pathfinder

There is a second, older spatial stack under `src/World/Grid/` used by the live open world
(`GridCoordinate`, A* `GridPathfinder`, `GridMovementRules`). One inconsistency to be aware of
when that layer is extended: `GridMovementRules.CanStep` permits **diagonal** one-tile steps (with
corner-cutting prevention), but `World.Grid.GridPathfinder` only expands the **4 orthogonal**
neighbours. So generated routes are never diagonal even though a step validator would allow them.
That is not a bug for the current engine work (the `Engine.Spatial` brain is strictly
4-connected), but the two should be reconciled before relying on diagonal movement in the world
layer.

## 5. Driving it from the Godot scene

The brain→transform bridge now exists as a production node, `Interaction.ActorAiAgent`, and is
exercised in the real engine (see §6). The actors carry their configuration and build a brain:

- `HostileActor`: exports `Archetype`, `WanderRadius`, `AggroRadius`, `LeashRadius`,
  `RunWhileChasing`; `BuildBrain(anchor, random)` returns a configured `ActorBrain`.
- `PrototypeNpc`: exports `WanderRadius`; `BuildBrain(anchor, random)` returns a `Passive` brain.
- `ActorAiAgent.Configure(body, brain, grid, tileSize)` then `Tick(perception)` each heartbeat
  advances the brain one tile and snaps the body's `GlobalPosition` to the mapped world tile
  (tile X → world X, tile Y → world Z, anchored at the body's spawn placement).

There is already a 0.6 s heartbeat in `GameManager.ProcessLocalTick`. The remaining scene wiring is:

1. On spawn, snapshot each actor's tile as its anchor, call `BuildBrain(anchor, random)`, add an
   `ActorAiAgent` child, and `Configure(...)` it.
2. Provide a `CollisionGrid` for the actor's region (or adapt the world grid — see below).
3. Each tick, build the `Perception` — `Perception.Of(playerTile, playerProfile.CombatLevel)`
   when the player is loaded, else `Perception.None` — and call `agent.Tick(perception)`.
4. If the returned `step.InAttackRange` and the actor is hostile, route an attack through the
   existing `GameManager.AttackHostile` path.
5. (Optional polish, a graphics concern) interpolate the body toward the agent's tile between
   ticks instead of snapping, the way `ClickToMoveController` does for the player.

**Grid adapter note.** `ActorBrain`/`ActorAiAgent` consume `Engine.Spatial.CollisionGrid` (a fixed
`bool[,]` plane). The live world uses chunked walkability (`World.Grid`,
`Func<GridCoordinate,bool>`). For a first integration, populate a `CollisionGrid` sized to the
active region from the world's walkability data once per region load. Longer term, the two spatial
stacks should converge (see §4) — that convergence is the natural home for this AI.

## 6. Testing — two layers

The project is verified at two layers; both must be green.

### Pure logic — `dotnet test`
All decision logic (levels, aggression, pathfinding, the brain state machine) is engine-pure and
covered by `tests/AiTests.cs`. This runs under plain xUnit and **does not** boot Godot:

```
dotnet test tests/DawnOfBlade.Tests.csproj
```

### In-engine behavior — headless Godot
Plain `dotnet test` links the GodotSharp managed assemblies but never starts Godot's native
runtime, so it cannot instantiate `Node`-derived types (`HostileActor`, `PrototypeNpc`,
`ActorAiAgent`). Those are covered by a headless runner — `test/HeadlessTests.tscn` hosting
`Testing.HeadlessTestMain` — which instantiates the real nodes in a live `SceneTree` and asserts
their actual `Node3D` transforms move (chase, leash-home, passive wander). Run it with:

```
pwsh tools/run-godot-tests.ps1     # builds C#, then launches Godot headless; exit code = failures
```

One-time setup (the binary is git-ignored under `tools/godot/`):

```powershell
# Download the Godot 4.2.2 .NET build this project targets and VERIFY its checksum before use:
$base = 'https://github.com/godotengine/godot/releases/download/4.2.2-stable'
Invoke-WebRequest "$base/SHA512-SUMS.txt" -OutFile tools/godot/SHA512-SUMS.txt
Invoke-WebRequest "$base/Godot_v4.2.2-stable_mono_win64.zip" -OutFile tools/godot/godot_mono.zip
# compare (Get-FileHash tools/godot/godot_mono.zip -Algorithm SHA512).Hash against the sums file,
# then Expand-Archive tools/godot/godot_mono.zip into tools/godot/.
```

Set `$env:GODOT_BIN` to point the runner at a Godot binary in a different location (e.g. CI).
