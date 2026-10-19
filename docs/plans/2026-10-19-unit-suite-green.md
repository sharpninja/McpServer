# Plan: the unit suite green on both lab hosts (proposed)

Proposed 2026-10-19T14:47 local by ClaudeCode (claude-opus-5-5, effort xhigh). Status: approved 14:50 ("Yes, I approve the change") with the amendments in sections 7 to 9; executed as recorded in section 10; no done state before hostile validation. Committed as `docs/plans/2026-10-19-unit-suite-green.md` on the effort branch.

## 1. Operator direction (verbatim, 2026-10-19)

- "BDPv4 requires all unit tests be green."
- "Gotta use the recommended packages."
- Decisions: "Plan first, approve later"; "Full suite on Windows too" (Windows shows 0 failed and 0 skipped; Linux keeps reporting its 16 Windows-only cases as skipped); "Tag them Integration (Recommended)" (verify the three provider tests are integration tests, tag them, and make them pass in `MigrationIntegrationTests` on PAYTON-LEGION2); "Leave merged, fix forward (Recommended)" (PR 82 stays merged as `ace70daa`).

## 2. Measured state (receipts)

- Linux, LAB-OMARCHY: run `pr82-winhost-final-20261019T1603Z` on PR 82 head `b9970b70` (merged into develop as `ace70daa`): 4663 results, 4633 passed, 14 failed, 16 not executed. A fresh baseline at `ace70daa` is step 0 below.
- Windows, PAYTON-LEGION2, worktree `F:/GitHub/McpServer/.worktrees/unit-green-baseline-20261019` at `ace70daa`, as lab-wsman over the WSMan route:
  - Run `unit-green-win-baseline-20261019T1433` (no override): restore of `build/_build.csproj` fails with NU1902 (Pkcs 10.0.10, GHSA-2fmv-rv2c-59x7) and NU1903 (Pkcs 10.0.10 GHSA-846f-f764-76mx; Xml 10.0.10 GHSA-7hqp-7c5m-4vq6, GHSA-c399-9853-6jmj, GHSA-r955-8q5j-3h83). No test runs.
  - Run `unit-green-win-diag-20261019T1435` (diagnosis only: `WarningsNotAsErrors=NU1902;NU1903` set in the remote process; not a gate and never used again): `Test` 4637 results (Client 306, Cqrs 33, Launcher 20, McpAgent 65, QBAgent 96, Repl.Core 868, Support.Mcp 3249), 7 failed, 2 not executed; `MigrationIntegrationTests` Support.Mcp 49 results with 1 failed, Build.Tests 16 results with 11 failed. Receipts: `~/.claude/scratch/pr82-review-20261019/runs/unit-green-win-diag-20261019T1435*.txt`.
- Linux restore of `build/_build.csproj` (09:01:08 today) resolved the same Pkcs and Xml 10.0.10 and logged 0 audit entries (`project.assets.json`). Why Linux did not raise NU1903 is not known; the fix below removes the vulnerable versions on both hosts.

## 3. Per-test diagnosis and exact fix

### A. Vulnerable packages (TR-MCP-QUALITY-001; operator: use the recommended packages)

- Cause: `Directory.Packages.props:91` pins `System.Security.Cryptography.Xml` 10.0.10, which brings `System.Security.Cryptography.Pkcs` 10.0.10. All five advisories name 10.0.13 as the first patched 10.x version (`gh api /advisories/<id>`, 14:35:12); nuget.org lists 10.0.13 as newest for both (14:36:23); both 10.0.13 nuspecs target net8.0, net9.0, net10.0 and netstandard2.0 (14:41:31).
- Fix: Xml 10.0.10 to 10.0.13 at `Directory.Packages.props:91`; add `<PackageVersion Include="System.Security.Cryptography.Pkcs" Version="10.0.13" />` (transitive pinning is on, `Directory.Packages.props:4`, so every project takes it). No `WarningsNotAsErrors`, `NoWarn`, `NuGetAudit` or audit-source change anywhere, and no override in any run from here on.
- Proof: a clean Nuke `Restore` and `Compile` on both hosts in fresh worktrees with zero NU1901 to NU1904 entries.

### B. Stale backslash expectations from the forward-slash key change (FR, TR and TEST-MCP-PATHNORM-001; oracle `ExpectedWorkspaceKey.Of`)

Production already implements FR-MCP-PATHNORM-001; these tests still assert the superseded spelling or depend on the host's `Path.GetFullPath`. Test-only fixes, each with a negative control (the old value rejected inside a passing harness).

- B1 `UseCaseGetWorkspaceResolutionContractTests.ProductionGate_EquivalentRawPath_UsesCanonicalWorkspace` (both hosts; TEST-MCP-USECASE-002): expects `F:\GitHub\TruckMate`, gets `F:/GitHub/TruckMate` because `WorkspaceKeyConversion` canonicalizes on save (`src/QBrainAi.Storage/WorkspaceKeyConversion.cs:24-26`). Fix: split the constant into `RawWorkspacePath = @"F:\GitHub\TruckMate"` (seed and query input) and `CanonicalWorkspaceId = "F:/GitHub/TruckMate"` (expected value and mock result). Negative control `MockGate_BackslashWorkspaceId_IsRejected`.
- B2 `RemoveTodoRecordsMigrationTests` x2 (both hosts; TEST-MCP-TRIAGE-004) and `Decompose4nfBackfillMigrationTests.Migrate_Decompose4nf_BackfillsJsonListColumnsIntoChildTables` (Category=Integration; Windows MigrationIntegrationTests): raw-SQL seeds write `Q:\__mcp_unit_test__\QBrainAi` and `C:\ProgramData\QBrainAi`; the migrations copy keys verbatim (`20260628122208_RemoveTodoRecords.cs`, `20260628194717_RepairTriageCreatedTodoWorkspace.cs`), but the LINQ predicates are converted to the canonical `Q:/...` form, so `Single` finds nothing. Fix: seed the canonical form (`Q:/__mcp_unit_test__/QBrainAi`, `C:/ProgramData/QBrainAi`), which is what a store holds under FR-MCP-PATHNORM-001. Rewriting pre-existing backslash rows is federation P1.5 (`--workspace-path-rekey`, its named test `WorkspaceRekeyRewritesStoredBackslashKeysThreeProviders`), not this effort.
- B3 `WorkspacePathNormalizerRealTests.ProcessNormalizer_ForeignAbsolutePath_IsNotReRooted` (Linux; TEST-MCP-FED-PATH-001): expects `C:\Users\kingd` unchanged, gets `C:/Users/kingd`. Fix: `Assert.Equal(ExpectedWorkspaceKey.Of(foreign), result)`; the not-re-rooted assertion stays.
- B4 `WorkspaceServiceAccessorTests` x2 (Linux; TEST-HANDOFF-006): expectations wrap `Path.GetFullPath(@"F:\first")`, which re-roots a foreign path under the Linux working directory; production deliberately does not (`HandoffWorkspacePaths.cs:37`). Fix: `ExpectedWorkspaceKey.Of(@"F:\first")` and the same for each literal.
- B5 `GitHubCliServiceTests` x2 (Linux; TEST-MCP-083): the repo roots `C:\workspace` and `C:\repo\workspace` are foreign on Linux, so the expected `Path.GetFullPath("C:\\workspace")` and the service's `Path.GetFullPath("C:/workspace")` (`GitHubCliService.cs:782-788`) differ only in the re-rooted separator. Fix: host-native temporary workspace directories, as the passing test at `GitHubCliServiceTests.cs:461` already does; expected `WorkingDirectory == Path.GetFullPath(workspacePath)`.
- B6 `WorkspaceResolutionMiddlewareTests` x3 (Linux; TR-MCP-MT-002): `ApiKey_ResolvesWorkspace_WhenNoHeader`, `EmptyHeader_SkipsToApiKeyFallback`, `DefaultToken_SetsIsDefaultKey`. The token service stores the canonical key `C:/projects/alpha`; the test's `IWorkspaceService` substitute matches with `Path.GetFullPath` on both sides, which on Linux never equates `C:/...` with `C:\...`. The real `WorkspaceService.GetAsync` normalizes both through the path normalizer (`WorkspaceService.cs:101-111, 644-645`). Fix: the substitute's matcher compares `ExpectedWorkspaceKey.Of` of both values, ordinal ignore case.

### C. Provider tests are integration tests (operator: tag them Integration)

- Verified: `SessionLogAgentSessionHeaderMigrationTests.SqlServerUpSql_LegacySessionLogs_AddsHeaderColumnsAndTagsIdempotently` opens LocalDB or `MCP_TEST_SQLSERVER_CONNECTION` (`:215-235`); `PostgreSqlUpSql_LegacySessionLogs_AddsHeaderColumnsAndTagsIdempotently` boots `EphemeralPostgresFixture` (`:305-319`); `EphemeralPostgresFixtureTests.EphemeralPostgresFixture_InitdbFailure_CapturesSanitizedInvocationStdoutAndStderr` runs `initdb.exe` or adopts an external server (`:23-53`). All three passed on Windows in the diagnostic `Test` run.
- Fix: method-level `[Trait("Category", "Integration")]` on the two UpSql tests (their class also holds SQLite unit tests) and class-level on `EphemeralPostgresFixtureTests`. They leave the unit `Test` filter (`build/Build.Test.cs:50,63`) and run in `MigrationIntegrationTests` (`build/Build.TestDependencies.cs:63-129`).

### D. The two Windows skips (TEST-MCP-PATHNORM-001)

- `WorkspaceNativePathMockDataTests.NonWindowsHost_IsUnchanged` and `WorkspaceNativePathRealTests.NonWindowsHost_IsUnchanged` skip on Windows (`Assert.SkipWhen(OperatingSystem.IsWindows(), ...)`, `WorkspaceNativePathContractTests.cs:42`) because `WorkspaceIdentityPath.ToNativePath` reads `OperatingSystem.IsWindows()` directly (`src/QBrainAi.Client/WorkspaceIdentityPath.cs:1043-1049`).
- Fix: add `ToNativePath(string path, WorkspacePathPlatform platform, IWorkspaceHostEnvironment host)` (decides on `host.Platform`); the existing two-argument method delegates with `SystemWorkspaceHostEnvironment.Instance`, so its behavior is unchanged. Move the non-Windows-host case into a new contract `WorkspaceNativePathHostContractTests` run against a simulated non-Windows host (`SimulatedWorkspaceHosts`) on every runner, with MockData and Real subclasses, so it executes on Windows too. Public API addition in the published `QBrainAI.Client` package: additive, not breaking.
- The 16 Linux skips (the Windows-only cases in `WorkspaceNativePathContractTests` and `WorkspaceRemotePathContractTests`) stay as they are, per the operator.

### E. Windows evidence route environment (not code)

- `RequirementsTwelfthAdversarialReviewTests.OpenFileForRead_DirectoryAliasSwap_NeverReadsExternalObject` and `OpenFileForWrite_DirectoryAliasSwap_NeverWritesExternalObject` fail with Win32Exception "start process 'pwsh.exe' ... cannot find the file"; `BugTriage139FourteenthHarnessTests.ProcessRunner_ExitedParentWithPipeHoldingDescendant_KillsTreeWithinBound` gets Win32Exception instead of TimeoutException and starts `pwsh.exe` on Windows (`:100-102`). Observed: `pwsh.exe` is not on the lab-wsman PATH; `$PSHOME` is `C:\Program Files\PowerShell\7-LabWsman` and holds `pwsh.exe`. In Build.Tests, `YamlObjectMutationTests` (start 'pwsh') and both `RequirementsWikiPublishScriptTests` (start 'pwsh.exe') have the same cause.
- Fix in the remote runner only: prepend `$PSHOME` to `PATH`, and keep `MCP_NUKE_POWERSHELL_BOOTSTRAPPED=1` because `build.ps1:10-24` re-launches through `wsmprovhost.exe` in a WSMan session (filed as `triage-report-aa178657cf1140e59e4065d10b3c67d5`). Inference to confirm on the first Windows run: these five pass once `pwsh.exe` resolves.

### F. Open diagnosis: `HostileReviewG4OverlayTests.HostileReviewWorker_InvalidOptions_FailStartup` (Windows only; TR-MCP-HOSTILEREVIEW-003)

- Observed: `host.StartAsync` throws, but `ex.ToString()` lacks "HostileReviewWorker"; the TRX shows only "System.AggregateException: An error occurred while"··· (`:474-475`). The cause is not known.
- Step: make the assertion report the full text (`Assert.True(ex.ToString().Contains("HostileReviewWorker", StringComparison.Ordinal), ex.ToString())`; same condition, not weaker), rerun the Windows gate, then fix by cause. A production cause comes back to you as a plan amendment before any production edit.

### G. `MigrationIntegrationTests` target-level failures on Windows (operator decision, asked separately)

- Decompose4nf: fixed by B2.
- Build.Tests, 8 Linux-only cases of `ServiceUpdateTests` (`LinuxPreflight_RejectsSpecialApphostAndTextInputs` x4, `LinuxPreflight_RejectsSpecialStageFiles`, `LinuxPreflight_RejectsSymlinkRootAndNonexecutableStage(symlink: False)`, `LinuxArchive_UsesPrivateMetadataPreservingCommands`, `LinuxReplacement_DoesNotFollowSymlinks`): `mkfifo` missing, "Unix file modes are not supported on this platform", access denied on the stale-file replacement. They cannot pass on Windows, and the target cannot run on Linux because `InstallTestDependencies` installs the Windows LocalDB MSI (`build/Build.TestDependencies.cs:236-272`). So these 8 pass on no host today.
- Build.Tests, 3 PATH cases: fixed by E.

## 4. Requirements first (BDPv4 step 1), through the plugin's requirements workflow

- TR-MCP-QUALITY-001: reopen the AC "NU1901 and NU1903 are not approved..." (develop violates it since the 2026-10-13 advisories) and strengthen it to "NU1901, NU1902, NU1903, and NU1904 are not approved. Vulnerable package advisories must be resolved by updating to the advisory's patched versions and a clean vulnerability audit on every lab host"; satisfied again only with the A evidence.
- TEST-MCP-PATHNORM-001: add the AC "The native-boundary behavior for a non-Windows host is proven on every test host without a host-conditional skip" (D).
- No new FR or TR, and no mapping change. TODO `IMPL-UNITGREEN-001` created through the plugin, linked to this plan and to TR-MCP-QUALITY-001 and TEST-MCP-PATHNORM-001.

## 5. Order, gates and receipts (Nuke targets only; no override; receipts under `TestResults/<run id>` and copied to `docs/receipts/unit-suite-green-20261019/`)

- Branch `fix/unit-suite-green-20261019` from `origin/develop` (`ace70daa`). Linux worktree `~/github/McpServer/.worktrees/unit-suite-green-20261019`; Windows worktree `F:/GitHub/McpServer/.worktrees/unit-suite-green-20261019` from the pushed branch, `safe.directory` per process only.
- Step 0: fresh Linux baseline `./build.ps1 Test --test-run-id unit-green-linux-base-<stamp>` at `ace70daa` (expected: the 14 failures listed in B and C, plus the 16 skips).
- Step 1 (A): package pins. Gate: `Restore` and `Compile` clean on both hosts, zero NU190x.
- Step 2 (tests and scripted data first): B1 to B6, C, D's new contract and MockData class, F's assertion message, each B change with its negative control.
- Step 3, mock gate (Linux): `./build.ps1 Test --test-run-id unit-green-mock-<stamp>`. Pass condition: 0 failed; every new and changed test and every negative control passes; not executed exactly the 16 named Windows-only cases. B and C bind to production that already implements FR-MCP-PATHNORM-001; D's Real binding is not written until step 4, so no test is red at any point.
- Step 4 (production): D's host-aware `ToNativePath` overload and the `WorkspaceNativePathHostRealTests` binding.
- Step 5, exit gates, all required:
  - Linux fresh worktree: `./build.ps1 Test`: 0 failed; not executed exactly the 16 named Windows-only cases.
  - Windows fresh worktree: `Test`: 0 failed, 0 not executed.
  - Windows: `MigrationIntegrationTests`: 0 failed, 0 not executed, including the three provider tests (scope of G per your answer).
  - `ValidateTraceability` on Linux; Build.Tests run as the infrastructure check because A changes the Nuke build project's package graph.
- Step 6: hostile validation by a reviewer outside the Claude family (agent and model asked below), unbounded review time, request and response jsonl under `docs/receipts/hv/`, the entire verdict in the reviewer's session-log turn, 98/98 bar. No done state before AGREE.
- Step 7: one PR into `develop`, signed comments. On the branch also correct `docs/handoffs/pr82-review-handoff-20261019T104723.md`, which records the baseline-equal gate as accepted.

## 6. Blast radius

- Test files: `UseCaseGetWorkspaceResolutionContractTests.cs`, `RemoveTodoRecordsMigrationTests.cs`, `Decompose4nfBackfillMigrationTests.cs`, `WorkspacePathNormalizerRealTests.cs`, `WorkspaceServiceAccessorTests.cs`, `GitHubCliServiceTests.cs`, `WorkspaceResolutionMiddlewareTests.cs`, `SessionLogAgentSessionHeaderMigrationTests.cs`, `EphemeralPostgresFixtureTests.cs`, `WorkspaceNativePathContractTests.cs` (plus the new host contract), `HostileReviewG4OverlayTests.cs`.
- Production: `Directory.Packages.props` (every project moves Pkcs and Xml from 10.0.10 to 10.0.13; for net8 and net9 consumers Pkcs 10.0.13 brings `Microsoft.Bcl.Cryptography` and `System.Formats.Asn1` 10.0.13, neither centrally pinned); `src/QBrainAi.Client/WorkspaceIdentityPath.cs` (additive overload).
- No migration, schema, configuration, wire contract or stored-data change. Breaking change: none.

## 7. Amendments and decisions after approval (2026-10-19)

- Approved 14:50 ("Yes, I approve the change"); G: "Host-scoped lane (Recommended)" as a plan amendment for approval; HV reviewer: Codex gpt-6-astra max.
- Step 1: FR, TR and TEST-MCP-PATHNORM-001 were absent from this store and TR-MCP-QUALITY-001 had no criteria here. Operator: "Copy from PAYTON-LEGION2". Copied verbatim through the PAYTON-LEGION2 plugin wrapper (receipt `runs/legion2-requirements-20261019T1459.txt`), then ac-21 reopened and strengthened and TEST-MCP-PATHNORM-001 `unitgreen-native-host` added. Store drift filed as `triage-report-89af8a4491394d1f87b6cc27d535e85f`.
- Overlap: PLAN-C4MODEL-001 (branch `docs/c4-structurizr-model-20261017`) had already fixed 10 of the 14 Linux failures (`ff55292e`), Decompose4nf (`d248230e`) and the Linux provider lane (TR-MCP-NUKE-018). Operator: "Narrow mine (Recommended)". This effort keeps A, B1, B2 (RemoveTodoRecords), B3, D, E, F and the Windows side of G; B4, B5, B6, C and the Decompose4nf change were restored to develop in `a4c358ef`. Exit gates run after PLAN-C4MODEL-001 is on develop and develop is merged here.

## 8. Proposed amendment G: host-scoped integration lane, Windows side (for approval)

- Requirement (step 1): new TR-MCP-NUKE-019 under FR-MCP-BUILD-001 (Nuke behavior is accountable through TR only; no TEST record): "MigrationIntegrationTests selects host-specific integration cases by the Platform trait".
  - AC-1: On a Windows host the target runs `Category=Integration` cases except those tagged `Platform=Linux`; on a Linux host it runs them except those tagged `Platform=Windows`; untagged cases run on both.
  - AC-2: Every case tagged `Platform=Linux` or `Platform=Windows` runs and passes in its own host's MigrationIntegrationTests, so the two lab hosts together execute every integration case, each host at 0 failed and 0 not executed.
  - AC-3: The run log names the selection filter (filter text only).
  - AC-4: Provisioning is unchanged: TR-MCP-NUKE-018 on Linux, LocalDB and PostgreSQL binaries on Windows.
- Observed today: the 8 failing Windows cases already carry `[Trait("Platform", "Linux")]` (`tests/Build.Tests/ServiceUpdateTests.cs:419-628`); on PLAN-C4MODEL-001's branch the target still filters only `Category=Integration` (`build/Build.TestDependencies.cs:139,148`), and its Linux run passes all 68 cases.
- Tests first (Build.Tests): a pure filter function for a host platform, contract cases for both hosts plus negative controls (a Windows filter that admits `Platform=Linux`, a Linux filter that admits `Platform=Windows`, and one that drops untagged cases are each rejected), mock gate, then the binding in `MigrationIntegrationTests`.
- Order: implemented on top of PLAN-C4MODEL-001's `build/Build.TestDependencies.cs` after it reaches develop, so the two efforts never edit the target concurrently. Superseded 16:02 to 16:38 by the ownership decision (this effort took the file over as `11681680`, blob-identical to `c2125c42`); approved 16:22 ("Yes, I approve the change") and built as `8c2b83ab`.
- Gates: MigrationIntegrationTests on PAYTON-LEGION2 and on LAB-OMARCHY, each 0 failed and 0 not executed; Build.Tests as the infrastructure check.

## 9. Day-one rule: zero platform-specific unit tests (operator 2026-10-19)

Operator, verbatim: "There should be ZERO platform specific UNIT tests.  If it depends on the environment, it's an integration test." and "No, it is not a requirement change.  It is a clarification of the rules since day one." Ownership (16:02 to 16:04): this effort owns all unit-suite code, including PLAN-C4MODEL-001's `ff55292e` and `d248230e` unit-suite files (copied byte for byte at `c2125c42`, gates rerun here: Linux Test 0 failed with only the 16 Windows-only not executed, run `unit-green-takeover-20261019T1611`; Linux MigrationIntegrationTests 68/0/0, run `unit-green-takeover-int-20261019T1616`); C4 keeps docs and the TR-MCP-NUKE-017 render tooling.

Inventory (read-only, `runs/platform-test-inventory-20261019T1612.txt`, the 7 projects the Test target runs, `build/Build.Test.cs:16-25`), judged from source:

- Real host dependence, moved to `Category=Integration` with `Platform=Linux` or `Platform=Windows` (unchanged assertions; they run in their host's MigrationIntegrationTests lane):
  - Linux: BugTriage139EighteenthLinuxTests (FUSE, mkfifo, non-root), BugTriage139SixteenthLinuxTests (FUSE, /bin/sh), RequirementsFourteenthLinuxNativeTests (mkfifo), BugTriage139FifteenthNativeTests (Linux branches, FUSE).
  - Windows: BugTriage139SixteenthIdentityWindowsTests, the BugTriage139SixteenthWindowsProcessTests named in the harness, WorkspaceRemotePathContractTests (Windows physical resolution), ProcessEnvironmentServiceTests (Windows PATH resolution), CliBrainSlotStrategyTests DefaultSharedTempDirectory_Windows.
  - Both hosts, untagged (real files, processes, ACLs or modes on each host): BugTriage139FourteenthHarnessTests and BugTriage139FifteenthHarnessTests (their compiled-inventory checks move with the native classes), BugTriage139EighteenthTraversalTests, HandoffContainedFileReaderTests, RequirementsTwelfthAdversarialReviewTests, FileGitHubWorkspaceTokenStoreTests, WorkspaceIdentityFourteenthNativeTests, CodexCliAgentExecutionStrategyTests Windows npm-shim case, TranscriptMcpStdioHostTests (launches the built executable), WorkspaceHostEnvironmentRealTests (binds the real host), WorkspacePathNormalizerRealTests.ProcessNormalizer_ForeignAbsolutePath_IsNotReRooted.
- Host-independent through the existing IWorkspaceHostEnvironment seam (stay unit): WorkspaceNativePathContractTests.WindowsPath_OnWindowsHost_UsesBackslashes (the host-aware ToNativePath overload with a simulated Windows host), WorkspaceTokenServiceTests (simulated-host normalizer instead of WorkspacePathNormalizer.Process).
- Fixed data, no OS choice (stay unit): McpAgentOptionsSectionTests (one literal path), GrokCliAgentExecutionStrategyTests (one literal name).
- Production branches on the real OS inside pure logic (FederationTopologyServiceTests comparison, BugTriage139SixteenthIdentityConsumerTests storage key, CodexCliAgentExecutionStrategyTests cmd.exe wrapper, GitHubCliServiceTests foreign-path helper): operator decision 16:19, "Integration + Platform (Recommended)". Built as per-platform classes (Windows and Linux) with fixed expectations.
- Not platform-specific (false positives): the "skip=3" query string, "pwsh" or "bash" as data or as a fake runner's recorded value, separators used only to build host-native paths, IngestionAllowlistContractTests' own scan pattern.
- Guard: IngestionAllowlistContractTests.TestSources_DoNotDeclareSkippedXunitTests misses `Assert.SkipUnless(` and `Assert.SkipWhen(` (`\bAssert\.Skip\b`, `:200`); it is strengthened to catch every skip form, plus a unit-suite guard that fails on OperatingSystem or RuntimeInformation checks and [SupportedOSPlatform] in unit (non-Integration) test classes.
- Fixture root: UnitTestPaths.Root chooses a host-shaped fake root (`UnitTestPaths.cs:13`); operator decision 16:19, "Host-neutral fixture (Recommended)": kept; the guard does not scan helper types, so the fixture is outside it.
- Requirements: FR-MCP-USECASE-016 and -017 (the only FRs that state the platform-test rule) are converted to TRs per the operator ("If it is in a FR, then that FR needs to be converted to a TR"; 17:03 answers "Merge into its TR (Recommended)", "FR-001, delete FRs (Recommended)", "Only 016 and 017 (Recommended)"): TR-MCP-USECASE-018 holds FR-016's 7 criteria (AC-006 restated for the integration lane) and TR-MCP-USECASE-019 FR-017's 8 (AC-008 restated), both under FR-MCP-USECASE-001 with TEST-MCP-USECASE-019/020; FR-016 and FR-017 deleted after read-back. FR-MCP-USECASE-015 stays an FR.
- Additional finding: McpAgentOptionsSectionTests' legacy-section binding goes through a validator that requires a path fully qualified on the running host, so it moved to an integration class and `QBrainAi.McpAgent.Tests` joined MigrationIntegrationTests (TR-MCP-NUKE-019 AC-2).

## 10. Execution record (2026-10-19)

- `cbc74e82` A; `28bf0b22` B1, B2, B3 tests; `de581353` D overload; `a4c358ef` narrowing (superseded); `b2319488` F; `11681680` takeover of PLAN-C4MODEL-001 unit-suite code (blob-identical to `c2125c42`); `8c2b83ab` G (TR-MCP-NUKE-019); `8a617334` merge of develop with PR 86 (BoundedProcess fix split out of PR 84 by operator decision 17:05); `b2bd50b3` section 9 (guard, skip-guard strengthening, reclassification).
- Mock gates: G `unit-green-g-mockgate-20261019T1633` 21/21; guard `unit-green-guard-mock-20261019T1720` 17/17.
- Linux: Test `unit-green-reclass-20261019T1745` 4611 total, 0 failed, 0 not executed; MigrationIntegrationTests `unit-green-reclass-int-20261019T1751` 154 passed, 0 failed, 0 skipped (filter `Category=Integration&Platform!=Windows`).
- Windows (PAYTON-LEGION2, HEAD `b2bd50b3` verified): Test `unit-green-win-reclass-20261019T1751` 4611 total, 0 failed, 0 not executed; MigrationIntegrationTests in the same run 128 passed, 0 failed, 0 not executed (filter `Category=Integration&Platform!=Linux`).
- Infrastructure: full Build.Tests (raw `dotnet test`) `unit-green-buildtests-20261019T1753` 422 passed, 0 failed, 0 not executed; ValidateTraceability `unit-green-traceability-20261019T1758` passed.
- Not done: the requirements wiki export (`workflow.requirements.generateDocument`, wiki, all) fails on Linux with the known defect `triage-report-a6927fca10364155ab9992506e1abc9c` (status grouped), so the generated `docs/Project` exports still list FR-MCP-USECASE-016 and -017, which the store no longer holds. Hostile validation has not run.
