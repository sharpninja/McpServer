# Plan: Resolve remaining BUG-TRIAGE-* TODOs

**Scope (live MCP `todo_list` `done: false`, 2026-08-19):** 27 medium items: 106, 107, 108, 113, 116, 117, 118, 120, 121, 122, 125, 130, 134, 140, 142, 144, 147, 150, 151, 152, 153, 154, 155, 156, 157, 158, 159.

**Master tracking TODO (create after approval):** `PLAN-TRIAGELEFTOVER-001`

**Durable plan path after approval:** `docs/plans/triage-cluster-002.md` (copy of this document)

**Process:** Byrd Development Process v4 (`docs/Development-Process-draft-v4.md`)

**Predecessor:** `PLAN-TRIAGECLUSTER-001` / `docs/plans/triage-cluster-001.md` (16 high items). Do not reopen that plan. Do not mark any leftover TODO `done: true` from cluster receipts alone.

**Breaking change:** Possible for G3 (108) if session-log reject/annotate foreign `filesModified`/commits. Not for `/health`. Not for `planFile`/`todoId` required-on-first-persist.

**Hostile gates:** H0 after requirements. Closeout hostile per closeout group. For each implementation worktree: H-red after tests, H-green after implementation, H-done before merge and before `done: true`. OverallVerdict AGREE required. Cite receipt path in `doneSummary`.

## Problem

27 medium BUG-TRIAGE TODOs remain `Done: false`. They are not 27 independent epics. Ten are the same superseded-persist 500 (`planFile`/`todoId` omitted). Two are the same AgentSession schema gap. Several others share `plugins/core/lib-ps/repl-invoke.ps1` and `plugin-hook.ps1`. Treating them as 27 PRs will re-edit the same persist and failsafe code.

Some titles already match code shipped on `develop` (`c81abaf0` / later `06200782`): STORE-006 canceled None stamp, SessionLogSchemaGuard, SyncAgentPlugins tarball from `package.json`, unified `{code,message,retryable,details}`, UserPromptSubmit background isolation. Those items still need independent closeout, not a rewrite.

## Value

Agents stop losing superseded turns to 500s, failsafe drain stops burning 503s, Codex closeout does not hijack another agent's turn, SessionEnd does not throw, transcript ingest does not stay degraded on known Codex record types, and audits stop mixing foreign-repo paths into the wrong workspace log.

## Grouping by area of concern

### G1 Supersede persist without planFile/todoId (closeout-first)

**IDs:** 134, 147, 150, 151, 152, 153, 154, 155, 156, 157 (ten items, one root).

**Claimed defect:** `Invoke-ReplSupersedeCurrentTurnIfInProgress` persists canceled without `planFile`/`todoId`; `ValidateForNewEntry` throws; 500 `internal_server_error`.

**Shipped overlap:** FR-MCP-TRIAGESTORE-001 / STORE-006 / cluster decision 5. Canceled/cancelled omitted fields stamp `None` then validate. Isolation now also reduces accidental supersede of root work turns.

**Unique leftover AC (only if closeout DISAGREE):** 134 concurrent `current-turn.yaml` clobber (file lock or per-requestId turn files). Do not invent a global unique `requestId` index.

### G2 AgentSession schema / pending migration (closeout-first)

**IDs:** 116, 118.

**Claimed defect:** SQL `Invalid column name` for AgentSession* columns; hand-written migration without EF attributes.

**Shipped overlap:** FR-MCP-TRIAGESCHEMA-001, `SessionLogSchemaGuard`, Sqlite/SqlServer/Postgres migrations `20260818205751` / `20260818205807` / `20260818205822`. Cluster S1 said 116/118 absorb if the same fail-closed path lands.

**Closeout proof:** live `sessionlog_query` on a SQL Server workspace (TruckMate if still the reporter) returns 200, not Invalid column name. Unit: missing-column fixture fails closed.

### G3 Session-log attribution, 113 residual, replace_section 503

**IDs:** 108 (new), 113 (partial leftover), 144.

**108:** Turn `filesModified`/commits from another repo recorded on the active workspace session. AC: reject or require explicit foreign-workspace/repo marker. Forward-only unless operator asks for historical rewrite.

**113 leftover after cluster:** Do **not** add a global unique `requestId` constraint. Uniqueness stays `(SessionLogId, RequestId)`. Document that cross-session duplicate requestIds are allowed. Remaining if still red: large `queryText` mapped as classified `persistence_error` (not generic EF text); submit merge-vs-replace documented. Tags, canceled, replace missing 404 are cluster-covered; closeout those sub-claims first.

**144:** `sessionlog_replace_section` transient storage failure must be retryable in the client-visible contract (`retryable: true` on tool JSON-RPC and HTTP 503 body). Turn must remain for a later dialog. Reuse `McpErrorClassifier` / `McpToolErrors`. Do not add a new availability subsystem.

### G4 Failsafe drain on 503

**ID:** 159.

**Defect:** `Test-ReplFailsafeBackendUnreachable` does not treat `backend_unavailable` or HTTP 503 as backend-down; `Invoke-ReplFailsafeDrainOnFirstSuccess` latches completed so `replayed=0 failed=1` never retries in-process.

**AC:** 503 still classified retryable and failsafe remains on disk; drain aborts without incrementing `drainAttempts` or quarantining; after storage answers, a later drain in the same process can replay; query shows the dialog.

### G5 StrictMode Count on updateTurn

**ID:** 158.

**Defect:** `New-McpPluginTurnUpsertRequest` / `Invoke-WorkflowUpdateTurn` read `.Count` under `Set-StrictMode -Version Latest` on `$null` or scalar tags/contextList.

**AC:** `workflow.sessionlog.updateTurn` succeeds for omitted, empty, or single scalar tags/contextList; exit 0; no `Count cannot be found`; success stdout stays silent.

### G6 Cross-agent CompleteTurn / empty-title closeout

**IDs:** 106, 142.

**Defect:** Shared `current-turn.yaml` rotates to another sourceType; `Assert-ReplCurrentTurnFresh` rebinds and Submit 500s without planFile/todoId; CompleteTurn on empty Title hijacks a concurrent turn.

**AC:** Different sourceType prefix (Codex vs GrokCode vs ClaudeCode) is refused or restored to the originating agent cache, not rebound. Same-agent session rotation still rebinds. CompleteTurn never closes a different `requestId` than the one it was asked to close. Empty title uses TR-MCP-REPL-015 omit, not fail.

### G7 SessionEnd unresolved cache

**ID:** 140.

**Defect:** SessionEnd calls `Resolve-McpCacheDir` with no StartPath and throws instead of `{}`. FR-MCP-115: status-only SessionEnd emits `{}` even with no session.

**AC:** Exit 0 and `{}` when cache cannot be resolved. When cwd / `CLAUDE_PROJECT_DIR` / hook payload identifies a workspace, flush that cache. No-op is not a Claude hook failure.

### G8 Codex verify wrapper

**IDs:** 120, 125, 130.

**120:** beginTurn Submit > 30s. Overlap with cluster 131 degraded/queued. Closeout-first; if DISAGREE, only the FAIL list.

**125:** `code-verify.ps1` unhandled `WriteAllText` when the drive is full. Typed disk-capacity failure; current-turn audit preserved.

**130:** Wrapper hangs in-process after local build with no child `dotnet`. Hard timeout; console released; documented timeout honored.

### G9 Codex transcript adapter

**ID:** 122.

**Defect:** No handlers for `inter_agent_communication_metadata`, `tool_search_call`, `tool_search_output`. importRecovery stays pending.

**AC:** Fixtures from real Codex JSONL produce zero unknown diagnostics for those types; paired tool-call events or documented info skip; successful Persist=true ingest deletes importRecovery and reports `persisted=true` `degraded=false`. Extend `CodexTranscriptAdapterCoverageTests` / TEST-MCP-TRANSCRIPT-011.

### G10 Stale open turns / completed-then-canceled

**ID:** 121.

**Lock:** Forward-only. Do not auto-cancel the 102 historical in_progress turns. Recurrence of "completed business turn later canceled" is the isolation defect already AGREE-closed for hostile briefs; extend tests so a later UserPromptSubmit does not cancel a **completed** root turn (already isolate-skip) and does not cancel an in_progress root work turn on a background prompt (already reuse). Add a query/filter or documented operator procedure to list stale `in_progress` older than N hours. Mass close is out of scope unless the operator later approves a one-shot reconcile.

### G11 SyncAgentPlugins tarball name (closeout-first)

**ID:** 107.

**Shipped:** `Build.SyncAgentPlugins.cs` derives packed name from `package.json` and fails if npm pack mismatches. Closeout: Pester/Build.Tests plus a real `npm pack` name check. If DISAGREE, only the FAIL list (legacy vendor `0.1.0.tgz` rename at next sync).

### G12 PowerShell.MCP cross-volume TEMP (ops, not PSGallery)

**ID:** 117.

**Lock:** Do not patch PSGallery `PowerShell.MCP`. Align TEMP/TMP to the workspace volume in plugin session-start / wrapper entrypoints when the workspace is not on the TEMP drive. Keep prompt-template guidance. Post-edit verify remains required. Failed move must not look like success.

## Locked decisions

1. **Group then slice.** Implement by the groups above. One worktree per implementation slice that shares files. Closeout groups do not get a worktree unless hostile DISAGREE.

2. **Closeout-first** for G1, G2, G11, and the cluster-covered sub-claims of 113 and 120. Independent hostile against original AC on current `develop`. AGREE: `done: true` with receipt. DISAGREE: implement only the FAIL list in a worktree.

3. **Worktrees live under the repo root:** `F:\GitHub\McpServer\.worktrees/<slice-id>/`. Add `.worktrees/` to `.gitignore` in S0. Create with `git worktree add .worktrees/<slice-id> -b triage/<slice-id>`. Subagents use that cwd (or `isolation: worktree` pointed at that path). No sibling-directory worktrees.

4. **Merge only after hostile AGREE** for that slice. Orchestrator on `develop` merges `--no-ff` from `triage/<slice-id>` only when the receipt OverallVerdict is AGREE, FAIL list empty, and slice tests Failed 0 / Skipped 0. Then flip MCP TODOs `done: true` with `doneSummary` citing the receipt. Never merge a DISAGREE branch.

5. **Do not relax** FR-MCP-SESSIONLOGCTX-001. Do not change `/health` liveness. Do not patch PSGallery. Do not global-unique `requestId`. Do not auto-purge historical in_progress turns.

6. **Shared file serialization.** `plugins/core/lib-ps/repl-invoke.ps1` and `plugin-hook.ps1` are one worktree (S2 Plugin-core leftovers: G4, G5, G6, G7, G8). Do not parallel two subagents on those files.

7. **Tests first** per increment. Red (mocks where required), then implement, then full current+prior suite of that slice plus previous merged slices Failed 0 / Skipped 0. No skipped placeholders.

8. **Requirements first (S0).** Dedicated FR/TR/TEST in the MCP store. Do not hang leftover AC only on generic FR-MCP-TRIAGE-002. Map and `ValidateTraceability` before product code. H0 AGREE before any worktree implementation.

9. **pwsh.exe and dotnet test only.** No Python.

10. **Deploy.** After plugin-core worktree merge: `./build.ps1 SyncAgentPlugins`. After server store/schema worktree merge if live AC needs LEGION2: elevated `./build.ps1 UpdateService`. Never hand-copy binaries.

11. **Orchestrator** does not implement slice product code. It creates requirements, worktrees, subagent briefs, runs hostile, merges, updates TODOs, deploys.

## Worktree and subagent protocol

For each implementation slice after H0:

1. Orchestrator: `git worktree add F:\GitHub\McpServer\.worktrees\<slice> -b triage/<slice> develop` (or from latest merged develop).
2. Spawn general-purpose subagent with cwd that worktree. Brief includes: group IDs, AC, named tests, files they may touch, files they must not touch, Byrd red-then-green, no `done: true`.
3. Subagent writes tests, shows red, implements, runs named suite Failed 0 Skipped 0.
4. Orchestrator hostile-validates H-red then H-green on that worktree (not on develop until merge).
5. If AGREE, merge to `develop` from the orchestrator workspace, delete the worktree branch after merge, `git worktree remove`.
6. If DISAGREE, subagent stays on the worktree and implements only the FAIL list. Repeat hostile. Do not merge.

Parallel after S0+H0 and after S1 closeout starts (closeout is read-mostly on develop):

- S2 Plugin-core leftovers (serial inside the one worktree)
- S3 Session-log leftovers (can parallel S2)
- S4 Transcript (can parallel S2/S3)
- S5 Ops TEMP (can parallel)
- S6 121 forward-only tests (can share S2 if it only extends isolation tests; otherwise tiny worktree after S2 merge)

S1 closeout hostiles run first or in parallel with S0 only if they do not edit product code.

## Slices (Byrd order)

**S0 Requirements (no product code, on develop)**

Create `PLAN-TRIAGELEFTOVER-001`. Create and map (names may be adjusted at S0 if store IDs collide; keep the AC):

- FR-MCP-SESSIONATTR-001 / TR-MCP-SESSIONATTR-001 / TEST-MCP-SESSIONATTR-001: foreign filesModified/commits (108)
- FR-MCP-FAILSAFE-001 / TR-MCP-FAILSAFE-001 / TEST-MCP-FAILSAFE-001: 503 drain abort and retry (159)
- FR-MCP-STRICTCOUNT-001 / TR-MCP-STRICTCOUNT-001 / TEST-MCP-STRICTCOUNT-001: updateTurn Count (158)
- FR-MCP-XAGENT-001 / TR-MCP-XAGENT-001 / TEST-MCP-XAGENT-001: refuse cross-sourceType CompleteTurn rebind (142, 106)
- FR-MCP-SESSIONEND-001 / TR-MCP-SESSIONEND-001 / TEST-MCP-SESSIONEND-001: SessionEnd `{}` (140)
- FR-MCP-VERIFYWRAP-001 / TR-MCP-VERIFYWRAP-001 / TEST-MCP-VERIFYWRAP-001: disk-full and hang timeout (125, 130)
- FR-MCP-TRANSCRIPT-SEARCH-001 / TR-MCP-TRANSCRIPT-SEARCH-001 / TEST-MCP-TRANSCRIPT-SEARCH-001: tool_search and inter_agent (122)
- FR-MCP-TEMPVOL-001 / TR-MCP-TEMPVOL-001 / TEST-MCP-TEMPVOL-001: same-volume TEMP for plugin wrappers (117)

Reuse existing TRIAGESTORE / TRIAGEERR / TRIAGEPLUGIN / TRIAGESCHEMA TESTs for G1/G2/G11 closeout; do not duplicate IDs.

`ValidateTraceability`. Add `.worktrees/` to `.gitignore`. Hostile H0.

**S1 Closeout G1+G2+G11** (develop, no product code unless DISAGREE)

Named checks: live sessionlog_query; SessionLogSchemaGuard tests; superseded persist tests (`UpsertTurnAsync_NewTurnWithoutPlanFile` vs canceled None); Build.Tests packed tarball name vs package.json.

Hostile H-closeout. AGREE: mark those TODOs done. DISAGREE: spawn `.worktrees/triage-closeout` for FAIL list only.

**S2 Plugin-core leftovers** (`.worktrees/triage-plugin-core`, branch `triage/plugin-core`)

Order inside the worktree (file-shared): 158 Count, 159 drain, 140 SessionEnd, 142/106 rebind, then 125/130 verify wrapper, then 120 only if S1 did not close it.

Named tests (write red first):

- Pester: updateTurn omitted/empty/scalar tags; `Test-ReplFailsafeBackendUnreachable` 503/`backend_unavailable`; drain does not increment attempts; drain retries after success; SessionEnd missing cache emits `{}` exit 0; CompleteTurn cross-sourceType refuse; CompleteTurn same-agent rebind; code-verify IOException disk full typed; code-verify timeout.
- C# only if 144 HTTP retryable needs ServiceDefaults (otherwise S3).

Touch: `plugins/core/lib-ps/repl-invoke.ps1`, `plugin-hook.ps1`, `McpPluginShim.psm1`, `code-verify.ps1`, `cache-manager.ps1`, `resolve-cache-dir.ps1`, matching Pester, Codex plugin only via SyncAgentPlugins after merge.

**S3 Session-log leftovers** (`.worktrees/triage-session-store`)

108 path-outside-workspace validation or foreign marker. 144 retryable on replace_section / HTTP 503 if still missing on live tool errors. 113 document merge semantics; classified large-payload persist if still generic EF.

Named tests: filesModified outside root rejected or tagged; replace_section unreachable storage retryable true and turn still gettable; optional persist payload size classified error.

Touch: `SessionLogService`, `SessionLogController`, `McpToolErrors` / `HttpErrorResponse` if 144 still lacks retryable on the observed path, `docs/context/session-log-schema.md`.

**S4 Transcript** (`.worktrees/triage-transcript`)

122 only. `CodexTranscriptAdapter` + coverage tests with inline JSONL fixtures.

**S5 TEMP volume** (`.worktrees/triage-tempvol`)

117: session-start/wrapper sets TEMP/TMP to a workspace-drive directory when they differ. Templates already document it; keep them. Pester: env alignment function.

**S6 121 forward-only** (after S2 merge, tiny worktree or on develop)

Isolation regression for completed-then-canceled. Query/docs for stale in_progress. No mass cancel.

**S7 Exit**

Hostile H-done: all 27 listed TODOs `done: true` with AGREE receipts, or operator-approved defer (none deferred here). `ValidateTraceability`. Slice suites Failed 0 Skipped 0. SyncAgentPlugins. UpdateService only if a live schema/store AC still needs LEGION2.

## Named tests (minimum)

- Pester `TriagePluginIdentity` plus new Its for Count, failsafe 503, SessionEnd `{}`, cross-agent refuse, isolation already present.
- `SessionLogTriageStoreTests` / controller error tests for 144 and 108.
- `CodexTranscriptAdapterCoverageTests` for 122.
- `Build.Tests` packed tarball for 107 closeout.
- `code-verify` Pester for 125/130.

## Merge and TODO closeout

- Merge command: from repo root, `git merge --no-ff triage/<slice> -m "merge triage/<slice> after hostile AGREE <receipt>"`.
- Then plugin `workflow.todo.update` `done: true` with `doneSummary` citing `docs/receipts/hostile-validator-<utc>.md`.
- Do not force-push. Do not merge DISAGREE.

## Out of scope

- Reopening PLAN-TRIAGECLUSTER-001 high items already `done: true`.
- QuadBrain / QBCODE / FILETOOLS / HANDOFF plans.
- PSGallery vendor patch.
- `/health` liveness change.
- Global requestId uniqueness.
- Auto-closing 102 historical in_progress turns.
- Azure wiki / merge to main (not this program unless the operator asks).

## Risks

- Closeout AGREE on G1 still leaves 134 concurrent lock if hostile expands AC. Contain to file lock, not a new store.
- Plugin-core worktree is large; keep increments red-green per ID inside S2.
- TruckMate live SQL for 116 may be unreachable; then unit fail-closed plus host workspace query is the bar, same as cluster D5 N/A rule when live deploy is not claimed.
- Disk pressure on F: when creating worktrees. Clean bin/obj in worktrees; do not copy unused RID runtimes.

## Approval

Stop here. Implement S0 only after explicit plan approval. Do not create PLAN-TRIAGELEFTOVER-001 or FR/TR/TEST until approved.
