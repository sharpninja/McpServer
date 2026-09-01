# Receipt: restart McpServer Windows service

TimestampUtc: 2026-08-17T23:38:29Z
WorkClass: user-directed general action (Windows service restart)
Implementer: GrokCode
SessionId: GrokCode-20260817T120000Z-agent-help-grok-cli
RequestId: req-20260817T233717Z-006-restart-mcpserver-service

## Restart

Single elevated command:

`gsudo pwsh.exe -NoProfile -NonInteractive -ExecutionPolicy Bypass -File docs/receipts/_restart-mcpserver-20260817T233717Z.ps1`

Output (exit 0):

- BEFORE Status=Running
- BEFORE Pid=5572
- AFTER Status=Running
- AFTER Pid=57744
- AFTER StartTimeUtc=2026-08-17T23:38:29.5863805Z

Later poll: Status=Running, Pid=57744

## Marker after restart

`AGENTS-README-FIRST.yaml`:

- pid: 57744
- startedAt: 2026-08-17T23:38:48.7047470+00:00
- serverStartedAtUtc: 2026-08-17T23:38:29.7115442+00:00
- apiKey: rotated (new value present; not copied here)

## Health

First check after start: HTTP 200, nonce echoed, `storage: unreachable`.
MCP Streamable HTTP returned 503 `backend_unavailable` during that window.

Later check (`docs/receipts/_health-ready-20260817T233717Z.ps1`):

- /health HTTP 200, nonce `postrestart3` echoed, `storage: reachable`
- /ready HTTP 200, storage check Healthy, workspace-ready Healthy

Version: `1.4.26+bd8a8d9e8cc3221bd25e7ce29479b460bc21b19e`

## Config survived

`C:\ProgramData\McpServer\appsettings.yaml` AgentHelp:

- DefaultExecutionStrategy=grok-cli
- HelperModel=grok-4.5
- Enabled=True

## Not done

- No binary deploy
- No SCM config change (still Auto / LocalSystem)
