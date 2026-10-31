## IMPL-UNITGREEN-001: the unit suite green on both lab hosts (BDPv4: 0 failed, 0 skipped)

Operator, 2026-10-19: "BDPv4 requires all unit tests be green." and "There should be ZERO platform specific UNIT tests.  If it depends on the environment, it's an integration test." Plan: `docs/plans/2026-10-19-unit-suite-green.md` (approved 14:50; amendments in sections 7 to 9, 11 and 17 to 20; execution record in sections 10, 12 and 14 to 20; decisions in section 13).

Draft until hostile validation (Codex gpt-6-astra max) returns AGREE at 98/98. No done state is claimed: every new or changed criterion is unsatisfied and every TODO task is unchecked.

### Commits (over develop `3da2db93`)

- `cbc74e82`: System.Security.Cryptography.Xml and Pkcs pinned to the advisory-patched 10.0.13 (TR-MCP-QUALITY-001 ac-21).
- `28bf0b22`, `de581353`: canonical workspace-key tests, provider traits, and the host-aware `WorkspaceIdentityPath.ToNativePath` overload (FR, TR and TEST-MCP-PATHNORM-001; additive public API).
- `a4c358ef`: narrowing to leave PLAN-C4MODEL-001's fixes to that effort (superseded by `11681680`).
- `b2319488`: the HostileReviewWorker startup test no longer depends on Event Log write access (TR-MCP-HOSTILEREVIEW-003).
- `11681680`: takeover of PLAN-C4MODEL-001's unit-suite code and Linux provider provisioning (TR-MCP-NUKE-018), blob-identical to `c2125c42`, by operator decision.
- `8c2b83ab`: host-scoped MigrationIntegrationTests lane (TR-MCP-NUKE-019 AC-1 to AC-4).
- `8a617334`: merge of develop with PR 86.
- `b2bd50b3`: platform-dependent tests classified as integration tests, with the unit-suite guard and a strengthened skip guard. Its first guard run, `unit-green-guard-mock-20261019T1720`, was red (4692 results, 1 failed, 16 not executed); see plan section 10.
- `4f668b76`: the plan and the PR 82 handoff correction.
- `59249260`: the interactive desktop-spawner case fails loudly as a Windows integration test.
- `69d8f3f5`: amendment H1, under which the Windows integration lane refuses a non-interactive session before any test (TR-MCP-NUKE-019 AC-5).
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
- `810ee8aa`: found by the widened hostile-environment gate. `McpDatabaseEncryptionTransitionCommandTests.TryParse_UsesEnvironmentInstance_WhenCliInstanceIsOmitted` set only `MCP_INSTANCE` while `QBRAINAI_INSTANCE` wins; it is now `Category=Integration` and clears `QBRAINAI_INSTANCE` for its duration; assertions unchanged; test-only.
- `8b7a49ea`, `780b6513`, `c3625a64`: docs only (the widened-gate receipts and plan section 18; the round 5 prelaunch records; the round 5 HV receipts as received).
- `9933796c`: round 5 finding R5-F1 (operator: "Fix both, strengthen ACs"). `DirectoryTreeWalker.IsReachable` requires the scan root and every folder between the profile or home and it to be a directory that is not a link; it is checked before walking each of the seven scan roots (three cache roots, four history roots). A profile or home that is itself a link is still scanned. Tests first: 16 scripted cases with seven wrong-check negative controls, mock gate `ug-r6-reach-mockgate-1` 4536/0/0 before any production change; then the binding, resolve-level cases and a real-link integration test in both lanes.
- `c8418301`: round 5 finding R5-F2 and the environment sweep (operator: "Approve, strengthen AC-006/008 (Recommended)"), test-only. 13 unit tests that read or write the process environment, or import it, move to `Category=Integration` with every variable they depend on controlled and restored and assertions unchanged; the classes that write the environment run in one serialized collection (`ProcessEnvironmentCollection`, `DisableParallelization = true`).
- `44e9d810`, `3ce97b3e`, `3a607fb1`: docs only (the round 6 run receipts and plan section 19; the round 6 prelaunch records and hostile gate v4; the round 6 HV receipts as received).
- `6a87791d`: round 6 finding R6-F1 (operator: "Strengthen AC, move 14 (Recommended)"), test-only. 14 unit tests (seven CliBrainSlot, three Codex strategy, four one-shot strategy) reached production temp-folder code that reads the CLI temp variables and creates the folder. They are now `Category=Integration` and run inside one shared helper, `TestSupport/CliTempVariables.RunWithTestFolderAsync`, which points all five temp variable names (`MCPSERVER_QUADBRAIN_CLI_TEMP`, `MCPSERVER_ONESHOT_TEMP`, `MCPSERVER_CODEX_ONESHOT_TEMP`, `MCPSERVER_CODEX_TRIAGE_TEMP`, `MCPSERVER_GROK_ONESHOT_TEMP`) at one test-owned folder under the official temporary folder and restores them; assertions unchanged. The Codex platform and shim cases use the helper too. Tests first: the helper's snapshot assertions with scripted positive and negative controls, mock gate `ug-r7-clitemp-mockgate-1` 4563/0/0 before the helper existed. Round 6 finding R6-F2: the requirement ids on the test lines this branch adds that are not in the store are labeled historical and make no active coverage claim.
- `04841148`: the helper fix, test-only, found by my pre-HV read of `6a87791d`. Pointing all five names at one folder made the primary-wins assertions pass whichever name the code read. Now the name each strategy reads first points at the test folder and every other name at a separate fallback folder; a name outside the chain is refused. Tests first: the chain assertion with scripted positive and negative controls, mock gate `ug-r7-chain-mockgate-1` 4553/0/0 before the helper changed; then a binding theory with each name as the primary. This is the last code, test or build change.
- Docs only after it: `c31f36d8` (the round 7 run receipts through `04841148`, so the gates run on a clean tree), `55c1400c` (the gate receipts at `c31f36d8`, the script-checking ledger and plan section 20 as built) and `8b97aa4b` (the round 7 prelaunch records). `git diff --name-only 04841148..HEAD` lists only `docs/` paths.

### Gates at `c31f36d8` (code-identical to `04841148`)

- Linux Test `ug-r7-linux-test-2` (committed tree, before the push): 4551 total, 4551 passed, 0 failed, 0 not executed.
- Hostile gate v5 (151 scripted variables, 28 of them folder variables), both runs: folder mode `ug-r7-hostile-v5-folder-2` 4551/0/0, and file mode `ug-r7-hostile-v5-file-2` (every folder variable at the path of a test-owned regular file) 4551/0/0.
- Windows Test `ug-r7-win-test-ssh-2` (PAYTON-LEGION2 over SSH as kingd, dry run first, HEAD verified): 4551 total, 4551 passed, 0 failed, 0 not executed (Support.Mcp 3169, Repl.Core 865, Client 306, QBAgent 94, McpAgent 64, Cqrs 33, Launcher 20).
- Windows MigrationIntegrationTests from kingd's interactive desktop session `ug-r7-win-lane-2` (pinned gate runner; proof `session=1 interactive=True head=c31f36d8`; log line `Integration lane filter: Category=Integration&Platform!=Linux`): 409 passed (Support.Mcp 397, McpAgent 1, Repl.Core 3, QBAgent 2, Build.Tests 6), 0 failed, 0 not executed; by name the round 6 lane plus the 14 moved tests and the 7 helper binding tests, none lost.
- Linux MigrationIntegrationTests `ug-r7-linux-lane-2`: 433 passed (Support.Mcp 411, McpAgent 1, Repl.Core 3, QBAgent 2, Build.Tests 16), 0 failed, 0 not executed.
- Full Build.Tests (raw `dotnet test`) `ug-r7-build-tests-2`, TMPDIR on `/home`: 436 total, 436 passed, 0 failed, 0 not executed. Operator: "Explicitly state that temporary files in official temporary folders are not a system change."
- ValidateTraceability `ug-r7-traceability-2`: passed. It reads the committed `docs/Project` exports (see "Information, not a gate").
- Slice evidence: negative control `ug-r7-hostile-v5-file-negative-2` at the round 6 code, 4544 passed and 14 failed, exactly the R6-F1 tests. `6a87791d`: `ug-r7-clitemp-mockgate-1` 4563/0/0, `ug-r7-binding-1` 4549/0/0 (by name the mock gate minus exactly the 14 moved tests), `ug-r7-precommit-lane-1` 428/0/0, and a full set of gates, all green, now history. `04841148`: `ug-r7-chain-mockgate-1` 4553/0/0, `ug-r7-chain-binding-1` 4551/0/0 (by name the mock gate minus the two removed controls), `ug-r7-precommit-lane-2` 433/0/0.
- Receipts: `docs/receipts/unit-suite-green-20261019/` (the `ug-r7-*` files, including the production-code search `ug-r7-env-search.txt`, the citation search `ug-r7-citations.txt` and the script-checking ledger `ug-r7-script-ledger.txt`).

### The hostile-environment gate

- Round 4 started it with five plugin-root variables and HOME. Round 5 widened it to every variable the unit projects or production read by literal name and both forms of the `ProductEnvironment.GetLegacy` names (operator: "If it needs to be done, do it."). v3 and v4 added the probe names, `GROK_HOME` and the keys of the round 6 moves.
- v5 adds the names a one-time search through production code found behind constants, name lists and the runtime: the REPL timeouts, `MCP_UNTRUSTED`, the QBAgent intent switches, `DATABASE_URL`, `POSTGRES_CONNECTION_STRING`, the MemoryBench live-key names and the sample host settings. It runs twice, folder variables at test-owned folders and then at regular files, as the amended AC-006 and AC-008 require.
- Kept usable for the build: PATH, TMPDIR, NUGET_PACKAGES, DOTNET_CLI_HOME and a scripted HOME. `XDG_DATA_HOME` stays unset so it follows the scripted HOME, because NuGet keeps its HTTP cache there; a first file-mode run with it at a regular file failed the build before any test (`ug-r7-hostile-v5-file-negative-1`, NU1900). The credential test's variable name is random per run and cannot be scripted.
- Negative controls: at `5b818847` the widened gate failed the test fixed in `810ee8aa`; at `9933796c` gate v3 failed exactly the R5-F2 test; at the round 6 code gate v5 in file mode failed exactly the 14 R6-F1 tests.

### Declared scanner limit

- `UnitSuitePlatformScanner` resolves a using alias whose target names a type, and a `using static` that names a type, each in its lexical scope. It does not resolve an alias whose target is another alias, or a `using static` that names an alias. Operator, 2026-10-21T05:10:50: "Declare the limit, no fix"; 05:43:12: "Correct the doc comments (Recommended)" (built in `ccc78b7a`) and "New TODO for the scanner".
- Operator, 2026-10-21T06:06:46: "I don't want any more scanner work.  None.  Add an operator override to the HV." Hostile validation carries that override. The scanner alias work is TODO `IMPL-SCANNERALIAS-001`, an open record with no work scheduled.
- TR-MCP-USECASE-018 AC-006 and TR-MCP-USECASE-019 AC-008 state the outcome, not the scanner. Operator, 2026-10-21T07:12:22: "It is a workspace rule, not something that needs to be mechanically enforced."

### Lost Windows lane results

- Lanes 1, 3 and 5 of round 3 each dropped 1 or 2 Support.Mcp results without any outcome; total still equaled executed, so each run is compared by name with a complete one. Cause: pg_ctl output garbled xUnit v3's message stream; fixed in `6ea425ab`; every lane since is complete by name.

### Hostile validation

- Round 1 (`unitgreen-r1`): DISAGREE, 95/93. Round 2 (`unitgreen-r2`): DISAGREE, 96/94. Round 3 (`unitgreen-r3`): DISAGREE, 97/96. Round 4 (`unitgreen-r4`): DISAGREE, 97/97. Round 5 (`unitgreen-r5`): DISAGREE, 97/96. Plan sections 14 to 19.
- Round 6 (`unitgreen-r6`, HEAD `3ce97b3e`, last code head `c8418301`): DISAGREE, 97/95, 13 PASS, 8 FAIL, 0 UNKNOWN, 1 OVERRIDDEN (the scanner). Findings R6-F1 (14 unit tests read the CLI temp variables through production code) and R6-F2 (five cited requirement ids absent from the store), both implementer edits; R5-F1 and R5-F2 closed PASS. Receipts: `docs/receipts/hv/20261026T072431-unitgreen-r6.*` and `hostile-validator-unitgreen-r6-20261026T072431.*` (`3a607fb1`); the full result is in reviewer turn `req-20261026T123039Z-unitgreen-r6`. It was the second DISAGREE in a row on environment-dependent unit tests, a goal stop condition; the operator chose "Strengthen AC, move 14 (Recommended)", then "Dependence + proof AC (Recommended)" for the criteria's wording. Plan section 20.
- Round 7 (`unitgreen-r7`, HEAD `8b97aa4b`, last code head `04841148`): the operator confirmed the round 7 projected problems ("Confirm, launch HV r7 (Recommended)"); the prelaunch records are `docs/receipts/hv/unitgreen-r7-prelaunch.*`. Launched after this body was published; no verdict yet.

### Requirements

- TR-MCP-NUKE-019 created under FR-MCP-BUILD-001 (AC-1 to AC-5).
- FR-MCP-USECASE-016 and -017 converted into TR-MCP-USECASE-018 and -019 under FR-MCP-USECASE-001 and deleted.
- TR-MCP-QUALITY-001 ac-21 reopened and strengthened; TEST-MCP-PATHNORM-001 gained `unitgreen-native-host`.
- Strengthened on 2026-10-21 through the plugin, each the only changed criterion on its record and unsatisfied: AC-TR-MCP-WS-005-001 (12:33:54) and AC-TR-MCP-SESSIONLOG-006-009 (12:34:05), a scan skips a linked scan root and every linked folder between the profile or home and it, and a linked profile or home is still scanned.
- Strengthened on 2026-10-26 through the plugin, each the only changed criterion on its record and unsatisfied: TR-MCP-USECASE-018 AC-006 and TR-MCP-USECASE-019 AC-008, first at 09:16:57 and 09:17:12 ("Strengthen AC, move 14 (Recommended)"), then at 10:33:51 and 10:34:03 ("Dependence + proof AC (Recommended)"), after my pre-HV read found the first wording reached every test that touches environment-reading code. Now: no unit test depends on the platform or on the process environment, directly, through a test helper or through the production code it calls. A test whose outcome can change with an environment variable is an integration test that sets and restores every variable it or the code it calls depends on, every name of a fallback chain included. Environment writers run one at a time. A unit test may reach code that reads a variable only where the hostile gate shows its outcome does not change; the gate runs twice, folder variables at folders and then at regular files, with XDG_DATA_HOME following the scripted HOME. Operator, 2026-10-21: "Strengthening Acceptance Criteria should always be the recommended option."
- After AGREE, these are set satisfied (plan section 13, D-B, as superseded by section 20): TR-MCP-NUKE-019 AC-1 to AC-5, TR-MCP-USECASE-018 AC-006, TR-MCP-USECASE-019 AC-008, TR-MCP-QUALITY-001 ac-21, TEST-MCP-PATHNORM-001 `unitgreen-native-host`, AC-TR-MCP-WS-005-001 and AC-TR-MCP-SESSIONLOG-006-009. The other 17 USECASE criteria stay unsatisfied. All 23 tasks of IMPL-UNITGREEN-001 are checked and it is marked done; IMPL-SCANNERALIAS-001 stays open.

### Open

- The scanner alias limit (above), recorded in TODO IMPL-SCANNERALIAS-001; no scanner work is planned (operator override).
- This store holds 1 criterion for TR-MCP-SESSIONLOG-006 where the committed export lists 8 (`triage-report-647e35bbdf9e43f5a8752cfc32f6c7a4`).
- Requirement ids that are absent from the store and cited on test lines this branch does not change (inherited store drift, and fixture data such as FR-MCP-900) are out of scope; on the lines this branch adds, each absent id is labeled historical.
- The writer list of the environment sweep is not discovered by a test; a new class that writes the process environment must be added to `ProcessEnvironmentCollectionTests` (stated limit).
- `hv-ug-r5-test-results.json` (98.19 MB, a round 5 reviewer evidence file) stays in branch history; removing it needs a force push.
- Correction of my own records: the round 1 to 6 self-reviews said every script was analyzed, and dry-run first where it changes state, before it ran. A ledger built from the session transcript (`ug-r7-script-ledger.txt`) shows runs in every round without a recorded clean analysis after the script's last edit (mostly the operator's timestamp tool) and 26 real runs in rounds 1 to 4 without a dry run first. The round 7 self-review gives the counts per round and the ledger's limits. No gate result depends on it; every gate run has its own receipt.

### Information, not a gate

- Operator, 2026-10-21T06:54:47: "Azure is no longer in the picture.  Create a global memory not to gate on azure related errors." (global MCP memory MEMORY-PROCESS-009). The requirements wiki export fails on its Azure-specific cleanup path (`triage-report-a6927fca10364155ab9992506e1abc9c`), so the generated `docs/Project` exports stay as committed. That is recorded here and is not a gate.
- Risk, flagged (operator: "Then flag the risk and roll on."): the race cause is an inference.
- Behavior change, declared: a directory link or other reparse point below a scanned profile root is no longer followed by either scan, and since `9933796c` a scan root that is a link, or one reached through a link below the profile or home, contributes nothing.
- Develop gained PR 88 (one docs file, not touched here) after the branch base; the merge base stays `3da2db93` and a test merge is clean (checked 2026-10-26T11:31:59).
- Route: Windows evidence runs over SSH as kingd (operator 2026-10-20), by IP 10.42.0.143.

Signed: claude-opus-5-5-xhigh@ClaudeCode | Confidence: correctness 96%, accuracy 97%, completeness 95%

🤖 Generated with [Claude Code](https://claude.com/claude-code)

