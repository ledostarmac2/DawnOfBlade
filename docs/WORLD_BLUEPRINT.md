# Dawn of Blade — World Blueprint: The World of Aethelgard

> **Status:** Content/data specification. This document is the authored creative layer that
> sits on top of the engine described in [`ENGINE_DESIGN.md`](ENGINE_DESIGN.md). All mechanics
> here resolve through the existing simulation core (`DawnOfBlade.Engine.*`); nothing in this
> document introduces a new formula. Where a number appears (a level, a drop chance, a tile
> coordinate, a heal value) it is *content* that the engine *consumes*.
>
> **Setting name reconciliation:** *Dawn of Blade* is the game; **Aethelgard** is the world it
> is set in. The two names are used as game-title and setting-name respectively throughout.
>
> **Ingestion map.** Every region and system below has a backing data file. The canonical
> record schemas (items, skills, npcs, dialogue, quests, equipment, shops) are unchanged from
> [`DATA_SCHEMA.md`](DATA_SCHEMA.md); new world-content schemas (zones, resource nodes,
> monsters, drop tables, recipes, transport) are defined in
> [`WORLD_DATA_SCHEMA.md`](WORLD_DATA_SCHEMA.md). Region content for the Core Province lives in
> the `*_oakhaven.json` / `data/world/*.json` files generated alongside this document.

---

## 0. How content binds to the engine

| Blueprint concept | Engine binding | Source of truth |
|-------------------|----------------|-----------------|
| "Level 1–30 zone" | `ExperienceTable` (cap 99) | `Engine.Progression` |
| "Drains sprint energy every 50 ticks" | 600 ms heartbeat → 50 ticks = 30 s | `Engine.Tick` |
| Resource node "coordinate" | integer `TrueTile` (x, y) | `Engine.Spatial` |
| Monster "combat level 120" | *derived aggregate* of capped (≤99) skills; display only | see §0.1 |
| "1/256 rare drop" | weighted roll vs `chanceDenominator` | `WORLD_DATA_SCHEMA.md` |
| "Anvil: 1 bar = dagger, 5 = platebody" | smithing recipe `barCount` | `smithing_recipes` |
| "Burn rate decreases with Cooking level" | `burnStopLevel` + `baseBurnChance` | `cooking_recipes` |
| Shop "sell = half buy" | `ShopService`, floored | `Engine.Economy` |
| Item market floor | `AlchemyTable` invariant coin value | `Engine.Economy` |
| Surplus high-tier deletion | `MarketSink` | `Engine.Economy` |

### 0.1 Combat level is derived, individual skills are capped at 99

Individual skills never exceed level **99** (`ExperienceTable`). A monster's advertised
**combat level** (e.g. *Magma Behemoth* "level 120") is a *display aggregate*, not a skill
level. It is computed once from the creature's combat skills so a single number can exceed 99
while every underlying stat respects the cap:

```
base   = floor( (Defense + Hitpoints + floor(Devotion / 2)) / 4 )
melee  = floor( 0.325 * (Attack + Strength) )
combatLevel = base + melee            // ranged/magic creatures swap melee term
```

This is the only place a "level" above 99 is legal, and it is never fed back into any roll —
the engine's accuracy/damage math (`CombatFormulas`) always reads the raw ≤99 skill levels.

### 0.2 Tick-time vocabulary

All durations are expressed in **ticks** (600 ms). Common conversions used below:

| Ticks | Wall-clock | Used for |
|------:|-----------|----------|
| 1 | 0.6 s | one walk step / one action resolution |
| 50 | 30 s | dehydration drain interval (Kharak) |
| 100 | 60 s | standard ore-node respawn |
| 167 | ~100 s | willow/maple regrowth |
| 500 | 5 min | mini-boss respawn |
| 5000 | 50 min | regional world-boss respawn |

---

# PART 1 — GEOGRAPHIC REGIONS & URBAN HUBS

Aethelgard's playable surface is a single contiguous integer grid. Regions are authored bands
of that grid with a **level tier** (the intended combat/skill bracket), **entry
prerequisites**, **environmental hazards**, and **transport edges**. Backing file:
[`data/world/zones.json`](../data/world/zones.json).

Continental layout (north is +Y):

```
                      ┌───────────────────────────────┐
   NORTH (high risk)  │   THE DESOLATION OF ASHEN-GRAVE │  Lv 80–120, open PvP
                      └───────────────┬───────────────┘
                      ┌───────────────┴───────────────┐
   WEST DESERT        │   THE SHIFTING SANDS OF KHARAK  │  Lv 31–60
                      └───────────────┬───────────────┘
                      ┌───────────────┴───────────────┐
   HEARTLAND (start)  │   OAKHAVEN & THE VERDAN LOWLANDS│  Lv 1–30
                      └───────────────────────────────┘
```

---

## 1. The Core Province — Oakhaven & The Verdan Lowlands (Levels 1–30)

**Zone id:** `verdan_lowlands` (overworld) containing sub-zones `oakhaven_city`,
`whispering_mill`, `verdan_copper_veins`, `sunken_crypt`. **Entry prerequisites:** none — this
is the spawn province. **Biome:** temperate grassland, hedgerow, river valley. **Hazards:**
none lethal; this is the safe-learning tier (no open PvP).

### 1.1 Urban Hub — Oakhaven

A decentralized, practical layout centered on the castle keep. The city occupies the grid
rectangle from tile **(3180, 3180)** (SW corner) to **(3260, 3250)** (NE corner). Districts:

| District | Anchor tile | Contents |
|----------|------------:|----------|
| Castle Ward (center) | (3220, 3216) | Stone keep, throne room of **Monarch Aldous IV**, notice board |
| Market Square | (3212, 3222) | General store, food stall, fur trader, market sink terminal |
| Western Banking Hub | (3196, 3220) | Bank chest (shared 28-slot stash), money-lender |
| Eastern Crafting Quarter | (3240, 3224) | Low-tier forge + anvil (3242, 3225), spinning wheels, tannery, cooking range |

**Town assets** (signposts, gates, fixed interactables) are enumerated in
[`data/world/oakhaven_assets.json`](../data/world/oakhaven_assets.json) — every gate,
bank chest, anvil, range, spinning wheel, and notice board carries a tile coordinate and an
`interactionVerb`.

**Resident NPCs** (full records in
[`data/npcs/npcs_oakhaven.json`](../data/npcs/npcs_oakhaven.json)):

| NPC id | Name | Role | Tile | Function |
|--------|------|------|-----:|----------|
| `taskmaster_donald` | Taskmaster Donald | Tutorial-giver | (3214, 3218) | Introductory quest chain, movement/combat tutorial |
| `quartermaster_hadrick` | Quartermaster Hadrick | Weaponsmith / armourer | (3241, 3226) | Bronze weapon + leather armour shop |
| `alchemist_sarah` | Alchemist Sarah | Apothecary | (3210, 3228) | Low-tier restoration vials |
| `miller_thomas` | Miller Thomas | Quest NPC | (3168, 3252) | "The Whispering Mill" quest |
| `banker_edda` | Banker Edda | Banker | (3196, 3220) | Opens the shared stash |
| `monarch_aldous` | Monarch Aldous IV | Sovereign | (3220, 3214) | Province lore, late-tier quest hook |

### 1.2 Surrounding Points of Interest

#### The Whispering Mill — `whispering_mill` (tiles ~3160–3176, 3246–3258)
A grain-processing node NW of the city. Patrolled by **Flour Pests (Lv 2)** and **Rogue
Thieves (Lv 3)**. Houses the *Oakhaven Flour Hopper* mechanism unlocked by the quest of the
same name. Sub-basement grid (`whispering_mill_cellar`) holds **Grave Spiders (Lv 8)**.

#### Verdan Copper Veins — `verdan_copper_veins` (tiles ~3150–3170, 3200–3220)
A low-tier open-pit quarry SW of Oakhaven. Copper + tin ore nodes (Mining Lv 1). Infested with
**Tunnel Rats (Lv 7)**. Primary leveling spot for early Mining and Attack.

#### The Sunken Crypt — `sunken_crypt` (three-room dungeon, entrance (3258, 3262))
An introductory dungeon: three rooms of **Skeleton Footmen (Lv 10)** leading to the mini-boss
**The Bone Overseer (Lv 15)**. First place a player meets a boss drop table.

Full monster placements with spawn tiles, respawn ticks, and combat profiles:
[`data/world/monsters_oakhaven.json`](../data/world/monsters_oakhaven.json).
Full ore/tree/fish placements: [`data/world/resource_nodes_oakhaven.json`](../data/world/resource_nodes_oakhaven.json).

### 1.3 Transport network

Edges out of the Core Province (file [`data/world/transport.json`](../data/world/transport.json)):

| Route id | Type | From → To | Cost | Gate |
|----------|------|-----------|-----:|------|
| `road_oakhaven_kharak` | walk road | Oakhaven S gate → Kharak N gate | free | Lv 31 recommended |
| `ferry_riverside_lowlands` | river ferry | Verdan riverside (3150,3240) → Willow Bank (3120,3270) | 30 gold | — |
| `cart_oakhaven_mill` | ox cart | Market Square → Whispering Mill | 5 gold | — |
| `crypt_descent` | stair | Crypt entrance → Crypt floor 1 | free | quest `the_whispering_mill` not required |

---

## 2. The Desert Frontier — The Shifting Sands of Kharak (Levels 31–60)

**Zone id:** `shifting_sands`. **Hub:** **Al-Kharak** (`alkharak_city`), a sandstone fortress
and economic crossroads with a high-security vault bank and an **alloy furnace** (smithing
bonus to coal-fueled smelts). **Entry:** walk the southern road; no hard gate, but the
**Dehydration** hazard makes it a soft level gate.

**Hazard — Dehydration.** In open desert tiles flagged `arid`, the engine drains **10 % of
sprint energy every 50 ticks** unless the player carries a `clay_waterskin`. Each drain tick
that the waterskin absorbs consumes one charge; at 0 charges it converts in-slot to
`empty_clay_jar`. Refill at any oasis tile. (Backing: `hazards` array on the `shifting_sands`
zone record + the two item records.)

POIs: **The Silica Depths** (`silica_depths`, Iron + Sandstone, **Desert Scorpions Lv 45**);
**The Obsidian Spire** (`obsidian_spire`, **Fire Cultists Lv 55** dropping magic catalysts).
NPCs: **Vizier Caleb** (political quest line), **Trader Jasmine** (silk/gem exchange),
**Farrier Robert** (high-tier pickaxes/tools). *Data for this region is stubbed in this pass
(`zones.json` contains the zone + hazard records); full POI/NPC content is a follow-up pack.*

---

## 3. The High-Risk Zone — The Desolation of Ashen-Grave (Wilderness)

**Zone id:** `ashen_grave`. Volcanic wasteland in the northernmost grid sector. Carries a
dynamic **Desolation Level (1–50)** that, per tile band, sets the maximum combat-level
disparity legal for open PvP — i.e. `abs(attackerCombat - defenderCombat) <= desolationLevel`.
POIs: **Bloodstone Crater** (exposed `bloodstone_ore`, open PvP); **Altar of Torment**
(crafts chaotic combat catalysts). Monsters: **Ash Stalkers (Lv 85)**; world boss **Magma
Behemoth (combat Lv 120)** dropping rare untradeable power-armour components. *Stubbed in
`zones.json` this pass; full content is a follow-up pack.*

---

# PART 2 — THE SKILL MATRIX & GATHERING TIER LOGISTICS

Full skill list: [`data/skills/skills_full.json`](../data/skills/skills_full.json). Gathering
tiers: [`data/skills/gathering_tiers.json`](../data/skills/gathering_tiers.json). Processing
recipes: [`data/skills/smithing_recipes.json`](../data/skills/smithing_recipes.json) and
[`data/skills/cooking_recipes.json`](../data/skills/cooking_recipes.json).

## 2.1 Extraction skills

### Mining (`mining`)
Each tier is a node type with a **required level**, the **ore item** produced, base XP, and a
**respawn** in ticks. Higher tiers respawn slower and need a better pickaxe (`requiredTool`).

| Lv | Ore | XP | Respawn (ticks) | Tool | Region |
|---:|-----|---:|---:|------|--------|
| 1 | Copper / Tin (`copper_ore`,`tin_ore`) | 18 | 5 | bronze pickaxe | Verdan Copper Veins |
| 15 | Iron (`iron_ore`) | 35 | 8 | bronze pickaxe | Silica Depths |
| 30 | Coal (`coal_ore`) | 50 | 50 | iron pickaxe | Silica Depths |
| 50 | Mithril (`mithril_ore`) | 80 | 200 | iron pickaxe | deep caves |
| 70 | Adamantite (`adamantite_ore`) | 95 | 400 | mithril pickaxe | deep wilderness |
| 85 | Runite (`runite_ore`) | 125 | 1500 | adamant pickaxe | deep wilderness |

> Iron is "high success / fast depletion": short respawn but a per-roll `depleteChancePercent`
> that empties the node on most successful swings (see node records).

### Arboriculture / Woodcutting (`woodcutting`)

| Lv | Tree (log item) | XP | Respawn | Notes |
|---:|-----------------|---:|---:|-------|
| 1 | Common (`common_logs`) | 12 | 5 | basic cooking fires |
| 15 | Oak (`oak_logs`) | 27 | 14 | multi-log yield per node |
| 30 | Willow (`willow_logs`) | 42 | 14 | riverside; high XP/hr |
| 45 | Maple (`maple_logs`) | 60 | 58 | valuable trade commodity |
| 60 | Yew (`yew_logs`) | 88 | 100 | valuable trade commodity |
| 75 | Elder (`elder_logs`) | 130 | 200 | very slow; top-tier staves |

### Fishing (`fishing`) — feeds Cooking
Levels: shrimp 1, herring 10, trout 20, lobster 40, swordfish 50, shark 76. Raw items map 1:1
into the cooking table below.

## 2.2 Processing skills

### Metallurgy / Smithing (`smithing`)
Two stages, both at fixed world anvils/furnaces in the Crafting Quarter.

**Smelting** (furnace, ore → bar): bronze = 1 copper + 1 tin; iron = 1 iron ore (50 % success
< Lv 30 without coal); steel = 1 iron + 2 coal; mithril = 1 mithril + 4 coal; etc.

**Smithing** (anvil, bars → gear). The anvil consumes a fixed **bar count** per piece:

| Piece | Bars | Smithing Lv (bronze→steel) |
|-------|-----:|----------------------------|
| Dagger | 1 | 1 / 15 / 30 |
| Shortblade / Sword | 2 | 4 / 19 / 34 |
| Helm | 3 | 7 / 22 / 37 |
| Shield (kiteshield) | 4 | 12 / 27 / 42 |
| Platebody | 5 | 18 / 33 / 48 |

### Culinary Arts / Cooking (`cooking`)
Each raw food has a `cookedItemId`, a `cookingLevel` to attempt, a **`burnStopLevel`** (the
integer Cooking level at or above which burning is impossible at a standard range), a
`baseBurnChance` (percent at the minimum level), and a **`healAmount`** for the cooked item.
Burn chance interpolates **linearly down to 0** between `cookingLevel` and `burnStopLevel`:

```
burnChance(L) = baseBurnChance * (burnStopLevel - L) / (burnStopLevel - cookingLevel)   // clamped [0, base]
```

| Raw | Cooked | Cook Lv | Burn-stop Lv | Base burn % | Heal |
|-----|--------|--------:|-------------:|------------:|-----:|
| Raw Herring | Herring | 5 | 41 | 55 | 5 |
| Raw Trout | Trout | 15 | 50 | 50 | 7 |
| Raw Lobster | Lobster | 40 | 74 | 45 | 12 |
| Raw Shark | Shark | 80 | 99 | 40 | 20 |

### Artifice (Tailoring `tailoring`, Firemaking `firemaking`, Crafting `crafting`)
Tailoring spins fibres (`spider_silk`, wool) into cloth/armour at the spinning wheels;
Firemaking burns logs for temporary light/cooking sources; Crafting cuts gems and shapes
leather. Recipe tables for these follow the same record shape as smithing/cooking and are
seeded in the recipe files (leather + silk lines included; gem-cutting is stubbed).

---

# PART 3 — ITEM CATALOGUE & DROP TABLES (summary; data in files)

Equipment tiers (full records in `data/equipment/equipment_oakhaven.json`, items in
`data/items/items_oakhaven.json`):

| Tier | Name | Req Atk/Def | Notes |
|------|------|------------:|-------|
| 1 | Vanguard Bronze | 1 | mass-produced at Oakhaven forge |
| 2 | Tempered Iron | 10 | standard mid-early |
| 3 | Refined Steel | 30 | competitive early-mid baseline |
| 4 | Cobalt | 40 | lightweight blue metal, balanced |
| 5 | Dread-Iron | 60 | heavy mitigation |

Drop tables use rarity bands by `chanceDenominator`: Common 1–10, Uncommon ~50, Rare ~256,
plus a guaranteed (`1`) "always" band. The Bone Overseer and Broodmother tables are authored
in [`data/world/drop_tables.json`](../data/world/drop_tables.json).

---

# PART 4 — NARRATIVE QUEST STRUCTURE (Oakhaven pass)

**Quest 1 — "The Whispering Mill"** is authored in full as ingestible data this pass:
quest record (`data/quests/quests_oakhaven.json`, id `the_whispering_mill`), the supporting
NPC `miller_thomas`, and its dialogue tree (`data/dialogue/dialogue_oakhaven.json`). Stages,
the `rusty_crowbar` / `acrid_venom_jar` quest items, and the reward tokens
(`xp:cooking:500`, `item:coins:300`) are all present. Quest 2 ("The Vizier's Secret") and
Quest 3 are scoped as Kharak-region follow-up packs and intentionally left as design notes
here.
