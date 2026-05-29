# Agent Handoff Protocol

This project can use local files for coordination between coding agents such as Codex and Claude without adding AI APIs, networking, or background services.

## Shared Board

Use `tools/agent-board.json` for shared, committed tasks. Use `tools/agent-board.local.json` for private work-in-progress notes; the local file is ignored by git.

The helper CLI in `tools/agentboard.py` can list, claim, delegate, update, and annotate tasks without hand-editing JSON:

```powershell
python tools/agentboard.py list
python tools/agentboard.py claim LQ-001 --owner codex
python tools/agentboard.py delegate LQ-001 --to claude --note "Needs scene wiring review."
python tools/agentboard.py state LQ-001 --to review --verify "JSON validation ok"
```

## Task States

- `open`: Ready for an agent to pick up.
- `claimed`: An agent is actively working on it.
- `blocked`: Waiting on a decision, dependency, or missing context.
- `review`: Implementation is ready for another agent or human to inspect.
- `done`: Completed and verified.

## Handoff Notes

Each handoff should include:

- Task ID.
- Owner.
- Current state.
- Files touched.
- Verification performed.
- Remaining questions.

Agents should keep handoff notes factual and concise. Do not store secrets, API keys, personal data, or proprietary source material in handoff files.
