## IMPL-UNITGREEN-001: the unit suite green on both lab hosts (BDPv4: 0 failed, 0 skipped)

Operator, 2026-10-19: "BDPv4 requires all unit tests be green." and "There should be ZERO platform specific UNIT tests.  If it depends on the environment, it's an integration test." Plan: `docs/plans/2026-10-19-unit-suite-green.md` (approved 14:50; amendments in sections 7 to 9, 11 and 17 to 25; execution record in sections 10, 12 and 14 to 25; decisions in section 13).

Draft until hostile validation (Codex gpt-6-astra max) returns AGREE at 98/98. No done state is claimed: every new or changed criterion is unsatisfied and every TODO task is unchecked.

### Commits (over develop; this branch's base was `3da2db93`, and develop `9b099716` is merged in docs only)

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
- `6ea425ab`: the Windows lane's lost results (operator: "Diagnose the loss in PR 87"): pg_ctl start and stop run with `-s`, so pg_ctl no longer writes into the xUnit v3 message stream (see "Lost Windows lane results" below).
- `395b4cd8`, `6b89998f`, `64fc4b14`: docs only (the round 2 HV receipts, the round 3 gate receipts and plan record, the round 3 pre-launch records).
- `e0b078c8`: pre-HV problem R3-P3 (operator: "Fold R3-P3 in first"): a plugin version file that exists but cannot be read no longer fails the marker.
- `0996a116`: docs only, the hostile validation round 3 receipts as received.
- `801e6862`: round 3 findings R3-F1 and R3-F2 (operator: "Structural fix, round 4 (Recommended)"). One source seam, `AgentPluginVersionSources`, holds every input marker plugin-version resolution reads; the unit-test assembly fixture installs test-owned sources and resolution tests pass explicit sources. The Roslyn scanner resolves aliases and static imports in their lexical scope, within the limit declared below.
- `c31c2918`: docs only, round 3 finding R3-F4 (the round 3 self-review sentence corrected in both copies).
- `26fc8348`: the two red results at `801e6862`. `HandoffDurabilityTests` gives each context its own connection to one temporary database file (operator: "Fix the race in PR 87 (Recommended)"; `triage-report-9b96ada18d72483dad2a3113e455c01b`, closed by this PR); the process sources and sibling rule moved back into `MarkerFileService`, so the `Path.GetFullPath` ratchet (TR-MCP-FED-PATH-001) passes again.
- `50b58ce5`: docs only, the round 4 run receipts through `26fc8348`.
- `97b78114`: the siblings of the race class found by the pre-HV sweep, `HandoffAuditDefectTests` and `MemoryS1Harness`, get the same fix (operator: "Extend fix, then round 4 (Recommended)").
- `b0db5b09`, `094e47f4`: docs only (the plan's round 4 record, the receipts at `97b78114`, the round 4 pre-launch records; the round 4 HV receipts as received).
- `8c254695`: round 4 finding R4-F3. The memory harness binding cites FR-MCP-MEMORY-002, TR-MCP-MEMORY-002 and TEST-MCP-MEMORY-002, present and mapped, instead of TEST-MCP-MEMORY-010, which neither lab store holds. Documentation comment only.
- `ccc78b7a`: round 4 finding R4-F1, as the operator decided (see "Declared scanner limit"). The scanner's own documentation states the limit. Documentation comments only.
- `3a9f8ff4`, `5b818847`: docs only (the round 5 run receipts, plan section 18, the corrected round 4 copies, the console logs and the Windows dry-run receipt).
- `810ee8aa`: `McpDatabaseEncryptionTransitionCommandTests.TryParse_UsesEnvironmentInstance_WhenCliInstanceIsOmitted` set only `MCP_INSTANCE` while `QBRAINAI_INSTANCE` wins; it is now `Category=Integration` and clears `QBRAINAI_INSTANCE` for its duration; assertions unchanged; test-only.
- `8b7a49ea`, `780b6513`, `c3625a64`: docs only (the round 5 run receipts and plan section 18; the round 5 prelaunch records; the round 5 HV receipts as received).
- `9933796c`: round 5 finding R5-F1 (operator: "Fix both, strengthen ACs"). `DirectoryTreeWalker.IsReachable` requires the scan root and every folder between the profile or home and it to be a directory that is not a link; it is checked before walking each of the seven scan roots. A profile or home that is itself a link is still scanned. Tests first: 16 scripted cases with seven wrong-check negative controls, mock gate `ug-r6-reach-mockgate-1` 4536/0/0 before any production change; then the binding, resolve-level cases and a real-link integration test in both lanes.
- `c8418301`: round 5 finding R5-F2 and the environment sweep (operator: "Approve, strengthen AC-006/008 (Recommended)"), test-only. 13 unit tests that read or write the process environment, or import it, move to `Category=Integration` with every variable they depend on controlled and restored and assertions unchanged; the classes that write the environment run in one serialized collection (`ProcessEnvironmentCollection`, `DisableParallelization = true`).
- `44e9d810`, `3ce97b3e`, `3a607fb1`: docs only (the round 6 run receipts and plan section 19; the round 6 prelaunch records; the round 6 HV receipts as received).
- `6a87791d`: round 6 finding R6-F1 (operator: "Strengthen AC, move 14 (Recommended)"), test-only. 14 unit tests reached production temp-folder code that reads the CLI temp variables and creates the folder. They are now `Category=Integration` and run inside one shared helper, `TestSupport/CliTempVariables`; assertions unchanged. Mock gate `ug-r7-clitemp-mockgate-1` 4563/0/0 before the helper existed. Round 6 finding R6-F2: the requirement ids on the test lines this branch adds that are not in the store are labeled historical and make no active coverage claim.
- `04841148`: the helper fix, test-only: the name each strategy reads first points at the test folder and every other name at a separate fallback folder; a name outside the chain is refused. Mock gate `ug-r7-chain-mockgate-1` 4553/0/0 before the helper changed.
- `c31f36d8`, `55c1400c`, `8b97aa4b`, `96b81bef`: docs only (the round 7 run receipts, the gate receipts at `c31f36d8` with the script-checking ledger and plan section 20, the round 7 prelaunch records, and the round 7 HV receipts as received).
- `f5cda3c1`: round 7 finding R7-F1 (operator: "Fix source, strengthen AC (Recommended)"), a production fix. `ReplCommandTimeout` (QBrainAi.Repl.Core) is the one parse rule for `MCPSERVER_REPL_COMMAND_TIMEOUT_SECONDS` and `MCPSERVER_REPL_STREAM_COMMAND_TIMEOUT_SECONDS`: a whole number of seconds from 1 to 4294967 is used; anything else gives the two-minute default. Mock gate `ug-r8-timeout-mockgate-1` 4554/0/0 before any production change; binding `ug-r8-timeout-binding-1` 4555/0/0.
- `7ab3c1c4`, `845f323b`: the Windows junction flake (operator: "Fix flake, strengthen AC (Recommended)"), test-only: `TestSupport/WindowsJunction` makes the junction in the test process; mock gate `ug-r8-junction-mockgate-1` 4557/0/0; the sibling `BugTriage139EighteenthTraversalTests` uses it too.
- `ccd9e91f`: TR-MCP-REPL-001 AC-002 at its call sites: both call sites read their variable once through `ReplCommandTimeout.Read` and an internal `ReadVariable` seam; mock gate `ug-r8-callsite-mockgate-1` 4561/0/0 before the seam; binding `ug-r8-callsite-binding-1` 4563/0/0.
- `43cb62a7`, `868bbb57`, `2175d827`: docs only (the round 8 run receipts and plan section 21; the round 8 prelaunch records; the stopped round 8's receipts as received).
- `8c651f13`: plan section 22, slice C (operator: "Nuke target, AC first (Recommended)", "Lane runs them (Recommended)", "Full lane now, all ACs (Recommended)"), test-only. The REPL integration tests run on a temporary instance they own (`tests/TestSupport.Shared/ScratchQBrainAi.cs`, checked by `TemporaryInstanceChecks`), never the live server or marker; the 12 probes that caught `HttpRequestException` and passed now assert (TR-MCP-NUKE-019 AC-6).
- `d5a23364`: slice B, build code: InstallOllama works on both hosts through `OllamaInstallLayout` and is a lane dependency. Mock gate `ug-r8b-ollama-mockgate-1` 447/0/0; binding 449/0/0.
- `6667c405`: slice A, build code: MigrationIntegrationTests selects its six projects through `IntegrationLaneProjects` (TR-MCP-NUKE-019 AC-2, narrowed by "Repl only; rest waits (Recommended)"). Mock gate 10/0/0 with nine faults rejected; binding 13/0/0.
- `2f679213`: the four REPL integration defects the first expanded lane found (the port race, an unregistered workspace header, two separator-document loops), test-only; restart contract mock gate `ug-r8b-restart-mockgate-2` 4578/0/0.
- `4fab4424`: provisioning downloads stream with no total time limit and a two-minute inactivity limit (the Windows lane at `2f679213` stopped in InstallOllama on the 1.6 GB zip). Mock gate 6/6 with five faults rejected; binding 8/8.
- `3e487b6b`: the marker walk cases run as unit tests over an in-memory file system (operator: "Unit test, mocked file system (Recommended)" and "That's not a defect.  Name each case."; TR-MCP-REPL-008 AC-1 to AC-4). Mock gate `ug-r8b-markerwalk-mockgate-1` 4588/0/0 with eight faults rejected; binding 4592/0/0.
- `751e34e4`: docs only, the round 8b run receipts.
- `433ce191`: documentation comments only in 25 build and test files, citing each TR without the criteria removed in plan section 24.
- Docs only after it: `4634c73c` (the gate receipts at `433ce191` and plan sections 22.8 to 24), `d43c1d12` (the merge of develop `9b099716`, 99 files all under `docs/`), `fbc2da86` (the TRX evidence at `433ce191` and plan section 25), `a69edb61` (plan 25.4 and the by-name lane comparisons).
- `a78640ba`: pre-HV problem R8b-P18 (operator: "Label it, regate, launch (Recommended)"). The class documentation comment of `RequirementScopeLayerReplIntegrationTests.cs` labels its TEST-MCP-REQSCOPE-REPL-001 citations historical, as R6-F2 did; that id is not in the store. Documentation comment only. This is the last code, test or build change.
- Docs only after it: the gate receipts at `a78640ba`, the TRX evidence at `a78640ba`, plan 25.7, and the round 8b prelaunch records. `git diff --name-only a78640ba..HEAD` lists only `docs/` paths.

### Gates at `a78640ba` (the last code head), each from a clean tree, each local suite alone

- Linux Test `ug-r8b-linux-test-6` (committed tree, before the push): 4592 total, 4592 passed, 0 failed, 0 not executed (Support.Mcp 3172, Repl.Core 903, Client 306, QBAgent 94, McpAgent 64, Cqrs 33, Launcher 20).
- Windows Test `ug-r8b-win-test-ssh-4` (PAYTON-LEGION2 over SSH as kingd, dry run first, origin and HEAD verified): 4592/0/0, per project the same counts.
- Windows MigrationIntegrationTests `ug-r8b-win-lane-5` from kingd's interactive desktop session (pinned runner; filter `Category=Integration&Platform!=Linux`): 589/0/0 (Support.Mcp.Tests 398, McpAgent 1, Repl.Core 3, QBAgent 2, Build.Tests 6, Repl.IntegrationTests 179), each project with its TRX.
- Linux MigrationIntegrationTests `ug-r8b-linux-lane-4`: 612/0/0 (Support.Mcp.Tests 411, McpAgent 1, Repl.Core 3, QBAgent 2, Build.Tests 16, Repl.IntegrationTests 179).
- Full Build.Tests (raw `dotnet test`) `ug-r8b-build-tests-3`, TMPDIR on `/home`: 470/0/0. Operator: "Explicitly state that temporary files in official temporary folders are not a system change."
- ValidateTraceability `ug-r8b-traceability-3`: passed. It reads the committed `docs/Project` exports (see "Information, not a gate").
- By name, against the lanes at `433ce191`: the same 589 results on Windows and the same 612 on Linux, none lost and none added.
- A failed run, not evidence: Linux Test `ug-r8b-linux-test-5` at `a78640ba` failed 4 of 4592 on the per-user quota of the `/tmp` tmpfs (two "Disk quota exceeded", two SQLite disk I/O errors). The operator asked for unused temp data to be cleaned ("We've had this issue before.  Cleanup unused tmp data"); `/tmp` use fell from 13.42 GB to 3.28 GB, and `ug-r8b-linux-test-6` passed with 12.32 GB free at its lowest.
- TRX evidence under HV policy 7.9: `docs/receipts/hv/unitgreen-r8b-trx-a78640ba/manifest.json` (27 TRX files from these runs, 15.71 MB, each copy SHA-256 equal to its original, committed byte for byte; every test project in the repository classified). The set at `433ce191` (`docs/receipts/hv/unitgreen-r8b-trx/`) stays committed as history.
- Receipts: `docs/receipts/unit-suite-green-20261019/` (the `ug-r8b-*` files).

### The hostile-environment runs: out of scope

- Operator, 2026-10-27: "Get ride of the hostile runs enforcing integration test separation.  I have already stated this once." and "Which I sated last night were out of scope." The gate clause is removed from TR-MCP-USECASE-018 AC-006, TR-MCP-USECASE-019 AC-008 and AC-TR-MCP-WS-005-001 (plan section 23); the separation rule itself stays. No exit gate or HV evidence carries a hostile run. The runs of rounds 4 to 8b are history.

### Acceptance criteria define unit tests (plan section 24)

- Operator, 2026-10-27: "Acceptance Criteria are for defining UNIT TESTS that must be covered as TDD during development.  That inherently excludes ENVIRONMENT specific criteria." Removed through the plugin, each read back: TR-MCP-NUKE-019 AC-4, AC-5, AC-7 and AC-8; TR-MCP-NUKE-018 AC-1, AC-2, AC-3, AC-5 and AC-6; TR-MCP-USECASE-019 AC-012. Their environment clauses dropped: TR-MCP-NUKE-019 AC-2, TR-MCP-QUALITY-001 ac-21 and TEST-MCP-PATHNORM-001 `unitgreen-native-host`. The code those criteria drove stays as fixes, tested by its Build.Tests and unit contracts.

### Declared scanner limit

- `UnitSuitePlatformScanner` resolves a using alias whose target names a type, and a `using static` that names a type, each in its lexical scope. It does not resolve an alias whose target is another alias, or a `using static` that names an alias. Operator, 2026-10-21T05:10:50: "Declare the limit, no fix"; 05:43:12: "Correct the doc comments (Recommended)" (built in `ccc78b7a`) and "New TODO for the scanner".
- Operator, 2026-10-21T06:06:46: "I don't want any more scanner work.  None.  Add an operator override to the HV." Hostile validation carries that override. The scanner alias work is TODO `IMPL-SCANNERALIAS-001`, an open record with no work scheduled.

### Lost Windows lane results

- Lanes 1, 3 and 5 of round 3 each dropped 1 or 2 Support.Mcp results without any outcome. Cause: pg_ctl output garbled xUnit v3's message stream; fixed in `6ea425ab`; every lane since is complete by name.

### Hostile validation

- Rounds 1 to 7: DISAGREE each (95/93, 96/94, 97/96, 97/97, 97/96, 97/95, 97/96). Plan sections 14 to 21.
- Round 8 (`unitgreen-r8`, HEAD `868bbb57`) was stopped at 58 percent with no verdict when the operator set HV policy 7.9 (2026-10-27T04:35): "HV requests must supply TRX files for all unit tests in the repo, and for any integration tests specific to the scope of the HV review. The HV reviewer is not to build code or run tests independently. If the HV reviewer feels the tests are inadequate, then it should fail the review and propose additional AC and tests." Operator: "Stop r8, relaunch under 7.9 (Recommended)". Its receipts are committed as received (`2175d827`); a stopped round is not evidence.
- Round 8b (`unitgreen-r8b`): the operator confirmed the round 8b projected problems; the prelaunch records are `docs/receipts/hv/unitgreen-r8b-prelaunch.*`. Launched after this body was published; no verdict yet.

### Requirements

- TR-MCP-NUKE-019 under FR-MCP-BUILD-001 now holds AC-1 (host filter), AC-2 (the six lane projects, each with its TRX), AC-3 (the filter in the log) and AC-6 (every lane case on a temporary instance it owns, never the live server; a case that needs a server asserts).
- TR-MCP-REPL-008 AC-1 to AC-4 (2026-10-27): the four marker walk cases, each named, as unit tests over an in-memory file system.
- TR-MCP-REPL-001 AC-002 (2026-10-26): the one parse rule for the two REPL timeout variables.
- TR-MCP-USECASE-018 AC-006 and TR-MCP-USECASE-019 AC-008 (last replaced 2026-10-27T22:07:50 and 22:08:02): no unit test depends on the platform or on the process environment, directly, through a test helper or through the production code it calls; such a test is a Category=Integration test that sets and restores what it depends on, and tests that write the process environment run one at a time; AC-008 also requires a fixture that needs a junction or link to create it with an operating-system call in the test process.
- AC-TR-MCP-WS-005-001 (2026-10-27T22:25:32) and AC-TR-MCP-SESSIONLOG-006-009 (2026-10-21T12:34:05): the scan fixes, with unit tests taking every plugin-version input from test-owned data.
- FR-MCP-USECASE-016 and -017 converted into TR-MCP-USECASE-018 and -019 under FR-MCP-USECASE-001 and deleted.
- After AGREE, these are set satisfied (plan section 24): TR-MCP-NUKE-019 AC-1, AC-2, AC-3 and AC-6; TR-MCP-REPL-008 AC-1 to AC-4; TR-MCP-REPL-001 AC-002; TR-MCP-QUALITY-001 ac-21; TEST-MCP-PATHNORM-001 `unitgreen-native-host`; TR-MCP-USECASE-018 AC-006; TR-MCP-USECASE-019 AC-008; AC-TR-MCP-WS-005-001 and AC-TR-MCP-SESSIONLOG-006-009. Removed criteria are not set. All 28 tasks of IMPL-UNITGREEN-001 are checked and it is marked done; IMPL-SCANNERALIAS-001 and IMPL-TXNINTEGRATION-001 stay open.

### Open (plan section 25.5)

- QBrainAi.Support.Mcp.IntegrationTests, QBrainAi.Acid.IntegrationTests and QBrainAi.TransactionSecurity.IntegrationTests are not lane projects until PLAN-SESSIONTRIAGEFIX-001 merges (TR-MCP-NUKE-019 AC-2, TODO IMPL-TXNINTEGRATION-001), so they have no TRX here; the chosen option says the reviewer may FAIL that under policy 7.9.
- The scanner alias limit (above).
- This store holds 1 criterion for TR-MCP-SESSIONLOG-006 where the committed export lists 8 (`triage-report-647e35bbdf9e43f5a8752cfc32f6c7a4`).
- Requirement ids absent from the store and cited on test lines this branch does not change are out of scope; on the lines this branch adds, each absent id is labeled historical. The exported TEST-MCP-REPL-007-1 text still names the moved integration case; its id does not match the locked TEST id format, so the plugin cannot update it.
- The writer list of the environment sweep is not discovered by a test (stated limit).
- `hv-ug-r5-test-results.json` (98.19 MB, a round 5 reviewer evidence file) stays in branch history.
- 130 of the store's 1300 criteria have environment-bound or non-unit wording; records outside this PR are not changed.

### Information, not a gate

- Operator, 2026-10-21T06:54:47: "Azure is no longer in the picture.  Create a global memory not to gate on azure related errors." The requirements wiki export fails on its Azure-specific cleanup path (`triage-report-a6927fca10364155ab9992506e1abc9c`), so the generated `docs/Project` exports stay as committed.
- Risk, flagged (operator: "Then flag the risk and roll on."): the race cause is an inference.
- Behavior changes, declared: a directory link or other reparse point below a scanned profile root is no longer followed, and a scan root that is a link, or one reached through a link below the profile or home, contributes nothing; a configured REPL timeout above 4294967 seconds gives the two-minute default instead of failing every request; the integration lane provisions Ollama on both hosts and runs QBrainAi.Repl.IntegrationTests; provisioning downloads stream with no total time limit.
- Develop (plan section 25): develop had taken Codex's QB-AGENT-001 checkpoint on 2026-10-27; with the operator's force-push approval that content is now on branch `cline-qb` (`d3df5353`, nothing deleted), and develop is PR 88 plus two cherry-picked docs commits (`9b099716`). This branch merged that develop docs only (`d43c1d12`). None of the checkpoint is in this PR.
- Lab configuration (not this repository): operator, 2026-10-28: "Git is supposed to ignore line endings anywhere in the lab environment." Both lab hosts now carry the global git attribute `* text=auto eol=lf`; the TRX evidence was committed byte for byte around it.
- Route: Windows evidence runs over SSH as kingd (operator 2026-10-20), by IP 10.42.0.143.

Signed: claude-opus-5-5-xhigh@ClaudeCode | Confidence: correctness 96%, accuracy 96%, completeness 95%

🤖 Generated with [Claude Code](https://claude.com/claude-code)
