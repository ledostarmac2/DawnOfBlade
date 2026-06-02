# Dawn of Blade

Dawn of Blade is an original Godot 4 + C# low-poly 3D RPG sandbox. It is inspired by classic tile/grid-aware fantasy RPG controls and progression loops: click-to-move navigation, skills, XP progression, inventory, NPC dialogue, quests, shops, basic combat, and language-learning mechanics.

This project is not a RuneScape private server and is not a clone using Jagex content. Do not use RuneScape/Jagex assets, cache files, names, maps, music, models, item data, NPC data, client code, server code, protocol code, or extracted data. All art, names, maps, systems, and content should be original or properly licensed from clean sources.

## Setup

1. Install Godot 4 with .NET/C# support.
2. Install a compatible .NET SDK for your Godot version.
3. Open this folder in Godot.
4. If Godot prompts to regenerate C# project files, allow it.
5. Open `scenes/Main.tscn` and confirm script paths are valid.
6. Build the C# solution from Godot before running the scene.

### VS Code / Cursor quick start

The project targets **.NET 8** (`global.json` pins SDK `8.0.421`). To bootstrap a
self-contained toolchain into `.tools/` (gitignored) and wire up the editor:

```powershell
powershell -ExecutionPolicy Bypass -File tools/setup-dev.ps1
```

This installs the .NET 8 SDK and ripgrep, then prepends the vendored SDK to your PATH.
VS Code's .NET Install Tool manages the separate runtime used by the C# extensions.
Use the workspace build task in VS Code / Cursor. In an already-open external terminal,
build and test with `.tools\dotnet\dotnet.exe build DawnOfBlade.sln` and
`.tools\dotnet\dotnet.exe test tests\DawnOfBlade.Tests.csproj`.

## Current status

A playable local prototype. From the login/account screen you enter an open low-poly world with a
customizable humanoid character and can:

- Move (click-to-move) under an orbit camera across an open overworld with scenery and NPCs.
- Train a RuneScape-style spread of **25 skills** on a 99-level XP curve — gathering
  (foraging/woodcutting/mining/fishing), crafting, and combat (attack/strength/defense/hitpoints).
- Fight hostile actors through a combat resolver with attack styles and a triangle-style
  accuracy/damage model.
- Buy and sell at a shop, equip gear with level requirements and bonuses, and track an objective-based
  quest.
- Practice **language-learning** vocabulary prompts (50 themed Spanish→English entries) for Language XP.
- Keep progress through account-scoped local saves (`SaveService`) with autosave.

All gameplay rules live in engine-independent C# and are covered by an xUnit suite
(`tests/DawnOfBlade.Tests.csproj`, net8.0).

## Where it's going

The project is architected to grow from this local-first prototype toward a server-authoritative,
grid-based sandbox MMORPG (600 ms tick, classless skills, combat triangle, chunked open world,
player-driven economy). The engine-independent foundations are in place:

- **Communication bus** (`src/Communication`) — in-process pub/sub + request/response with
  transport-neutral envelopes, ready for a future network adapter.
- **Simulation tick** (`src/Simulation`) — deterministic 600 ms tick loop with buffered commands and
  pluggable systems.
- **Grid world** (`src/World/Grid`) — integer tiles, 32×32 chunks, interest filtering, A* pathing,
  and line-of-sight.
- **HUD presentation models** (`src/UI/Presentation`) — engine-independent state the Godot HUD binds to.

Design plans: `docs/HUD_ARCHITECTURE.md`, `docs/PRODUCTION_BACKEND_ARCHITECTURE.md`. Phase history and
next steps: `docs/ROADMAP.md`.
