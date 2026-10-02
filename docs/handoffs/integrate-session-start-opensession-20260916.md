# Prompt: integrate session-start OpenSession persist into current work

Copy everything below the line into the other agent.

---

Integrate this in-progress fix into your current McpServer work. Do not re-diagnose. Do not start a parallel rewrite. Take these files, finish the remaining gates, and keep going on your assigned task.

## What was wrong

Grok plugin `session-start` wrote `.mcpServer/grok/session-state.yaml` with `status: verified` after marker HMAC + `/health` nonce only. It never created a server session.

Verified 2026-09-16:

- Cache claimed `GrokCode-20260916T184241Z-plugin-session` `status: verified`.
- Native `sessionlog_query` `agent=GrokCode` `from=2026-09-16T18:00:00Z` returned `totalCount=0`.
- GET `/mcpserver/sessionlog/GrokCode/GrokCode-20260916T184241Z-plugin-session` returned 404.
- Control GET of `GrokCode-20260909T130459Z-plugin-session` returned 200. The GET route works.

Root cause in `Start-PluginSession` (`plugins/core/lib-ps/plugin-hook.ps1`): after `Invoke-FullBootstrap` it minted a sessionId and wrote `status: verified`. No `client.SessionLog.OpenSessionAsync`. `workflow.sessionlog.openSession` is still a local YAML write (`Invoke-WorkflowOpenSession`). Native `sessionlog_open` does persist (this handoff session was created that way: turnId 45128).

This is not BUG-TRIAGE-031 (that only required a non-empty sessionId). Related but different: BUG-TRIAGE-180 (turn-opened on degraded beginTurn), BUG-TRIAGE-170/171 (failsafe drain). Do not expand this slice into those unless your current task already is those TODOs.

## What is already on disk

Canonical source:

- `plugins/core/lib-ps/plugin-hook.ps1`
  - New `Invoke-PluginOpenSession`: log-seam uses `Invoke-PluginRepl`; live path runs nested `pwsh -File repl-invoke.ps1 -Method client.SessionLog.OpenSessionAsync` so `exit 1` cannot kill the hook.
  - `Start-PluginSession` calls that, then writes `status: verified` only if exit code 0, else `persist-failed`. Keeps sessionId either way.

Tests:

- `plugins/core/test-fixtures/pester/PluginPowerShellRuntime.Tests.ps1`
  - `TEST-MCP-BUGTRIAGE-199 session-start calls OpenSessionAsync before writing verified`
  - `TEST-MCP-BUGTRIAGE-199 session-start does not write verified when OpenSessionAsync fails`
  - `TEST-MCP-BUGTRIAGE-047` repl stub now accepts `client.SessionLog.OpenSessionAsync` and `exit 0` (required; recovery's third prompt now goes through OpenSession).

Copies already made (not in `git diff --stat` if those trees are unsynced/untracked):

- `F:\GitHub\mcpserver-grok-plugin\lib\plugin-hook.ps1`
- `plugins/core/.staged-plugin/lib/plugin-hook.ps1`

`git diff --stat` on the McpServer worktree at handoff: `plugin-hook.ps1` +46/-1, `PluginPowerShellRuntime.Tests.ps1` +148. Confirm with `git diff` before you commit.

## Receipts already in hand

Focused Pester (this session):

- After red: 199 was 0 passed / 2 failed (no OpenSession call, logs missing).
- After green: 199 is 2 passed / 0 failed / 0 skipped.
- Follow-up filter `199+031+047+032+053+041 plugin hook`: 8 passed / 0 failed.

Live proof (temp cache, did not overwrite `.mcpServer/grok`):

- Hook: `F:\GitHub\mcpserver-grok-plugin\lib\plugin-hook.ps1` `-HookName session-start`
- `MCP_SESSION_ID=GrokCode-20260916T190852Z-hook-open-proof`
- Cache: `C:\Users\kingd\AppData\Local\Temp\mcp-hook-open-proof-20260916T190852Z\session-state.yaml` `status: verified`
- GET `/mcpserver/sessionlog/GrokCode/GrokCode-20260916T190852Z-hook-open-proof` returned **200**
- `mcpserver-repl` at `C:\Users\kingd\.dotnet\tools\mcpserver-repl.exe`

Handoff MCP session (native, persisted): `GrokCode-20260916T185700Z-fix-hook-local-session` / `req-20260916T185700Z-001-fix-hook-local-session` / turnId 45128.

## What you must still do

1. Re-run and keep a receipt for the full file:
   `Invoke-Pester plugins/core/test-fixtures/pester/PluginPowerShellRuntime.Tests.ps1`
   The full 126-test run was started and interrupted. Do not claim the whole file green without that output. Failed 0, Skipped 0.

2. Sync remaining agent plugins if your work includes plugin packaging. Canonical is `plugins/core`. Grok and staged copies were copied by hand. Other siblings (`mcpserver-claude-code-plugin`, Codex, Copilot, Cline, OpenCode) were not synced this turn. Prefer `./build.ps1 SyncAgentPlugins` if that is your deploy/sync path; do not leave only Grok fixed if you are shipping all hosts.

3. If other Pester stubs `throw "Unexpected method $Method"` and a path now hits `Start-PluginSession`, allow `client.SessionLog.OpenSessionAsync` the way 047 does. Do not succeed OpenSession in tests that must stay `no-session` (example: TEST-MCP-BUGTRIAGE-032 foreign-agent reject). 032 still passed without that stub change.

4. Hostile-validate this slice before any `done: true` on a TODO. Accuracy and completeness both >= 98. Request + response jsonl under `docs/receipts/hv/`. Full verdict in the MCP session log. Self-check is not HV.

5. Do not mark BUG-TRIAGE-180, 170, 171, or 031 done on the strength of this slice.

## Out of scope unless your current task already is it

- `Invoke-WorkflowOpenSession` remains a local `session-state.yaml` write. Agents using native `sessionlog_open` are fine. Fixing the workflow verb is a follow-on.
- The 89 Grok failsafe `session_submit` YAML files and 2 quarantine records. Drain timeout in current `repl-invoke.ps1` is already 120s (`Get-ReplMethodTimeoutSeconds` while draining). That is not this bug.
- Do not rewrite `.mcpServer/grok/session-state.yaml` for the dead `GrokCode-20260916T184241Z-plugin-session` id. Next real session-start should mint a new id and OpenSession it.

## How to integrate

- If you have uncommitted plugin-hook work: merge `Start-PluginSession` persist + `Invoke-PluginOpenSession` into your copy. Keep fail-closed: never write `verified` without OpenSession exit 0.
- If you are mid-plan on plugin persist: treat TEST-MCP-BUGTRIAGE-199 as the red/green tests for session-start honesty. Do not delete them.
- If you are wrapping up / committing: include the two canonical files, the 047 stub change, and plugin copies your sync target requires. Commit message should say session-start no longer claims verified without `client.SessionLog.OpenSessionAsync`.

## Operator constraints that still apply

PowerShell only (`pwsh.exe -NoProfile -NonInteractive`). No Python. No em-dashes. MCP-only TODO/session/requirements storage. Native `sessionlog_*` tools persist; plugin cache is not proof. Byrd: tests first, Failed 0 Skipped 0 to leave a slice.
