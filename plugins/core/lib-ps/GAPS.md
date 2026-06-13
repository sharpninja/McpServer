# lib-ps parity gaps (deliberate scope decisions)

The PowerShell side is a thin shim (~300 lines of logic vs ~4300 in
lib-sh/repl-invoke.sh). Per the Phase 2 reconciliation report, only the
items it marked "port NOW" were ported in this pass:

Ported now:

1. Turn upsert + failsafe flow in `repl-invoke.ps1`
   (`Invoke-ReplTurnUpsertParams`, rewritten `Invoke-ReplPersistTurn` with
   `Write-ReplFailsafe`/`Clear-ReplFailsafe` and a `method_not_found`
   fallback to full-session `client.SessionLog.SubmitAsync`). Pester mirror
   of `tests/session-log-upsert.bats` lives at
   `test-fixtures/repl-invoke-upsert.ps1.tests.ps1`.
2. `marker-resolver.ps1` `$markerPid` fix (grok commit 03f6d4a is the
   canonical base; the claude-code/cowork copy assigns the read-only
   automatic `$PID` and throws at runtime in `Test-MarkerSignature`).
3. `resolve-cache-dir.ps1` generic knobs (cowork structural base;
   `MCPSERVER_WORKSPACE_PATH`/`MCP_WORKSPACE_PATH` short-circuit kept,
   `COWORK_*` names dropped from core, `MCP_WORKSPACE_START_DIR` >
   `CLAUDE_PROJECT_DIR` > cwd start chain, `MCP_PLUGIN_ROOT` before
   `CLAUDE_PLUGIN_ROOT` in the last-resort root chain).
4. `Invoke-McpPlugin.ps1` (renamed from `Invoke-ClaudeMcpPlugin.ps1`):
   merged codex `-TimeoutSeconds` (default 90, env
   `MCP_PLUGIN_TIMEOUT_SECONDS`) with bounded `WaitForExit` + taskkill,
   copilot `ProgramFiles(x86)`/bash-name-loop probing, status-script
   discovery (`mcp.*.status.sh` glob, `MCP_STATUS_SCRIPT` override) instead
   of the hardcoded `mcp.claude.status.sh`, and `MCP_PLUGIN_ROOT`/
   `MCP_WORKSPACE_START_DIR` child-env exports alongside the legacy
   `CLAUDE_*` names. Hosts that need the legacy filename ship a copy or
   shim named `Invoke-<Host>McpPlugin.ps1` at sync time.
5. `plugin-env.ps1` (new): ps1 twin of the plugin-env knob surface.

Documented gaps (deferred by the report as Phase 2 scope decisions; do not
silently treat as parity debt):

- No ps1 twins for: `cache-scope.sh`, `detect-shell.sh`,
  `final-response.sh`, `memory-context.sh`, `mcp.<host>.status.sh`,
  `hook-lib.sh`. The `Invoke-McpPlugin.ps1` entry script shells into the
  bash implementations instead.
- `Invoke-ReplMethod` still treats `workflow.sessionlog.beginTurn` /
  `openSession` as pure no-ops (no ps1 turn caching). The sh shim owns the
  turn cache.
- The upsert flow's "compat marker" first attempt
  (`_repl_invoke_raw_in_workspace ... compat`) is not ported: the ps1 shim
  has no compat-marker/workspace-re-anchoring machinery, so it issues a
  single plain `client.SessionLog.UpsertTurnAsync` call before the
  `method_not_found` fallback. Failsafe files land under
  `MCPSERVER_FAILSAFE_DIR`/`MCP_FAILSAFE_DIR` or `<cacheDir>/failsafe`
  rather than the sh side's v4 `.mcpServer/failsafe/<agent>/workspaces/...`
  layout (no `cache_scope_v4_failsafe_root` twin yet).
- No session-scoped pending dirs in `cache-manager.ps1` (depends on a ps1
  cache-scope).
- No Gate 3 audit counters on the ps1 side.
- Whether codex/copilot repos receive lib-ps at all is a packaging
  decision; their top-level `Invoke-{Codex,Copilot}McpPlugin.ps1` forks
  should be replaced by the merged `Invoke-McpPlugin.ps1` when that
  decision lands.
