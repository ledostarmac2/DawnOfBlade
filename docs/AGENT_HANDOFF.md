# Agent Handoff Protocol

This project can use local files for coordination between coding agents such as Codex and Claude without adding AI APIs, networking, or background services.

## Shared Board

Use `tools/agent-board.example.json` as the template for a working `tools/agent-board.local.json` file. The local file is ignored by git if it ever includes temporary notes, credentials, private URLs, or work-in-progress context.

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

