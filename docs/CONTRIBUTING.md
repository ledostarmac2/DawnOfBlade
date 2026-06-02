# Contributing

Dawn of Blade should remain original and license-clean.

## Rules

- Do not commit proprietary game assets, extracted files, cache files, copied maps, copied names, copied item/NPC data, copied music, copied models, copied client code, or copied protocol code.
- Do not add networking or multiplayer until the project has an explicit design and threat model. The architecture is drafted in `docs/PRODUCTION_BACKEND_ARCHITECTURE.md`; keep the game local-first and the authoritative rules engine-independent until that staged rollout begins.
- Do not add external dependencies without a clear reason.
- Keep classes small and focused.
- Keep game logic separate from UI where practical.
- Prefer plain JSON content definitions until the data pipeline needs more.

## Code Style

- Use the `DawnOfBlade` root namespace.
- Put Godot node scripts near the feature they adapt.
- Keep pure data models independent of Godot types when practical.
- Add TODO comments only where scene wiring or future system work is genuinely required.

