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

`SaveService` + `SaveSerializer` persist player position, inventory, skill XP, quest progress, and unlocked vocabulary to `user://savegame.json`; the save loads automatically on entering the game.

## Phase 8: Combat Prototype ✅

`CombatStats` plus a clickable `HostileActor` (Training Dummy): trading blows, defeat state, timed enemy revival, and player knock-out recovery.

## Phase 9: Polish and Content Tools ✅

Branded login/account-creation screen, data-driven content workflow, and a runnable xUnit test suite (`tests/`) including data-schema validation. Further art direction remains an ongoing effort.

## Phase 10: Content Expansion and Toolchain ✅

Reconciled two parallel branches into one buildable game. Expanded the skill set to a RuneScape-style spread (25 skills on the 99-level curve; combat trains attack/strength/defense/hitpoints by style), grew the language vocabulary to 50 themed Spanish→English entries, and added woodcutting/mining/fishing resource nodes (data-driven `ResourceNode`). Vendored a self-contained **.NET 8** project toolchain under `.tools/` via `tools/setup-dev.ps1`; VS Code's .NET Install Tool manages the separate runtime used by C# extensions.

## Next Steps

- Wire the remaining artisan skills (smithing, cooking, firemaking, fletching, …) to crafting actions.
- Persist in-progress quest objective counts and unlocked vocabulary across saves.
- Expand quests/dialogue/shops content and add more hostile actors with Slayer-style rewards.
