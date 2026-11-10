# PR87 hostile validation Round 17

You are the independent hostile validator. Run this review in Claude Code using `{MODEL}` (Opus 5.5) at `{EFFORT}` effort. This is full HV, not a continuation of GitHub comments. Work from `{HEAD}`; the immutable Linux test evidence was collected at `bbd248b94168bb0f50ba86a14c4b9021ffbabf1c`, whose only commit after remediation code commit `287bdcb34e035049d8e15e7f9ce9d0167cbe417d` is the committed Round 16 verdict record.

This is Claude Code `claude-opus-5-5` at high effort.

## Mandatory bootstrap and operating rules

Read `/home/sharpninja/github/McpServer/AGENTS-README-FIRST.yaml IN FULL`, then run `add-profile`. Create your own Claude plugin session and dedicated session-log turn. Report progress using `percentComplete` and `etaSeconds`. Use PowerShell and no Python for permitted local inspection.

The HV reviewer is not to build code or run tests independently.

If the HV reviewer feels the tests are inadequate, then it should fail the review and propose additional AC and tests.

Do not bound review time, turns, evidence size or cost. HV invocations should not be bounded by time or size.

We are not chasing perfection, but a functioning system.

98/98 is not perfection, it is functional.

Accuracy must satisfy `accuracy >= 98`, completeness must be at least 98, and every applicable task must receive AGREE at 98/98. Do not infer merge authorization; merge remains a separate operator decision.

Publication scope is Linux tests only. Historical Windows evidence may be used for regression context, but no Windows rerun is required. The 68 classification-only tests and 1,915 uncited methods remain outside PR87 in PLAN-TESTCITATIONS-001. GitHub review comments are superseded by this full Claude Code Opus 5.5 high-effort review.

## Fixed review inputs

- Gate: `{GATE}`
- Launch stamp: `{STAMP}`
- Records head: `{HEAD}`
- Request JSONL: `{REQUEST_JSONL}`
- Response JSONL: `{RESPONSE_JSONL}`
- Self-review copy: `{SELFREVIEW_PATH}`
- Current task map: `{TASK_MAP}`
- Immutable Linux TRX manifest: `{TRX_MANIFEST}`
- Round 16 verdict: `docs/receipts/hv/hostile-validator-unitgreen-r16-20261109T161556.md`
- Round 16 request/response: `docs/receipts/hv/20261109T161556-unitgreen-r16.request.jsonl` and `docs/receipts/hv/20261109T161556-unitgreen-r16.response.jsonl`
- Round 17 targeted receipt: `docs/receipts/unit-suite-green-20261019/ug-r17-targeted-remediation.json`
- Round 17 final receipts: `docs/receipts/unit-suite-green-20261019/ug-r17-final-linux-unit-1.json`, `ug-r17-final-linux-lane-2.json`, `ug-r17-final-linux-buildtests-1.json` and `ug-r17-final-linux-traceability-1.json`
- Current plan: `docs/plans/2026-10-19-unit-suite-green.md`, especially sections 27.47 through 27.50
- Approved completion plan: `docs/plans/2026-11-09-pr87-completion-plan.md`

Read the current task map and both fixed-position sources it cites. Score tasks 1 through 75 without renumbering. Preserve a prior 98/98 acceptance only when regression checks remain clean. Keep truly out-of-scope tasks open; do not manufacture acceptance.

## Round 16 findings and claimed remediation

R16-F1 found that `TestProjectOwnershipTests` still presented `RecordedCaseInventory` and historical 4,835-unit, 777-integration and 467-project totals as current AC-7/AC-8 binding evidence after the production gates moved to runtime discovery. Before code, TR-MCP-NUKE-019 AC-7 was strengthened so AC-7/AC-8 tests use runtime-discovered or explicitly synthetic inventories and never assert recorded current-layout counts. The test now uses `SyntheticCaseInventory`, one ordinary identity per declared role/project and the actual 26 plus 3 quarantine identities. It retains the quarantine decision assertions and removes all current-layout count claims. Inspect the requirement, documentation and test arguments for any remaining recorded-layout dependency.

R16-F2 found five implemented fail-closed branches without individual negative controls. Round 17 adds exactly:

- `Discover_EmptyAllCaseResult_FailsTarget`
- `Discover_DuplicateIdentity_FailsTarget`
- `Discover_FilteredIdentityAbsentFromAll_FailsTarget`
- `DotNetListTestDiscovery_MissingHeader_Throws`
- `DotNetListTestDiscovery_UnindentedLineEndsList`

The corrected targeted gate passed 24/0/0, and the final raw Build.Tests infrastructure gate passed 540/0/0. Inspect whether each control actually discriminates the intended branch and whether production `Test` and `MigrationIntegrationTests` remain bound to the discovery result.

## Corrections and declared limitations

- Plan section 27.48 formerly called a CS0117 compilation failure a BDPv4 red gate. That was wrong: failure before the intended assertion is not discriminating red evidence. Section 27.50 corrects it.
- Historical citations to TR-MCP-NUKE-019 AC-7/AC-8 before their 2026-11-06 replacement mean the removed historical criteria; citations on or after replacement mean the current ownership and quarantine criteria.
- Build.Tests uses the established raw infrastructure runner with `Platform!=Windows`; this is deliberate even though product gates are Nuke targets.
- The scanner is under the operator's no-more-scanner-work override. Its declared technical limit remains: it resolves an alias whose target names a type and a `using static` that names a type in lexical scope, but it does not resolve an alias whose target is another alias or a `using static` that names an alias. Do not require scanner work.
- The retained startup-health wait remains outside D-B and unsatisfied. It counts 120 probes with five-second sleeps rather than an elapsed ten minutes; HTTP requests may take up to three seconds and state reads have a much larger command bound, so total time is not fixed near ten minutes. Several transport/cancellation failures are treated as not-yet healthy, and the constants/call are not fully bound by tests. Do not treat it as accepted PR87 work.
- GitHub Actions, Azure, hostile-environment runs, held-out integration projects and the two citation backlogs are outside this publication gate under recorded operator decisions.

## Failed-run disclosure

The first Round 17 lane attempt, `ug-r17-final-linux-lane-1`, placed TMPDIR inside the checkout. Two memory-benchmark artifact controls correctly rejected that environment: EnvironmentIntegrationTests reported 459 passed and 2 failed, while McpAgent passed 1 and QBAgent passed 3 before Codex stopped the invalid run. No code changed. The corrected run used a test-owned external TMPDIR and passed all 748 cases. The invalid run is history, not publication evidence, and is intentionally absent from `{TRX_MANIFEST}`.

The first targeted Round 17 attempt was aborted before tests by sandbox-denied VSTest sockets. The unsandboxed corrected targeted gate passed 24/0/0. Earlier failed/scratch-only runs remain disclosed in sections 27.48 and 27.49; do not infer that their ignored raw files are committed.

## Final-head evidence claims

At tested head `bbd248b94168bb0f50ba86a14c4b9021ffbabf1c` on LAB-OMARCHY:

- Test: 4,847 passed, 0 failed, 0 skipped. Project totals: Client 311, Cqrs 33, Launcher 20, McpAgent 64, QBAgent 90, Repl.Core 1,035 and Support.Mcp 3,294. Runtime discovery logged 4,815 selected identities; data-driven identities expand to 4,847 TRX result rows.
- MigrationIntegrationTests: 748 passed, 0 failed, 0 skipped. Project totals: EnvironmentIntegrationTests 461, McpAgent 1, QBAgent 3, NukeBuild 96, Repl 181 and Support.Mcp.IntegrationTests 6.
- Build.Tests raw infrastructure runner with `Platform!=Windows`: 540 passed, 0 failed, 0 skipped in one TRX.
- ValidateTraceability passed and produced no TRX by contract. It reported no SQLite path resolved for UseCaseFrLinks coverage; no live database readback is claimed.
- `{TRX_MANIFEST}` contains 14 copied TRX, 11 source pins and 9,457,342 bytes. Its SHA-256 is `3EDBAD5D60F5FE07D6E232EE8B2C01A3EA527D5E01B8D2834DE3E1175D386968`. Recompute every hash and inspect source pins, but do not rerun tests.

## Required review method

1. Inspect the complete diff from the PR base through `{HEAD}`, focusing on runtime discovery, the synthetic ownership decision tests, the five new controls and the embedding-fixture cleanup contract.
2. Reconcile every current requirement and task-position claim against live/store-backed records and committed evidence.
3. Test the argument mentally with hostile counterexamples: an empty all-case result, duplicate identity, filtered identity missing from all, missing list header, unindented parser boundary, new Integration traits in unit projects, missing Integration traits in lane projects, persistent IOException and non-IOException cleanup failures.
4. Check that no final claim depends only on ignored scratch evidence, every historical failure is accurately disclosed, and no count conflates discovery identities with expanded TRX results.
5. Check propagation through build targets, requirements, task text, plan, PR title/body, receipts and launcher metadata.
6. Write progress and the full verdict to your dedicated session-log turn. Preserve the complete stream in the paired JSONL paths.

## Required output

Return exactly one overall `AGREE` or `DISAGREE`, with accuracy, completeness and confidence. Provide a position-preserving result for all 75 tasks, identifying prior accepted tasks checked for regression, applicable open tasks, overridden items and out-of-scope items. For any disagreement, cite file and line, the violated criterion, a concrete counterexample, proposed acceptance-criterion language and exact discriminating tests. AGREE is valid only when overall and every applicable task are at least 98/98.
