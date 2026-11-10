## IMPL-UNITGREEN-001: the unit suite green on both lab hosts (BDPv4: 0 failed, 0 skipped)

Operator, 2026-10-19: "BDPv4 requires all unit tests be green." and "There should be ZERO platform specific UNIT tests.  If it depends on the environment, it's an integration test." Plan: `docs/plans/2026-10-19-unit-suite-green.md` (approved 2026-10-19 14:50; amendments in sections 7 to 9, 11 and 17 to 27; execution record in sections 10, 12 and 14 to 27; decisions in section 13; D-B in 27.21 as amended by 27.22 and 27.23). Handoff: `docs/plans/2026-10-30-unit-suite-green-handoff.md` (its top section supersedes the rest).

Draft until hostile validation round 11 (Codex CLI, gpt-6-astra, max effort, on the merged OMARCHY worktree only) returns AGREE at 98/98. No done state is claimed: every new or changed criterion is unsatisfied and all 40 tasks of IMPL-UNITGREEN-001 are unchecked.

### Commits (over develop; this branch's base was `3da2db93`, and develop `9b099716` is merged in docs only)

Through round 8b (plan sections 1 to 25):

- `cbc74e82`: System.Security.Cryptography.Xml and Pkcs pinned to the advisory-patched 10.0.13 (TR-MCP-QUALITY-001 ac-21).
- `28bf0b22`, `de581353`: canonical workspace-key tests, provider traits, and the host-aware `WorkspaceIdentityPath.ToNativePath` overload (FR, TR and TEST-MCP-PATHNORM-001; additive public API).
- `a4c358ef`: narrowing to leave PLAN-C4MODEL-001's fixes to that effort (superseded by `11681680`).
- `b2319488`: the HostileReviewWorker startup test no longer depends on Event Log write access (TR-MCP-HOSTILEREVIEW-003).
- `11681680`: takeover of PLAN-C4MODEL-001's unit-suite code and Linux provider provisioning (TR-MCP-NUKE-018), blob-identical to `c2125c42`, by operator decision.
- `8c2b83ab`: host-scoped MigrationIntegrationTests lane (TR-MCP-NUKE-019).
- `8a617334`: merge of develop with PR 86.
- `b2bd50b3`: platform-dependent tests classified as integration tests, with the unit-suite guard and a strengthened skip guard. Its first guard run, `unit-green-guard-mock-20261019T1720`, was red (4692 results, 1 failed, 16 not executed); see plan section 10.
- `4f668b76`: the plan and the PR 82 handoff correction.
- `59249260`: the interactive desktop-spawner case fails loudly as a Windows integration test.
- `69d8f3f5`: amendment H1, under which the Windows integration lane refuses a non-interactive session before any test.
- `12746834`: the invalid-handle final-path case runs only on Windows, without a silent return.
- `83417029`, `6b4295b7`: docs only (the plan as built, the code-head receipts, the 19:41 decisions).
- `49cfec92`: hostile validation round 1, findings F3 and F5 (one skip definition for both guards; interpolation holes kept as code).
- `b4ad42aa`: round 1 finding F2 (36 native-boundary and tracked-file cases move to Integration, assertions unchanged).
- `a94579db`: docs only (the round 1 receipts and record, the round 2 gate receipts).
- `45a3197c`: round 2 finding R2-F2. `UnitSuitePlatformScanner` reads Roslyn syntax trees instead of regex lines; the five central Roslyn pins move from 5.3.0 to 5.9.0 (operator: "Use the latest Roslyn tools!").
- `27c43bcc`: round 2 finding R2-F1. One repository-root locator, `tests/Shared/TestRepositoryRoot.cs`, refuses outside `Category=Integration` (operator: "Looks like an opportunity to DRY"); 34 locator copies delegate to it; the tests it refused, plus link-creating and host-state tests, move to Integration with assertions unchanged; Repl.Core.Tests and QBAgent.Tests join MigrationIntegrationTests.
- `538d0ccf`: the user-profile scan fix (operator: "Fix scans in PR 87 (Recommended)"; this PR carries the fix for `triage-report-b0e41c36f93c4c91b1bf2524eb579ef8`). `DirectoryTreeWalker` (QBrainAi.Services) serves the marker plugin-cache scan and the session-log history scan: no directory link or other reparse point is followed, and a directory whose listing throws an IO or access error is skipped. The 12 import-path tests are tagged Integration.
- `56d14d7c`: a race in the new hermetic-profile check, found by the pre-HV what-if pass, fixed.
- `6042dbb3`, `0210244d`, `68fa7b39`: the LocalDB flake (operator: "Fix the flake in PR 87 (Recommended)", then "Serialize SQL Server tests (Recommended)"): one LocalDB default with a 120 s connect timeout, and the four classes that open LocalDB in one collection defined with `DisableParallelization = true`.
- `6ea425ab`: the Windows lane's lost results (operator: "Diagnose the loss in PR 87"): pg_ctl start and stop run with `-s`, so pg_ctl no longer writes into the xUnit v3 message stream.
- `395b4cd8`, `6b89998f`, `64fc4b14`: docs only (the round 2 HV receipts, the round 3 gate receipts and plan record, the round 3 pre-launch records).
- `e0b078c8`: pre-HV problem R3-P3 (operator: "Fold R3-P3 in first"): a plugin version file that exists but cannot be read no longer fails the marker.
- `0996a116`: docs only, the hostile validation round 3 receipts as received.
- `801e6862`: round 3 findings R3-F1 and R3-F2 (operator: "Structural fix, round 4 (Recommended)"). One source seam, `AgentPluginVersionSources`, holds every input marker plugin-version resolution reads; the unit-test assembly fixture installs test-owned sources and resolution tests pass explicit sources.
- `c31c2918`: docs only, round 3 finding R3-F4.
- `26fc8348`: the two red results at `801e6862`. `HandoffDurabilityTests` gives each context its own connection to one temporary database file (operator: "Fix the race in PR 87 (Recommended)"; `triage-report-9b96ada18d72483dad2a3113e455c01b`, closed by this PR); the process sources and sibling rule moved back into `MarkerFileService`, so the `Path.GetFullPath` ratchet (TR-MCP-FED-PATH-001) passes again.
- `50b58ce5`: docs only, the round 4 run receipts through `26fc8348`.
- `97b78114`: the siblings of the race class, `HandoffAuditDefectTests` and `MemoryS1Harness`, get the same fix (operator: "Extend fix, then round 4 (Recommended)").
- `b0db5b09`, `094e47f4`: docs only (round 4 records and receipts).
- `8c254695`: round 4 finding R4-F3. The memory harness binding cites FR-MCP-MEMORY-002, TR-MCP-MEMORY-002 and TEST-MCP-MEMORY-002, present and mapped. Documentation comment only.
- `ccc78b7a`: round 4 finding R4-F1, as the operator decided (see "Scanner: operator override"). Documentation comments only.
- `3a9f8ff4`, `5b818847`: docs only (round 5 run receipts and plan section 18).
- `810ee8aa`: `McpDatabaseEncryptionTransitionCommandTests.TryParse_UsesEnvironmentInstance_WhenCliInstanceIsOmitted` is `Category=Integration` and clears `QBRAINAI_INSTANCE` for its duration; assertions unchanged; test-only.
- `8b7a49ea`, `780b6513`, `c3625a64`: docs only (round 5 records and receipts).
- `9933796c`: round 5 finding R5-F1 (operator: "Fix both, strengthen ACs"). `DirectoryTreeWalker.IsReachable` requires the scan root and every folder between the profile or home and it to be a directory that is not a link. Mock gate `ug-r6-reach-mockgate-1` 4536/0/0 before any production change.
- `c8418301`: round 5 finding R5-F2 and the environment sweep (operator: "Approve, strengthen AC-006/008 (Recommended)"), test-only. 13 unit tests that read or write the process environment move to `Category=Integration` with every variable controlled and restored; the writers run in one serialized collection (`ProcessEnvironmentCollection`).
- `44e9d810`, `3ce97b3e`, `3a607fb1`: docs only (round 6 records and receipts).
- `6a87791d`, `04841148`: round 6 findings R6-F1 and R6-F2 (operator: "Strengthen AC, move 14 (Recommended)"), test-only: the 14 CLI temp-folder tests run inside `TestSupport/CliTempVariables`; absent requirement ids on lines this branch adds are labeled historical.
- `c31f36d8`, `55c1400c`, `8b97aa4b`, `96b81bef`: docs only (round 7 records and receipts).
- `f5cda3c1`: round 7 finding R7-F1 (operator: "Fix source, strengthen AC (Recommended)"), a production fix: `ReplCommandTimeout` is the one parse rule for the two REPL timeout variables.
- `7ab3c1c4`, `845f323b`: the Windows junction flake (operator: "Fix flake, strengthen AC (Recommended)"), test-only: `TestSupport/WindowsJunction`.
- `ccd9e91f`: the REPL timeout rule at its two call sites through `ReplCommandTimeout.Read` and an internal `ReadVariable` seam.
- `43cb62a7`, `868bbb57`, `2175d827`: docs only (round 8 records; the stopped round 8's receipts as received).
- `8c651f13`, `d5a23364`, `6667c405`, `2f679213`, `4fab4424`, `3e487b6b`: plan section 22 (slices A, B, C), the REPL integration defects, streamed provisioning downloads, and the marker walk cases as unit tests over an in-memory file system.
- `751e34e4`, `433ce191`, `4634c73c`, `d43c1d12`, `fbc2da86`, `a69edb61`: docs, documentation comments, and the docs-only merge of develop `9b099716` (99 files, all under `docs/`).
- `a78640ba`: pre-HV problem R8b-P18, documentation comment only; the code head round 8b reviewed.
- `2b0f65d8`, `366238a2`, `a9d6a8d7`, `332e0d31`, `00399174`: docs only (round 8b records, its three attempts, and the round 8b receipts as received, DISAGREE 96/95).

Round 9 (plan sections 26 and 27 to 27.21):

- `0dd224da`, `fb19eeab`: docs only (the handoff at the 2026-10-30 pause).
- `765490a9` (committed by Codex, gpt-6.1-sol; Claude wrote slices D to H, K and L, and Codex wrote I, J, M, N and O): the round 8b fixes of plan section 26 (slice D, the explicit-baseline environment delta for R8B-F1; slice E, the owned-instance environment for R8B-F3; slice F, the two Support.Mcp.IntegrationTests classes in the lane for R8B-F2; slice G and the sibling sweep), slice H (the FR-MCP-QBOPENAI-001 ac-5/ac-6 port, operator: "Port ac-5/ac-6 into PR 87") and the holistic-review slices I to O (27.2 to 27.16), each criterion first, contract and mock gate before the code it binds. 470 paths, 247 outside `docs/`. The section 26, H, K and L runs and four of Codex's runs had no durable receipt until plan 27.21 wrote them retrospectively from each run's own files.
- `679eb788` (Codex): docs and run evidence (the handoff, the incomplete Linux lane receipt and its five TRX), plus the two lane-rewritten benchmark files (restored in `611a5dc5`).
- `611a5dc5`: slices Q (Nuke target TestGroups, TR-MCP-NUKE-020 AC-1 to AC-5), R (owned-instance provider key, FR-MCP-TESTRESOURCE-001 ac-4; the GitHubCliService GetFullPath ratchet) and P (stored requirement times, FR-MCP-040 ac-1), the traceability reconciliation, the benchmark files restored to develop. 63 paths.
- `62c5882d`: dated XML change logs per MEMORY-FACT-008 on every member and test class slices Q, R and P changed (documentation only), and the final2 receipts. 38 paths.
- `23955af0`: docs only (plan 27.20, the gates at `62c5882d`, the HV TRX set at `62c5882d`, now history).
- `878c366f`: slice S (plan 27.21), test and test-support only: `TemporaryInstanceChecks.CheckTestOutputFolder` (FR-MCP-TESTRESOURCE-001, the criterion on generated test artifacts); the two memory-bench stub cases write to a checked temporary folder, so a lane run no longer rewrites `docs/benchmarks/results`; the offset-less stored-time row for FR-MCP-040 ac-1 ("UTC"). The code head round 9 reviewed.
- `46270931`, `e3eb8369`, `a9825017`: docs only (the gates and HV TRX evidence at `878c366f`, plan 27.21, the retrospective receipts, the round 9 prelaunch records, and the round 9 receipts as received, DISAGREE 95/94).

Round 10 (plan 27.22):

- `6082b116`: slice T, the round 9 findings (operator: "Strengthen 5 ACs, fix all 8 (Recommended)"). `TestGroupExecution.CheckClasses` counts a class only through an executed result joined to its definition by test id (R9-F1, TR-MCP-NUKE-020 AC-5); `TemporaryInstanceChecks.CheckHealthAnswer` compares the JSON nonce field with the whole nonce (R9-F2, FR-MCP-TESTRESOURCE-001 ac-12); the generated-artifact and owned-root checks compare path segments under the path's own syntax (R9-F3 and its sibling, the criterion on generated test artifacts and ac-4); `RequirementStoredTimestamp.Read` never fills a missing date or year from the clock (R9-F4, FR-MCP-040 ac-1); one model rule, `build/QuadBrainOllamaModel.cs`, used by InstallOllama and the QuadBrain cases (R9-F5, TR-MCP-NUKE-016 AC-001); XML documentation on the 38 undocumented members of slices Q, R, P and S (R9-F8). The code head round 10 reviewed.
- `03df5a6a`, `4e4a227a`: docs only (the gates and HV TRX evidence at `6082b116`, plan 27.22, the round 10 prelaunch records, and the round 10 receipts as received, DISAGREE 96/95).

Round 11 (plan 27.23), fixed between rounds under the operator's "don't gate on me between rounds":

- `2e121015`: slice U, the round 10 findings. `RequirementStoredTimestamp.Read` accepts only exact formats that state the year, the month and the day (R10-F1, FR-MCP-040 ac-1); `TemporaryInstanceChecks` parses an explicit path root (the POSIX root, a drive letter, or a UNC server and share with either separator) and its containment and equality require the same root (R10-F2, FR-MCP-TESTRESOURCE-001 ac-4 and the criterion on generated test artifacts); a test-owned `ScriptedClock` feeds the session-state fake and the auth stub, and six unit cases assert exact scripted times with no sleep (R10-F3, TR-MCP-USECASE-018 AC-006). Each criterion first through the plugin, contracts and wrong implementations, mock gate `ug-r11-mockgate-u-1`, binding `ug-r11-binding-u-1`. 16 paths.
- `cb174ac8`: slice V, the R10-F3 siblings my first sweep missed and stale slice U documentation, found in the holistic read before round 11. TR-MCP-USECASE-018 AC-006 appended first (every recorded time a unit test checks, including service-stamped creation and update times). `GraphRagService` takes an optional `TimeProvider` and `MemoryService` gains a constructor that takes one (the system clock otherwise); `GraphRagEntityCrudTests` (two cases) and `MemoryModelTests.Timestamps_CreateAndUpdate` assert exact scripted times through the new `StampedTimeContractTests` with no live-clock range and no sleep; two `AuthRotationTests` cases and `GraphRagEntityStorageTests.PersistsEntityWithAllFields` use fixed instants; the `TemporaryInstanceChecks` summaries state the slice U root rule and no longer cite the removed `SegmentComparison` (documentation only). Mock gate `ug-r11-mockgate-v-1`, binding `ug-r11-binding-v-1`, sweep receipt `ug-r11-clock-sweep.txt` (history since slice W). 12 paths.
- `1f51d4b2`: slice W, found in the holistic read before round 11: TR-MCP-USECASE-018 AC-006 also says a unit test asserts no wall-clock bound, which my slice V record had read only against its recorded-time sentence. AC-006 appended first (an assertion whose outcome depends on how much real time passes is an integration test; a hang-guard timeout is not a wall-clock bound). Tests only: `HandoffDurabilityTests.ProcessingLease_RenewsAndFencesTerminalUpdates` waits for the renewed lease expiry and asserts it exactly instead of sleeping 250 ms and asserting "later"; `QBAgentHeartbeatOverlayTests.RunAsync_Heartbeat_RotatesGlyphWithoutStillWorkingLines` and `SessionLogFileWatcherTests.WhenFileCreatedThenWatcherTriggersImport` are `Category=Integration`, assertions unchanged. Binding `ug-r11-binding-w-1`; timing sweep receipt `ug-r11-timing-sweep.txt` (every timer-bounded wait and recorded-time comparison in the unit projects, mapped to the unit TRX and classified). 3 paths. The last code, test or build change.
- Docs only after it: `142a93f0` (the gates and HV TRX evidence at `1f51d4b2`, plan 27.23, the receipts of slices U, V and W, the round 11 prelaunch records). `git diff --name-only 1f51d4b2..HEAD` lists only `docs/` paths (D-A).

### Gates at `1f51d4b2` (the last code head), all green

- Nuke Test, Linux `ug-r11w-final-linux-unit-1`: 4733/0/0, 0 NU190x. Windows `ug-r11w-final-win-unit-1` (over SSH as kingd, child branch at `1f51d4b2`): 4733/0/0, ValidateTraceability passed, 0 NU190x. By name, each host lost exactly the two cases slice W moved, and the two hosts hold the same 4733 names.
- MigrationIntegrationTests, Linux `ug-r11w-final-linux-lane-1`: 741/0/0, 7 TRX. Windows `ug-r11w-final-win-lane-1` (kingd's interactive session, proof head `1f51d4b2`): 718/0/0, 7 TRX. By name, each lane holds every name of its slice V lane and the two moved cases.
- Build.Tests (infrastructure, raw dotnet test), Linux `ug-r11w-final-linux-buildtests-1` (both categories, Platform!=Windows): 529/0/0. Windows `ug-r11w-final-win-buildtests-1` (the cases the Windows lane does not run): 433/0/0. Both the same names as round 10.
- ValidateTraceability on Linux `ug-r11w-final-linux-traceability-1`: passed. 0 NU190x in the Linux gate logs.
- Receipts in `docs/receipts/unit-suite-green-20261019/` (`ug-r11w-*`); the HV TRX set is `docs/receipts/hv/unitgreen-r11-trx-1f51d4b2/` (30 TRX, manifest SHA-256 `1AEED77D...`).

The gates at `cb174ac8` (both Test runs 4735/0/0, Linux lane 739/0/0, Windows lane 716/0/0), at `2e121015` (both Test runs 4724/0/0, Linux lane 739/0/0, Windows lane 716/0/0) and at `6082b116` and earlier heads are history (plan 27.23 and earlier).

### Operator rulings (plan 27.19 to 27.23)

- Reviewer: Codex CLI, gpt-6-astra, max effort, run only on the merged OMARCHY worktree, never on Windows.
- "Use dedicated targets or an integration target that accepts an array of tests groups to run." (TestGroups, TR-MCP-NUKE-020); "This HAS to be fixed." (stored requirement times, FR-MCP-040 ac-1); Windows-only work on a child branch (`fix/unit-suite-green-20261019-windows`, which holds no commit of its own); "Do not stop on triage failures. Ensure the failsafe was written, then move on."
- On the round 9 projected problems: a stored time without an offset reads as UTC ("UTC"); the memory-bench rewrite and the deployed server's read-time createdAt are fixed in scope ("Fix in scope"; the second by a post-merge deployment, "Deploy after merge, verify live (Recommended)"); out of scope, stated explicitly: the Azure-path wiki export rejection, the held-out integration projects, the hook session submits landing in failsafe (triaged), the plugin's Invoke-CodeVerify false success (triaged), and SQL error 1223 on releasing the EF migration lock.
- FR-MCP-USECASE-016/017: "They are coming back because they keep getting suggested as the recommended action.  Figure out what should actually be done and do it." A requirement delete is a soft delete that any create revives, so the remedy is the reference, not another delete: TODO BUG-TRIAGE-139's dangling links repointed to FR-MCP-USECASE-001 and TR-MCP-USECASE-019; retirement notices in the notes of TR-MCP-USECASE-018 and -019; Workspace memory MEMORY-PROCESS-014. That the link prompted the earlier restorations is an inference.
- New TODO PLAN-LINKVALIDATOR-001 (a validator for FR, TR, AC, UC and TODO links to deleted or missing items); not work of this PR.
- On round 9's findings: "Strengthen 5 ACs, fix all 8 (Recommended)" (slice T). Six criteria were written against the option's five: the sixth, FR-MCP-TESTRESOURCE-001 ac-4, is the R9-F3 sibling the sweep found. It was written at 21:45, before I asked, and the operator approved it afterwards ("Approvef", 2026-11-02T22:52:56, verbatim).
- 2026-11-03: "Confirmed, launch (Recommended)" (the round 10 projected problems); "don't gate on me between rounds" (06:13: round 10's findings and slice V were fixed under the standing rules, and round 11 launches after its projected problems are shown and committed, without a confirmation stop); "Timestamps are not optional." (06:14); "You should pause and wait until 30 minutes before HV eta estimate to conserve usage." (07:29); "If you are getting full HV via codex, ignore review comments on PR." (09:59: review comments on this PR are not read, fixed or cited; findings come only from the HV verdict).

### Scanner: operator override

- Operator, 2026-10-21T06:06:46: "I don't want any more scanner work.  None.  Add an operator override to the HV." The scanner alias work is TODO `IMPL-SCANNERALIAS-001`, open with no work scheduled.

### The hostile-environment runs: out of scope

- Operator, 2026-10-27: "Get ride of the hostile runs enforcing integration test separation.  I have already stated this once." and "Which I sated last night were out of scope." No exit gate or HV evidence carries a hostile run.

### Acceptance criteria define unit tests

- Operator, 2026-10-27: "Acceptance Criteria are for defining UNIT TESTS that must be covered as TDD during development.  That inherently excludes ENVIRONMENT specific criteria." Operator, 2026-10-29/30: "AC should primarily exist on FR." and "Move criteria that are misplaced.  Unit tests are for FR, Integration tests are for TR." (plan 27.1 moved TR-MCP-REPL-001 AC-002, TR-MCP-REPL-008 AC-1 to AC-4, AC-TR-MCP-WS-005-001 and the unit half of AC-TR-MCP-SESSIONLOG-006-009 to their FRs).

### Hostile validation

- Rounds 1 to 7: DISAGREE each (95/93, 96/94, 97/96, 97/97, 97/96, 97/95, 97/96). Round 8 stopped under policy 7.9 with no verdict. Round 8b (`unitgreen-r8b`): DISAGREE, accuracy 96, completeness 95, findings R8B-F1 to R8B-F3 (plan section 26). Round 9 (`unitgreen-r9`): DISAGREE, accuracy 95, completeness 94, findings R9-F1 to R9-F8 (27.22). Round 10 (`unitgreen-r10`): DISAGREE, accuracy 96, completeness 95, findings R10-F1 to R10-F3, all implementer edits (27.23).
- Round 11 (`unitgreen-r11`): the request supplies the TRX of every unit project and of both lanes and both Build.Tests runs at `1f51d4b2` (policy 7.9) and carries MEMORY-PROCESS-013 verbatim. It launches after its projected problems are shown and committed (`docs/receipts/hv/unitgreen-r11-prelaunch.projected-problems.md`).

### Requirements and D-B (plan 27.21 as amended by 27.22 and 27.23, the one statement)

- After AGREE at 98/98 or better, set satisfied through the plugin, each read back, only where the gates and slice receipts prove the whole criterion: TR-MCP-NUKE-019 AC-1, AC-2, AC-3 and AC-6; FR-MCP-REPL-001 ac-1; FR-MCP-REPL-007 ac-1 to ac-4; TR-MCP-QUALITY-001 ac-21; TEST-MCP-PATHNORM-001 `unitgreen-native-host`; TR-MCP-USECASE-018 AC-006 (as appended in slices U, V and W); TR-MCP-USECASE-019 AC-008; FR-MCP-018 ac-1; AC-FR-MCP-SESSIONLOGCTX-001-008; FR-MCP-TESTRESOURCE-001 ac-4 to ac-12 and its criterion on generated test artifacts; FR-MCP-FED-PATH-001 AC5; FR-MCP-047 ac-1; AC-TR-MCP-WS-005-002; AC-TR-MCP-SESSIONLOG-006-010; FR-MCP-REPL-008 ac-4; TR-MCP-NUKE-020 AC-1 to AC-5; and, for the ac-5/ac-6 port this PR carries ("Port ac-5/ac-6 into PR 87"), FR-MCP-QBOPENAI-001 ac-5, ac-6 (strengthened 2026-11-02, operator: "Strengthen ac-6 (Recommended)": output that formats to nothing defers to ac-7) and ac-7, TEST-MCP-QBOPENAI-001 AC-001 and TEST-MCP-QBOLLAMA-001 AC-001 to AC-003.
- Not set: TEST-MCP-QBOLLAMA-001 AC-004 (names a lab host); TR-MCP-NUKE-016 AC-001 (the Build.Tests cases its earlier text names are not in this branch; slice T proves only its appended model rule); FR-MCP-040 ac-1 until the post-merge deployment and a live read show the stored createdAt; removed criteria.
- Then all 40 tasks of IMPL-UNITGREEN-001 are checked; it is marked done after the merge, the deployment and the live read. IMPL-SCANNERALIAS-001, IMPL-TXNINTEGRATION-001, PLAN-AZURERETIRE-001 and PLAN-LINKVALIDATOR-001 stay open.

### Open and declared (plan 27.20 to 27.23)

- The held-out integration projects and cases (out of scope by ruling; IMPL-TXNINTEGRATION-001).
- The deployed server and REPL tool report read-time createdAt until the post-merge deployment.
- The packaged QBrainAI.Repl.Core interfaces IFrItem, ITrItem, ITestItem and IMappingItem change CreatedAt/UpdatedAt from string to string? (binary compatible): a behavior change, null where nothing is stored, not breaking. The client models gain `createdAtUtc`/`updatedAtUtc` (additive).
- Slice T behavior changes: the QuadBrain cases fail when the selected Ollama model is not installed, instead of using the first installed one; two binding facts moved, unchanged, to `TemporaryInstanceChecksBindingTests`, so their test names change class.
- Slice U behavior changes: a stored requirement time whose text matches none of the exact complete-date formats reads as null (for example a space-separated date and time without seconds; the store writes round-trip text); the test-support containment treats a single-rooted path, another UNC share or server and another drive as different roots; the session-state fake and the auth stub use a scripted stepping clock by default; one case is renamed (`CurrentSelection_HasTimestamp_ReturnsRecentTimestamp` to `...ReturnsScriptedTimestamp`).
- Slice V changes: an additive public constructor on `MemoryService` and an optional constructor argument on the internal `GraphRagService`, with behavior unchanged on the system clock; the relationship stamps and the memory removal time read the same clock but no unit test checks them yet; two assertions are stronger (exact times instead of "not earlier" and instead of "present"); the two `AuthRotationTests` cases assert the values their NSubstitute doubles were set to return.
- Slice W changes: two unit cases move to the lanes (4735 to 4733 unit cases on both hosts, by name exactly those two); the lease case asserts the exact renewed expiry. The unit cases that keep a timer (hang guards far above the awaited step, cancellation waits, waits that prove an order, fake latency) are each classified in `ug-r11-timing-sweep.txt` under AC-006 as appended at 10:22:56, which the operator may revert.
- `ScriptedClock` (Repl.Core.Tests) and `ScriptedTimeProvider` (Support.Mcp.Tests) script times in two test projects that share no test-support link; the duplication is declared, not merged.
- The Ollama model variables stay provisioning inputs the QuadBrain cases read through the shared rule, not values the cases set and restore.
- Lenient date parses with the R9-F4 shape in code this PR does not change (SessionLogWorkflow, TodoWorkflow, YamlSerializer, ReplCommandDispatcher, the transcript adapters) are out of scope.
- Product behavior and configuration changes of rounds 9 to 11 (plan section 6):
  - The REPL host reads its verified-marker cache folder from `QBRAINAI_REPL_CACHE_DIR`, then `MCP_REPL_CACHE_DIR`, then the profile cache (FR-MCP-REPL-008 ac-4). These are new configuration inputs, and the public `MarkerFileClientOptionsResolver.ResolveVerifiedCacheDirectory` is an additive API.
  - For a workspace of the other platform, GitHub CLI calls run with `--repo` from `Mcp:GitHub:Repository` and no working directory, and are refused with a logged message when no repository is configured (FR-MCP-FED-PATH-001 AC5).
  - The OpenAI-compatible QuadBrain endpoint gives a no-tools non-envelope reply after one round, and on a spent budget the last Arbiter output, formatted, or the plain budget message (FR-MCP-QBOPENAI-001 ac-5 to ac-7).
  - The scan reachability check compares lexically normalized paths under the boundary's own path syntax.
  - The owned-root containment of the test support accepts a Windows case alias of the owned root and requires the same root.
  - Stored requirement times read only from complete-date formats; the GraphRAG and memory services take an optional clock.
- Build.Tests classes run inside the Nuke lane and TestGroups although the operator ruled on 2026-10-11 that Nuke does not run its own tests; both TRs are operator approved.
- TestGroups ran live on Linux only; on Windows its composition and checks are proven by the Build.Tests rows.
- TR-MCP-FED-PATH-001 has no criterion for the no-direct-GetFullPath rule.
- FR-MCP-SERVICEUPDATE-001's criteria have empty ids (triage d88dc14a).
- The four identifier-scrub assertions stay as flagged lab-bound exceptions; the 417 heuristic physical-I/O candidates stay out of scope.
- The failed binding `ug-r9-binding-r-1` and the incomplete lane `ug-r9-final-linux-lane-1` are history, not evidence.
- MEMORY-FACT-008's dated change logs cover slices Q, R, P, S, T, U, V and W; earlier PR 87 code is not retrofitted (the memory: not "an unrelated retrospective sweep").
- The items of plan 25.5 that still stand.

### Information, not a gate

- Operator, 2026-10-21T06:54:47: "Azure is no longer in the picture.  Create a global memory not to gate on azure related errors." The requirements wiki export's Azure-specific cleanup path fails (`triage-report-a6927fca10364155ab9992506e1abc9c`); the projections were regenerated through the plugin's projection export instead.
- Windows evidence runs over SSH as kingd; the Windows TRX copied from the read-only mount carry file mode 100755 (content and hashes equal to the originals).

Signed: claude-opus-5-5-xhigh@ClaudeCode | Confidence: correctness 94%, accuracy 95%, completeness 93%

🤖 Generated with [Claude Code](https://claude.com/claude-code)

