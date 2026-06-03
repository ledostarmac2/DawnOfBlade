# World Data Schema (Aethelgard content types)

Extends [`DATA_SCHEMA.md`](DATA_SCHEMA.md). The canonical types there (items, skills, npcs,
dialogue, quests, equipment, shops) are unchanged. This file documents the **new** world-content
arrays introduced by the Aethelgard blueprint. All ids are lowercase `snake_case` and stable.
All coordinates are integer `TrueTile` `(x, y)` values (see `Engine.Spatial`). All durations are
in **ticks** (600 ms each).

These files are plain JSON arrays of records, parseable by the same
`DefinitionDatabase.ParseList<T>` mechanism — add matching record types and load paths when
wiring them in.

---

## `data/world/zones.json` — Zone

- `id`: Stable zone id.
- `displayName`: Player-facing region name.
- `continent`: Grouping label (e.g. `heartland`, `desert`, `wilderness`).
- `levelMin` / `levelMax`: Intended skill/combat bracket.
- `biome`: Short biome tag.
- `entryPrerequisites`: Array of tokens (`quest:<id>`, `level:<skill>:<n>`, `item:<id>`); empty = open.
- `pvp`: `none` | `open` | `dynamic`.
- `desolationLevelMax`: For dynamic-PvP zones, max combat-level disparity (else `0`).
- `connectedZones`: Array of adjacent zone ids.
- `hazards`: Array of hazard records (below); empty if safe.
- `description`: Design/lore summary.

### Hazard (embedded in `zone.hazards[]`)
- `id`: Stable hazard id.
- `kind`: `dehydration` | `pvp_disparity` | `radiation` | etc.
- `tileFlag`: The tile flag that arms it (e.g. `arid`).
- `intervalTicks`: How often it fires.
- `effect`: Token, e.g. `drain:sprint:10pct`.
- `mitigationItemId`: Item that absorbs/prevents it (or `null`).
- `mitigationConvertsTo`: Item the mitigation degrades into at 0 charges (or `null`).

## `data/world/oakhaven_assets.json` — TownAsset

- `id`: Stable asset id.
- `zoneId`: Owning zone.
- `displayName`: Player-facing label.
- `kind`: `gate` | `bank_chest` | `anvil` | `furnace` | `range` | `spinning_wheel` | `tannery` | `notice_board` | `signpost`.
- `tileX` / `tileY`: Position.
- `interactionVerb`: Verb shown on hover (`Open`, `Smith`, `Smelt`, `Cook`, `Spin`, `Read`, `Enter`).

## `data/world/resource_nodes_oakhaven.json` — ResourceNodePlacement

- `id`: Stable node id.
- `zoneId`: Owning zone.
- `displayName`: Label (e.g. "Copper Vein").
- `skillId`: Skill trained (`mining`, `woodcutting`, `fishing`, `foraging`).
- `itemId`: Item granted on success.
- `requiredLevel`: Minimum skill level to gather.
- `experience`: XP per success (matches `gathering_tiers` baseExperience).
- `tileX` / `tileY`: Position.
- `respawnTicks`: Ticks to regrow after depletion.
- `depleteChancePercent`: Chance (0–100) a success depletes the node.
- `requiredTool`: Item id of the minimum tool, or `null`.

## `data/skills/gathering_tiers.json` — GatheringTier

- `id`: Stable tier id.
- `skillId`: `mining` | `woodcutting` | `fishing`.
- `displayName`: Tier label.
- `requiredLevel`: Unlock level.
- `itemId`: Raw resource produced.
- `baseExperience`: XP per gather.
- `respawnTicks`: Default node respawn.
- `requiredTool`: Minimum tool item id (or `null`).
- `region`: Free-text where it is primarily found.

## `data/skills/smithing_recipes.json` — SmithingRecipe
Two `stage` values: `smelt` (ore→bar) and `forge` (bars→gear).
- `id`: Stable recipe id.
- `stage`: `smelt` | `forge`.
- `outputItemId`: Result item id.
- `outputQuantity`: Count produced (usually 1).
- `requiredLevel`: Smithing level.
- `experience`: XP granted.
- `inputs`: Array of `{ itemId, quantity }`.
- `barCount`: For `forge`, the bars consumed (mirrors `inputs`); `0` for smelts.
- `station`: `furnace` | `anvil`.

## `data/skills/cooking_recipes.json` — CookingRecipe
- `id`: Stable recipe id.
- `rawItemId`: Raw input.
- `cookedItemId`: Cooked output.
- `burntItemId`: Item produced on burn.
- `cookingLevel`: Level to attempt.
- `burnStopLevel`: Level at/above which burning is impossible (standard range).
- `baseBurnChancePercent`: Burn % at `cookingLevel`; interpolates to 0 at `burnStopLevel`.
- `experience`: XP on success.
- `healAmount`: Hitpoints restored by the cooked item.
- `station`: `range` | `fire`.

## `data/world/monsters_oakhaven.json` — MonsterPlacement
Combat stats map directly onto `CombatProfile(attack, strength, defense, hitpoints)` and the
`HostileActor` exports. The AI fields map onto `HostileActor.BuildBrain` / `ActorBrain`
(see [`AI_SYSTEMS.md`](AI_SYSTEMS.md)). `combatLevel` is computed by
`DawnOfBlade.Engine.Progression.CombatLevel` from the four stats (display only; never fed back
into a roll).
- `id`: Stable monster id.
- `displayName`: Player-facing name.
- `zoneId`: Owning zone.
- `archetype`: One of `Passive`, `Defensive`, `Aggressive`, `Predator` (`MonsterArchetype`).
- `combatLevel`: Derived display level (`CombatLevel.Compute(atk, str, def, hp)`).
- `maxHitpoints`, `attackLevel`, `strengthLevel`, `defenseLevel`: Raw ≤99 stats.
- `maxHit`: Top end of its damage roll.
- `attackSpeedTicks`: Ticks between its swings.
- `wanderRadius`: Chebyshev tiles it roams from spawn while idle.
- `aggroRadius`: Chebyshev tiles within which it notices a target (ignored for Passive/Defensive).
- `leashRadius`: Chebyshev tiles from spawn beyond which it abandons a chase and returns.
- `runWhileChasing`: Whether it pursues at 2 tiles/tick instead of 1.
- `dropTableId`: Id into `drop_tables.json`.
- `spawnTileX` / `spawnTileY`: Spawn anchor (the brain's `WanderArea.Anchor`).
- `respawnTicks`: Ticks to respawn after death.
- `examine`: Flavor examine text.

## `data/world/drop_tables.json` — DropTable
- `id`: Stable table id.
- `displayName`: Label.
- `rolls`: Number of independent rolls on kill (usually 1; bosses may roll the `always` band + 1 weighted band).
- `entries`: Array of drop records:
  - `itemId`: Item dropped.
  - `minQuantity` / `maxQuantity`: Inclusive quantity range.
  - `chanceDenominator`: `1/N` chance. `1` = always.
  - `rarity`: `always` | `common` | `uncommon` | `rare`.

## `data/world/transport.json` — TransportRoute
- `id`: Stable route id.
- `kind`: `road` | `ferry` | `cart` | `stair` | `portal`.
- `fromZone` / `toZone`: Endpoints.
- `fromTileX`/`fromTileY`/`toTileX`/`toTileY`: Endpoint tiles.
- `costCoins`: Gold cost (0 = free).
- `travelTicks`: Transit duration.
- `requiredQuestId`: Gate quest id (or `null`).
- `recommendedLevel`: Soft level guidance (0 = none).
