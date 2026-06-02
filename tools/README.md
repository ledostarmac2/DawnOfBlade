# Tools

This folder is for local project utilities and agent handoff/coordination files.

`setup-dev.ps1` bootstraps the vendored .NET 8 toolchain into `.tools/` and restores packages
(see the README "VS Code / Cursor quick start"). Content validation is covered by the xUnit suite
(`ContentIntegrityTests`, `DefinitionParseTests`), not a standalone script. The agent board
(`agentboard.py` + `agent-board.json`) needs Python; nothing else here has external dependencies.

## Agent Board

Use `agentboard.py` to coordinate local tasks between humans and coding agents:

```powershell
python tools/agentboard.py list
python tools/agentboard.py show LQ-001
python tools/agentboard.py claim LQ-001 --owner codex
```
