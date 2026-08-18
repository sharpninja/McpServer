# Receipt: session-log backend_unavailable diagnosis

TimestampUtc: 2026-08-18T00:35:30Z
WorkClass: user-directed incident diagnosis
Implementer: GrokCode
SessionId: GrokCode-20260817T120000Z-agent-help-grok-cli
RequestId: req-20260818T003125Z-008-sessionlog-backend-unavailable

## Current live state (this agent, McpServer workspace)

- Service Running, PID 57744
- /health HTTP 200, nonce echoed, storage=reachable
- /ready HTTP 200, storage Healthy, workspace-ready Healthy (2069ms)
- sessionlog_begin_turn succeeded (turnId 41593)
- sessionlog_query succeeded (this session has 8 turns)

Storage is not down right now.

## What the user/Claude actually hit

Claude plugin failsafe `F:\GitHub\McpServer\.mcpServer\claude\failsafe\20260818T001252Z-session_submit-f830.yaml`:

- method: client.SessionLog.SubmitAsync
- lastDrainError.code: method_invocation_error
- lastDrainError.message: internal_server_error
- lastDrainError.exceptionType: McpServer.Client.McpServerException

That is not `backend_unavailable`.

Canceled turn in that payload:

- requestId: req-20260818T001140Z-prompt-e2bf
- status: canceled
- planFile: omitted
- todoId: omitted

Later failsafe `20260818T003049Z-session_submit-cb97.yaml` same pattern: canceled turn without planFile/todoId.

## Server log at the same minute

`C:\ProgramData\McpServer\logs\mcp-20260817.log` from 19:31:53 through 19:35:19 local (00:31-00:35Z):

- Repeated: `System.ArgumentException: Invalid session turn planFile/todoId: planFile is omitted. (Parameter 'planFile')`
- Zero `POST /mcpserver/sessionlog completed with 503`

Since restart (~18:38 local):

- sessionlog completed-with-503 count: 0
- planFile-omitted lines: 221
- backend_unavailable string hits: 11, and the ones listed are successful 200/201 sessionlog lines whose request/response bodies mention that phrase

## Why SubmitAsync throws

`SessionLogService.SubmitAsync` calls `ApplyTurnContext` -> `ValidateForNewEntry` for new requestIds. Omitted planFile is invalid.

Plugin superseded/canceled turns that never persisted are submitted as new turns without `planFile`/`todoId`. That is a 500 `internal_server_error`, not a storage outage.

Existing-turn path already uses `ValidateIfSupplied` (omit keeps stored). New-turn path does not default omitted to `None`.

## Historical real backend_unavailable (not this incident)

05:42 local today: MCP `requirements_update` for `F:\GitHub\vice-sharp` failed because SQL Server `192.168.1.77` / `McpServer_PAYTON_LEGION2` was unreachable. That is a different workspace and a different tool.

## Not fixed in this turn

- Did not change SessionLogService
- Did not change the Claude plugin
- Did not restart the service

## Verdict

Session-log storage is reachable. The live Claude submit failure is omitted planFile on new/canceled turns, reported as internal_server_error. The backend_unavailable label does not match this incident's failsafe or the post-restart sessionlog 503 count.
