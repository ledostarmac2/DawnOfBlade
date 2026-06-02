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

## Baseline Status

The repository currently contains the first architecture baseline:

- Godot project metadata and placeholder main scene.
- C# scripts for core game state, player movement, orbit camera, interaction, inventory, skills, combat, dialogue, quests, shops, learning prompts, saves, and debug command routing.
- Example JSON data definitions using fake original content.
- Design, technical, roadmap, data schema, contribution, asset sourcing, and agent handoff documentation.

## First Milestone

The first playable prototype should support:

- A simple 3D plane/map.
- A placeholder player capsule.
- Orbit camera follow, rotate, and zoom.
- Click-to-move target movement.
- One clickable NPC.
- One dialogue popup.
- One vocabulary prompt.
- Simple inventory and XP data models.

See `docs/ROADMAP.md` for phase planning.
