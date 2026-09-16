# Hostile Validator Receipt

TimestampUtc: 2026-08-22T04:13:27Z
ValidatorIdentity: GrokSubagentHostile
Workspace: F:\GitHub\McpServer
WorkClass: 1 (project implementation PLAN-PLUGINHANDOFF-001 Phase C-red-P14 ONLY, PLUGIN_ROOT_OVERRIDE red tests). Surfaces A+B+C+D all apply. Review only. No product implementation by this validator. Do not require P14 green. Do not require P15-P20. PLAN-PLUGINHANDOFF-001 and MCP-PLUGININT-001 must remain not done.
add-profile: executed yes. Profile files read: 18 (every non-skill *.md under C:\Users\kingd\.claude\profile\; excluded add-profile.grok.md). Also read add-profile SKILL.md and hostile-validator SKILL.md.
Plugin: F:\GitHub\mcpserver-grok-plugin (.grok-plugin/plugin.json version 1.100.0; .version 1.100.0). Live workflow/client calls used Invoke-McpPlugin.ps1. Tool registry search included exact name mcpserver-grok-plugin. Marker HMAC Test-MarkerSignature true. Health nonce nonce-hv-20260822040409-83681 echoed exactly. Plugin Status available.
Active plan: F:\GitHub\McpServer\docs\plans\PLAN-PLUGINHANDOFF-001.md section 7 P14: P14 red Theory_{Agent}_PluginRootOverride_WritesOnlyWorkspaceMcpServerAgent_PoisonEmpty. Maps TEST-MCP-PLUGININT-001 AC4. Hostile C-red-P14. Then P14 green.
Requirement IDs: FR-MCP-PLUGININT-001 AC PLUGIN_ROOT_OVERRIDE cannot redirect; TR-MCP-PLUGININT-001; TEST-MCP-PLUGININT-001 AC4.
Prior receipt: docs/receipts/hostile-validator-20260822T034932Z.md (OverallVerdict AGREE, FailCount 0, C-green-P11-P13; full Failed 0 Passed 54 Skipped 0 Total 54; P14NamedTestsPresent false).
Collector: docs/receipts/_hv-c-red-p14/
OverallVerdict: AGREE

Default was FAIL or UNKNOWN until this pass independently re-read PluginSessionLogWorkflowAdapterTests.cs, PluginSessionLogWorkflowResult.cs, and PluginSessionLogWorkflowAdapter.cs, grepped P15/P16 names (absent in PluginIntegration.Tests), grepped Skip (absent), live-got PLAN-PLUGINHANDOFF-001 and MCP-PLUGININT-001 plus FR/TR/TEST/listMappings through the Grok plugin, listed PluginIntegration tests (62 total, 8 P14, 0 P15, 0 P16), and independently re-ran:

- `dotnet test tests/McpServer.PluginIntegration.Tests -c Debug --filter FullyQualifiedName~Theory_Agent_PluginRootOverride_WritesOnlyWorkspaceMcpServerAgent_PoisonEmpty` first TRX: executed 8 passed 0 failed 8 notExecuted 0. All eight host kinds failed at line 171 with "{hostKind} must reject PLUGIN_ROOT_OVERRIDE." Log truncated when the wrapper killed Tee-Object; testhosts were left running and completed the TRX. Proof: docs/receipts/_hv-c-red-p14/trx-p14/p14-filter.trx and filter-trx-summary.json.
- Clean recovery `dotnet test tests/McpServer.PluginIntegration.Tests -c Debug --no-build --filter FullyQualifiedName~Theory_Agent_PluginRootOverride_WritesOnlyWorkspaceMcpServerAgent_PoisonEmpty` ExitCode 1. Console: Failed! Failed: 8, Passed: 0, Skipped: 0, Total: 8, Duration: 2 m 33 s. TRX executed 8 passed 0 failed 8 notExecuted 0. rejectMessageCount 8. Log: docs/receipts/_hv-c-red-p14/dotnet-p14-filter-rerun.log TRX: docs/receipts/_hv-c-red-p14/trx-p14-rerun/p14-filter-rerun.trx.

Implementer chat and implementer logs were not trusted as proof. This review did not implement the override guard.

## Clock and live MCP

Health GET /health?nonce=nonce-hv-20260822040409-83681 returned status Healthy, storage reachable, nonce echoed exactly. Plugin Test-MarkerSignature true. Tool registry exact name mcpserver-grok-plugin present.

## Session log proof

Dedicated persistence is through plugin `client.SessionLog.*` as GrokSubagentHostile. SessionId GrokSubagentHostile-20260822T040409Z-c-red-p14. Turn requestId req-20260822T040409Z-001-hostile-c-red-p14. client.SessionLog.OpenSessionAsync created=true. BeginTurnAsync turnId 42878. CompleteTurnAsync same turnId. QueryAsync agent=GrokSubagentHostile sessionId match: turn status completed, filesModified includes docs/receipts/hostile-validator-20260822T041327Z.md and .json, 7 actions with integer order, 4 processingDialog items (observation + decision), 2 designDecisions. workflow.sessionlog.queryHistory lists this session first. Proof: docs/receipts/_hv-c-red-p14/sl-open.txt, sl-begin.txt, sl-dialog.txt, sl-patch.txt, sl-complete.txt, sl-query-sid-after.txt, sl-query-history-after.txt.

## Mandatory surface that could not be evaluated

None.

## Explicit FAIL list

None.

## Explicit UNKNOWN list

None.

## Residual (not extra FAILs)

- First filter console log was truncated (wrapper timeout killed Tee-Object). vstest kept running; first TRX is complete (failed 8 passed 0). Clean --no-build rerun supplied the console Failed 8 Passed 0 Skipped 0 line. This review waited for leftover testhosts instead of killing them.
- TRX Counters.skipped attribute is missing (null). Console Skipped: 0; TRX notExecuted=0; skippedNames empty.
- Homemade HMAC Match=false. Plugin Test-MarkerSignature true. Same payload-construction residual as C-green-P11-P13.
- PythonProcessCount 1 is Google Cloud SDK gcloud.py auth login (PID 69564). This validator did not invoke python/python3/py.
- workflow.* result envelopes include deprecated: true. Treated as success metadata.
- FR/TR/TEST createdAt stamps look like query time. AC text is present. isSatisfied remains false; this red gate does not claim store AC complete.
- PLAN remaining text still says next is C-green-P11-P13 then P14 red. Combined PLAN task "C P14 red + hostile then green" remains done:false because green has not started. Correct for this red-only gate.
- git status --porcelain shows ?? tests/McpServer.PluginIntegration.Tests/*.cs. Untracked does not by itself block a red gate.
- Marker plugin_version 1.97.0 drifted. Plugin .version and plugin.json are 1.100.0 (authoritative).
- A later competing C-green-P11-P13 receipt docs/receipts/hostile-validator-20260822T035559Z.md is DISAGREE (typed-client persist). Parent brief cites docs/receipts/hostile-validator-20260822T034932Z.md AGREE as the prior green gate. This C-red-P14 review does not re-litigate P12 persist and does not treat that DISAGREE as a P14 red FAIL.
- P14 method does not assert fixture.Port != 7147. PluginIntegrationServerFixture.AllocateFreePort excludes ReservedServicePort 7147. ExecuteCanonicalTurnAsync starts from that isolated fixture.
- Adapter still omits PluginRootOverrideRejected (defaults false) and still persists via fixture.CreateTrustedClient after discarded LaunchAsync. Expected for this red: the guard is not implemented. Persist-path is later/green work, not a C-red-P14 FAIL.
- Collector adapter-scan TestsP14InlineCount 0 was a regex miss. File read plus --list-tests show eight InlineData host kinds. Cited evidence is the file and list-tests-summary.json, not that regex.
- This review did not re-run the full 62-test PluginIntegration.Tests project. --list-tests listed 62 (54 prior + 8 P14). Parent required the P14 filter only. P15/P16 names are absent from discovered tests.

## Claims reviewed

### A Requested

#### A1. Named test Theory_Agent_PluginRootOverride_WritesOnlyWorkspaceMcpServerAgent_PoisonEmpty exists as a Theory with eight InlineData PluginHostKind rows. File: tests/McpServer.PluginIntegration.Tests/PluginSessionLogWorkflowAdapterTests.cs. No Skip. Maps TEST-MCP-PLUGININT-001 AC4.

Verdict: PASS

Evidence: File read this review (LastWriteTimeUtc 2026-08-22T03:54:31.8660036Z). Method at line 154. `[Theory(Timeout = 120000)]` at line 145. Eight `[InlineData(PluginHostKind.*)]` rows at lines 146-153: Codex, ClaudeCode, ClaudeCowork, Copilot, Grok, Cline, ClineV2, OpenCode. XML summary: "Maps TEST-MCP-PLUGININT-001 AC4." Grep Skip / Fact(Skip / Theory(Skip / Assert.Skip / [Skip in PluginIntegration.Tests: no matches. --list-tests listed the same eight hostKind rows (list-tests-summary.json p14Count 8, skipListed 0).

#### A2. Tests currently fail because PluginSessionLogWorkflowResult.PluginRootOverrideRejected is false (override guard not implemented). Independent re-run of the named filter must show Failed 8 Passed 0 Skipped 0. Error "must reject PLUGIN_ROOT_OVERRIDE." for all eight host kinds. Tests set PLUGIN_ROOT_OVERRIDE to a poison temp directory, start the isolated fixture (not 7147), run ExecuteCanonicalTurnAsync, and assert PluginRootOverrideRejected, cache under {workspace}/.mcpServer/{cacheFolder}, poison empty.

Verdict: PASS

Evidence: Adapter return at lines 140-148 does not set PluginRootOverrideRejected (adapter-scan AdapterReturnOmitsPluginRootOverrideRejected true, AdapterSetsPluginRootOverrideRejectedTrue false, AdapterMentionsPluginRootOverride false). Result property defaults false (PluginSessionLogWorkflowResult.cs lines 26-30). Test sets PLUGIN_ROOT_OVERRIDE to poison (lines 158-161), starts PluginIntegrationServerFixture (lines 164-165), calls ExecuteCanonicalTurnAsync (lines 166-169), asserts PluginRootOverrideRejected with "{hostKind} must reject PLUGIN_ROOT_OVERRIDE." (line 171), cache not under poison, contains .mcpServer/{cacheFolder} and fixture.WorkspacePath, poison empty (lines 172-175). Fixture AllocateFreePort excludes 7147 (PluginIntegrationServerFixture.cs lines 16, 416-430). Independent first TRX failed 8 passed 0 notExecuted 0 rejectMessageCount 8. Independent --no-build rerun ExitCode 1, console Failed! Failed: 8 Passed: 0 Skipped: 0 Total: 8 Duration 2 m 33 s, TRX failed 8 passed 0, eight host messages all "must reject PLUGIN_ROOT_OVERRIDE."

#### A3. P15/P16 named tests are absent (Success_NoPendingFailsafe, FailedSubmit_RetainsRootIdPending, RetrySuccess_DeletesOnlyMatchingPending, AiTheory_). Do FAIL if those greens/reds are mixed in, or if the eight P14 rows pass, or if any P14 row is skipped.

Verdict: PASS

Evidence: --list-tests totalListed 62, p15Success 0, p15FailedSubmit 0, p15Retry 0, p16AiTheory 0. Grep those names in tests/McpServer.PluginIntegration.Tests: 0 hits. Filter and rerun TRX p15Count 0 p16Count 0 p14Passed 0 p14Skipped 0 passedNames empty skippedNames empty.

#### A4. PLAN-PLUGINHANDOFF-001 and MCP-PLUGININT-001 remain done:false. Live todo_get.

Verdict: PASS

Evidence: workflow.todo.get and client.Todo.GetAsync via Invoke-McpPlugin. PLAN-PLUGINHANDOFF-001 top done: false (todo-plan.txt line 9). Combined task `C P14 red + hostile then green; P15 red + hostile then green.` done: false (todo-plan.txt lines 830-831). MCP-PLUGININT-001 done: false. P14 implementationTask done: false (todo-pluginint.txt lines 8, 51-52). This review wrote no todo_update. git status --porcelain for docs/Project/TODO.yaml and docs/todo.yaml was empty.

#### A5. Prior C-green-P11-P13 AGREE is docs/receipts/hostile-validator-20260822T034932Z.md (Passed 54 Failed 0 Skipped 0; P14 absent). These P14 tests are new reds.

Verdict: PASS

Evidence: Prior md OverallVerdict AGREE; JSON Phase C-green-P11-P13, FailCount 0, AllPassed 54, AllFailed 0, AllTotal 54, AllSkippedConsole 0, P14NamedTestsPresent false. Current tests file LastWriteTimeUtc 2026-08-22T03:54:31Z is after that receipt (2026-08-22T03:53:31Z). Adapter LastWriteTimeUtc remains 2026-08-22T02:51:04Z (no override guard). --list-tests 62 = prior 54 plus 8 P14. P14HitsTestsSrcCount 1 (the new method).

### B Workspace rules

#### B1. Honesty and receipts. Every done/green/red claim re-verified with machine evidence.

Verdict: PASS

Evidence: Independent unique logs and TRX under docs/receipts/_hv-c-red-p14/ with timestamps 20260822T040409Z bootstrap, first TRX 2026-08-22T04:08:07Z, rerun console 2026-08-22T04:12:52Z. Implementer Failed 8 Passed 0 Skipped 0 matched this rerun. Guard confirmed unimplemented by file read and grep, not by implementer narrative.

#### B2. Byrd v4 phase-order for this class-1 slice. Phase-order is scored at inter-phase hostile gates, not FR-vs-file timestamps.

Verdict: PASS

Evidence: C-green-P11-P13 hostile AGREE exists (docs/receipts/hostile-validator-20260822T034932Z.md). P14 red tests were added after that AGREE. Adapter guard is not implemented, so the new tests are currently red. Plan order is C-red-P14 AGREE, then P14 green. This review does not FAIL B2 from FR createdAt versus file mtimes. This review does not implement P14 green.

#### B3. MCP-only TODO/session/requirements storage.

Verdict: PASS

Evidence: Live todo_get and requirements get/listMappings via Invoke-McpPlugin.ps1. This validator did not read or write TODO.yaml or session-log store files as a substitute.

#### B4. PowerShell-only / no Python lab rule.

Verdict: PASS

Evidence: All bootstrap, test, parse, and session commands used pwsh.exe -NoProfile -NonInteractive. No python / python3 / py.exe invocation by this validator. Observed python.exe PID 69564 is Google Cloud SDK gcloud.py, not this review.

#### B5. Look-before-delete and review-only.

Verdict: PASS

Evidence: This review created only docs/receipts/_hv-c-red-p14/ collector scripts/logs and the receipt pair. It did not edit product tests or adapter code. Leftover testhosts from the truncated first run were inspected by command line then waited out, not blindly killed mid-TRX.

### C Requirements

#### C1. FR-MCP-PLUGININT-001 AC that PLUGIN_ROOT_OVERRIDE cannot redirect exists.

Verdict: PASS

Evidence: workflow.requirements.getFr id FR-MCP-PLUGININT-001. AC3 text: "Setting PLUGIN_ROOT_OVERRIDE cannot redirect session or turn cache state." isSatisfied false. Status pending. This red gate does not require isSatisfied true.

#### C2. TR-MCP-PLUGININT-001 exists and is mapped from the FR.

Verdict: PASS

Evidence: workflow.requirements.getTr id TR-MCP-PLUGININT-001, six AC, status pending. workflow.requirements.listMappings frId FR-MCP-PLUGININT-001 returns FR->TR and FR->TEST (totalCount 2).

#### C3. TEST-MCP-PLUGININT-001 AC4 exists, is testable, and is mapped by the named P14 theory. Tests are shown red against that AC.

Verdict: PASS

Evidence: workflow.requirements.getTest id TEST-MCP-PLUGININT-001 AC4: "A legacy PLUGIN_ROOT_OVERRIDE value is injected and proven unable to alter the expected cache path." isSatisfied false. Named theory XML docs map TEST-MCP-PLUGININT-001 AC4. Independent rerun Failed 8 Passed 0 Skipped 0. Suite-green is not required at C-red.

#### C4. AC are appropriate for the claimed P14 red scope (inject override, workspace cache path, poison empty, reject flag).

Verdict: PASS

Evidence: TEST AC4 is testable. The P14 theory injects PLUGIN_ROOT_OVERRIDE, asserts PluginRootOverrideRejected, asserts cache under fixture workspace .mcpServer/{cacheFolder}, and asserts poison empty. FR AC3 is the product AC. Missing sibling-cache extra wording from the MCP-PLUGININT-001 P14 task text is residual versus the plan's named PoisonEmpty theory and TEST AC4.

#### C5. Do not require TEST AC3 AiTheory, AC5 native suite, TR failsafe cleanup, or store isSatisfied. Those are later slices.

Verdict: PASS

Evidence: Parent brief: C-red-P14 only, not P15-P20, not P14 green. P15/P16 named tests absent. isSatisfied remains false on FR/TR/TEST.

### D Current plan holistically

#### D1. Plan P14 red DoD is the named theory mapping TEST-MCP-PLUGININT-001 AC4, currently failing, then hostile C-red-P14. Not plan closeout.

Verdict: PASS

Evidence: docs/plans/PLAN-PLUGINHANDOFF-001.md line 380. Named theory exists, maps AC4, independently Failed 8 Passed 0 Skipped 0. This receipt is the C-red-P14 gate. Parent forbids treating this as plan closeout.

#### D2. PLAN-PLUGINHANDOFF-001 and MCP-PLUGININT-001 remain done:false. Combined C P14 task includes green and stays open.

Verdict: PASS

Evidence: Live todo_get. Combined PLAN task "C P14 red + hostile then green" done: false. MCP-PLUGININT-001 P14 done: false. This review wrote no done:true.

#### D3. Do not require P14 green, P15-P20, or PLUGININT done.

Verdict: PASS

Evidence: Adapter guard not implemented. Tests red. Parent brief matches. This AGREE authorizes P14 green to start, not to skip it.

#### D4. Cross-step: a later competing DISAGREE on P12 persist does not complete or block this P14 red DoD under the parent-cited C-green AGREE.

Verdict: PASS

Evidence: Parent named docs/receipts/hostile-validator-20260822T034932Z.md as prior AGREE (Passed 54 Failed 0 Skipped 0, P14 absent). This review verified that file. Competing DISAGREE 20260822T035559Z is residual, not a P14 red FAIL. Persist-path remains later work.

Accuracy: 97. Independent TRX and --no-build console match. First console log was truncated and recovered. Collector InlineData regex missed; file plus --list-tests used instead.

Completeness: 95. Surfaces A+B+C+D evaluated. Full 62-test PluginIntegration.Tests project not re-run; --list-tests plus the required P14 filter were. TEST AC3/AC5 and P15-P20 remain later slices by brief.

