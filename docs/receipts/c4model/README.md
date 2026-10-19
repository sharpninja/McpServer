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

## Unit failures fixed in this plan (operator answers "Fix all 14 in this plan", "Fix 10 here; 4 via PR 82/83", "Trait + Linux provisioning")

- 20261019T110522-g1b-unit-prefix.log with TestResults/c4model-20261019T1105-g1b (not committed; generated): ./build.ps1 Test --test-run-id c4model-20261019T1105-g1b after the test fixture fixes (ExpectedWorkspaceKey oracle in WorkspaceResolutionMiddlewareTests and WorkspaceServiceAccessorTests, host temp workspaces in GitHubCliServiceTests) and before the product fix. 4644 total, 4620 passed, 8 failed, 16 Windows-only cases not executed on Linux. The 8: ListIssuesAsync_WithForeignWorkspace_RunsWithoutWorkingDirectory (NullReferenceException, the GitHubCliService defect the test was written to expose); 3 database tests with no Linux server (sqllocaldb missing, PostgreSQL binaries missing twice); 4 tests that PR 82 and PR 83 change (UseCaseGetWorkspaceResolutionContractTests.ProductionGate_EquivalentRawPath_UsesCanonicalWorkspace, two RemoveTodoRecordsMigrationTests, WorkspacePathNormalizerRealTests.ProcessNormalizer_ForeignAbsolutePath_IsNotReRooted).
- 20261019T114955-g1-mockgate-nuke018.log and .trx: TR-MCP-NUKE-018 BDPv4 mock gate before any production provisioning code: 14 passed, 0 failed, 0 skipped (8 contract scenarios against a scripted provisioner, 1 coverage fact, 5 wrong provisioners rejected: value leak, foreign container, file read although set, missing file ignored, probe skipped).
- 20261019T115833-g1-bind-nuke018.log and .trx: the same contract bound to the production LinuxProviderTestProvisioner: 23 passed (14 mock gate, 9 production), 0 failed, 0 skipped.
- 20261019T115844-g2-build-tests-full-r3.log and .trx: full Build.Tests with the provisioner: 432 passed, 0 failed, 0 skipped.
- 20261019T120330-g4-provider-nuke018.log with TestResults/c4model-20261019T1203-g4-provider (not committed; generated): ./build.ps1 MigrationIntegrationTests --test-run-id c4model-20261019T1203-g4-provider on LAB-OMARCHY (TR-MCP-NUKE-018 AC-6). InstallTestDependencies loaded both variables from the test-database env file and found localhost:55432 and localhost:51433 reachable; the log carries variable names, the file path and host:port only. QBrainAi.Support.Mcp.Tests Category=Integration: 52 total, 51 passed, 1 failed, 0 skipped; Build.Tests Category=Integration: 16 passed. The three tests moved to Integration passed against the containers. The failure is Decompose4nfBackfillMigrationTests.Migrate_Decompose4nf_BackfillsJsonListColumnsIntoChildTables (SQLite): its seed stores the legacy key Q:\__mcp_unit_test__\QBrainAi, and its workspace-filtered queries go through WorkspaceKeyConversion, which canonicalizes the parameter to Q:/__mcp_unit_test__/QBrainAi, so no row matches. The PostgreSQL and SQL Server variants do not filter by workspace and passed. This category never ran on Linux before this provisioning; the fix is an operator decision.
- AC-4 receipt (scratchpad check-no-secrets.ps1, 12:08:42): both connection strings from the env file and the password inside each (4 values) occur 0 times in 590 files under docs/receipts/c4model, TestResults/c4model-20261019T1203-g4-provider, build and tests/Build.Tests. Only counts and key names were printed.
- 20261019T120610-g1c-unit-postfix.log with TestResults/c4model-20261019T1206-g1c (not committed; generated): ./build.ps1 Test --test-run-id c4model-20261019T1206-g1c after the GitHubCliService fix and the Integration traits. 4641 total, 4621 passed, 4 failed, 16 Windows-only cases not executed on Linux. The 4 are exactly the tests PR 82 and PR 83 change (PR 82 is merged into develop; PR 83 is open). The two tracked memory-bench JSON files were rewritten again (bench_workspace_id only, triage-report-46080e351ff8473e9ccb03212b66bc98) and restored to HEAD after the diff was checked.
- 20261019T121059-g4-negative-missing-envfile.log: ./build.ps1 InstallTestDependencies --test-db-env-file pointing at a missing file fails (exit 255) and names the file (TR-MCP-NUKE-018 AC-3).
- 20261019T121113-g4-negative-migration-missing-envfile.log: ./build.ps1 MigrationIntegrationTests with the same missing file: InstallTestDependencies Failed, MigrationIntegrationTests NotRun, exit 255; no test is skipped (TR-MCP-NUKE-018 AC-6).

## Decompose4nf SQLite test (operator answer "Fix test here (Recommended)")

- 20261019T141209-g1d-mockgate-migration-keys.log with TestResults/c4model-20261019T1411-g1d (not committed; generated): BDPv4 mock gate for tests/QBrainAi.Support.Mcp.Tests/Storage/MigrationKeyAssertions.cs before the Decompose4nf test was changed. MigrationKeyAssertionsMockGateTests: 6 passed (the parent key accepted; child canonical respelling, mixed keys, empty child table, rekeyed parent and a second workspace rejected). Suite: 4647 total, 4627 passed, the same 4 PR 82 and PR 83 failures, 16 Windows-only cases not executed. The memory-bench JSON files were rewritten again (bench_workspace_id only) and restored to HEAD.
- 20261019T141810-g4-provider-decompose4nf-bound.log with TestResults/c4model-20261019T1418-g4b-provider (not committed; generated): after the four workspace-filtered queries were replaced by unfiltered reads plus MigrationKeyAssertions raw-SQL key checks, ./build.ps1 MigrationIntegrationTests --test-run-id c4model-20261019T1418-g4b-provider: QBrainAi.Support.Mcp.Tests 52 passed, Build.Tests 16 passed, 0 failed, 0 skipped, exit 0. The P5 provider gate is green on LAB-OMARCHY.

## P3 renderer: content fitted (TR-MCP-NUKE-017 AC-2)

- 20261019T142210-p3-baseline-render.log: ./build.ps1 ArchitectureDiagrams on the P2 workspace: validate passed, 0 inspection errors, 12 views rendered, all 12 over the AC-5 page (even KeyServerComponents at 1866 x 1166). Screenshot inspection showed the export's viewBox is the whole paper with about 400 units of empty margin: the renderer did not fit the content, which AC-2 requires. The viewer crops (margin 50) only in its PNG export.
- 20261019T142400-p3-spacing-100.log: autolayout separations lowered from the default 300 to 100 in every view: sizes dropped (ServerComponents 20750 x 9320 to 9795 x 6633) but all 12 still overflowed.
- 20261019T142851-g1-mockgate-svgfit.log and .trx: BDPv4 mock gate for ISvgContentFitter before production code: 17 passed (10 contract scenarios against a scripted fitter, 1 coverage fact, 6 wrong fitters rejected: no margin, no clamp, inward rounding, every viewBox text replaced, empty content accepted, culture formatting).
- 20261019T142951-g2-bind-svgfit.log and .trx: the same contract bound to the production SvgContentFitter: 28 passed.
- 20261019T143000-p3-fitted-render.log: with the content measured in Chromium and cropped by SvgContentFitter, 4 views meet AC-5 (KeyServerComponents 1180 x 638, DeploymentFrpRailway 1434 x 638, DeploymentFrpLocal 1406 x 638, DeploymentLinuxSystemd 1469 x 1250); 8 still need splitting per functional group.
- 20261019T143051-p3-fitted-render-r2.log: a second run; SHA-256 of all 12 renders identical to the first run (AC-2 byte-identical renders).
- 20261019T143121-g2-build-tests-full-r4.log and .trx: full Build.Tests: 460 passed, 0 failed, 0 skipped.

## P3 model of the whole codebase

- 20261019T143906-p3-entities-r1.log: first entity render: 9 Structurizr inspection errors (entities without foreign keys were disconnected). Fixed with content (ADR 0006): every entity is mapped by its DbContext component ("Maps table").
- 20261019T144107-p3-entities-r2.log: inspection clean; the mapping relationships implied edges up to the system, so the old include-star views pulled all 88 entities in (SystemContext 4350 x 35946); entity views 7 of 25 within the page.
- 20261019T144339-p3-entities-r3.log: entity box height 220 and chunks fitted by an estimate of ranks and breadth: 26 of 27 entity views within the page.
- 20261019T145208-p3-whole-model-r1.log: whole-codebase model (Shared Libraries, Compatibility Packages, McpAgent Sample Host, Test Suites, Nuke Build, Developer Tools, Operations Scripts, plugin core components) with the 12 hand views replaced by 147 generated neighbourhood views; 182 renders, 151 within the page.
- 20261019T145648-p3-whole-model-r2.log: left to right for one-sided groups: worse (45 over), because edge labels widen the ranks.
- 20261019T145758-p3-whole-model-r3.log: top to bottom with 50 between siblings: 24 over.
- 20261019T145915-p3-whole-model-r4.log and 20261019T150006-p3-whole-model-r5.log: hubs that overflowed fed back with smaller groups (21 hubs at 2), Docker and Windows deployment views split: 213 renders, 0 over the page.
- 20261019T150348-p3-gate-g3-r1.log and 20261019T150432-p3-gate-g3-r2.log: gate G3 after the operator cleared deleting the 22 retired originals ("Delete all 22 (Recommended)"): validate passed, 0 inspection errors, 213 renders, 0 page, link or render findings; AC-3 red only for documents not yet converted (435 fences, 4 Marketing .mmd files). SHA-256 of all 213 renders identical across the two runs.
- Migration tools (D9, outside the repository): ~/.cache/c4-migration/Convert-EfErToDsl.ps1 (entities, run with -AreaChunkSizes AGENTS=3), New-C4Views.ps1 and Update-HubSizes.ps1 with hub-sizes.json (21 hubs at group size 2). Each was checked by PSScriptAnalyzer with 0 findings and dry-run first.

## View key probe (D5 amendment)

- 2026-10-19T15:09:42, scratch probe workspace with dynamic views keyed SD-PROBE-001.alt1 and SD-PROBE-001-alt2, pinned structurizr/cli inspect: "View keys can only contain the following characters: a-zA-Z0-9_- at line 8 ... dynamic * "SD-PROBE-001.alt1"". D5 branch and loop keys use a hyphen (operator answer "Hyphen (Recommended)").

## Develop base and Planned excludes

- 20261019T152708-develop-merge-compile.log: operator correction "All work must be based on develop". origin/develop ace70daa (PR 82) was merged into the effort branch as e1d38ea9; ./build.ps1 Compile exit 0. The P4 worktrees were fast-forwarded to e1d38ea9 and each was verified to contain origin/develop.
- 20261019T153147-planned-excludes-render.log: D3 excludes (`exclude "element.tag==Planned"` and `exclude "relationship.tag==Planned"`) after the include line of the 174 generated C4 views and the 31 entity views: validate passed, 0 inspection errors, 213 renders, 0 page, link or render findings; AC-3 red only for documents not yet converted (435 fences, 4 Marketing .mmd files).
- Byte churn against the committed renders: 1 of 213 renders identical, 212 changed. With the JointJS generated ids (j_N, v-N, v_N) renumbered per file, all 213 equal the committed renders, so no view content changed. In 210 views the ids are shifted by a constant +12 (j_) and +151 (v-) starting with the first view rendered, so the offset arises once before any view is drawn and does not accumulate per view. Inference: the cause is the style additions of 1501f2b0, which were committed without a re-render, not the per-view excludes. AC-2 (byte-identical renders across runs of one workspace) is unaffected; any workspace change that shifts the page-load id counter rewrites every render.

## Order note

AC-5 was amended on 2026-10-19 (operator answer "Show it; widen AC-5 to 2:1"). The requirement and the contract fixtures were changed first, but the production constant was edited before the rules mock gate was rerun. The mock gate uses a scripted mock that does not read the constant, so its result is unaffected; the deviation from the step order is recorded here.
