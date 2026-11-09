# PR87 hostile validation Round 16

You are the independent hostile validator. Run this review in Claude Code using `{MODEL}` (Opus 5.5) at `{EFFORT}` effort. This is full HV, not a continuation of GitHub comments. Work from `{HEAD}`, while the immutable code/test head under review is `167afdcb1bbe4ce8e54770fb5882cc8a48d82b01`.

## Mandatory bootstrap and operating rules

Read `/home/sharpninja/github/McpServer/AGENTS-README-FIRST.yaml IN FULL`, then run `add-profile`. Create your own Claude plugin session and a dedicated session-log turn. Report progress using `percentComplete` and `etaSeconds`. Use PowerShell and no Python for permitted local inspection.

The HV reviewer is not to build code or run tests independently.

If the HV reviewer feels the tests are inadequate, then it should fail the review and propose additional AC and tests.

Do not bound review time, turns, evidence size or cost. HV invocations should not be bounded by time or size.

We are not chasing perfection, but a functioning system.

98/98 is not perfection, it is functional.

Accuracy must be `accuracy >= 98`, completeness must be at least 98, and every applicable task must receive AGREE at 98/98. Do not infer merge authorization: merge remains a separate operator decision.

Publication scope is Linux tests only. Historical Windows evidence may be used for regression context, but no Windows rerun is required. The 68 classification-only tests and 1,915 uncited methods remain outside PR87 in PLAN-TESTCITATIONS-001. GitHub review comments are superseded by this full Claude Code Opus 5.5 high effort review.

## Fixed review inputs

- Gate: `{GATE}`
- Launch stamp: `{STAMP}`
- Records head: `{HEAD}`
- Request JSONL: `{REQUEST_JSONL}`
- Response JSONL: `{RESPONSE_JSONL}`
- Self-review copy: `{SELFREVIEW_PATH}`
- Current task map: `{TASK_MAP}`
- Immutable Linux TRX manifest: `{TRX_MANIFEST}`
- Round 15 verdict: `docs/receipts/hv/hostile-validator-unitgreen-r15-20261109T142334.md`
- Round 15 request/response: `docs/receipts/hv/20261109T142334-unitgreen-r15.request.jsonl` and `docs/receipts/hv/20261109T142334-unitgreen-r15.response.jsonl`
- Round 16 targeted receipt: `docs/receipts/unit-suite-green-20261019/ug-r16-targeted-remediation.json`
- Round 16 final receipts: `docs/receipts/unit-suite-green-20261019/ug-r16-final-linux-unit-1.json`, `ug-r16-final-linux-lane-1.json`, `ug-r16-final-linux-buildtests-1.json` and `ug-r16-final-linux-traceability-1.json`
- Current plan: `docs/plans/2026-10-19-unit-suite-green.md`, especially 27.47 through 27.49
- Approved completion plan: `docs/plans/2026-11-09-pr87-completion-plan.md`

Read the current task map and its two referenced fixed-position sources. Score tasks 1 through 75 without renumbering. Preserve prior 98/98 acceptances only when regression checks remain clean. Keep truly out-of-scope tasks open; do not manufacture acceptance.

## Round 15 findings and claimed remediation

R15-F1: ownership formerly depended on recorded counts. TR-MCP-NUKE-019 AC-7 now requires runtime discovery. `TestProjectCaseDiscovery` uses real `dotnet test --list-tests` results, and Test plus MigrationIntegrationTests bind the discovered inventories before execution. The exact controls are `OwnershipInventory_ComesFromDiscoveredCases_NotRecordedCounts`, `UnitOwnedProject_WithDiscoveredIntegrationCase_FailsTestTarget`, `IntegrationOwnedProject_WithDiscoveredUnitCase_FailsLane` and `RecordedCountDrift_IsRejected`. The intentional mutation ignored Integration discovery and produced the expected 3 failed, 2 passed, 0 skipped; after restoration all five passed. Inspect whether the production target wiring actually makes new role/category drift fail.

R15-F2: failed and noncommitted evidence is now declared. The first final-head Build.Tests attempt at `a1e19d73` ran 530 tests and failed 93 before assertions because `.mcpServer` in TMPDIR made the fixture fall back to `/QBrainAiGateTemp`; two earlier filtered attempts also exited 1. No code change followed those infrastructure attempts. A corrected run passed 530/0/0 at that historical head. The Round 16 final run passes 535/0/0. The targeted mutation and cleanup runs retain raw TRX only in ignored TestResults storage; their committed receipt states this rather than claiming the raw files are committed. The 14 publication-gate TRX files are committed and hash-verified in `{TRX_MANIFEST}`. A duplicate unit launch later failed only on the Nuke build-log lock while the original run was still active; the original completed 4,847/0/0. Treat the deliberate mutation's 3 failures as negative-control evidence, not a product gate failure.

R15-F3: this launch substitutes the real records head, real stamp, and actual request/response/self-review paths. The task denominator is 75, not 72. The current task map and manifest are committed and hash-gated by the launcher. Check for any remaining nonexistent path, stale head or boilerplate value.

R15-F4: TR-MCP-EMBEDDING-001 `test-owned-fixture` now requires bounded retries only for transient IOException and propagation of the final IOException or any non-IOException. The two exact controls are `DeleteOwnedRootAsync_PersistentIOException_ThrowsAfterBoundedAttempts` and `DeleteOwnedRootAsync_NonIOException_IsNotRetried`; the five-case fixture group and final 461-case EnvironmentIntegration project pass. Evaluate the intentional discovery mutation as the valid red evidence for R15-F1 and the cleanup contract's negative controls as the discriminating evidence for R15-F4.

R15-F5: live TODO tasks 72 and 74 were amended to Linux-only final publication evidence while retaining the green `03eea747` two-host baseline and Windows composition evidence as history. The 75 positions and done flags were not changed.

## Final-head evidence claims

At code head `167afdcb1bbe4ce8e54770fb5882cc8a48d82b01` on LAB-OMARCHY:

- Test: 4,847 passed, 0 failed, 0 skipped across seven TRX.
- MigrationIntegrationTests: 748 passed, 0 failed, 0 skipped across six TRX.
- Build.Tests raw infrastructure runner with `Platform!=Windows`: 535 passed, 0 failed, 0 skipped in one TRX.
- ValidateTraceability passed and produced no TRX; the receipt records its zero-test contract and output.
- `{TRX_MANIFEST}` contains 14 copied TRX, 10 source pins, 9,450,351 bytes, and SHA-256 `72B8BF052F25B9418BCA953E7F568891A83DDFA13703D2F4644C34E90D8D40FB`. Recompute hashes and inspect source pins, but do not rerun tests.

## Required review method

1. Inspect the complete diff from the PR base through `{HEAD}`, with special attention to the runtime discovery and embedding-fixture cleanup changes.
2. Reconcile every current requirement and every task-position claim against live/store-backed records and committed evidence.
3. Test the argument mentally with hostile counterexamples: new Integration traits in unit projects, missing traits in integration projects, count drift, duplicate discovery identities, persistent IOException, and non-IOException cleanup failures.
4. Check that no final claim depends only on ignored scratch evidence and that every historical failure is disclosed accurately.
5. Check propagation through build targets, requirements, task text, plan, PR body, receipts and launcher metadata.
6. Write progress and the full verdict to your dedicated session-log turn. Preserve the complete stream in the paired JSONL paths above.

## Required output

Return exactly one overall `AGREE` or `DISAGREE`, accuracy, completeness and confidence. Provide a position-preserving result for all 75 tasks, identifying prior accepted tasks checked for regression, applicable open tasks, overridden items and out-of-scope items. For any disagreement, cite file and line, the violated criterion, a concrete counterexample, proposed AC language and exact discriminating tests. AGREE is valid only when overall and every applicable task are at least 98/98.
