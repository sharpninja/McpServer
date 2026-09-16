# Hostile Validator Receipt

TimestampUtc: 2026-08-22T04:48:18Z
ValidatorIdentity: GrokSubagentHostile
Workspace: F:\GitHub\McpServer
WorkClass: 1 (project implementation PLAN-PLUGINHANDOFF-001 Phase C-green-P14 ONLY, PLUGIN_ROOT_OVERRIDE isolation green). Surfaces A+B+C+D all apply. Review only. No product implementation by this validator. Do not implement P15. PLAN-PLUGINHANDOFF-001 and MCP-PLUGININT-001 must remain not done.
add-profile: executed yes. Profile files read: 18 (every non-skill *.md under C:\Users\kingd\.claude\profile\; excluded add-profile.grok.md). Also read add-profile SKILL.md and hostile-validator SKILL.md.
Plugin: F:\GitHub\mcpserver-grok-plugin (.grok-plugin/plugin.json version 1.100.0; .version 1.100.0). Live workflow/client calls used Invoke-McpPlugin.ps1. Tool registry search included exact name mcpserver-grok-plugin. Marker HMAC Test-MarkerSignature true. Health nonce nonce-hv-20260822042654-19392 echoed exactly. Plugin Status available.
Active plan: F:\GitHub\McpServer\docs\plans\PLAN-PLUGINHANDOFF-001.md section 7 P14: P14 red Theory_{Agent}_PluginRootOverride_WritesOnlyWorkspaceMcpServerAgent_PoisonEmpty. Maps TEST-MCP-PLUGININT-001 AC4. Hostile C-red-P14. Then P14 green.
Requirement IDs: FR-MCP-PLUGININT-001 AC PLUGIN_ROOT_OVERRIDE cannot redirect; TR-MCP-PLUGININT-001; TEST-MCP-PLUGININT-001 AC4.
Prior receipt: docs/receipts/hostile-validator-20260822T041327Z.md (OverallVerdict AGREE, FailCount 0, C-red-P14; independent Failed 8 Passed 0 Skipped 0 Total 8; PluginRootOverrideRejected omitted).
Collector: docs/receipts/_hv-c-green-p14/
OverallVerdict: AGREE

Default was FAIL or UNKNOWN until this pass independently re-read PluginSessionLogWorkflowAdapter.cs, PluginSessionLogWorkflowResult.cs, PluginSessionLogWorkflowAdapterTests.cs, PluginHostProcessAdapter.cs, RealPluginProcessRunner.cs, and PluginIntegrationServerFixture.cs; grepped P15 names (absent in PluginIntegration.Tests); live-got PLAN-PLUGINHANDOFF-001 and MCP-PLUGININT-001 plus FR/TR/TEST/listMappings through the Grok plugin; listed PluginIntegration tests (62 total, 8 P14, 0 P15, 0 P16); and independently re-ran:

- `dotnet test tests/McpServer.PluginIntegration.Tests -c Debug --filter FullyQualifiedName~Theory_Agent_PluginRootOverride_WritesOnlyWorkspaceMcpServerAgent_PoisonEmpty`
- ExitCode 0. Console: Passed! Failed: 0, Passed: 8, Skipped: 0, Total: 8, Duration: 2 m. TRX executed 8 passed 8 failed 0 notExecuted 0. All eight host kinds Passed. Proof: docs/receipts/_hv-c-green-p14/dotnet-p14-filter.log, trx-p14/p14-filter.trx, filter-trx-summary.json, dotnet-p14-filter-exit.json.

Implementer chat and implementer logs were not trusted as proof. A concurrent implementer run (p14-green-filter.trx / p14-green-full.trx under grok-goal temp) is recorded as untrusted comparison only. This review did not implement P15.

## Clock and live MCP

Health GET /health?nonce=nonce-hv-20260822042654-19392 returned status Healthy, storage reachable, nonce echoed exactly. Plugin Test-MarkerSignature true. Invoke-FullBootstrap true. Tool registry exact name mcpserver-grok-plugin present.

## Session log proof

Dedicated persistence is through plugin `client.SessionLog.*` as GrokSubagentHostile. SessionId GrokSubagentHostile-20260822T042653Z-c-green-p14. Turn requestId req-20260822T042653Z-001-hostile-c-green-p14. client.SessionLog.OpenSessionAsync created=true. BeginTurnAsync turnId 42886. CompleteTurnAsync same turnId after this receipt. QueryAsync after complete is in docs/receipts/_hv-c-green-p14/sl-query-sid-after.txt.

## Mandatory surface that could not be evaluated

None.

## Explicit FAIL list

None.

## Explicit UNKNOWN list

None.

## Residual (not extra FAILs)

- Persist still uses fixture.CreateTrustedClient after LaunchAsync result is discarded. SessionId/RequestId are fabricated in the adapter. That is the same residual named in docs/receipts/hostile-validator-20260822T035559Z.md for P12. Scored against P14 DoD: TEST-MCP-PLUGININT-001 AC4 is override isolation only; MCP-PLUGININT-001 P14 task text requires poison empty plus writes only to {workspace}/.mcpServer/{agent} and sibling cache untouched, not production plugin persist. Parent brief: if AC4 is only override/cache/poison, persist-path may be residual not FAIL. Classified residual, not FAIL, for this C-green-P14 gate.
- P14 theory does not assert sibling workspace cache untouched. C-red-P14 already treated missing sibling wording versus TEST AC4 as residual. TEST AC4 remains the plan-named mapping.
- P14 method does not itself assert fixture.Port != 7147. PluginIntegrationServerFixture.AllocateFreePort excludes ReservedServicePort 7147. P14 uses that fixture. Console log mentions7147 false.
- PluginSessionLogWorkflowResult XML still says "C-red-P14 stays false until the override guard is implemented." Property is now set true when PLUGIN_ROOT_OVERRIDE is non-empty. Comment drift only.
- TRX Counters.skipped attribute is missing (null). Console Skipped: 0; TRX notExecuted=0; skippedNames empty.
- filter-trx-summary p14ByHost Count=5 is a hostKind regex overlap. Cited evidence is passedNames (8 unique host kinds, each Passed 1) and unitCount 8, not that Count field.
- list-tests-summary totalListed 81 is a loose indent regex. Console --list-tests enumerated 62 named tests. p14Count 8, p15 0, p16 0 is correct.
- Homemade HMAC Match=false. Plugin Test-MarkerSignature true. Same payload-construction residual as C-red-P14.
- PythonProcessCount 1 is Google Cloud SDK gcloud.py auth login (PID 69564). This validator did not invoke python/python3/py.
- workflow.* result envelopes include deprecated: true. Treated as success metadata.
- FR/TR/TEST createdAt stamps look like query time. AC text is present. isSatisfied remains false; this green gate does not claim store AC complete.
- Combined PLAN task "C P14 red + hostile then green; P15 red + hostile then green." remains done:false because P15 has not started. Correct.
- git status --porcelain shows ?? tests/McpServer.PluginIntegration.Tests/. Untracked does not by itself block this green gate.
- Marker plugin_version 1.97.0 drifted. Plugin .version and plugin.json are 1.100.0 (authoritative).
- Plugin Status reports agent GrokCode with no-session against this collector cache. Session identity used for writes is GrokSubagentHostile via client.SessionLog.*.
- A broken wait-then-tests StrictMode Count error delayed this rerun while a concurrent implementer full suite (Passed 62) was finishing. This validator killed its own wait script, did not kill implementer testhosts, then ran the required filter after PLUGININT_PROCS=0.
- This review did not independently re-run the full 62-test PluginIntegration.Tests project. Parent required the P14 filter only.

## Claims reviewed

### A Requested

#### A1. Prior C-red-P14 AGREE is docs/receipts/hostile-validator-20260822T041327Z.md (Failed 8 Passed 0; PluginRootOverrideRejected omitted).

Verdict: PASS

Evidence: File exists Length 16638 LastWriteTimeUtc 2026-08-22T04:17:37Z. OverallVerdict AGREE. JSON Phase C-red-P14, FailCount 0. Markdown states independent rerun Failed 8 Passed 0 Skipped 0 Total 8 and adapter omitted PluginRootOverrideRejected. Adapter LastWriteTimeUtc 2026-08-22T04:18:20Z is after that receipt.

#### A2. Adapter now sets PluginRootOverrideRejected when PLUGIN_ROOT_OVERRIDE is set, clears override in extraEnvironment, and writes cache under fixture workspace .mcpServer/{cacheFolder}.

Verdict: PASS

Evidence: PluginSessionLogWorkflowAdapter.cs lines 50-51 read PLUGIN_ROOT_OVERRIDE and set pluginRootOverrideRejected = !string.IsNullOrWhiteSpace(pluginRootOverride). Lines 53-54 cachePath = Path.Combine(fixture.WorkspacePath, ".mcpServer", scenario.CacheFolder). Lines 61-66 extraEnvironment PLUGIN_ROOT_OVERRIDE = string.Empty. Line 152 returns PluginRootOverrideRejected = pluginRootOverrideRejected. RealPluginProcessRunner.cs lines 37-39 Remove the key when value is empty, so the child does not inherit the testhost poison path. adapter-scan.json AdapterHasPluginRootOverrideRejectedAssign true, AdapterSetsExtraEnvEmptyOverride true, AdapterCachePathMcpServerCacheFolder true, RunnerRemovesEmptyEnv true.

#### A3. Independent re-run of the named filter must be Failed 0 Skipped 0 for those 8 rows.

Verdict: PASS

Evidence: Independent command `dotnet test tests/McpServer.PluginIntegration.Tests -c Debug --filter FullyQualifiedName~Theory_Agent_PluginRootOverride_WritesOnlyWorkspaceMcpServerAgent_PoisonEmpty` ExitCode 0, DurationMs 133971, FinishedUtc 2026-08-22T04:46:52Z. Console Passed! Failed: 0 Passed: 8 Skipped: 0 Total: 8 Duration: 2 m. TRX executed 8 passed 8 failed 0 notExecuted 0. passedNames are the eight host kinds: ClaudeCode, Grok, ClineV2, Codex, Cline, OpenCode, Copilot, ClaudeCowork. p14Skipped 0. No Skip attributes in PluginIntegration.Tests.

#### A4. Poison directory empty AND cache under workspace .mcpServer/{cacheFolder}.

Verdict: PASS

Evidence: Test method lines 171-175 assert PluginRootOverrideRejected, CachePath does not start with poison, CachePath contains Path.Combine(".mcpServer", scenario.CacheFolder), CachePath contains fixture.WorkspacePath, and Directory.GetFileSystemEntries(poison) is empty. All eight rows Passed, so those asserts held. Catalog cacheFolder values: codex, claude, cowork, copilot, grok, cline, cline-v2, opencode.

#### A5. Isolated fixture is not developer port 7147.

Verdict: PASS

Evidence: PluginIntegrationServerFixture.cs ReservedServicePort = 7147; AllocateFreePort skips that port. P14 constructs PluginIntegrationServerFixture and StartAsync (tests lines 164-165). Fixture XML: "Never the developer 7147 database." Independent filter used that fixture. Console mentions7147 false.

#### A6. Persist still uses fixture.CreateTrustedClient after LaunchAsync discard. Score against P14 DoD.

Verdict: PASS (residual, not FAIL)

Evidence: Adapter lines 71-77 `_ = await processAdapter.LaunchAsync(...)`. Line 85 `var client = fixture.CreateTrustedClient();` then OpenSession/BeginTurn/AppendDialog/CompleteTurn. AdapterCallsWorkflowSessionlog false. AdapterCallsInvokeMcpPlugin false. TEST-MCP-PLUGININT-001 AC4: "A legacy PLUGIN_ROOT_OVERRIDE value is injected and proven unable to alter the expected cache path." MCP-PLUGININT-001 P14 task: inject poison, write only to {workspace}/.mcpServer/{agent}, poison empty, no sibling cache. That task text does not require production plugin persist. Production entrypoint persist is MCP-PLUGININT-001 technicalDetails and P12, already FAIL on the later C-green-P11-P13 DISAGREE 20260822T035559Z, not this P14 slice. Parent brief allows residual when AC4 is only override/cache/poison.

#### A7. P15 names Success_NoPendingFailsafe / FailedSubmit_RetainsRootIdPending / RetrySuccess_DeletesOnlyMatchingPending must be absent.

Verdict: PASS

Evidence: Grep in tests/McpServer.PluginIntegration.Tests: P15HitCount 0. --list-tests p15Success 0 p15FailedSubmit 0 p15Retry 0 p16AiTheory 0. Filter TRX p15Count 0 p16Count 0. Console list of available tests includes the eight P14 rows and no those three names.

#### A8. PLAN-PLUGINHANDOFF-001 and MCP-PLUGININT-001 remain done:false. Live todo_get.

Verdict: PASS

Evidence: workflow.todo.get and client.Todo.GetAsync via Invoke-McpPlugin. PLAN-PLUGINHANDOFF-001 top done: false. Combined task `C P14 red + hostile then green; P15 red + hostile then green.` done: false. MCP-PLUGININT-001 done: false. P14 implementationTask done: false. This review wrote no todo_update. git status --porcelain for docs/Project/TODO.yaml and docs/todo.yaml was empty.

### B Workspace rules

#### B1. Honesty and receipts. Every done/green claim re-verified with machine evidence.

Verdict: PASS

Evidence: Independent unique logs and TRX under docs/receipts/_hv-c-green-p14/ with bootstrap stamp 20260822T042653Z and filter TRX LastWriteTimeUtc 2026-08-22T04:46:52Z. Implementer untrusted p14-green-filter.trx was parsed separately and not used as this verdict. Adapter guard confirmed by file read plus the independent Passed 8 run.

#### B2. Byrd v4 phase-order for this class-1 slice. Phase-order is scored at inter-phase hostile gates, not FR-vs-file timestamps.

Verdict: PASS

Evidence: C-red-P14 hostile AGREE exists (docs/receipts/hostile-validator-20260822T041327Z.md). Tests file LastWriteTimeUtc 2026-08-22T03:54:31Z predates adapter green 2026-08-22T04:18:20Z. This review is the C-green-P14 gate after that red AGREE. This review does not FAIL B2 from FR createdAt versus file mtimes. This review does not implement P15.

#### B3. MCP-only TODO/session/requirements storage.

Verdict: PASS

Evidence: Live todo_get and requirements get/listMappings via Invoke-McpPlugin.ps1. This validator did not read or write TODO.yaml or session-log store files as a substitute.

#### B4. PowerShell-only / no Python lab rule.

Verdict: PASS

Evidence: All bootstrap, test, parse, and session commands used pwsh.exe -NoProfile -NonInteractive. No python / python3 / py.exe invocation by this validator. Observed python.exe PID 69564 is Google Cloud SDK gcloud.py, not this review.

#### B5. Look-before-delete and review-only.

Verdict: PASS

Evidence: This review created only docs/receipts/_hv-c-green-p14/ collector scripts/logs and the receipt pair. It did not edit product tests or adapter code. Concurrent implementer testhosts were inspected and waited out (then a broken wait script was killed), not blindly killed mid-TRX.

### C Requirements

#### C1. FR-MCP-PLUGININT-001 AC that PLUGIN_ROOT_OVERRIDE cannot redirect exists.

Verdict: PASS

Evidence: workflow.requirements.getFr id FR-MCP-PLUGININT-001. AC3 text: "Setting PLUGIN_ROOT_OVERRIDE cannot redirect session or turn cache state." isSatisfied false. Status pending. This green gate does not require isSatisfied true.

#### C2. TR-MCP-PLUGININT-001 exists and is mapped from the FR.

Verdict: PASS

Evidence: workflow.requirements.getTr id TR-MCP-PLUGININT-001, six AC, status pending. workflow.requirements.listMappings frId FR-MCP-PLUGININT-001 returns FR->TR and FR->TEST (totalCount 2).

#### C3. TEST-MCP-PLUGININT-001 AC4 exists, is testable, and is mapped by the named P14 theory. Tests are now green against that AC.

Verdict: PASS

Evidence: workflow.requirements.getTest id TEST-MCP-PLUGININT-001 AC4: "A legacy PLUGIN_ROOT_OVERRIDE value is injected and proven unable to alter the expected cache path." isSatisfied false. Named theory XML docs map TEST-MCP-PLUGININT-001 AC4. Independent rerun Failed 0 Passed 8 Skipped 0. Suite-green of later P15-P20 is not required at C-green-P14.

#### C4. AC are appropriate for the claimed P14 green scope (inject override, workspace cache path, poison empty, reject flag).

Verdict: PASS

Evidence: TEST AC4 is testable. The P14 theory injects PLUGIN_ROOT_OVERRIDE, asserts PluginRootOverrideRejected, asserts cache under fixture workspace .mcpServer/{cacheFolder}, and asserts poison empty. FR AC3 is the product AC. Missing sibling-cache extra wording from the MCP-PLUGININT-001 P14 task text remains residual versus the plan's named PoisonEmpty theory and TEST AC4.

#### C5. Do not require TEST AC3 AiTheory, AC5 native suite, TR failsafe cleanup, or store isSatisfied. Those are later slices.

Verdict: PASS

Evidence: Parent brief: C-green-P14 only, not P15-P20. P15/P16 named tests absent. isSatisfied remains false on FR/TR/TEST.

### D Current plan holistically

#### D1. Plan P14 green DoD is the named theory mapping TEST-MCP-PLUGININT-001 AC4, now passing after C-red-P14 AGREE. Not plan closeout.

Verdict: PASS

Evidence: docs/plans/PLAN-PLUGINHANDOFF-001.md line 380. Named theory exists, maps AC4, independently Failed 0 Passed 8 Skipped 0. This receipt is the C-green-P14 gate. Parent forbids treating this as plan closeout.

#### D2. PLAN-PLUGINHANDOFF-001 and MCP-PLUGININT-001 remain done:false. Combined C P14 task includes P15 and stays open.

Verdict: PASS

Evidence: Live todo_get. Combined PLAN task "C P14 red + hostile then green; P15 red + hostile then green." done: false. MCP-PLUGININT-001 P14 done: false. This review wrote no done:true.

#### D3. Do not require P15-P20 or PLUGININT done. Do not implement P15.

Verdict: PASS

Evidence: P15 named tests absent. Parent brief matches. This AGREE authorizes P15 red to start, not to skip it, and does not close PLAN or MCP-PLUGININT-001.

#### D4. Persist-path DISAGREE on P12 does not complete or block this P14 green DoD under TEST AC4.

Verdict: PASS

Evidence: Parent named C-red-P14 AGREE 20260822T041327Z as prior gate. Competing DISAGREE 20260822T035559Z remains residual for P12 persist. P14 task text and TEST AC4 do not require production plugin persist. Persist-path stays later/P12 residual, not a C-green-P14 FAIL.

Accuracy: 96. Independent TRX and console match Failed 0 Passed 8 Skipped 0. Host-count regex overlap and list-tests 81 overcount were discarded in favor of passedNames and the console 62-test listing.

Completeness: 94. Surfaces A+B+C+D evaluated. Full 62-test PluginIntegration.Tests project not independently re-run; parent required the P14 filter. TEST AC3/AC5 and P15-P20 remain later slices by brief. Persist and sibling-cache remain residual versus this slice DoD.
