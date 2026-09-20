# Legion UpdateService + REPL + plugins + caches

Class 2 lab ops on PAYTON-LEGION2. Not a plan-done claim. No PRs. No push. No add-profile publish. No Linux `/opt/mcpserver` or Cursor Cloud touch.

UTC written: 2026-09-19T17:26:21Z
Machine: PAYTON-LEGION2 (hostname and COMPUTERNAME both PAYTON-LEGION2)
Workspace: F:\GitHub\McpServer
Session: GrokCode-20260919T171236Z-legion-update-service
Turn: req-20260919T171236Z-001-legion-update-service-plugins

## Before

Health GET `http://127.0.0.1:7147/health?nonce=2302d3ed81894dd9a26732746bee2a85` (2026-09-19T17:12:36Z):

- status: Healthy
- version: `1.4.38+6a72d445dda56f1d288660de044a2b020291630d`
- nonce echoed: `2302d3ed81894dd9a26732746bee2a85`
- storage: reachable
- service: McpServer Running, StartType Automatic
- REPL: `1.4.38+6a72d445dda56f1d288660de044a2b020291630d` (`sharpninja.mcpserver.repl` 1.4.38)
- git HEAD: `8f30caf98410166bb8cb11958dde5445491d3d61` on develop
- origin/develop after fetch: `bedc35cfd7c80da4939451967ce3c2003a363d85` (behind 1)
- live overlay `C:\ProgramData\McpServer\appsettings.yaml` `TurnTransactions: Enabled: true`

## Git fast-forward

Command: `git fetch origin` then `git merge --ff-only origin/develop`

- FETCH_EXIT=0
- MERGE_EXIT=0
- HEAD after: `bedc35cfd7c80da4939451967ce3c2003a363d85`
- origin/develop: same SHA
- subject: `docs(refresh): PLAN-TXNKEYSERVER-001 box deploy + HV AGREE close-out`
- remaining dirty (pre-existing, not this ops): `.worktrees/triage-stale-turns`, `.worktrees/triage-transcript`

## Nuke UpdateService (elevated)

Command (single gsudo):

`gsudo.exe --wait --chdir F:\GitHub\McpServer pwsh.exe -NoLogo -NoProfile -NonInteractive -ExecutionPolicy Bypass -File %TEMP%\McpServer-UpdateService-20260919T171236Z.ps1`

- GSUDO_EXIT=0
- UpdateService status: Succeeded, duration 2:00
- GitVersion bump inside target: 1.4.38 -> 1.4.39 (staged `GitVersion.yml`; not committed; not pushed)
- config backup/restore: `appsettings.yaml` backed up and restored
- data restore: docs, graphrag-global, mcp-data, mcp.db, mcp.db.migrated, templates
- archive: `C:\Users\kingd\McpServer-Backups\McpServer-backup-20260919-121602508.zip`
- service after: McpServer Running
- Nuke health line: `1.4.39+bedc35cfd7c80da4939451967ce3c2003a363d85` HTTP 200
- WSHealth: OK (41/42); one workspace skipped because disabled (`vice-sharp-romm-worktree`)
- temp wrapper script deleted after run

Did not hand-copy binaries into ProgramData.

## After health (independent)

Health GET `http://127.0.0.1:7147/health?nonce=54c4e0e9a2f4445abe9fbe9cb4a3c522` (2026-09-19T17:18:30Z):

- status: Healthy
- version: `1.4.39+bedc35cfd7c80da4939451967ce3c2003a363d85`
- nonce echoed: `54c4e0e9a2f4445abe9fbe9cb4a3c522`
- storage: reachable
- exe ProductVersion: `1.4.39+bedc35cfd7c80da4939451967ce3c2003a363d85`
- exe FileVersion: 1.4.39.0
- exe LastWriteUtc: 2026-09-19T17:17:27Z
- deploy manifest generatedUtc: 2026-09-19T17:17:32.8466013Z
- McpServer.Support.Mcp.exe sha256: `f3a846ee2fbb3a19d5c37eaa5121a0f2738ad9dae24a4538e6987e1b2cf3fbf9`
- McpServer.Launcher.exe sha256: `c3fdb8450e938e2f573baf718d64e60fe70e8cf273644896418bbcad809023ae`
- `TurnTransactions: Enabled: true` still present under live `appsettings.yaml` (not disabled)

SHA is not `6a72d445…`. SHA matches deployed tip `bedc35cf…`.

Marker rotated on restart (pid 168132, startedAt 2026-09-19T17:17:50Z). API key not copied here.

## REPL

Companion target: `.\build.ps1 InstallReplTool --UpdateTool`

- First run (2026-09-19T17:19:06Z local): PackReplTool succeeded (`SharpNinja.McpServer.Repl.1.4.39.nupkg`). InstallReplTool failed: `dotnet tool uninstall --global SharpNinja.McpServer.Repl` exited 1 (`Access to the path 'McpServer.Client.dll' is denied.`). INSTALLREPL_EXIT=-1.
- After that failure the tool was already gone from `dotnet tool list` (half-uninstall). No leftover `sharpninja.mcpserver.repl` store folder. handle.exe showed no remaining McpServer.Client.dll locks.
- Retry (2026-09-19T17:22:25Z local): InstallReplTool Succeeded. INSTALLREPL_RETRY_EXIT=0.
- Verify: `mcpserver-repl --version` -> `1.4.39+bedc35cfd7c80da4939451967ce3c2003a363d85`
- `dotnet tool list --global`: `sharpninja.mcpserver.repl` 1.4.39

## Plugins (local ff-only, no push)

Pin from plugin artifacts (not marker `plugin_version`): 1.107.0 (`.version` and `.grok-plugin/plugin.json`).

Did **not** run `SyncAgentPlugins`. That target bumps plugin package versions and dirties sibling plugin repos. Pin stayed 1.107.0 after origin pull, so version bump was out of scope. No public plugin PR. No add-profile files copied into plugin repos.

ff-only to origin (all FF_EXIT=0):

- mcpserver-claude-code-plugin `ac218be` -> `7d57066` (main)
- mcpserver-claude-cowork-plugin `6ea254e` -> `3d7154b` (main)
- mcpserver-cline-plugin `8df5d9a` -> `4ded634` (main)
- mcpserver-codex-plugin `b1c5847` -> `b776df5` (main)
- mcpserver-copilot-plugin `010718d` -> `f70c6b2` (main)
- mcpserver-grok-plugin `4d8ba19` -> `a527ffc` (main)
- mcpserver-opencode-plugin `24b66dd` -> `0e85124` (master)

Incoming commits are memory S7b / required-memory injection plus docs. Untracked `testResults.xml` left in several plugin repos (pre-existing).

## Agent plugin caches

Claude cache scrub (only `mcpserver*` / `mcpserver-cowork*` under `%USERPROFILE%\.claude\plugins\cache`):

- Before: `mcpserver-local\mcpserver\1.107.0` and `mcpserver-cowork\mcpserver-cowork\1.107.0` only. No older version folders to delete.
- Left `caveman` untouched (unknown host).
- Refreshed 1.107.0 trees from ff-only plugin repos via robocopy excluding `.git` and `node_modules`.
- After: same two version folders. `skills\memory\SKILL.md` exists in both.

Grok known-host cache (this session host):

- Refreshed `C:\Users\kingd\.grok\installed-plugins\f--github-mcpserver-grok-plugin-67f1f31f` from `F:\GitHub\mcpserver-grok-plugin`.
- `skills\memory\SKILL.md` exists after copy.

Codex/Cline/Copilot/OpenCode caches were not rewritten. Operator named Claude stale-scrub explicitly and said skip unknown hosts.

## Smoke (plugin wrapper)

- `mcpserver-repl --version` = `1.4.39+bedc35cfd7c80da4939451967ce3c2003a363d85`
- `lib\mcp-status.ps1` status=available, hasSession=true, hasCurrentTurn=true, pendingCount=0 (wrapper default cacheDir `F:\GitHub\McpServer\.mcpServer\claude`; not used as version authority)
- `lib\repl-invoke.ps1` method `workflow.sessionlog.queryHistory` agent GrokCode limit 5: QUERY_EXIT=0. First row is this session `GrokCode-20260919T171236Z-legion-update-service`, turnCount 1, persisted after service restart.
- Native `sessionlog_query` agent GrokCode also returns the same session (totalCount 392). Persistence is server-side, not marker-only.

## Out of scope (honored)

- No Integration/Validation/Review/AiReview
- No PLAN / TODO done marks
- No non-Legion deploys
- TurnTransactions left Enabled=true
- No public add-profile
- GitVersion.yml remains staged locally; operator said no push unless asked
- If UpdateService had failed, work would have stopped. It did not fail.

## End-state check

1. develop == origin/develop `bedc35cf…`: yes
2. UpdateService exit 0: yes. InstallReplTool exit 0 on retry: yes
3. Health Healthy, version SHA not `6a72d445…`, matches tip: yes (`1.4.39+bedc35cf…`)
4. REPL 1.4.39 matches GitVersion next-version; plugins 1.107.0 from plugin artifacts: yes
5. Claude mcpserver/cowork caches refreshed at 1.107.0; caveman skipped: yes
6. This receipt: yes
