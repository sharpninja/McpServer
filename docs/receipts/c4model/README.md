# PLAN-C4MODEL-001 receipts

Chronological receipts for docs/plans/PLAN-C4MODEL-001.md (BDPv4). File names start with the local start time (yyyyMMddTHHmmss, LAB-OMARCHY, US Central). Logs are force-added because the repository ignores *.log.

## P1 mock gate (G1)

- 20261019T101620-g1-mockgate-build.log and .trx: Build.Tests filtered to MockGate before any production rule or runner code existed: 23 passed, 0 failed, 0 skipped (rules contract with scripted mock and negative controls; runner contract with 7 scenarios and 7 wrong runners).
- 20261019T101840-g1-mockgate-test165-unit-suite.log: void run. ./build.ps1 Test without --test-run-id writes no TRX, and the first launcher redirected output through the launching process, which exited; no output and no exit code were captured. Kept as the record of the attempt.
- 20261019T103345-g1-unit-suite-runid.log with TestResults/c4model-20261019T1034-g1 (not committed; generated): ./build.ps1 Test --test-run-id c4model-20261019T1034-g1. TEST-MCP-165 mock gate passed (ImportedDiagramViewContract_AcceptsConvertedFixture, ImportedDiagramViewContract_RejectsDeviations). Suite: 4642 total, 4612 passed, 14 failed in QBrainAi.Support.Mcp.Tests, with Windows-only cases not executed on Linux. The operator ruled the 14 failures are fixed in this plan.

## P2 production binding (G2, infrastructure)

- 20261019T102525-g2-build-tests-full.log: first attempt; the build failed with CS0649 on the new WhatIf parameter field (never assigned), fixed with an explicit initializer as the other Nuke parameters have.
- 20261019T102537-g2-build-tests-full.log and .trx: full Build.Tests with the P2 code: 409 total, 408 passed, 1 failed (BoundedProcessTests on OfficialPluginCoreSyncTestSupport.cs), 0 skipped; all 34 ArchitectureDiagram tests passed.
- 20261019T103108-baseline-boundedprocess-ec47a0b6.log: the same BoundedProcessTests failure on a clean worktree of ec47a0b6 (no P2 code); triage-report-1b7f4954cf214dea9fbadebe1ecc65da. The operator chose to fix it in this plan.
- 20261019T103230-p2-live-whatif-smoke.log: ./build.ps1 ArchitectureDiagrams --what-if against the first workspace: validate passed, JSON export worked, 18 inspection errors blocked rendering, 435 Mermaid fences and 25 .mmd files and 12 missing renders reported (each finding appears twice because Nuke repeats errors in its summary).
- 20261019T104511-g2-build-tests-full-r2.log and .trx: full Build.Tests after the BoundedProcess fix and the AC-5 amendment: 409 passed, 0 failed, 0 skipped.

## Order note

AC-5 was amended on 2026-10-19 (operator answer "Show it; widen AC-5 to 2:1"). The requirement and the contract fixtures were changed first, but the production constant was edited before the rules mock gate was rerun. The mock gate uses a scripted mock that does not read the constant, so its result is unaffected; the deviation from the step order is recorded here.
