# Plan: Remediate session-log persist 503s, failsafe drain, sanitizer closeout, and planFile/todoId store-close

**Scope (live MCP, 2026-08-20):** BUG-TRIAGE-160, 161, 162; MCP-SESSIONLOG-001; MCP-SESSIONLOG-002. Same-root absorb: BUG-TRIAGE-164 (beginTurn PersistTurn 503). Not 163 (wrong repo).

**Master tracking TODO (create after approval):** `PLAN-SESSIONLOGREMEDIATE-001`

**Durable plan path after approval:** `docs/plans/sessionlog-remediate-001.md`

**Process:** Byrd Development Process v4. Tests covering AC first (shown red, mocks/stubs where needed). Implementation only after those tests are correct. Current-plus-prior suite Failed 0 Skipped 0 to exit a slice. Hostile between phases. Do not mark any listed TODO `done: true` without H-done OverallVerdict AGREE.

**Continuity:** After explicit approval, execute S0 through S7 without pausing for extra go-aheads. Stop only for hostile DISAGREE (fix and re-run) or a true external blocker (disk full, UAC for UpdateService). Do not wait between slices for conversational confirmation.

**Predecessor:** leftover-27 is closed. Do not reopen it. 160-164 were outside leftover-27 on purpose.

**Breaking change:** Plugin persist on HTTP 503 `backend_unavailable` currently throws after writing failsafe. This plan makes that path match the existing timeout degrade-queue (no throw, current-turn stays, failsafe retained). Incremental `appendDialog` stops sending a full-session `SubmitAsync` upsert. `/health` liveness is unchanged. Not PSGallery.

## Problem

Five open Session Logging TODOs look like five jobs. They are two product slices plus one persist/failsafe defect with three report IDs.

Observed on HEAD `20db61aa` from live `todo_get` and `plugins/core/lib-ps/repl-invoke.ps1`:

- `Invoke-WorkflowAppendDialog` mutates local turn cache then calls `Invoke-ReplPersistTurn`.
- `Invoke-ReplPersistTurn` always writes a `session_submit` failsafe and POSTs `client.SessionLog.SubmitAsync` with a full `UnifiedSessionLogDto` (entire session plus one turn).
- Server `SessionLogService.SaveChangesBudgetedAsync` wraps `SaveChangesAsync` in `StorageCommandBudget` (5s). Budget/lock/busy maps to HTTP 503 `backend_unavailable` retryable true.
- Timeout in PersistTurn degrades and queues. 503 does not: it throws (`Session log persistence failed for request...`). That is 160, 162, and 164.
- REST already has incremental `POST .../dialog` (`SessionLogController.AppendDialogAsync` -> `AppendProcessingDialogAsync`). Typed `SessionLogClient.AppendDialogAsync` exists. Plugin does not call it.
- After any successful `Invoke-ReplRaw`, `Invoke-ReplFailsafeDrainOnFirstSuccess` replays queued `SubmitAsync` at the default 30s REPL timeout. `Invoke-ReplRaw` `Write-Error` under `ErrorActionPreference Stop` can skip `Test-ReplFailsafeBackendUnreachable` and hit the drain `catch` that prints `Failsafe queue drain failed`. That is 161. Leftover 159 already taught 503 abort-without-latch; 161 is the timeout/Write-Error sibling.
- Linked FR/TR on 160-162-164 is `FR-MCP-TRIAGE-002` / `TR-MCP-TRIAGE-004` (async triage grouping). That does not govern persist or drain. New FR/TR/TEST/AC are required.
- MCP-SESSIONLOG-001 FR/TR/TEST exist. S0-S14 shipped (outermost `SessionLogSanitizingService` in `Program.cs` and `McpStdioHost.cs`). S15-S19 (controller secret fixture, query-semantics, stdio/federation live, config docs, gate) are open. Existing AC on FR-MCP-SESSIONLOGSAN-001 is not fully proven on HTTP/stdio.
- MCP-SESSIONLOG-002 FR/TR/TEST exist. All implementationTasks true. Prior hostile 185650Z AGREE. Remaining: in-repo complete; live UpdateService was a non-goal. Store-close still requires this plan's H-done plus live beginTurn/get of `planFile`/`todoId`.

## Value

Hostile reviews, wrap-up, and refresh-docs stop losing dialog to 503s. getFr/getTr stop stalling 30s on drain. Failsafe records actually replay after storage answers. Session-log reads stop leaking secrets on HTTP/stdio. Turns always carry `planFile`/`todoId` on live GET.

## Root causes and overlap

**RC1 Full-session upsert for incremental verbs (160, 162).** Plugin appendDialog (and other mutation verbs) serialize the whole session. A 5s SaveChanges budget plus SQLite lock from concurrent TODO/requirements reads yields 503 even though reads succeed.

**RC2 PersistTurn treats 503 as fatal throw (160, 162, 164).** Timeout is degraded/queued. 503 is retryable per classifier but the plugin throws, so callers fail even though failsafe is on disk.

**RC3 Drain of those same `session_submit` records blocks and misfires (161).** First successful plugin call (getFr/getTr) drains the queue with a 30s SubmitAsync timeout. Write-Error under Stop bypasses the unreachable test. Catch prints drain failed. Drain may latch completed so replay never happens.

**RC4 False backend_unavailable (164, 160).** Storage is reachable (health, TODO, requirements succeed). Busy/locked SQLite or budget expiry is classified as backend down.

**RC5 Sanitizer HTTP/stdio AC unproven (SESSIONLOG-001 S15-S19).** Core sanitizer and DI exist. Controller integration, query-semantics, stdio/federation, and config docs do not.

**RC6 SESSIONLOG-002 not store-closed.** Code and tests exist. Live hosted GET of planFile/todoId after deploy was explicitly a non-goal of the prior plan.

Do not treat these as five PRs that each rewrite `repl-invoke.ps1`. One persist/failsafe worktree. One sanitizer worktree. One 002 closeout (proof, not rewrite, unless H-closeout DISAGREE).

## Locked decisions

1. New FR/TR/TEST for persist/failsafe. Do not overload FR-MCP-TRIAGE-002.
2. Keep FR-MCP-SESSIONLOGSAN-001 and FR-MCP-SESSIONLOGCTX-001. Fill AC evidence; do not invent a second sanitizer FR.
3. Include BUG-TRIAGE-164 in G1. Same PersistTurn 503. Leaving it out would re-hit beginTurn 503 after 160 is "fixed" via dialog-only.
4. `/health` nonce/liveness unchanged.
5. Incremental dialog uses existing REST/client. Do not invent a second persist stack.
6. PersistTurn 503/backend_unavailable uses the same degrade-queue as timeout. Failsafe stays. current-turn stays in_progress.
7. Drain: timeout and 503 abort without incrementing drainAttempts, without latching `ReplFailsafeDrainCompleted`, without `Write-Error` under Stop, without blocking the successful caller for 30s. After storage answers, a later drain in-process can replay.
8. Server: SQLITE_BUSY / lock wait inside the storage budget is not `backend_unavailable` when the process can still serve TODO/requirements. Prefer busy-timeout/retry over raising the 5s budget blindly. Prove with a concurrent-read fixture.
9. appendActions/updateTurn/completeTurn may still full-upsert after G1 if no incremental actions API exists. They still get RC2 (503 degrade). Dialog gets RC1 incremental. Do not block G1 on a new actions REST surface unless H-red says appendActions is required AC. If needed, that is a follow-on FR in S0 notes, not silent scope.
10. SESSIONLOG-002: closeout-first. If live GET returns planFile/todoId None-or-value and omit-on-new-entry 400, store-close after H-closeout AGREE. If DISAGREE, only then write more tests/code.
11. Nuke UpdateService only if a live server AC requires deployed bits. SyncAgentPlugins after plugin-core merge.
12. Worktrees: `.worktrees/session-persist` (G1), `.worktrees/session-sanitizer` (G2). Merge `--no-ff` only after slice H-green and before next depends-on merge. H-done before any `done: true`.
13. pwsh.exe only. No Python. JSON/YAML from objects.

## Requirements to capture (S0, MCP only)

Create after approval. Do not implement product in S0.

**FR-MCP-SESSIONPERSIST-001** Incremental dialog persist. Plugin `workflow.sessionlog.appendDialog` persists dialog items through `SessionLogClient.AppendDialogAsync` (POST `.../dialog`) without a full-session SubmitAsync upsert when the turn already exists.

AC:
- After a successful beginTurn, appendDialog returns success while TODO/requirements queries succeed in the same process.
- Server turn GET then contains the appended dialog items.
- Network trace / test double: method is AppendDialogAsync or POST dialog, not SubmitAsync of the full session DTO.
- Missing turn is classified not-found, retryable false, failsafe not used for that 404.

**FR-MCP-SESSIONPERSIST-002** Retryable persist degrade-queue. When SubmitAsync (beginTurn/updateTurn/completeTurn/legacy upsert) returns timeout, HTTP 503, or `backend_unavailable`, the plugin retains the session_submit failsafe, keeps current-turn.yaml in_progress, does not throw, and reports degraded/queued like today's timeout path.

AC:
- 503 and timeout produce the same degrade-queue contract.
- Failsafe file remains until a later successful persist or drain replay.
- Query after recover shows the turn.

**FR-MCP-SESSIONPERSIST-003** Failsafe drain does not poison successful calls. A successful getFr/getTr/queryHistory is not delayed by a 30s SubmitAsync drain timeout, does not print `Failsafe queue drain failed` for timeout/503, does not latch drain completed, and a later drain after storage answers replays the record.

AC:
- getFr EXIT 0 with body when a queued session_submit times out or 503s.
- stderr has no `Failsafe queue drain failed` for that class.
- `ReplFailsafeDrainCompleted` stays false on abort.
- Next drain in-process replays after a stubbed success.

**TR-MCP-SESSIONPERSIST-001** Plugin appendDialog uses SessionLogClient.AppendDialogAsync. Failsafe label for incremental dialog is not a full session_submit upsert (or is a dialog-specific failsafe that replays the dialog POST).

**TR-MCP-SESSIONPERSIST-002** PersistTurn maps 503/backend_unavailable to the existing timeout degrade branch in `Invoke-ReplPersistTurn`.

**TR-MCP-SESSIONPERSIST-003** Drain: Invoke-ReplRaw errors are inspected without `Write-Error` under Stop skipping `Test-ReplFailsafeBackendUnreachable`. Drain of SubmitAsync uses a bounded timeout shorter than 30s or runs after the caller returns.

**TR-MCP-SESSIONPERSIST-004** SessionLog SaveChanges SQLITE_BUSY / lock under budget is classified as retryable persist contention, not storage-down, when subsequent TODO/requirements reads succeed. `/health` still process liveness plus nonce.

**TEST-MCP-SESSIONPERSIST-001** Pester: appendDialog incremental; PersistTurn 503 degrade; drain abort; getFr not blocked.

**TEST-MCP-SESSIONPERSIST-002** C#: concurrent read vs SubmitAsync does not 503 when SQLite is up; AppendProcessingDialogAsync persists items; 404 missing turn.

Map FR-MCP-SESSIONPERSIST-001..003 to those TRs and TESTs.

Existing (do not duplicate):
- FR-MCP-SESSIONLOGSAN-001 / TR-MCP-SESSIONLOGSAN-001 / TR-MCP-SESSIONLOGSAN-002 / TEST-MCP-SESSIONLOGSAN-001..002
- FR-MCP-SESSIONLOGCTX-001 / TR-MCP-SESSIONLOG-006 / TEST-MCP-SESSIONLOG-006

Unlink 160-162-164 from FR-MCP-TRIAGE-002 as the governing FR (keep a note that the reports arrived via triage). Point them at FR-MCP-SESSIONPERSIST-*.

## Grouping

### G1 Persist and failsafe (worktree `.worktrees/session-persist`)

IDs: 160, 161, 162, 164.

Named tests (red first):
- Pester: `Invoke-WorkflowAppendDialog` hits AppendDialogAsync / dialog POST, not full SubmitAsync, when current-turn exists.
- Pester: PersistTurn on HTTP 503 backend_unavailable returns false, sets LastReplPersistenceDetails degraded/queued, leaves failsafe, does not throw.
- Pester: drain timeout/503 does not increment drainAttempts, does not set ReplFailsafeDrainCompleted, does not Write-Error drain failed, leaves yaml on disk; later drain replays.
- Pester: getFr path with a queued session_submit that 503s returns EXIT 0 before 30s.
- C#: AppendProcessingDialogAsync appends; missing turn 404 classified.
- C#: concurrent TODO query during SubmitAsync does not yield backend_unavailable when the DB file is valid (busy-timeout/retry). If this cannot be proven without a flake, document the fixture and keep TR-004 as the server bar.

Touch: `plugins/core/lib-ps/repl-invoke.ps1`, matching Pester, `SessionLogService` SaveChanges classification only if RC4 tests require it, `McpErrorClassifier` only if a new contention code is introduced (prefer reuse retryable 503 vs new code unless AC needs a distinct code). Client already has AppendDialogAsync.

### G2 Sanitizer remaining (worktree `.worktrees/session-sanitizer`)

ID: MCP-SESSIONLOG-001 S15-S19.

Named tests:
- SessionLogController integration: raw secret fixture in every DTO section; query and GET replacements; DB rows unsanitized.
- Query-semantics: secret still matches filter; TotalCount/order/Limit/Offset unchanged.
- stdio tools/list plus sessionlog query/get; federated remote fixture; no raw secrets in JSON-RPC.
- Config example under Mcp:SessionLogSanitization without real credentials.
- Gate: sanitizer/options/service + Support.Mcp + HTTP integration + stdio/federation Failed 0 Skipped 0.

Can start after S0. Merge after G1 if both touch Program.cs; otherwise parallel. Prefer sequential merge if Program.cs conflict risk is real (sanitizer already registered; G2 tests should not re-register).

### G3 SESSIONLOG-002 closeout (no feature rewrite unless DISAGREE)

Live: beginTurn with planFile/todoId None or real values; GET returns both; omit on new entry 400. Named tests already claimed green. H-closeout on live + unit. UpdateService only if live schema lacks columns.

## Slices

**S0 Requirements.** Create FR/TR/TEST/AC/mappings via MCP. Link 160-164 and SESSIONLOG-001/002. Copy this file to `docs/plans/sessionlog-remediate-001.md`. Create PLAN-SESSIONLOGREMEDIATE-001. H0 hostile on AC completeness and overlap. Do not write product code.

**S1 G1 tests red.** Write named Pester/C# tests. Show red. H-red.

**S2 G1 implementation green.** Incremental appendDialog, PersistTurn 503 degrade, drain abort, server contention if tests require. Current-plus-prior Failed 0 Skipped 0. H-green. Merge persist worktree. SyncAgentPlugins.

**S3 G2 sanitizer S15-S19.** Red then green. H-red then H-green. Merge. Do not mark SESSIONLOG-001 done yet.

**S4 G3 closeout 002.** Live proof. H-closeout. If AGREE, eligible for done at S7. If DISAGREE, only the listed gaps.

**S5 Named suite gate.** Pester persist/drain + sanitizer/controller + sessionlog planFile tests. Support.Mcp session-log scopes. Failed 0 Skipped 0. ValidateTraceability.

**S6 Live.** If G1/G2/G3 need deployed server: elevated `./build.ps1 UpdateService`. Prove appendDialog persists; beginTurn 503 degrade or success under concurrent reads; GET sanitizes; GET planFile/todoId present. `/health?nonce=` still echoes.

**S7 H-done.** All five listed TODOs plus 164 `done: true` with AGREE receipt in doneSummary, or operator-approved defer (none planned). PLAN-SESSIONLOGREMEDIATE-001 done last.

## Named tests (minimum)

- Pester PluginPowerShellRuntime / TriagePluginIdentity extensions for appendDialog incremental, PersistTurn 503, drain abort, getFr not blocked.
- C# SessionLogController/service tests for dialog append and contention classification.
- Existing SessionLogSanitizer* plus new controller/query/stdio tests for S15-S17.
- Existing planFile/todoId tests remain green (no skip).

## Merge and closeout

From repo root: `git merge --no-ff sessionpersist/<slice> -m "merge sessionpersist/<slice> after hostile AGREE <receipt>"`. Then plugin `workflow.todo.update` done true with doneSummary citing `docs/receipts/hostile-validator-<utc>.md`. Do not merge DISAGREE. Do not force-push.

## Out of scope

- BUG-TRIAGE-163 avalonia-remote.
- QuadBrain, FILETOOLS, Handoff, Octopus, wiki dump, hostile-review queue.
- `/health` liveness change.
- PSGallery vendor patch.
- Mass-completing pending FRs.
- wrap-up/commit-sync/wiki push unless the operator asks after H-done.

## Risks

- Incremental dialog 404 if beginTurn was only failsafe-queued. AC: 404 classified; retry after drain; do not upsert the whole session as a silent fallback unless tests prove it does not 503.
- Server contention fixture flakes. Prefer busy-timeout + deterministic lock test, not wall-clock 5s.
- SyncAgentPlugins required after plugin-core or hostile will DISAGREE D4 like leftover S7.
- F: disk pressure. Clean worktree bin/obj. Receipts to `docs/receipts/` and scratch.

## Approval

Stop here. Implement S0 only after explicit plan approval. Do not create PLAN-SESSIONLOGREMEDIATE-001 or the persist FR/TR/TEST until approved.
