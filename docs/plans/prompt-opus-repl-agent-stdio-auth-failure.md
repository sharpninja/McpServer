# Opus Prompt: Diagnose + Fix REPL Agent-STDIO Auth Failure

## Task

`mcpserver-repl --agent-stdio` cannot authenticate against the running MCP server for any server-bound method (`client.SessionLog.SubmitAsync`, `workflow.sessionlog.openSession`, etc). The local-only path (`workflow.sessionlog.bootstrap`) succeeds. The server is reachable and `/health` echoes nonces correctly. Both auth surfaces named in the failure message - `BearerToken` (OIDC) and `ApiKey` (marker file) - are present on disk but the REPL does not load either into its `SessionLogClient` credential when running in `--agent-stdio` mode.

Diagnose the root cause and produce a fix. The agent-stdio path is the contract surface that plugin hooks rely on for session log writes; if it cannot authenticate, the plugin-required policy in `AGENTS-README-FIRST.yaml` forces every agent to log `MCP_PLUGIN_UNAVAILABLE` and fall back to local failsafe files. That is happening now.

## Environment

- Host: Windows 11 Pro 10.0.26200, PowerShell 7+.
- Workspace: `F:\GitHub\vice-sharp` (ViceSharp). Marker file present at `F:\GitHub\vice-sharp\AGENTS-README-FIRST.yaml` (workspace = vice-sharp, port 7147).
- Server: `http://PAYTON-LEGION2:7147`, version `1.0.0+7a198950e027f4a60c03034afbd957eaa6b1f80c`, started `2026-05-16T06:26:03Z`, signature valid.
- REPL tool: `C:\Users\kingd\.dotnet\tools\mcpserver-repl.exe` (dotnet tool). Source: `F:\GitHub\McpServer\src\McpServer.Repl.Host\` (per stack traces).
- Plugin install: `C:\Users\kingd\.claude\plugins\cache\mcpserver-local\mcpserver\1.1.0\` (this is `mcpserver-claude-code-plugin` v1.1.0 surface).
- Agent identity: `Claude` (mcpserver-claude-code-plugin); Codex / Copilot / Cline plugins likely hit the same issue.

## Repro

```powershell
Set-Location F:\GitHub\vice-sharp
$env:MCPSERVER_API_KEY      = '<workspace api key from AGENTS-README-FIRST.yaml>'
$env:MCPSERVER_BASE_URL     = 'http://PAYTON-LEGION2:7147'
$env:MCPSERVER_WORKSPACE_PATH = 'F:\GitHub\vice-sharp'

@'
type: request
payload:
  requestId: req-20260517T200000Z-probe-001
  method: workflow.sessionlog.openSession
  params:
    agent: Claude
    sessionId: Claude-20260517T200000Z-probe
    title: Auth probe
    model: claude-opus-4-7
'@ | mcpserver-repl --agent-stdio
```

Observed response:

```yaml
type: error
payload:
  requestId: req-20260517T200000Z-probe-001
  code: method_invocation_error
  message: 'Authentication required: no credential is configured on this client. Set BearerToken (for interactive users via OIDC) or ApiKey (for agents via the AGENTS-README-FIRST.yaml marker file) before calling any endpoint.'
  details:
    methodName: workflow.sessionlog.openSession
    exceptionType: System.InvalidOperationException
```

`workflow.sessionlog.bootstrap` (no params, no server hit) returns `initialized: true` against the same invocation, confirming the REPL is up and the dispatcher routes workflow.* correctly. Only the server-bound calls fail.

## What was tried (all failed identically)

1. `MCPSERVER_API_KEY=<key>` env var (the value the marker file holds in `apiKey:`).
2. `MCPSERVER_BEARER_TOKEN`, `MCP_BEARER_TOKEN`, and `BEARER_TOKEN` env vars set to the cached access token at `C:\Users\kingd\.mcpserver\tokens.json`.
3. Setting `MCPSERVER_BASE_URL`, `MCP_SERVER_URL`, `MCPSERVER_WORKSPACE`, `MCPSERVER_WORKSPACE_PATH`, and `MCP_WORKSPACE_PATH` to match the marker.
4. Running `mcpserver-repl --agent-stdio` from inside `F:\GitHub\vice-sharp` so `find_marker_file` discovers `AGENTS-README-FIRST.yaml` by walking up (this is what `lib/repl-invoke.sh` line ~905 does).
5. Prior `--interactive` device-flow login succeeded (`Logged in as plbyrd ... Token expires at 15:26:19 (3600s)`). Cached token written to `~/.mcpserver/tokens.json`. agent-stdio still says no credential.
6. Direct `client.SessionLog.SubmitAsync` envelope: same error, with `clientName: SessionLog, methodName: SubmitAsync` in `details`.

`/health?nonce=...` echoes nonces correctly with the marker's `apiKey` in the `X-Api-Key` header, proving the key is valid - the failure is on the client side, not the server.

## What works

- `workflow.sessionlog.bootstrap` (local init, no server call): returns `initialized: true`.
- Direct REST POSTs to `/mcpserver/sessionlog` with `X-Api-Key: <markerKey>` (verified by hand) - but the plugin contract forbids using raw REST as a substitute for plugin tools, so this only proves the server side is fine.
- `--interactive` mode: device flow login completes, token cached. But interactive mode then crashes on workspace selection with `Cannot show selection prompt since the current terminal isn't interactive` - separate but related bug for non-tty stdin scenarios.

## Suspected root cause

`McpServer.Repl.Host.AgentStdioHandler` does not run the credential bootstrap that the interactive/workflow paths run. Specifically the `SessionLogClient` (and presumably every other `client.*` instance) is constructed without a credential, so the first server-bound call hits the `InvalidOperationException` quoted in the error message.

The plugin shim (`C:\Users\kingd\.claude\plugins\cache\mcpserver-local\mcpserver\1.1.0\lib\repl-invoke.{sh,ps1}`) configures env vars and cwd before invoking the REPL but does not pass any explicit credential envelope - so the REPL must auto-discover from one of:

- the marker file in cwd (via `find_marker_file` and `apiKey:`),
- `MCPSERVER_API_KEY` env,
- the cached OIDC token at `~/.mcpserver/tokens.json`.

None of these auto-discovery paths fire in agent-stdio mode.

## What I want from you

1. Read `F:\GitHub\McpServer\src\McpServer.Repl.Host\AgentStdioHandler.cs` and the related client wiring. Confirm whether credential init is skipped in agent-stdio (vs interactive). Identify the exact line where the `SessionLogClient` is instantiated without a credential.
2. Compare against the equivalent code path in `InteractiveHandler.cs` (or wherever the `client.*` instances pick up `BearerToken` / `ApiKey`).
3. Implement a fix that, when running in `--agent-stdio` mode, configures credentials in this priority order: explicit hello-envelope -> `MCPSERVER_API_KEY` env -> marker file `apiKey` (via cwd walk-up) -> cached OIDC bearer at `~/.mcpserver/tokens.json`. Plumb whichever is found into every client instance the REPL exposes via the dispatcher.
4. Add a regression test: an agent-stdio envelope test that sends `workflow.sessionlog.openSession` and expects `type: result` (not `type: error / Authentication required`) when the marker file is present in cwd. Mock the server at `http://localhost:<port>` if needed - the assertion is that the credential header is set, not that the server processes the call.
5. Update `docs/REPL-AGENT-GUIDE.md` (or `REPL-MIGRATION-GUIDE.md`) with the resolved credential precedence and a one-line example for plugin authors.
6. Bump REPL package version and document the upgrade in the plugin contract (`AGENTS-README-FIRST.yaml` and the agent-plugin contract digests will need to be updated by the plugin owners separately - just note this in the PR description).

Follow BDP: write the regression test first against the unfixed REPL, watch it fail, then implement the credential wiring, then watch it pass. Full suite must stay green.

## Artifacts captured this session

The Claude session that produced this prompt could not write its session log to the MCP server because of the very bug being reported. The failsafe artifacts and the Claude Code transcript:

- Plan file: `C:\Users\kingd\.claude\plans\using-mcpserver-claude-code-plugin-start-adaptive-harp.md`
- Workspace midpoint handoff (ViceSharp): `F:\GitHub\vice-sharp\handoff-2026-05-17.md`
- Auth-failure log: `F:\GitHub\vice-sharp\.dotnet-home\MCP_PLUGIN_UNAVAILABLE.log`
- Replayable envelope (drop this through the fixed REPL to backfill the session record): `F:\GitHub\vice-sharp\.dotnet-home\repl-pickup-sequence.yaml`
- Claude Code session transcript (JSONL with every tool call and response): `C:\Users\kingd\.claude\projects\F--GitHub-vice-sharp\1bb420dc-f6de-438e-b3d7-ce8b5a9276d5.jsonl`
- Proposed-but-unposted MCP session ID: `Claude-20260517T191830Z-pickup-handoff`
- Active turn request ID (still open server-side never): `req-20260517T191830Z-prompt-3f1d`

Read the transcript JSONL for the exact stderr traces, the verbatim error responses, and the env-var permutations attempted. The plugin install where `repl-invoke.{sh,ps1}` lives is `C:\Users\kingd\.claude\plugins\cache\mcpserver-local\mcpserver\1.1.0\`.

## Acceptance criteria

- Repro envelope above returns `type: result` (or a different error not related to authentication) when run from a directory containing a valid `AGENTS-README-FIRST.yaml`.
- New regression test covers the agent-stdio credential discovery and is added to the REPL test suite (`F:\GitHub\McpServer\tests\McpServer.Repl.Host.Tests\` or wherever the existing tests live - find them).
- Full mcpserver test suite stays green.
- Replaying `F:\GitHub\vice-sharp\.dotnet-home\repl-pickup-sequence.yaml` through the fixed REPL persists the Claude-20260517T191830Z-pickup-handoff session with one completed turn for `req-20260517T191830Z-prompt-3f1d` on `PAYTON-LEGION2:7147`.

## Out of scope

- The interactive-mode workspace-selection crash (`Cannot show selection prompt since the current terminal isn't interactive`) - file a separate issue.
- The `~/.mcpserver/tokens.json` cache having `"authority": ""` - unrelated; investigate only if it turns out to be the actual blocker.
- Rotating the workspace API key or changing the marker signature contract - the auth values are correct.
