# Plan: Finish the approved session-log lifecycle plan and obtain hostile-review AGREE

## Goal kind
code-change

## Acceptance criteria
1. The approved session-log lifecycle plan (S0 through S8) is fully carried out: store requirements exist for that plan, and the shipped session-log behavior matches that plan's durability-first metadata rules, three persist outcomes, degraded-open honesty, replay and contention rules, stop-hook stale-pin rule, and the schema/uniqueness and dialog-classification outcomes named there.
2. The plan's exit gate passes on the branch that contains the work: the full plugin Pester directory reports Result Passed, FailedCount 0, SkippedCount 0, FailedBlocksCount 0, FailedContainersCount 0, TotalCount greater than 0, and PassedCount equal to TotalCount; `./build.ps1 Test` reports Failed 0 and Skipped 0 for every project it runs.
3. An independent hostile review of the completed work returns overall AGREE with accuracy and completeness both at least 98, and none of the 35 scoped BUG-TRIAGE items are marked done before that AGREE.

## Verification plan
1. gating: Run the new or updated plugin tests that call the shipped session-log functions from their real start state (degraded begin cache, hook status, metadata modes, wrapper exit outcomes, quarantine repair, stop-hook pin). Capture the Pester result to `{SCRATCH}/pester-lifecycle.txt`. Pass only if the assertions for criterion 1 fail on the pre-change behavior where the approved plan required a red test, and pass after the change.
2. gating: Run `Invoke-Pester -Path plugins/core/test-fixtures/pester -PassThru` and `./build.ps1 Test`. Capture both to `{SCRATCH}/pester-full.txt` and `{SCRATCH}/build-test.txt`. Pass only if the counts in criterion 2 are present in those captures.
3. gating: Read the hostile-review receipt written for this completion. Pass only if it states AGREE, accuracy >= 98, and completeness >= 98, and a TODO query shows the 35 scoped ids still not done before that receipt, or done only in a change that cites that receipt.
4. evidence: Confirm the MCP requirement records named by the approved plan (FR/TR/TEST SESSIONLIFE and their mappings) are retrievable, and that proof-only items were not given a manufactured failing test.

## Non-goals
- Keyserver product redesign, `BUG-TRIAGE-235`, and `PLAN-TXNKEYSERVER-001`.
- adb_step, Codex code-verify, use-case diagram passthrough, and plugin-identity cache bugs.
- Reopening `docs/plans/sessionlog-remediate-001.md` or changing the 120-second drain default that the approved plan marked proof-only.
- Marking the 35 TODOs done without the hostile-review AGREE.

## Assumed scope
Approved plan at the session `plan.md` for the 35 BUG-TRIAGE ids in the session-log lifecycle cluster. Plugin session-log scripts and their Pester fixtures, session-log service/controller/schema guard, error classification used by persist contention, transcript import-recovery ingestion, and the MCP requirements store. Work continues on `grok/session-lifecycle` where S0 records and the first cache-retention test already exist.

## Implementation approach
Keep each slice's tests calling the shipped functions. Show a required red test before the production edit for that behavior. Leave proof-only paths (drain timeout default, generic dialog-method dispatch, coordinator bypass) as green assertions with no duplicate implementation. Finish one implementation slice's full regression gate before starting the next slice's production edits.

## Task checklist
- [x] Confirm S0 requirement records and the six bypass TODO notes are present; add any mapping the approved plan still lacks.
- [x] Extend the lifecycle Pester so the remaining S1 behaviors fail on the current production functions, and capture that red run.
- [x] Implement degraded-open cache retention, hook status, and 404 recovery until those tests pass.
- [ ] Add and turn green the metadata, three-outcome exit, child-deadline, and stop-hook tests, then the replay, import-recovery, contention, schema, and dialog-classification tests.
- [ ] Run the full Pester directory gate and `./build.ps1 Test`; save both outputs under `{SCRATCH}`.
- [ ] Obtain hostile-review AGREE at 98/98 or better on the completed branch before any scoped TODO is marked done.

## Deviations
- Stop-hook stale pin uses local turn status as the completion signal because this hook path has no session-log query client.
- SQLite memory migrations use an empty Down because that provider cannot emit DropColumn; Postgres and SQL Server drop the soft-delete columns.
- MemoryService.RemoveAsync soft-deletes the row. A physical Remove cleared MemoryVersions.MemoryId and failed SQLite NOT NULL.
- Plugin integration tests resolve sibling plugin repos from the primary clone when the checkout is a git worktree.

## Risks / Contradictions
- `./build.ps1 Test` plus a full Pester directory is long; a slice is not complete until both captures exist.
- Some scoped TODOs still describe coordinator rollback that effective FR-MCP-173 already bypasses. Close those only as proof under that existing requirement, not by reimplementing the bypass.

