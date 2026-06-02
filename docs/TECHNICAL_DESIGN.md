# Technical Design

## Architecture

- Godot scenes own presentation: nodes, transforms, camera placement, UI layout, collision, and editor wiring.
- C# systems own gameplay logic: inventory, skills, dialogue flow, quests, learning prompts, saves, and debug commands.
- JSON files in `data/` provide definitions for items, NPCs, skills, dialogue, quests, equipment, shops, and vocabulary.
- Save/load uses a local, account-scoped serialized save model (`SaveService` + `SaveSerializer`). A networked persistence service is designed (not yet built) in `docs/PRODUCTION_BACKEND_ARCHITECTURE.md`.

## Engine-independent layers

These foundations are pure C# (no Godot dependency) so they are unit-tested off-engine and can later
run on a server unchanged:

- `src/Communication` — message bus (pub/sub + request/response) with transport-neutral envelopes.
- `src/Simulation` — deterministic 600 ms tick loop with a buffered command queue and system hooks.
- `src/World/Grid` — integer tiles, chunks, interest filtering, A* pathing, and line-of-sight.
- `src/UI/Presentation` — HUD state models the Godot HUD binds to (see `docs/HUD_ARCHITECTURE.md`).
- Plus the gameplay domain: inventory, skills, combat, quests, shops, equipment, learning.

## Separation

UI should display state and send user intent. It should not own inventory rules, XP curves, quest completion logic, or dialogue branching.

Data files should describe content. They should not require code changes for every new item, prompt, NPC, quest, or skill.

Gameplay systems should remain testable without Godot where practical. Godot-specific scripts should adapt node events, input, raycasts, and scene wiring into calls on small C# classes.

## Exclusions

Do not add AI APIs, paid assets, proprietary assets, or copied game data.

Networking and multiplayer are the project's long-term direction but are **deferred**: keep the game
local-first and the authoritative rules engine-independent until the staged backend in
`docs/PRODUCTION_BACKEND_ARCHITECTURE.md` is implemented. Do not add a live transport, server, or
external service until that stage is reached (see also `docs/CONTRIBUTING.md`).

