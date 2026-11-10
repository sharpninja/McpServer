# PR87 hostile validation Round 17

Claude Code completed the review at 2026-11-09T17:57:12-06:00. The final verdict block's earlier stamp was not measured with the timestamp guard; Claude's correction retained the verdict unchanged.

- Verdict: AGREE
- Accuracy: 98
- Completeness: 98
- Confidence: 92
- Reviewer: Claude Code 2.1.318, `claude-opus-5-5`, high effort
- Reviewed records head: `02ed8d6c44957ec5adf8a191d3820496b55e9bba`
- Tested evidence head: `bbd248b94168bb0f50ba86a14c4b9021ffbabf1c`
- Remediation code commit: `287bdcb34e035049d8e15e7f9ce9d0167cbe417d`
- Claims: 31 PASS, 0 FAIL, 0 UNKNOWN, 3 OVERRIDDEN, 4 OUT OF SCOPE
- Regression summary: none
- Reviewer builds/tests: none

## Findings and task results

Round 16 findings R16-F1 and R16-F2 are fixed. The ownership decision tests use explicitly synthetic inventories instead of recorded current-layout counts, the production Test and MigrationIntegrationTests paths retain runtime ownership discovery before execution, and five distinct controls bind the empty-result, duplicate-identity, missing-subset, missing-header and unindented-boundary fail-closed branches.

- Judged in full, AGREE 98/98: tasks 64, 71 and 72.
- Previously accepted and regression-checked with no regression: 8, 10, 11, 14, 16, 25, 29, 30, 31, 40, 62, 63 and 65 through 70, 73 and 74.
- Previously accepted with no changed requirement: 1 through 7, 9, 15, 17 through 24, 26 through 28, 32 through 34, 36 through 39 and 41 through 61.
- Out of scope and left open: 12, 13, 35 and 75.

## Verified evidence

Claude recomputed the manifest hash, every one of the 14 TRX hashes, 11 source pins, the 9,457,342-byte total, four final receipt hashes, the task-map hash and the prompt hash. The TRX results are 4,847/0/0 unit, 748/0/0 integration and 540/0/0 Build.Tests. Runtime discovery's 4,815 unit identities and the 4,847 data-expanded TRX rows are correctly reported as different measures.

## Processing and observations

After a read-only preview, Codex applied the accepted verdict through the required plugin: 26 task flags were checked, leaving 71 of 75 done and tasks 12, 13, 35 and 75 open; exactly 54 mapped criteria across 23 live requirement records were set satisfied and read back. The TODO remains open. FR-MCP-040 ac-1, TR-MCP-NUKE-016 AC-001 and the startup-health-wait criterion remain unsatisfied.

The records now describe the quarantine inputs as synthetic cases carrying the real 26-and-3 counts required by task 73. The tested source's AC-8 count attribution is retained at the immutable tested head. The generated technical-requirements projection is stale; the live store remains authoritative until wrap-up regeneration.

The plugin attributed Claude's review turn to the Default session; Claude filed duplicate triage `triage-report-5c9e7d9f6c32422fa3f2352bd7f9d4e7`. Claude used Bash for read-only inspection because its write prohibition prevented script creation, omitted numeric progress, and corrected ten timestamp-format violations. None affected the verdict. Six live-marker key occurrences were redacted; the request and response each have zero invalid JSONL lines and the response contains zero live-key matches.

The complete output is preserved in `20261109T174415-unitgreen-r17.request.jsonl` and `.response.jsonl`. The review turn is `req-20261109T234703Z-hv-unitgreen-r17`. Merge remains a separate operator decision.
