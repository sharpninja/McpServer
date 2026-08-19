# Plan: Remediate 16 high BUG-TRIAGE items

**Scope TODOs (all `Done: false`, high):** BUG-TRIAGE-110, 111, 112, 114, 115, 119, 123, 124, 126, 128, 131, 132, 139, 143, 148, 149

**Master tracking TODO (create after approval):** PLAN-TRIAGECLUSTER-001

**Process:** Byrd Development Process v4 (`docs/Development-Process-draft-v4.md`)

**Status:** Approved 2026-08-18 with one amendment (unified error envelope). S0 in progress.

**Breaking change:** Yes. Session-level tags no longer silently drop. Agent Help echo fallback may not complete a turn as a diagnosis. Error payloads across REST, MCP tools, REPL, and plugins change from mixed `{error: message}` / untyped 500 / ProblemDetails-only to one machine-readable envelope. No `/health` contract change.

**Hostile gates:** H0 after requirements. Then H{n}-red and H{n}-green for each implementation slice. H-done before any of the 16 TODOs flip to `done: true`. OverallVerdict AGREE required at each gate.

## Problem

These 16 high items are open in the MCP TODO store and still match live code. They cluster into a small set of roots:

- SQL/schema drift and hang-until-REPL-timeout
- Opaque EF `DbUpdateException.Message` on tools and REST
- Session-log Submit/Replace semantics (additive merge, required `planFile`/`todoId`, superseded persist 500)
- Plugin cache/session pointer and 30s REPL timeout
- Dual-store EXEC-TODO vs execution-state
- Legacy TR id pre-check vs list
- Agent Help plan-mode + echo fallback
- BUG-TRIAGE-139: large already-written remediation still `done: false` pending independent approval

Treating them as 16 independent epics will re-fix the same serializer, the same Submit path, and the same plugin timeout table.

## Value

Agents stop losing turns, triage intake, and EXEC-TODO test-plan writes to opaque 500s and 30s hard fails. Operators can get/update/delete listed legacy TRs. Agent Help either diagnoses or fails honestly.

## Locked decisions

1. **Cluster into slices.** Implement and hostile-gate by slice. Close the listed high TODOs when that slice's AC and hostile AGREE land. Do not open a new architecture program.

2. **114 and 115 are one defect.** Same missing `AgentSession*` columns / migration `20260722214500_AddAgentSessionHeaderFields`. One ops+hardening slice. Medium 116 and 118 are absorbed if the same databases and the same fail-fast land; they are not extra implementation.

3. **139 is closeout first, not a rewrite.** Note, Remaining, and DoneSummary already describe eleven remediation passes and explicit `done=false` pending independent adversarial approval. Slice S9 runs independent hostile against the *original* AC (workspace parent row or classified not_found; classified DbUpdateException; create returns id and lists). If AGREE, mark 139 done and stop. If DISAGREE, implement only the FAIL list; do not restart the 139 saga.

4. **Do not break `/health`.** Storage-only outages keep process liveness and nonce echo (TR-MCP-HEALTH-003). Fail-fast and classify on intake/submit paths. `/ready` or `health.storage` already reports unreachable.

5. **Do not relax `planFile`/`todoId` required-on-first-persist (FR-MCP-SESSIONLOGCTX-001).** Supersede/rebind persist must stamp `None` when the hook turn omitted them. That is the default explanation for 148 and several medium 134/147/150-156 siblings.

6. **Normalized error envelope everywhere (119, operator amendment 2026-08-18).** All `/mcpserver` REST failures, MCP STDIO/Streamable HTTP tool errors, REPL `type: error` payloads, and plugin shim failures use one machine-readable shape so plugins and Agent Help can branch without parsing prose:

   - `code` (stable snake_case): `backend_unavailable`, `persistence_error`, `validation_error`, `not_found`, `conflict`, `timeout`, `turn_immutable`, plus existing REPL codes.
   - `message` (human, no raw SqlClient retry ads).
   - `retryable` (boolean).
   - `details` (optional object): innermost provider text, field, constraint, requestId.

   REST carries those four fields as RFC 7807 ProblemDetails extensions (`code`, `retryable`, `details`) in addition to `title`/`detail`/`status`. MCP tools stop emitting `{ error: exception.Message }` as the only shape. REPL already has `code`/`message`/`details`; add `retryable` and map storage/EF through the same classifier. Plugin shims must not wrap a classified server error as opaque `internal_server_error`. `DbUpdateException` / `AggregateException` always populate `details.inner`. This is S2 and is a prerequisite for S3/S4/S6/S8.

7. **Session-level tags persist (112).** Silent drop is forbidden. Add session-scoped tags on `SessionLogEntity` (new nullable collection + three-provider migration). Query returns them. Do not invent a 400-only path that still lists tags as accepted on submit.

8. **ReplaceTurn missing turn is 404.** Align with ReplaceTurnSection. Do not upsert a missing requestId on replace. Submit remains the create/upsert path. Cross-session requestId reuse stays allowed and documented; uniqueness stays `(SessionLogId, RequestId)`.

9. **Identical full `actions[]` resubmit does not duplicate rows.** Tighten `SameAction` (stable identity: order + type + filePath + description) so an identical payload is idempotent. `replace_turn` / section replace remains the exact-set tool. Do not add a new `replaceCollections` flag in this program.

10. **`canceled` / `cancelled` are first-class terminal statuses.** Persist, re-query, document in `docs/context/session-log-schema.md`. Superseded hook turns use `canceled` plus a response that names the new requestId.

11. **Triage/SQL fail-fast budget is 5 seconds** for connect+command on intake (`TriageService.SubmitReportAsync`) and on session-log SaveChanges used by beginTurn persist. Map timeout to `backend_unavailable` / storage-unavailable. Plugin failsafe stays on disk. After restore, the next submit succeeds.

12. **beginTurn timeout is recoverable, not `turn-open-failed` only (131).** If `SubmitAsync` exceeds the short REPL budget after failsafe write: keep `current-turn.yaml` active, keep failsafe queued, return explicit `queued`/`degraded` so hooks can continue. Happy path unchanged. Do not raise the default 30s for all sessionlog methods (BUG-TRIAGE-072 hang risk). `workflow.agenthelp.submitTurn` *does* get the long timeout (HelperTimeout, default 120s).

13. **completeTurn after sessionId rebind (143).** Persist against the sessionId captured on the turn at open (rewrite into the persist identity), or import/supersede that requestId onto the active session then complete it. Leftover failsafe is not success.

14. **Root session pointer is sticky (111).** Background/child `openSession`/`bootstrap` must not overwrite workspace `session-state.yaml` / `active-session` used by UserPromptSubmit. Child sessions write under `sessions/<sessionKey>/` only. Pester extends TEST-MCP-BUGTRIAGE-028/034.

15. **Cache version swap (124).** `ReplacePluginCache` retains version N until no open turn references it, *or* hooks re-resolve to the newest installed cache when N is gone and emit a named version-drift error only if no replacement exists. Marker regeneration must not keep a missing `CODEX_PLUGIN_ROOT`. Automatic discovery of newest installed cache when env root is missing.

16. **PowerShell.Mcp console rollover (126).** We do not patch PSGallery internals in this plan. Product fix: every hook/wrapper entrypoint re-asserts workspace from host params (`-WorkspacePath` / hook payload cwd) before `Resolve-McpCacheDir`. New console cwd must become the workspace, not the user profile. Update `templates/prompt-templates.yaml` PowerShell.Mcp routing text. Regression: hook with cwd=profile and empty env still opens a turn when workspace path is in the hook payload.

17. **EXEC dual-store (123).** On `FindTodo` miss, resolve via durable `ITodoService.GetByIdAsync` for the same workspace (path-normalized). If found and EXEC/Byrd-shaped, rehydrate a `TodoExecutionRecord` and continue. Errors distinguish durable-missing vs execution-state-only-missing.

18. **create_todos_from_plan (132).** `GenerateNextTodoIdAsync` / `CreateAsync` ignore query filters for soft-deleted collisions (revive or skip id). Batch failure leaves no net new durable rows and no phase membership (transaction or compensating hard-clear/revive). Invalid dependsOn still fails before any insert. Errors use the shared innermost serializer.

19. **Legacy TR ids (128).** Strict `TrIdPattern` on **create** (and batch create) only. get/update/delete accept any non-empty id and resolve the store (404 if missing). Every `listTr` id is `getTr`-able.

20. **Agent Help (149).** Progress-only grok-cli output stays incomplete and names missing `FINAL ANSWER`. CLI failure/timeout/unavailable does **not** complete via `UseEchoHelperFallback`. Default `UseEchoHelperFallback` to false for production diagnosis, or keep the flag but never set status completed on echo text. `workflow.agenthelp.submitTurn` is a long REPL method.

21. **Tests first per increment.** Write the AC-covering unit/Pester tests, show red, then implement. Full current+prior suite of that slice plus previous slices must be Failed 0 / Skipped 0 to exit a slice. No skipped placeholders.

22. **Requirements first (Phase 0).** Create dedicated FR/TR/TEST for these slices in the MCP store (do not keep everything on generic FR-MCP-TRIAGE-002 / TR-MCP-TRIAGE-004). Map and `ValidateTraceability` before product code.

23. **No Python.** `pwsh.exe` and `dotnet test` only.

24. **Deploy.** After plugin-core slices (S5) and after server slices that must run on LEGION2 to close ops AC (S1 schema, S3 fail-fast), deploy with elevated `./build.ps1 UpdateService` and `./build.ps1 SyncAgentPlugins`. Never hand-copy binaries.

25. **Out of scope unless absorbed by the same AC.** Medium items not in the operator list. QuadBrain/QBAgent/file-tools plans. Rewriting federation. Changing `/health` liveness. Federated products. New hostile-review product (MCP-HOSTILEREVIEW-001).

## Slice map

**S0 Requirements (no product code)**
- Create and map the IDs below. Hostile H0.

**S1 Schema drift (114, 115)**
- Confirm/apply `20260722214500_AddAgentSessionHeaderFields` on every workspace DB the host uses (including TruckMate if it shares the instance).
- Startup fail-fast: if `SessionLogs` lacks the four nullable agent header columns, log a pending-migration error and do not serve sessionlog query as raw SQL `Invalid column name`.
- Tests: coordinator reports the migration; a fixture DB missing columns fails closed with a named error; after migrate, query with and without text filter succeeds.
- Live AC: `sessionlog_query` on TruckMate (or the shared SQL Server store) returns 200/empty, not Invalid column name.
- Absorbs medium 116/118 if the same schema and fail-fast cover them.

**S2 Normalized errors (119 plus operator amendment)**
- Red tests: forced `DbUpdateException` on submit/replace_turn/create_todos tool path includes `code=persistence_error`, `retryable=false`, and `details.inner`.
- Red tests: REST validation and not-found return the same four fields; REPL error payload includes `retryable`; plugin does not collapse classified errors to `internal_server_error`.
- Implement one classifier used by `McpToolErrors`, SessionLogController (and other `/mcpserver` exception filters), REPL error envelope builder, and plugin shim mapping.
- Unblocks diagnosis of 132 and 148. Does not by itself close 148.

**S3 Storage fail-fast (110)**
- Red tests: unreachable SQL on `SubmitReportAsync` fails within about 5s with storage-unavailable; `/health` still Healthy + nonce; no partial triage rows.
- Implement short CancellationToken + SqlClient connect/command timeouts on intake; map to classified error; failsafe queue still works.
- After restore, next `workflow.triage.report` persists.

**S4 Session-log store (112, 148)**
- Red then green increments: (a) identical actions[] resubmit idempotent; (b) session tags persist + query; (c) replace_turn missing session/turn is structured 404; (d) canceled/cancelled round-trip + schema doc; (e) SQLITE_BUSY retried or mapped retryable; (f) beginTurn supersede persist of a hook turn with omitted planFile/todoId writes `None`/`None`, status canceled, no 500, failsafe cleared, query shows superseded-by text.
- Prefer `UpsertTurnAsync` for superseded persist instead of a one-turn whole-session Submit.
- Medium 113, 134, 147, 150-156 close only if their AC is fully covered by (f) plus S2; otherwise leave open.

**S5 Plugin identity and timeouts (111, 124, 126, 131, 143)**
- Pester first (red):
  - Root session A, child bootstrap B, UserPromptSubmit beginTurn uses A.
  - Turn open on cache A, A replaced by B, code-verify/status still works or named drift.
  - Hook cwd=profile, env cleared, payload has workspace path, `Resolve-McpCacheDir` succeeds.
  - SubmitAsync timeout after failsafe: beginTurn returns degraded/queued, failsafe retained, current-turn present.
  - completeTurn after sessionId rotation on current-turn returns true, failsafe cleared.
- Implement cache-scope sticky root, cache retain-or-rebind, hook workspace re-assert, degraded beginTurn, persist-identity on completeTurn.
- Sync plugins via Nuke. Mirror contract in Codex plugin only through shared `plugins/core` (no one-off fork).

**S6 EXEC dual-store and plan create (123, 132)**
- Red: durable EXEC-TODO present, execution-state missing row, `set_todo_test_plan` succeeds; neither present is a distinct not-found; soft-deleted EXEC id does not opaque-UNIQUE; failed batch is retry-clean; invalid dependsOn fails before insert.
- Implement FindTodo durable fallback + path normalize; CreateAsync/GenerateNextTodoId IgnoreQueryFilters; transactional or compensating batch; shared error serializer.

**S7 Legacy TR ids (128)**
- Red: seed `TR-066`, `listTr` returns it, `getTr`/`updateTr`/`deleteTr` succeed; `createTr` of `TR-066` still rejected.
- Change `RequirementsWorkflow` to validate canonical form only on create/batch create.

**S8 Agent Help (149)**
- Red: plan-only grok-cli body is incomplete (names FINAL ANSWER); CLI failure with fallback flag on is not status completed; submitTurn timeout >= HelperTimeout.
- Implement strategy/timeout/fallback locks. Do not treat SessionLog 500 as this bug.

**S9 139 closeout**
- Independent hostile on original AC + claimed GREEN suite evidence. No product edits unless DISAGREE.

**S10 Exit**
- Hostile H-done on the claim that all 16 listed TODOs that this program promised are either `done: true` with AGREE cited, or explicitly deferred with operator approval (none deferred in this plan).
- `ValidateTraceability` green. Slice suites Failed 0 / Skipped 0.

## Requirements to create (S0, before product code)

Create in the MCP store, map FR to TR and TEST, export so `./build.ps1 ValidateTraceability` is green. Do not hang new AC only on FR-MCP-TRIAGE-002.

**Functional**
- **FR-MCP-TRIAGEERR-001** Every failure on REST `/mcpserver/*`, MCP tools, REPL workflow errors, and plugin shims returns `{ code, message, retryable, details? }` (REST as ProblemDetails extensions). Plugins and Agent Help can branch on `code` without scraping prose. Innermost EF/provider text lives in `details.inner`.
- **FR-MCP-TRIAGESTORE-001** Session-log persist is diagnosable and idempotent on identical action resubmit; session tags persist; replace missing turn is 404; canceled is queryable; superseded hook turns persist canceled with None sentinels and no opaque 500.
- **FR-MCP-TRIAGESTORE-002** Session-log and triage mutating calls fail fast with a classified storage-unavailable error when SQL is unreachable, without flipping `/health` liveness.
- **FR-MCP-TRIAGESCHEMA-001** After host start, sessionlog query never fails with missing AgentSession* column names; missing schema fails closed as pending-migration.
- **FR-MCP-TRIAGEPLUGIN-001** Root UserPromptSubmit stays on the root session while background agents run; cache replacement does not break in-flight hooks; new PS consoles inherit workspace identity; beginTurn timeout is degraded/queued; completeTurn survives sessionId rebind.
- **FR-MCP-TRIAGETODO-001** `set_todo_test_plan` rehydrates from durable EXEC-TODO; `create_todos_from_plan` is retry-clean and never returns bare EF outer text.
- **FR-MCP-TRIAGEREQ-001** Every TR id returned by listTr is get/update/delete-able; create stays canonical.
- **FR-MCP-TRIAGEHELP-001** Agent Help turns are either a FINAL ANSWER diagnosis or an incomplete/error; never a completed echo-fallback.

**Technical**
- **TR-MCP-TRIAGEERR-001** One shared classifier (C# + plugin mapping) produces `{ code, message, retryable, details }`. `McpToolErrors`, `/mcpserver` exception filter, REPL error envelope, and plugin shim consume it. `backend_unavailable` stays retryable true. Persistence/validation/not_found/conflict are retryable false unless SQLITE_BUSY/deadlock.
- **TR-MCP-TRIAGESTORE-001** SessionLogService merge/`SameAction`, SessionLogTag rows, ReplaceTurn 404, None-default on plugin superseded persist, UpsertTurn for supersede, controller/tool innermost DbUpdateException mapping, 5s save budget on intake/submit.
- **TR-MCP-TRIAGESCHEMA-001** Startup schema probe for the four agent header columns; apply `20260722214500_AddAgentSessionHeaderFields` on all host DBs.
- **TR-MCP-TRIAGEPLUGIN-001** plugins/core cache-scope sticky root; ReplacePluginCache retain-or-rebind; Resolve-McpCacheDir host-payload fallback; Get-ReplMethodTimeoutSeconds long list includes `workflow.agenthelp.submitTurn`; beginTurn degraded path; completeTurn persist identity.
- **TR-MCP-TRIAGETODO-001** TodoExecutionService durable fallback + path normalize; EfTodoService IgnoreQueryFilters on id allocate/create; batch compensation.
- **TR-MCP-TRIAGEREQ-001** RequirementsWorkflow ValidateTrId create-only.
- **TR-MCP-TRIAGEHELP-001** AgentHelpConversationService / GrokCliAgentExecutionStrategy: no completed echo; long REPL timeout.

**Tests**
- **TEST-MCP-TRIAGEERR-001** Tool, REST, and REPL each emit the four-field envelope for validation, not-found, persistence (with inner), and backend_unavailable.
- **TEST-MCP-TRIAGESTORE-001** through **007** covering S2-S4 AC (unit + targeted integration).
- **TEST-MCP-TRIAGESCHEMA-001** missing-column fail-closed + post-migrate query.
- **TEST-MCP-TRIAGEPLUGIN-001** through **005** Pester for S5.
- **TEST-MCP-TRIAGETODO-001** and **002** for 123/132.
- **TEST-MCP-TRIAGEREQ-001** for 128.
- **TEST-MCP-TRIAGEHELP-001** for 149.

Reuse existing TR-MCP-HEALTH-003, FR-MCP-SESSIONLOGCTX-001, TR-MCP-REPL-016/017, TR-MCP-PLUGIN-012, TEST-MCP-BUGTRIAGE-028/034 as parents where the new tests extend them. Do not weaken those IDs.

## Tests to write first (per slice, expected red)

Named before implementation:

- `McpToolErrorsTests.Serialize_DbUpdateException_IncludesInnermostMessage` (S2)
- `SessionLogControllerTests.SubmitAsync_DbUpdateException_ReturnsPersistenceProblem` (S2)
- `TriageServiceTests.SubmitReportAsync_UnreachableSql_FailsFastWithStorageUnavailable` (S3)
- `SessionLogServiceTests.SubmitAsync_IdenticalActions_DoesNotDuplicate` (S4)
- `SessionLogServiceTests.SubmitAsync_SessionTags_RoundTrip` (S4)
- `SessionLogServiceTests.ReplaceTurnAsync_MissingRequestId_ReturnsNotFound` (S4)
- `SessionLogServiceTests.SubmitAsync_CanceledStatus_RoundTrips` (S4)
- `ReplInvoke.SupersedeInProgressTurn_PersistsCanceledWithNoneSentinels` (S4/S5 Pester)
- `CacheScope.BackgroundOpenSession_DoesNotRebindRootActiveSession` (S5)
- `PluginCache.ReplaceWhileTurnOpen_ResolvesOrNamedDrift` (S5)
- `ResolveCacheDir.ProfileCwdWithoutEnv_UsesHookWorkspacePath` (S5)
- `BeginTurn.SubmitTimeoutAfterFailsafe_ReturnsDegradedQueued` (S5)
- `CompleteTurn.SessionIdRebind_PersistsAndClearsFailsafe` (S5)
- `TodoExecutionServiceTests.SetTestPlanAsync_DurableOnly_Rehydrates` (S6)
- `EfTodoServiceTests.CreateAsync_SoftDeletedId_RevivesOrSkips` (S6)
- `RequirementsWorkflowTests.GetTrAsync_LegacyId_ReturnsPersisted` (S7)
- `RequirementsWorkflowTests.CreateTrAsync_LegacyId_Rejected` (S7)
- `AgentHelpConversationServiceTests.ProgressOnly_IsIncomplete` (S8)
- `AgentHelpConversationServiceTests.CliFailure_DoesNotCompleteViaEcho` (S8)

Expected red: types/branches named above do not exist or assert the old behavior.

## Validation scope per slice

- S0: `./build.ps1 ValidateTraceability` (Failed 0 on FR errors).
- S1-S4, S6-S8: `dotnet test tests/McpServer.Support.Mcp.Tests` plus the touched sibling project (`Client.Tests`, `Repl.Core.Tests`) with Failed 0 / Skipped 0 on the executed filter, then the full `./build.ps1 Test` to exit the slice.
- S5: Pester for plugins/core plus `./build.ps1 Test` (plugin scripts must not break C#). Then `./build.ps1 SyncAgentPlugins`.
- S1 live AC and S3 live AC: after `./build.ps1 UpdateService`, query TruckMate/sessionlog and a storage-down drill only if a safe lab toggle exists; do not take production SQL down without operator presence.
- S9: hostile receipt only.
- S10: hostile H-done + all 16 `todo_get` Done=true citing that receipt.

## Rollout

1. Approve this plan.
2. Create PLAN-TRIAGECLUSTER-001 and the S0 requirements. Do not start S1+ until H0 AGREE.
3. Implement S2 then S1/S3 (shared diagnostics + fail-fast), then S4, then S5+S6 in parallel, then S7 and S8 (independent). Run S9 as soon as H0 is done (it does not block others).
4. Nuke UpdateService when server AC must be proven live. Nuke SyncAgentPlugins after S5.
5. Mark each BUG-TRIAGE id done only after its slice hostile AGREE.

## Risks

- 148 500 may not be only missing planFile/todoId. S2 lands first so the next reproduce names the constraint. If it is a different constraint, stay on S4 until the named error is fixed; do not guess.
- 114/115 may already be applied on McpServer's own DB (live sessionlog_query works here) but still fail on TruckMate/shared SQL Server. S1 is ops-verify plus fail-fast, not "rewrite SessionLogService mapping".
- 139 closeout may DISAGREE. Budget a follow-on slice only for that FAIL list; do not stall S2-S8.
- PowerShell.Mcp pool is third-party. If hook re-assert is insufficient, stop and report; do not vendor-patch PSGallery in this plan.
- Raising all sessionlog timeouts would reintroduce hangs. Only agenthelp is long; beginTurn degrades instead.

## Hostile checkpoints

- **H0** S0 requirements+mappings+traceability
- **H2-red / H2-green** S2
- **H1-red / H1-green** S1 (after H2 so fail-fast errors are classified)
- **H3-red / H3-green** S3
- **H4-red / H4-green** S4
- **H5-red / H5-green** S5
- **H6-red / H6-green** S6
- **H7-red / H7-green** S7
- **H8-red / H8-green** S8
- **H9** S9 139 closeout
- **H-done** all listed high TODOs done with receipts

## What this plan will not do

- Mark any of the 16 done from this document alone
- Re-implement 139 before a fresh hostile DISAGREE
- Make `/health` fail on storage loss
- Allow create of non-canonical TR ids
- Treat echo-fallback text as a successful Agent Help diagnosis
