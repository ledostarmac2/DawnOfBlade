# Roadmap

Status legend: ✅ done · 🔧 in progress · ⬜ not started

## Phase 0: Repo Baseline ✅

Create the project structure, docs, Godot placeholders, data examples, and initial C# systems.

## Phase 1: Player Movement, Camera, Prototype Map ✅

Wire a playable scene with a placeholder map, player capsule, orbit camera, click-to-move target movement, and navigation TODOs.

## Phase 2: Interaction System and NPC Dialogue ✅

Add click detection, interactable NPCs, dialogue state, and a simple dialogue popup. Dialogue now reads from `data/dialogue/dialogue.example.json`.

## Phase 3: Inventory and Skills ✅

Load item and skill definitions via `DefinitionDatabase`, track inventory, add XP, and expose debug UI (status/quest readout, craft/save/load buttons) for testing.

## Phase 4: One Gathering Skill and One Crafting Skill ✅

Gather Sunleaf (foraging XP) from a resource node and craft a Practice Chisel (crafting XP) from 2 Sunleaf.

## Phase 5: Language-Learning Prompt System ✅

Vocabulary is loaded from `data/vocabulary/vocabulary.example.json` and presented as multiple-choice prompts through NPC dialogue; correct answers grant language XP and unlock terms.

## Phase 6: Quest System ✅

`QuestLog`/`QuestState` track objectives and progress; rewards (`xp:` / `item:` tokens) are granted on completion and shown in the HUD. The "First Words" quest is wired to gathering and prompts.

## Phase 7: Save/Load ✅

`SaveService` + `SaveSerializer` persist player position, inventory, skill XP, equipment, appearance, quest progress, and unlocked vocabulary to an account-scoped `user://savegame_<account>.json` (with one-time migration from the legacy `user://savegame.json`); the save loads automatically on entering the game, autosaves periodically, and writes on quit.

## Phase 8: Combat Prototype ✅

`CombatStats` plus a clickable `HostileActor` (Training Dummy): trading blows, defeat state, timed enemy revival, and player knock-out recovery.

## Phase 9: Polish and Content Tools ✅

Branded login/account-creation screen, data-driven content workflow, and a runnable xUnit test suite (`tests/`) including data-schema validation. Further art direction remains an ongoing effort.

## Phase 10: Content Expansion and Toolchain ✅

Reconciled two parallel branches into one buildable game. Expanded the skill set to a RuneScape-style spread (25 skills on the 99-level curve; combat trains attack/strength/defense/hitpoints by style), grew the language vocabulary to 50 themed Spanish→English entries, and added woodcutting/mining/fishing resource nodes (data-driven `ResourceNode`). Vendored a self-contained **.NET 8** project toolchain under `.tools/` via `tools/setup-dev.ps1`; VS Code's .NET Install Tool manages the separate runtime used by C# extensions.

## Phase 11: Open World and MMORPG Foundations ✅

Reframed the prototype toward a server-authoritative, grid-based sandbox MMORPG while keeping it
locally playable. Added an open overworld (`OpenWorldBuilder`) with scenery, primitive **humanoid
visuals** (`HumanoidVisual`) and in-game appearance customization, and a per-account save. Built the
engine-independent foundations the future server will run unchanged:

- **Communication bus** (`src/Communication`): in-process pub/sub + request/response with
  transport-neutral envelopes; adopted by `GameManager` to publish gather/level-up/defeat events.
- **Simulation tick** (`src/Simulation`): deterministic 600 ms loop with a buffered command queue
  (late commands deferred to the next tick), monotonic clock, and ordered system extension points.
- **Grid world** (`src/World/Grid`): integer tiles, 32×32 chunks, 3×3 interest filtering, cardinal
  A* pathing, Bresenham line-of-sight, and zone-risk profiles.
- **HUD presentation models** (`src/UI/Presentation`): coordinate/tab state, trailing vital gauges,
  run-energy rules, and hit-marker metadata — engine-independent and unit-tested.

Architecture for the next stages is written up in `docs/HUD_ARCHITECTURE.md` and
`docs/PRODUCTION_BACKEND_ARCHITECTURE.md`.

## Next Steps

- Drive gameplay through `src/Simulation` (Stage 1 of the backend rollout): route movement, gathering,
  and combat as tick-scheduled commands instead of immediate calls.
- Gate scenery, NPCs, and resource nodes through `ChunkInterestManager`, and apply zone-risk rules
  (safe / contested / wilderness) to combat and death.
- Build the responsive HUD `Control` tree from `docs/HUD_ARCHITECTURE.md`, replacing the prototype HUD.
- Wire the remaining artisan skills (smithing, cooking, firemaking, fletching, …) to crafting actions.
- Persist in-progress quest objective counts; expand quests/dialogue/shops content and hostile actors.
