# Tools

This folder is for local project utilities, content validation scripts, and agent handoff templates.

No external tool dependencies are required for the baseline.

## Agent Board

Use `agentboard.py` to coordinate local tasks between humans and coding agents:

```powershell
python tools/agentboard.py list
python tools/agentboard.py show LQ-001
python tools/agentboard.py claim LQ-001 --owner codex
```
