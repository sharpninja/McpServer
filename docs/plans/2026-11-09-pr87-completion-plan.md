# PR87 completion plan after the Claude-to-Codex handoff

Author: Codex. Drafted 2026-11-09, based on the audit observations ending 09:25:44 local.
Status: approved by the operator on 2026-11-09. The separate merge-go boundary remains in force.
Primary TODO: `IMPL-UNITGREEN-001`. PR: https://github.com/sharpninja/McpServer/pull/87.
Planning session: `Codex-20261109T150912Z-plugin-session`, turn `req-20261109T152301Z-turn-8796`.

Execution update, 2026-11-09T11:21:52-06:00: P0 and P1 are complete. The live primary TODO has 75 tasks,
45 accepted and 30 open; no new task was marked accepted. P1 removed the four vacuous embedding passes, retained the
12-row mock gate and diagnostic failures, passed the real binding on Linux and Windows with zero skips, and passed
Compile with zero warnings/errors. Evidence: `../receipts/unit-suite-green-20261019/2026-11-09-pr87-p1-embedding.md`.
P2 implementation and launch preparation are substantially complete: plan sections 27.46 and 27.47, corrected command
examples, five reconciled record drafts, and the direct Claude Code launcher with 12 passing success/refusal scenarios
are present. The launcher uses Claude Code 2.1.318, exact model `claude-opus-5-5` and high effort. P2 remains open only
because the self-review intentionally retains P3-dependent PENDING and FILL tokens, which correctly refuse a live launch.
P3 final-head gates, records finalization, push and ready transition have not run. No Claude HV verdict exists for these
changes, and no merge is authorized.

Execution update, 2026-11-09T13:15:06-06:00: the operator narrowed PR87 publication validation to Linux only. This
supersedes the two-host P3 and prepublication P4 instructions below without rewriting their approved historical text.
No further Windows checkout or test work is required for publication. The final code/test head is
`a1e19d7331a2e9804c66ef96a27ee50b2365510c`; its bounded fixture-cleanup correction is test-only and was pushed to the
PR branch. The fresh Linux phase-exit receipts are `ug-r15p3-linuxonly-final-unit-1` (4,847/0/0),
`ug-r15p3-linuxonly-final-lane-1` (746/0/0), `ug-r15p3-linuxonly-final-buildtests-2` (530/0/0), and
`ug-r15p3-linuxonly-final-traceability-1` (passing exit, no TRX by contract). All test receipts pin the six in-scope
test paths and the exact code head. Records reconciliation, the records commit, ready transition and full Claude HV
remain pending. No merge is authorized.

## 1. Outcome, baseline and authority

Finish PR87 through a verified full-suite gate on both hosts, independent Claude HV, operator-authorized merge, deployment through Nuke, and a live stored-timestamp check. Mark the primary TODO done only when every actual task has its own acceptance evidence and the post-merge obligations are fulfilled.

Baseline: code head `03eea74797827aacf7030ac577cc19d46ebee5d2`; docs head `c8b231c13164956ac98d6234fb2c6fb459c0cefb`; base develop `9b099716e3f4b773787a1caef44353e112bda556`. PR is open, draft and mergeable. The live TODO has 72 tasks, 45 completed and 27 open. The audit report is `2026-11-09-pr87-handoff-audit.md`; its mechanical receipt is `../receipts/unit-suite-green-20261019/2026-11-09-codex-handoff-audit.txt`.

The original plan remains the implementation history: `2026-10-19-unit-suite-green.md`, especially 27.19-27.45 and its D-B acceptance amendments. This document supplies the new execution order and reviewer selection. Preserve earlier entries and receipts; add dated corrections instead of silently rewriting historical claims. Read the original plan and all review evidence end to end before HV; this planning pass is not claimed as that holistic read.

Operator decisions incorporated:

- Implementer: Codex. Reviewer: **Claude Code, Opus 5.5, high effort**, replacing Codex CLI gpt-6-astra max. Runtime receipts must prove the actual reviewer family, model and effort before its verdict is used.
- The **68 classification-only tests and 1,915 uncited methods remain outside PR87**. Separate investigation: `PLAN-TESTCITATIONS-001`. No new broad quarantine sweep belongs in this PR.
- **Full HV only**: GitHub review comments are outside the review/fix queue. On the operator's explicit follow-up, the two automated top-level comments were minimized as outdated, independently read back on GitHub. There were zero inline review threads. This is workflow supersession, not a claim that those comments' findings were fixed.
- Existing scanner-work override, held-out integration projects and hostile-environment-run exclusions remain in effect. GitHub Actions and Azure are not gates or substitute evidence.
- New requirements or acceptance changes are captured through the Codex plugin before tests and implementation. No live requirement is satisfied merely because this plan describes it.

## 2. Scope and blast radius

In scope: repair the four known vacuous embedding integration cases; preserve and explain test identity through separation and renames; complete the round15 records; correct stale test-command examples; prepare a Claude-specific HV launcher; run final gates; process per-task verdicts; merge and deploy when authorized.

Expected source changes are confined to test fixtures, shared assertion/test support, their project links, and XML documentation. Product behavior and public APIs remain unchanged. The embedding fixture must not depend on the user's model cache. The launcher is effort-local PowerShell, with no general HV framework or new application feature. Requirement changes formalize the behavior the existing four embedding tests already assert and their genuine integration execution; they do not introduce a new embedding algorithm.

Documentation: append tracked plan section 27.46 from the audited scratch draft, then a dated continuation section for this plan and later outcomes; update both handoffs' current pointers, round15 brief/self-review/projected-problems/PR-description/body records, and CLAUDE.md/copilot test examples. Keep requirement projections generated by QBrain; never edit them directly.

MCP: preserve all 45 accepted task flags, correct tasks 66/67, retain all existing positions, append narrowly scoped open tasks for quarantine, embedding verification and review-launch/record recovery where not already covered. Do not renumber or duplicate existing S0-S10 tasks. Create/read back the separate citation investigation; never make it a dependency of PR87 completion.

Deployment changes live binaries through existing `UpdateService` and `InstallReplTool` targets only. No database migration, bulk requirement rewrite, force push, reset, clean, stash or unrelated dependency upgrade is planned. Retain original files when copying evidence; remove only task-created disposable fixture data or an original whose removal the operator already authorized.

## 3. Phase P0: reconcile current state and acceptance records

1. Recheck branch, clean/dirty path classification, live origin/base refs, PR draft state, current effective `layer-1` requirements and the primary TODO. Record head and parent handoff IDs. Preserve the audit/planning docs as intentional changes. Verify both Windows checkout identities over SSH before any host work; do not infer them from the handoff.
2. Authenticate previous per-task acceptances from the round14 request/response JSONL and complete verdict, particularly the 45 tasks already done. Keep their accepted state unless a reviewer finds a regression. Inspect the 27 open task texts and build a fixed-number task-to-requirement map. Correct the reviewer wording in task 12 to the new selection; task 66 names NukeBuild.IntegrationTests and task 67 names QBrainAi.Support.Mcp.EnvironmentIntegrationTests. Add an explicit open quarantine task for TR-MCP-NUKE-019 AC-8 and its `03eea747` evidence. Keep the now-73-plus task denominator explicit after each append.
3. Resolve the renamed reconciliation row with source/history, not count equality alone: compare the old omission control for Build.Tests with the intermediate and current omission controls for the build's integration-owned project. Require the new DropsNukeBuildIntegrationTests row to be present and Passed in both infrastructure TRX. Retain the original report with its exact FAIL verdict and hash; write a supplement mapping the old display name to its successor and explaining the ownership change. No unexplained lost case may remain. Copy the scratch report and supplement into committed receipts with original hashes verified and originals retained.
4. Establish embedding traceability before test changes. The effective FR-MCP-004 currently has no acceptance criteria. Add descriptive criterion `embedding-vector-contract` to FR-MCP-004, formalizing its existing four source-pinned outcomes: an available model produces a nonzero vector for hello world, equal vectors for identical text, distinct vectors for the two existing different texts, and a nonzero 384-value vector for the existing 500-word input. Create `TEST-MCP-EMBEDDING-001` for those same assertions, positive fixtures and rejected wrong responses. Create `TR-MCP-EMBEDDING-001` for the real integration binding and fixture: a test-owned model/vocabulary, required availability, no early-success return on missing/corrupt/unloadable artifacts, no home/cache mutation and cleanup confined to its owned root. Its `model-backed-assertions` criterion requires all four original cases to execute their assertions. Map both new records under FR-MCP-004. Check ID absence again before creation; never overwrite a concurrently created record. These are proposed pending records, not records claimed created by this draft.
5. Preserve all existing criteria and satisfied flags when appending. Use object-based, dry-run-capable Codex plugin wrappers for mutations, then read back complete objects. Document criteria, test IDs, rationale and alternatives as both decision dialog and actions. Do not use another agent's writer scripts.

Exit: current refs and task map captured; original reconciliation preserved with a justified mapping; requirements/TEST/mappings recorded and read back before any test-support edit. No implementation-complete flag changes in this phase.

## 4. Phase P1: remove vacuous embedding passes, BDPv4 test-only slice

Contract and mock gate first:

- Add a pure shared assertion contract in test support and `EmbeddingModelAssertionContractTests` in QBrainAi.Support.Mcp.Tests. It uses supplied vectors and an availability value, never disk, environment, network, native inference or the clock.
- Twelve named theory rows: four correct responses for the four original behaviors; four independently wrong responses, one per behavior (all-zero, unstable same-input output, equal different-input output, wrong-sized/all-zero long-input output); four unavailable-model responses, one per behavior. The positive rows pass every relevant assertion. Each negative row passes only by proving that the intended assertion rejects it. A checker that returns before asserting must fail a negative-control test.
- Pin fixture/expected-data hashes and demonstrate a green mock gate with all 12 rows and zero failures/skips before changing the real integration fixture or bindings. Do not substitute an intentionally red production suite for this gate. Supply the same shared assertions to the real bindings afterward.

Real binding:

- Keep the original four integration method names, text inputs and assertions. Replace their early returns with required availability checks and calls to the shared assertion contract.
- Add an asynchronous class fixture owning a unique temporary model directory. Acquire the existing product model/vocabulary from the same upstream sources used by EmbeddingService, once per fixture, stream to owned files, and record source URLs, artifact hashes and lengths. The fixture passes ModelPath explicitly and disables implicit auto-download. A failed download, corrupt/unloadable model or unavailable service fails the fixture and therefore its cases. Never use the user's default model folder or catch the failure and pass. Dispose the service and delete only the unique directory this fixture created.
- Keep the stub-mode unit tests hermetic. Do not call the production downloader as a fixture workaround: it writes to LocalApplicationData rather than the explicit test ModelPath. No product downloader behavior change belongs in this slice.
- Shared assertion code has one definition, linked where needed. Add XML docs with dated canonical requirement/criterion IDs, preserving prior entries. Correct the integration class's misleading unit-test summary as part of this slice.

Exact targeted runs, each with a unique measured local-time run ID and WhatIf first:

```powershell
./build.ps1 TestGroups --test-groups "QBrainAi.Support.Mcp.Tests:EmbeddingModelAssertionContractTests" --test-run-id <mock-run>
./build.ps1 TestGroups --test-groups "QBrainAi.Support.Mcp.Tests:EmbeddingModelAssertionContractTests,EmbeddingServiceTests" "QBrainAi.Support.Mcp.EnvironmentIntegrationTests:EmbeddingServiceTests" --test-run-id <binding-run>
./build.ps1 Compile
```

Run bindings on both hosts, plus an isolated missing/corrupt-fixture negative-control harness that proves setup failure without making a required suite intentionally red. Retain chronological receipts proving criteria, mock gate, real fixture edit, binding and Compile order. Expected unit count change is +12 if the prescribed 12 theory rows are the only added cases; verify actual discovery and explain every additional case. The real lane retains its four original embedding case identities. No full gate set after individual edits.

Exit: the mock gate discriminates all specified wrong responses; all four real model assertions pass on each host; missing model cannot yield a passing case; compile has zero errors and unapproved warnings. Commit the slice with the actual runtime identity in the signature.

## 5. Phase P2: records and Claude review-launch preparation

1. Treat scratch edits as untrusted drafts. Read facts-r15 and plan-r15; compare each Old anchor to a fresh current copy. Apply unique anchors with WhatIf first, review the diff, preserve chronology and add dated corrections. Fill every FILL-GATES-R15 and FILL-RECORDS-R15 token from verified receipts; retain the `03eea747` gates as historical baseline after P1 changes the code head.
2. Make section 27.46 state the audited as-built layout, 253/113 rewording receipt, quarantine, original gate counts and reconciliation mapping. Add a continuation section stating the new code head, P1 changes, current task denominator, this reviewer selection, exclusions and final gate evidence. The acceptance set is the canonical D-B list of 27.21 as amended through the later sections, plus AC-7/AC-8 and the embedding records. Do not reconstruct an abbreviated list from memory. Read and reconcile every clause and current stored criterion before publication.
3. Replace stale command examples with existing tests: EfTodoServiceTests and QueryAsync_FiltersByPriorityAndKeyword, using Nuke TestGroups for class selection. Mechanically confirm that every named source, run ID, receipt and command target exists. Fix records' obsolete round14 identities and secret-scan claims. Preserve all declared order slips; no retrospective claim can make a missing original mock-before-code receipt exist.
4. Prepare an effort-local `Invoke-ClaudeHvRound.ps1` and runner, adapted from the existing launcher contract without Codex command/identity assumptions. Analyze parser and PSScriptAnalyzer Warning/Error findings; justify any suppression; exercise WhatIf and scripted launcher checks before a live launch. Test refusal for unresolved FILL/PENDING/brace tokens, dirty or unpushed tree, missing/wrong manifest hash, missing task map, missing TRX run/host, missing standing-rule evidence and reviewer model/effort mismatch. Test successful dry-run command generation without starting a reviewer. No BDP product framework is added.
5. Claude launch contract: noninteractive Claude Code, explicit Opus 5.5 model identifier confirmed by that CLI, explicit high effort, print mode and complete stream-json output, using a PowerShell ProcessStartInfo argument list and stdin/file prompt. Read local CLI help and runtime receipts instead of guessing flags. No timeout, turn cap, evidence-size cap or cost cap. The sandboxed CLI probe in this planning turn hit a mise installation/read-only error, so reviewer installation/authentication remains a launch precondition; no successful Claude invocation is claimed. Repair the normal authorized tool route if needed, and do not silently substitute another model or family.
6. Request JSONL and response JSONL live under docs/receipts/hv with measured local-time names. Record actual command, model, effort, code head, reviewed head, operator directives, evidence paths/hashes, full prompt and complete event stream. The reviewer runs add-profile, proves its actual runtime model/effort, uses its own Claude plugin/session/turn, and emits meaningful progress entries with percentComplete and ETA. It never builds or runs tests. No product edits while HV runs; check progress read-only and stop only after more than 30 minutes without a stream update. Preserve interrupted runs as interrupted, never as a verdict.

Exit: records agree on one current state, all launch checks work, no unresolved placeholders, and the Claude launch route is verified. Preparation is not an HV pass.

## 6. Phase P3: final code-head gates and pre-HV package

Freeze the final code/test/build head after all P1 fixes. Run one gate at a time per host. Both hosts may run independently if no shared service/resource conflicts exist. Use the correct host's native checkout; Windows lane requires kingd's interactive desktop. Verify each checkout and ref before launch. Every Nuke test run has a unique --test-run-id, and every state-changing command receives its dry run first.

```powershell
./build.ps1 Test --test-run-id <host-unit-run>
./build.ps1 MigrationIntegrationTests --test-run-id <host-lane-run>
./build.ps1 ValidateTraceability --test-run-id <host-traceability-run>
```

Build.Tests remains the separately labeled infrastructure check, using the explicitly recorded operator exception in live TR-MCP-NUKE-019 notes (2026-10-11 and 2026-10-19): raw dotnet test for the whole infrastructure project, Platform!=Windows on Linux and Platform!=Linux on Windows, TRX logger and explicit results directory. Do not add a new target merely to change that historically authorized runner. Keep Linux temporary storage on suitable disk as the existing runner requires.

Acceptance: every selected project executes applicable cases, zero failed, zero skipped/not executed, no quarantined case executed by Test/lane, exact per-project filters logged, all intended host-specific cases execute on their host, and zero NU190x. Report the excluded 29 cases separately. Reconcile counts and names against `03eea747`, accounting for the 12 mock-control rows and every intentional change; reject unexplained omissions/duplicates. ValidateTraceability's passing exit does not prove live-store coverage when its output says no store source; supplement with plugin reads and an explicit FR/TR/TEST/mapping check.

If a gate fails, fix only its supported cause under BDPv4, run affected TestGroups, then run the full gate set once at the new final code head. No partial-filter green or old-head TRX can authorize exit. Refresh hashes, host/run/head/command provenance and selection JSON in a new committed TRX manifest; preserve the `03eea747` manifest unchanged.

Then perform and record the full holistic read, per-requirement what-if pass (crash/restart/replay, ordering, concurrency, malformed/boundary input, unsupported hosts, lifecycle, isolation, upgrade/rollback and operator output), propagation audit, mapping lint, scope checks and source-pinned doc-claim checks. Scan the exact paths intended for the records commit for secrets without printing secrets. Fix findings; a code change returns to the code-head gate above, while docs-only changes retain gate validity only with a verified code-identical range.

Write one signed records commit with exactly named paths, push normally, verify remote HEAD and fast-forward the Windows child branch only after inspecting every intervening commit. Update the PR body from its committed current description, and mark ready only when no code remains pending. Show every projected issue to the operator. The original effort's projected-problem confirmation already exists; do not invent a new between-round approval gate. An actual scope/requirement choice the operator owns still requires its answer.

Exit: clean pushed reviewed tree, final-head green evidence, committed complete records and no undeclared finding. No overall or per-task done claim yet.

## 7. Phase P4: full Claude HV and findings loop

Launch round15 (or the next unused round number verified from actual receipt history) on the merged OMARCHY worktree only, with Claude Code Opus 5.5 high. Supply all final unit and in-scope integration TRX, both infrastructure TRX, traceability outputs, current task snapshot, prior full verdicts, source-pinned reconciliation and the full original/current plans. The request explicitly excludes GitHub review comments, Azure/Actions gates, hostile-environment runs, held-out integration projects, scanner work under the existing override and the separately tracked uncited populations.

Review surfaces: requested claims, workspace rules, applicable FR/TR/TEST/AC and current plan. The reviewer returns the requirement-per-task/change matrix, in-flux requirements, every open task adjudicated in full with its own verdict/accuracy/completeness, regression checks only for completed tasks sharing in-flux requirements, and out-of-scope tasks listed without adjudication. Every FAIL/UNKNOWN is disclosed. Overall AGREE and each task AGREE require both scores at least 98. Infrastructure incidents are recorded separately under MEMORY-HV-001 rule 6; they do not fabricate a product failure or acceptance.

Persist the entire verdict in the reviewer's MCP turn, split into ordered items if needed, and preserve full paired JSONL. Read it back through the supported plugin when available. Do not substitute a digest. On return, validate findings against source and sweep the underlying failure class. Check off only tasks with their own qualifying AGREE and no unresolved regression; reopen a regressed accepted task. Out-of-scope open tasks stay open. If any necessary completion task is unadjudicated, run a subsequent review with its requirements explicitly in scope; an overall AGREE alone cannot close it.

On DISAGREE, retain the verdict as received, return PR to draft while code is pending, strengthen the deficient criteria first, execute the smallest BDPv4 slice, rerun required final-head gates and records, then repeat full HV without asking the operator to reconfirm routine between-round work. Stop for real operator-owned scope changes. After processing each verdict and updating tasks, refresh the existing PR87 FR dashboard using its established artifact destination; do not publish a new public destination as a substitute.

## 8. Phase P5: merge, deployment and actual completion

When overall AGREE and all pre-merge tasks have qualifying per-task AGREE, read the canonical D-B set from the plan and current effective store, and set satisfied only the exact criteria their complete evidence proves. Preserve other flags, especially the exclusions in 27.21/27.22 and later amendments. FR-MCP-040 ac-1 remains unsatisfied until deployment/live verification.

Present the concrete reviewed head, current PR body, complete verdict scores, residual exclusions and gate/TRX receipts, then obtain the operator's **merge go**, as the accepted handoff requires. This is the final pre-merge approval, not a request for permission to prepare the work. Recheck the base/head; if either code changes, classify the delta and refresh applicable gates/HV before merging. Use a merge commit, never force.

After merge, deploy from the merged source using `./build.ps1 UpdateService` and `./build.ps1 InstallReplTool --update-tool`, with the normal elevation and platform configuration those targets require. Analyze any wrapper, dry-run first, verify backup/preserve/rollback behavior and record deployed version/hash. Do not hand-copy binaries or mutate unrelated configuration. On failure use the target's retained rollback path; do not mark deployment or timestamp verification complete.

Refresh marker trust and plugin connection after restart. Through the supported deployed server/client/REPL route, compare stable records' returned createdAt/updatedAt with authoritative stored-time evidence and repeat reads after a measured interval. A stable repeated wrong value alone is insufficient: verify equality to the stored timestamp, including null/no-date semantics where applicable. Do not modify a canonical record merely to invent verification evidence. Record real deployment/getter responses and the authoritative comparison. Only then satisfy FR-MCP-040 ac-1 if its whole criterion is proven.

Submit post-merge obligations and retained live evidence to Claude HV when their tasks require acceptance, without reviewer builds/tests. Mark remaining tasks done only on their own AGREE at 98/98. Finally reread every task and satisfied flag; only when every required task is checked, no regression or required gate failure remains, deployment/live reads are proven and all receipts are linked may IMPL-UNITGREEN-001 become done. Leave PLAN-TESTCITATIONS-001 and unrelated deferred work open.

## 9. Approval boundary

This draft is ready for operator review, not execution. Approval authorizes phases P0-P4 and their stated in-scope test/record fixes and review loop. The existing separate merge-go requirement remains at P5. The broader uncited investigation remains separately tracked, with no automatic implementation authorization.

Author assessment: accuracy 98% for the pinned baseline and explicit decisions; completeness 96% pending operator plan review and full pre-HV source/record read. These are author assessments, not hostile verdicts. No goal, requirement or implementation task is claimed accepted by these scores.
