# Receipt: live Agent Help smoke test

TimestampUtc: 2026-08-18T00:13:16Z
WorkClass: user-directed general action (live service smoke test)
Implementer: GrokCode
SessionId: GrokCode-20260817T120000Z-agent-help-grok-cli
RequestId: req-20260818T001157Z-007-test-agent-help-live

## Pre-check

- Get-Service McpServer: Running
- Win32_Service ProcessId: 57744 (matches marker pid)
- GET /health nonce c2fc9d0ee14745a2bf6661364515e2da: HTTP 200, nonce echoed, storage=reachable
- Version: 1.4.26+bd8a8d9e8cc3221bd25e7ce29479b460bc21b19e

## Create session

MCP tool `agent_help_create_session`:

- sessionId: help-20260818001213-0aa9f6de59d2403296130363aa94bb75
- status: idle
- executionStrategy: grok-cli
- modelRequested: grok-4.5
- modelResolved: grok-4.5
- corpus: 10 excerpts, topic live-agent-help-smoke

## Submit turn

MCP tool `agent_help_submit_turn`:

- turnId: turn-0001
- status: completed
- latencyMs: 55827
- guardResult.allowed: true
- assistantDisplayText: Agent Help is responding and available for MCP Server diagnosis on this workspace.

## Status / transcript

`agent_help_get_status`:

- status: idle
- lastTurnId: turn-0001
- turnCounter: 1
- executionStrategy: grok-cli
- isTurnActive: false
- terminated: false

`agent_help_get_transcript`: 3 items (system corpus, user prompt, assistant reply matching the submit-turn text).

Service log `C:\ProgramData\McpServer\logs\mcp-20260817.log`:

- POST /mcp-transport agent_help_submit_turn completed HTTP 200 in 55909.97ms
- Same sessionId, status completed, latencyMs 55827

## Not proved in this test

- Did not parse grok.exe argv for `--effort high` on this turn.
- Did not test SSE/WebSocket streaming.
- This is a smoke reply, not a full diagnosis of a real MCP failure.

## Verdict

Agent Help is working on the running service for create-session, one-shot grok-cli turn, status, and transcript.
