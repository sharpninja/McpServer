# PLAN-REQRECOVERY-ENABLER-20260928

QBrainAi enabler plan. ViceSharp requirements apply stays out of scope.

## Slice 1 — Dense session-log graph load

`SessionLogService.FindExistingSessionAsync` loads the session graph with `AsSplitQuery()` inside `StorageCommandBudget.ExecuteAsync`. The budget is `Mcp:SessionLog:SubmitCommandBudgetSeconds` (default 30, valid 1 through 300). SQL deadlock 1205 throws `StorageGraphMaterializationException`. Budget expiry throws `StorageCommandBudgetExceededException`. Both classify as retryable `backend_unavailable`. A failed load does not report the session as missing and does not persist the mutation. Submit `SaveChanges` uses the same 30 second budget. Triage intake and replace/section `SaveChanges` stay on the 5 second default.

Proof: `SessionLogDenseGraphTests` and `SessionLogSubmitBudgetTests`.

## Slice 2 — Atomic requirements recovery

`FR-MCP-REQRECOVERY-001`, `TR-MCP-REQRECOVERY-001`, and `TEST-MCP-REQRECOVERY-001`.

- REST `POST /qbrainai/requirements/recovery` with `mode` `dry-run` or `apply`, and `GET /qbrainai/requirements/recovery/{idempotencyKey}`.
- REPL `workflow.requirements.planRecovery`, `applyRecovery`, and `getRecovery`.
- `RequirementsRecoveryRunEntity` primary key `(WorkspaceId, IdempotencyKey)`.
- Forward migrations `20260929020000_AddRequirementsRecoveryRuns` on SQLite, SQL Server, and PostgreSQL.
- Apply opens `IsolationLevel.Serializable`, upserts every requirement row, inserts the run, and calls `SaveChanges` once. Dry-run writes nothing, so GET after dry-run only is 404.
- The same key and payload replays the stored result. A different payload is 409 and does not change rows.
- Validation failures are 400. Storage budget exhaustion is 503 `backend_unavailable`.
- A failed save rolls back. No requirement row and no run row remain.

Proof: `RequirementsRecoveryTests`, `RequirementsRecoveryControllerTests`, `RequirementsRecoveryMigrationTests`, and `RequirementsRecoveryWorkflowTests`.

## Slice 3 — Agent plugin sync

`plugins/core/lib-ps/mcp-status.ps1` lists `planRecovery`, `applyRecovery`, and `getRecovery` for the host matrix Codex, Claude Code, Claude Cowork, Copilot, Grok, Cline, Cline v2, and OpenCode. The sync script copied that file into `plugins/core/.staged-plugin` (gitignored) and wrapper generation wrote the claude-code hooks there.

This cloud checkout's parent directory is `/`. None of the eight `mcpserver-*-plugin` repositories are present there, so SyncAgentPlugins has no sibling root to update. Those repositories are outside this repo and were not cloned or edited. The Nuke target invokes `pwsh.exe`; this VM has `pwsh`, and the same script and wrapper files were run with `pwsh`.

`dotnet test tests/QBrainAi.PluginIntegration.Tests --filter PluginInt=Deterministic` compiled and ran: 13 passed, 81 failed, 0 skipped. Failures throw `Plugin repository root is missing: mcpserver-codex-plugin` from `PluginSessionLogCatalog.LoadAndValidate` before a host scenario runs. `PluginInt=AI` was not run. It needs `aiunit-grok-json.cmd` for the grok-build strategy in `appsettings.aiunit.json`, and that command is not on this VM. Native plugin suite receipts are absent for the same reason.

## Slice 4 — Operator deploy checklist

This cloud VM does not run the Legion deploy. The operator runs it on the target host after this branch is merged.

Linux:

```powershell
sudo --chdir . pwsh -NoProfile -ExecutionPolicy Bypass -File ./build.ps1 UpdateService
```

Windows:

```powershell
pwsh -NoLogo -NoProfile -NonInteractive -File .\build.ps1 UpdateService
```

`UpdateService` selects Windows or Linux, preserves live configuration and file data, and restarts the service. It requires elevation.

Post-deploy proof on the live host, after `/health` succeeds:

1. Dense session-log mutation. Submit or begin/complete a turn on a session that already has actions and tags. The call finishes without the previous 40–45 second cartesian load, and a failed load returns retryable HTTP 503 `backend_unavailable` instead of an empty session.
2. Recovery dry-run. `POST /qbrainai/requirements/recovery` with `mode: dry-run`, a new idempotency key, and one valid item. The response status is `planned`. `GET /qbrainai/requirements/recovery/{idempotencyKey}` returns 404. Requirement rows are unchanged.
3. Optional apply proof. Repeat with `mode: apply`. A second apply of the same body returns `replay: true`. A second apply with a different body returns 409 and leaves the first rows in place.

## Slice 5 — ViceSharp requirements apply

Out of scope. This plan does not edit ViceSharp and does not claim `generateDocument` markdown output is fixed.
