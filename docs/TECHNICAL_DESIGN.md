# Technical Design

## Architecture

- Godot scenes own presentation: nodes, transforms, camera placement, UI layout, collision, and editor wiring.
- C# systems own gameplay logic: inventory, skills, dialogue flow, quests, learning prompts, saves, and debug commands.
- JSON files in `data/` provide definitions for items, NPCs, skills, dialogue, quests, and vocabulary.
- Save/load will start as a local serialized save model. SQLite can be considered later if JSON saves or content files become limiting.

## Separation

UI should display state and send user intent. It should not own inventory rules, XP curves, quest completion logic, or dialogue branching.

Data files should describe content. They should not require code changes for every new item, prompt, NPC, quest, or skill.

Gameplay systems should remain testable without Godot where practical. Godot-specific scripts should adapt node events, input, raycasts, and scene wiring into calls on small C# classes.

## Exclusions

Do not add networking, multiplayer, AI APIs, paid assets, proprietary assets, or copied game data in the baseline.

