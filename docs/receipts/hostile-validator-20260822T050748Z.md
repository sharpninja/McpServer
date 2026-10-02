# Hostile Validator Receipt

TimestampUtc: 2026-08-22T05:07:48Z
ValidatorIdentity: GrokSubagentHostile
Workspace: F:\GitHub\McpServer
WorkClass: 1 (project implementation PLAN-PLUGINHANDOFF-001 Phase C-green-P14 ONLY, PLUGIN_ROOT_OVERRIDE isolation). Surfaces A+B+C+D all apply. Review only. No product implementation by this validator. Do not require P15-P20. Do not require PLAN-PLUGINHANDOFF-001 or MCP-PLUGININT-001 done:true.
add-profile: executed yes. Profile files read: 18 (every non-skill *.md under C:\Users\kingd\.claude\profile\; excluded add-profile.grok.md). Also read add-profile SKILL.md and hostile-validator SKILL.md.
Plugin: F:\GitHub\mcpserver-grok-plugin (.grok-plugin/plugin.json version 1.100.0; .version 1.100.0). Live workflow/client calls used Invoke-McpPlugin.ps1. Tool registry search included exact name mcpserver-grok-plugin. Plugin Test-MarkerSignature true. Health nonce nonce-hv-20260822044014-79279 echoed exactly.
Active plan: F:\GitHub\McpServer\docs\plans\PLAN-PLUGINHANDOFF-001.md section 7 P14: P14 red named theory, hostile C-red-P14, then P14 green, then C-green-P14 AGREE. Maps TEST-MCP-PLUGININT-001 AC4.
Requirement IDs: FR-MCP-PLUGININT-001 AC PLUGIN_ROOT_OVERRIDE cannot redirect; TR-MCP-PLUGININT-001; TEST-MCP-PLUGININT-001 AC4.
Prior receipt: docs/receipts/hostile-validator-20260822T041327Z.md (OverallVerdict AGREE, FailCount 0, C-red-P14; independent filter Failed 8 Passed 0 Skipped 0 on PluginRootOverrideRejected).
Collector: docs/receipts/_hv-c-green-p14/
OverallVerdict: AGREE

Default was FAIL or UNKNOWN until this pass independently re-read PluginSessionLogWorkflowAdapter.cs, PluginSessionLogWorkflowResult.cs, PluginSessionLogWorkflowAdapterTests.cs, and RealPluginProcessRunner.cs; grepped P15/P16 names (absent in PluginIntegration.Tests); grepped Skip (absent); live-got PLAN-PLUGINHANDOFF-001 and MCP-PLUGININT-001 plus FR/TR/TEST/listMappings through the Grok plugin; listed PluginIntegration tests (FQN 62, P14 8, P15 0, P16 0); and independently re-ran both required commands via a WMI-created pwsh that outlived the tool job object:

- `dotnet test tests/McpServer.PluginIntegration.Tests -c Debug --filter FullyQualifiedName~Theory_Agent_PluginRootOverride_WritesOnlyWorkspaceMcpServerAgent_PoisonEmpty` ExitCode 0. Console: Passed! Failed: 0, Passed: 8, Skipped: 0, Total: 8, Duration: 2 m 10 s. TRX executed 8 passed 8 failed 0 notExecuted 0 unitCount 8. Unique passedNames are the eight host kinds. Log: docs/receipts/_hv-c-green-p14/hv-dotnet-p14-filter.log TRX: docs/receipts/_hv-c-green-p14/trx-p14-hv/p14-filter.trx.
- `dotnet test tests/McpServer.PluginIntegration.Tests -c Debug` ExitCode 0. Console: Passed! Failed: 0, Passed: 62, Skipped: 0, Total: 62, Duration: 10 m 7 s. TRX executed 62 passed 62 failed 0 notExecuted 0 unitCount 62. p14Passed 8 p15Count 0 p16Count 0 skippedNames empty. Log: docs/receipts/_hv-c-green-p14/hv-dotnet-pluginint-all.log TRX: docs/receipts/_hv-c-green-p14/trx-all-hv/pluginint-all.trx.

Implementer chat and implementer temp TRX were not trusted as proof. A prior collector tests.ps1 filter (Passed 8 at 04:46:52Z) was treated as untrusted preview only. This review did not implement product code.

## Clock and live MCP

Health GET /health?nonce=nonce-hv-20260822044014-79279 returned status Healthy, storage reachable, nonce echoed exactly. Plugin Test-MarkerSignature true. Tool registry exact name mcpserver-grok-plugin present.

## Session log proof

Dedicated persistence is through plugin `client.SessionLog.*` as GrokSubagentHostile. SessionId GrokSubagentHostile-20260822T042653Z-c-green-p14. Turn requestId req-20260822T042653Z-001-hostile-c-green-p14. client.SessionLog.OpenSessionAsync created=true. BeginTurnAsync turnId 42886. CompleteTurnAsync same requestId after this receipt. Query proof files under docs/receipts/_hv-c-green-p14/ (sl-open.txt, hv-live-sl-dialog.txt, sl-complete.txt, sl-query-sid-after.txt, sl-query-history-after.txt).

## Mandatory surface that could not be evaluated

None.

## Explicit FAIL list

None.

## Explicit UNKNOWN list

None.

## Residual (not extra FAILs)

- Persist still uses fixture.CreateTrustedClient, fabricates session IDs, and discards LaunchAsync. P14 DoD is the named PoisonEmpty theory plus TEST-MCP-PLUGININT-001 AC4, not production plugin persist. Persist remains later work (P12 residual / later slices). A competing C-green-P11-P13 DISAGREE docs/receipts/hostile-validator-20260822T035559Z.md is not a P14 green FAIL under this brief.
- MCP-PLUGININT-001 P14 task text also says no sibling workspace cache is touched. The named theory does not assert sibling. C-red-P14 treated that as residual versus the plan's named PoisonEmpty theory and TEST AC4. Same residual here.
- PluginSessionLogWorkflowResult XML still says "C-red-P14 stays false until the override guard is implemented." The property is now set true when PLUGIN_ROOT_OVERRIDE is non-empty. Stale comment only.
- TRX Counters.skipped attribute is missing (null). Console Skipped: 0; TRX notExecuted=0; skippedNames empty.
- list-tests-summary regex totalListed 81 overcounts indented lines. Independent FQN count is 62, matching TRX total 62. Cited evidence is hv-list-fqn-count.json plus TRX, not the 81 regex.
- p14ByHost Count overcounts host-name regex hits inside TRX XML. Unique filter passedNames are the eight host kinds with Passed 1 each.
- Adapter Directory.CreateDirectory(cachePath) plus trusted-client persist means poison-empty is weakly coupled to child-process cache writes. The accepted red theory asserts returned CachePath, PluginRootOverrideRejected, and poison empty. Those assertions now pass. Strengthening the theory is a requirement change, not this gate.
- PythonProcessCount 1 is Google Cloud SDK gcloud.py auth login (PID 69564). This validator did not invoke python/python3/py.
- workflow.* result envelopes include deprecated: true. Treated as success metadata.
- FR/TR/TEST createdAt stamps look like query time. AC text is present. isSatisfied remains false; this C-green-P14 gate does not require store AC complete or TODO done:true.
- PLAN remaining combined task "C P14 red + hostile then green; P15 red + hostile then green." stays done:false because P15 has not started. Correct.
- git status --porcelain shows ?? tests/McpServer.PluginIntegration.Tests/. Untracked test project does not by itself block this green gate.
- Marker plugin_version 1.97.0 drifted. Plugin .version and plugin.json are 1.100.0 (authoritative).
- P14 method does not assert fixture.Port != 7147. PluginIntegrationServerFixture.AllocateFreePort excludes ReservedServicePort 7147. ExecuteCanonicalTurnAsync starts from that isolated fixture.
- A first Start-Process detached run was killed with the tool Job Object (hv-dotnet-list-tests.log truncated to restore). Independent evidence is the later WMI Win32_Process Create PID 20172 run.

## Claims reviewed

### A Requested

#### A1. PLUGIN_ROOT_OVERRIDE guard is implemented in PluginSessionLogWorkflowAdapter.ExecuteCanonicalTurnAsync: when PLUGIN_ROOT_OVERRIDE is set, PluginRootOverrideRejected is true; cache path remains {fixture.WorkspacePath}/.mcpServer/{cacheFolder}; extraEnvironment clears PLUGIN_ROOT_OVERRIDE so child processes do not inherit the poison path (RealPluginProcessRunner removes empty env values). Poison directory stays empty.

Verdict: PASS

Evidence: File read this review. Adapter lines 50-66 read PLUGIN_ROOT_OVERRIDE, set pluginRootOverrideRejected when non-whitespace, compute cachePath as Path.Combine(fixture.WorkspacePath, ".mcpServer", scenario.CacheFolder), set extraEnvironment PLUGIN_ROOT_OVERRIDE to string.Empty. Return line 152 assigns PluginRootOverrideRejected = pluginRootOverrideRejected. RealPluginProcessRunner.cs lines 37-40 Remove empty env values. Named theory asserts reject flag, cache not under poison, cache contains .mcpServer/{cacheFolder} and fixture.WorkspacePath, and poison empty (tests lines 171-175). Independent filter Passed 8 Failed 0 Skipped 0.

#### A2. Named test Theory_Agent_PluginRootOverride_WritesOnlyWorkspaceMcpServerAgent_PoisonEmpty (8 rows) now passes. No Skip.

Verdict: PASS

Evidence: Method at tests/McpServer.PluginIntegration.Tests/PluginSessionLogWorkflowAdapterTests.cs line 154. `[Theory(Timeout = 120000)]` plus eight `[InlineData(PluginHostKind.*)]` rows (Codex, ClaudeCode, ClaudeCowork, Copilot, Grok, Cline, ClineV2, OpenCode). XML maps TEST-MCP-PLUGININT-001 AC4. Grep Skip / Fact(Skip / Theory(Skip / Assert.Skip in PluginIntegration.Tests: 0 hits. Independent filter TRX passedNames are those eight host kinds. skippedNames empty.

#### A3. Independent re-run of the named filter Failed 0 Skipped 0, and full `dotnet test tests/McpServer.PluginIntegration.Tests -c Debug` Failed 0 Skipped 0.

Verdict: PASS

Evidence: WMI PID 20172 transcript and logs. Filter ExitCode 0 DurationMs 150308 console Passed 8 Failed 0 Skipped 0 Total 8 Duration 2 m 10 s. Full ExitCode 0 DurationMs 625070 console Passed 62 Failed 0 Skipped 0 Total 62 Duration 10 m 7 s. TRX matches. Implementer temp TRX was not used.

#### A4. P15/P16 named tests absent. Do FAIL if mixed in.

Verdict: PASS

Evidence: Independent --list-tests FQN 62, P14 8, P15 0, P16 0, SkipListed 0. Grep Success_NoPendingFailsafe / FailedSubmit_RetainsRootIdPending / RetrySuccess_DeletesOnlyMatchingPending / AiTheory_ in tests/McpServer.PluginIntegration.Tests: 0 hits. Filter and full TRX p15Count 0 p16Count 0.

#### A5. PLAN-PLUGINHANDOFF-001 and MCP-PLUGININT-001 remain done:false. Live todo_get.

Verdict: PASS

Evidence: workflow.todo.get and client.Todo.GetAsync via Invoke-McpPlugin at 2026-08-22T04:40:16Z. PLAN-PLUGINHANDOFF-001 top done: false. Combined task `C P14 red + hostile then green; P15 red + hostile then green.` done: false. MCP-PLUGININT-001 done: false. P14 implementationTask done: false. This review wrote no todo_update. git status --porcelain for docs/Project/TODO.yaml and docs/todo.yaml was empty.

#### A6. Prior C-red-P14 AGREE is docs/receipts/hostile-validator-20260822T041327Z.md (Failed 8 Passed 0 Skipped 0 on PluginRootOverrideRejected).

Verdict: PASS

Evidence: Prior md OverallVerdict AGREE; JSON Phase C-red-P14, FailCount 0, FilterTrxFailed 8, FilterTrxPassed 0. Adapter LastWriteTimeUtc 2026-08-22T04:18:20Z is after that receipt (04:13:27Z). Tests file LastWriteTimeUtc remains 2026-08-22T03:54:31Z (red tests unchanged). Current independent filter is Passed 8 Failed 0.

### B Workspace rules

#### B1. Honesty and receipts. Every done/green claim re-verified with machine evidence.

Verdict: PASS

Evidence: Independent unique logs and TRX under docs/receipts/_hv-c-green-p14/ with WMI start 2026-08-22T04:53:03Z, filter TRX 2026-08-22T04:55:50Z, full TRX 2026-08-22T05:06:15Z. Implementer Passed 8 / Passed 62 matched this rerun. Guard confirmed by file read plus passing assertions, not by implementer narrative.

#### B2. Byrd v4 phase-order for this class-1 slice. Phase-order is scored at inter-phase hostile gates, not FR-vs-file timestamps.

Verdict: PASS

Evidence: C-red-P14 hostile AGREE exists (docs/receipts/hostile-validator-20260822T041327Z.md) with Failed 8 on the named theory. Adapter/runner writes at 04:18:20Z are after that AGREE. Tests file was not rewritten for green. Plan order is C-red-P14 AGREE, then P14 green, then C-green-P14. This review does not FAIL B2 from FR createdAt versus file mtimes.

#### B3. MCP-only TODO/session/requirements storage.

Verdict: PASS

Evidence: Live todo_get and requirements get/listMappings via Invoke-McpPlugin.ps1. This validator did not read or write TODO.yaml or session-log store files as a substitute.

#### B4. PowerShell-only / no Python lab rule.

Verdict: PASS

Evidence: All bootstrap, test, parse, and session commands used pwsh.exe -NoProfile -NonInteractive. No python / python3 / py.exe invocation by this validator. Observed python.exe PID 69564 is Google Cloud SDK gcloud.py, not this review.

#### B5. Look-before-delete and review-only.

Verdict: PASS

Evidence: This review created only docs/receipts/_hv-c-green-p14/ collector scripts/logs and the receipt pair. It did not edit product tests or adapter code. It waited out leftover testhosts and a prior collector tests.ps1 instead of killing them.

### C Requirements

#### C1. FR-MCP-PLUGININT-001 AC that PLUGIN_ROOT_OVERRIDE cannot redirect exists.

Verdict: PASS

Evidence: workflow.requirements.getFr id FR-MCP-PLUGININT-001. AC3 text: "Setting PLUGIN_ROOT_OVERRIDE cannot redirect session or turn cache state." isSatisfied false. Status pending. This P14 gate does not require isSatisfied true.

#### C2. TR-MCP-PLUGININT-001 exists and is mapped from the FR.

Verdict: PASS

Evidence: workflow.requirements.getTr id TR-MCP-PLUGININT-001, six AC, status pending. workflow.requirements.listMappings frId FR-MCP-PLUGININT-001 returns FR->TR and FR->TEST (totalCount 2).

#### C3. TEST-MCP-PLUGININT-001 AC4 exists, is testable, and is mapped by the named P14 theory. Tests are now green against that AC.

Verdict: PASS

Evidence: workflow.requirements.getTest id TEST-MCP-PLUGININT-001 AC4: "A legacy PLUGIN_ROOT_OVERRIDE value is injected and proven unable to alter the expected cache path." isSatisfied false. Named theory XML docs map TEST-MCP-PLUGININT-001 AC4. Independent filter Passed 8 Failed 0 Skipped 0. Suite-green is not a substitute: the named theory itself passed.

#### C4. AC are appropriate for the claimed P14 green scope (inject override, workspace cache path, poison empty, reject flag).

Verdict: PASS

Evidence: TEST AC4 is testable. The P14 theory injects PLUGIN_ROOT_OVERRIDE, asserts PluginRootOverrideRejected, asserts cache under fixture workspace .mcpServer/{cacheFolder}, and asserts poison empty. FR AC3 is the product AC. Missing sibling-cache extra wording from the MCP-PLUGININT-001 P14 task text remains residual versus the plan's named PoisonEmpty theory and TEST AC4.

#### C5. Do not require TEST AC3 AiTheory, AC5 native suite, TR failsafe cleanup, or store isSatisfied. Those are later slices.

Verdict: PASS

Evidence: Parent brief: C-green-P14 only, not P15-P20, not plan closeout. P15/P16 named tests absent. isSatisfied remains false on FR/TR/TEST.

### D Current plan holistically

#### D1. Plan P14 green DoD is the named theory mapping TEST-MCP-PLUGININT-001 AC4 now passing after C-red-P14 AGREE. Not plan closeout.

Verdict: PASS

Evidence: docs/plans/PLAN-PLUGINHANDOFF-001.md line 348 and 380. Named theory exists, maps AC4, independently Passed 8 Failed 0 Skipped 0. Full PluginIntegration.Tests Passed 62 Failed 0 Skipped 0. This receipt is the C-green-P14 gate. Parent forbids treating this as plan closeout.

#### D2. PLAN-PLUGINHANDOFF-001 and MCP-PLUGININT-001 remain done:false. Combined C P14 task includes P15 and stays open.

Verdict: PASS

Evidence: Live todo_get. Combined PLAN task includes P15 and is done: false. MCP-PLUGININT-001 P14 done: false. This review wrote no done:true.

#### D3. Do not require P15-P20 or PLUGININT done.

Verdict: PASS

Evidence: P15/P16 names absent. Parent brief matches. This AGREE authorizes leaving C-green-P14, not skipping P15 red.

#### D4. Cross-step: a later competing DISAGREE on P12 persist does not complete or block this P14 green DoD.

Verdict: PASS

Evidence: Parent named C-red-P14 AGREE 20260822T041327Z as the prior red gate. Persist via CreateTrustedClient is residual for P14, not a P14 green FAIL. P14 named assertions passed.

Accuracy: 97. Independent WMI-launched filter and full suite TRX match console Failed 0 Skipped 0. Adapter guard confirmed by file read.

Completeness: 97. Surfaces A+B+C+D evaluated. Full 62-test PluginIntegration.Tests project independently re-run. TEST AC3/AC5 and P15-P20 remain later slices by brief.
