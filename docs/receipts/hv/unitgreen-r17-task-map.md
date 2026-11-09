# PR87 Round 17 fixed task map

Recorded 2026-11-09 by Codex from the live `IMPL-UNITGREEN-001` TODO after the operator's Linux-only publication decision and the Round 16 hostile review.

Denominator: 75 fixed-position tasks.

- Fixed-position task text: `docs/receipts/hv/unitgreen-r15-tasks-a1e19d73.txt` contains all 75 live-store entries in order.
- Requirement mapping: `docs/receipts/unit-suite-green-20261019/2026-11-09-pr87-p0-task-map.md` maps the open task positions to the acceptance set.
- Current-store readback: the TODO retained 75 entries after the task 72 and 74 amendments. No task was added, removed, reordered, or checked by those amendments or by Round 16 remediation.
- Round 16 returned AGREE 98/98 for tasks 62 and 74.
- Round 16 returned DISAGREE 97/95 for tasks 64 and 71 on R16-F1/R16-F2, and DISAGREE 98/96 for task 72 because remediation moved the final tested head.
- Round 16 retained the selected prior acceptances with no regression, including tasks 8, 25, 29, 30, 63, 65, 66, 67, 68, 69, 70 and 73.
- Tasks 12, 13, 35 and 75 remain outside this review's acceptance set and open. Other prior 98/98 acceptances require regression checks, not rescoring by position changes.

## Current text overrides

These two entries supersede only the corresponding text in the fixed-position source above.

- Task 72: `R14-F4 S10: the Linux publication gate set at the final code/test head (Test, MigrationIntegrationTests, Build.Tests and ValidateTraceability, 0 failed and 0 skipped), retaining the green 03eea747 two-host gate set as the cross-host baseline and the final unit-code Windows composition evidence; the TRX manifest for the final Linux project set, a holistic read, the records commit, PR 87 marked ready, then full Claude Code Opus 5.5 high-effort HV. Operator 2026-11-09: "I want you to focus on the Linux tests only to get PR87 published."`
- Task 74: `PR87 completion embedding verification: satisfy FR-MCP-004 embedding-vector-contract, TEST-MCP-EMBEDDING-001 and TR-MCP-EMBEDDING-001 through the 12-row pure assertion contract, a test-owned real model fixture, Linux bindings of the four original EmbeddingServiceTests cases, the unavailable-model control, bounded cleanup persistent-IO and non-IO negative controls, Compile, and Linux final-gate evidence; retain the green 03eea747 Windows baseline and the 12-row Windows composition binding at identical source hashes as historical cross-host evidence. Operator 2026-11-09: "I want you to focus on the Linux tests only to get PR87 published."`

## Round 17 review rule

Claude must issue a position-preserving result for tasks 1 through 75. Apply the two overrides above before scoring. A prior 98/98 acceptance remains accepted only if the Round 17 regression check finds no regression. R16-F1 and R16-F2 must be evaluated against the strengthened AC-7, the explicitly synthetic ownership inventory, the five exact negative controls and the final 540-case Build.Tests TRX. Tasks outside PR87 remain open and are not converted into artificial passes.
