# Tools

This folder is for local project utilities and agent handoff/coordination files.

`setup-dev.ps1` bootstraps the vendored .NET 8, Godot 4.2.2 .NET, and ripgrep toolchain into
`.tools/` and restores packages (see the README "VS Code / Cursor quick start"). `check-dev.ps1`
runs the xUnit suite and the in-engine Godot headless test scene. `run-game.ps1` launches the
project through the same pinned local toolchain. The agent board (`agentboard.py` +
`agent-board.json`) needs Python; nothing else here has external dependencies after setup.

## Agent Board

Use `agentboard.py` to coordinate local tasks between humans and coding agents:

```powershell
python tools/agentboard.py list
python tools/agentboard.py show LQ-001
python tools/agentboard.py claim LQ-001 --owner codex
```
