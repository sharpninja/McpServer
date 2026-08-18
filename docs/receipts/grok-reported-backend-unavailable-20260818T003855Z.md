# Receipt: Grok reported session-log backend_unavailable

TimestampUtc: 2026-08-18T00:38:55Z
WorkClass: user-directed incident correction
Implementer: GrokCode
SessionId: GrokCode-20260817T120000Z-agent-help-grok-cli
RequestId: req-20260818T003855Z-009-grok-reported-backend-unavailable

## Correction

The previous turn treated Claude as the reporter. The operator said Grok reported it. That is correct.

## Grok report 1: this session, real 503

This Grok agent called `sessionlog_replace_section` after the service restart and received HTTP 503 `backend_unavailable` at 2026-08-17T23:52:23.8885231Z (trace `00-aab0888980690d5c55a8af5c029f0bd1-9c0f446ccbcb5618-01`).

Server log `C:\ProgramData\McpServer\logs\mcp-20260817.log`:

- 18:52:13 -05:00 Health check storage Unhealthy after 5008ms: Storage connectivity probe timed out after 5s
- 18:52:23 -05:00 Unhandled exception POST /mcp-transport same trace
- Exception: `Microsoft.Data.SqlClient.SqlException` Named Pipes error 40, inner `Win32Exception (5): Access is denied`
- Stack: `WorkspaceService.EnsureBootstrappedAsync` line 407
- 18:52:26 -05:00 Health check storage Unhealthy: Storage backend is unreachable

Live config: `Mcp.Database.Provider=sqlserver` (not sqlite). Session-log uses that SQL Server.

The earlier hostile DISAGREE on "first health after start was unreachable" still stands for the 18:38-18:42 window. The 503 this Grok hit was 14 minutes later, at 18:52.

## Grok report 2: plugin SubmitAsync (not 503)

Grok plugin session `GrokCode-20260818T001225Z-plugin-session` failsafe `F:\GitHub\McpServer\.mcpServer\grok\failsafe\20260818T001239Z-session_submit-a650.yaml`:

- lastDrainError: method_invocation_error / internal_server_error / SessionLog.SubmitAsync
- canceled turn `req-20260818T001131Z-prompt-b813` omits planFile/todoId

That is the same omitted-planFile submit bug, on the Grok plugin, not Claude.

Later submits for that session succeeded (201 at 19:37:08 local).

## Grok report 3: older 503

Grok failsafe quarantine `20260817T024328Z-session_submit-80d4.yaml` quotes a Grok observation: `sessionlog_replace_section` HTTP 503 backend_unavailable at 2026-08-17T01:52:08Z during MCP-HANDOFFPLAN-001.

## Not claimed

- SQL Server Access is denied is not fully root-caused (service account LocalSystem vs SQL permission / named pipes).
- Plugin planFile omit is not fixed in this turn.
