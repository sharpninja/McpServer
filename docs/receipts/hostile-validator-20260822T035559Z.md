# Hostile Validator Receipt

TimestampUtc: 2026-08-22T03:55:59Z
ValidatorIdentity: GrokSubagentHostile
Workspace: F:\GitHub\McpServer
WorkClass: 1 (project implementation PLAN-PLUGINHANDOFF-001 Phase C-green-P11-P13, plugin Session Log workflow adapter greens). Surfaces A+B+C+D all apply. Review only. No product implementation by this validator. This AGREE would not authorize P14. PLAN-PLUGINHANDOFF-001 and MCP-PLUGININT-001 must remain not done.
add-profile: executed yes. Profile files read: 18 (every non-skill *.md under C:\Users\kingd\.claude\profile\; excluded add-profile.grok.md). Also read add-profile SKILL.md and hostile-validator SKILL.md.
Plugin: F:\GitHub\mcpserver-grok-plugin (.grok-plugin/plugin.json version 1.100.0; .version 1.100.0). Live workflow/client calls used Invoke-McpPlugin.ps1. Tool registry search included exact name mcpserver-grok-plugin (TOOL_SEARCH_EXACT=True).
Active plan: F:\GitHub\McpServer\docs\plans\PLAN-PLUGINHANDOFF-001.md section 7 P11-P13.
Requirement IDs: FR-MCP-PLUGININT-001, TR-MCP-PLUGININT-001 AC3, TEST-MCP-PLUGININT-001 AC1/AC2.
Prior receipt: docs/receipts/hostile-validator-20260822T023808Z.md (OverallVerdict AGREE, FailCount 0, C-red-P11; filter Failed 8 Passed 0; P12/P13 absent; ExecuteCanonicalTurnAsync still threw not implemented).
Collector: docs/receipts/_hv-c-green-p11-p13/
OverallVerdict: DISAGREE

Default was FAIL or UNKNOWN until this pass independently re-read PluginSessionLogWorkflowAdapter.cs and PluginSessionLogWorkflowAdapterTests.cs, grepped P14 in tests/src (absent), live-got PLAN-PLUGINHANDOFF-001 and MCP-PLUGININT-001, live-got FR/TR/TEST/listMappings, re-ran `dotnet test tests/McpServer.PluginIntegration.Tests -c Debug` (Passed 54 Failed 0 Skipped 0), and re-ran the required filter after the first filter process aborted. Implementer chat and implementer logs were not trusted as proof.

## Clock and live MCP

Health GET /health?nonce=a6f4a99ac05b4938ad16c7415d2dc4db returned status Healthy, storage reachable, nonce echoed exactly (docs/receipts/_hv-c-green-p11-p13/health-nonce.json). Plugin Test-MarkerSignature true; Invoke-FullBootstrap true (plugin-marker-sig.json). Homemade HMAC in marker-sig.json Match=false is a payload-construction false-negative; plugin Test-MarkerSignature is the authoritative check. Plugin Status available after wrapper invoke. Tool registry search includes exact name mcpserver-grok-plugin.

## Session log proof

SessionId GrokSubagentHostile-20260822T032633Z-c-green-p11-p13. Turn requestId req-20260822T032633Z-001-hostile-c-green-p11-p13. client.SessionLog.OpenSessionAsync created=true. BeginTurnAsync turnId 42874. Persistence is proven by client.SessionLog.QueryAsync after completeTurn (see sl-query-sid-after.txt written by finish-session.ps1).

## Mandatory surface that could not be evaluated

None.

## Explicit FAIL list

- A2-persist: ExecuteCanonicalTurnAsync persists through fixture.CreateTrustedClient() (typed McpServer.Client REST), not plugin production session-log tools. PluginHostProcessAdapter.LaunchAsync result is discarded. SessionId/RequestId are fabricated in the adapter. CachePath is Directory.CreateDirectory under the fixture workspace. SourceSha is SHA256 of entrypoint file bytes. Stdin is a fake JSON object, not a plugin workflow envelope. AdapterCallsWorkflowSessionlog false. AdapterCallsInvokeMcpPlugin false.
- C3: TEST-MCP-PLUGININT-001 AC2 is not proven as the real plugin Session Log workflow. P12/P13 assert IDs after the client shortcut. Durable query via McpServer.Client is the P13 half only.
- C5: TR-MCP-PLUGININT-001 AC3 requires PowerShell/hook and Node SDK plugins invoked through supported production entrypoints, not by bypassing them with raw REST. Launching the entrypoint for 5 seconds and ignoring stdout does not satisfy AC3 when persist is the typed client.
- D4: Plan P12 DoD is bootstrap/begin/append/complete on the plugin (MCP-PLUGININT-001 P12: "bootstrap the plugin"; technicalDetails: "shared library bypasses are prohibited"). Green tests that persist via McpServer.Client do not complete that slice DoD. P13 client query is in-plan. This review does not treat P14 as in-scope.

## Residual (not extra FAILs)

- First required filter command aborted with ExitCode -1, DurationMs 243380, no TRX (dotnet-adapter-filter-exit.json, filter-trx-summary.json exists=false). A concurrent testhost was also running PluginIntegration.Tests into trx-adapter. Independent recovery: wait until RemainingHostCount 0, then `dotnet test ... --no-build --filter FullyQualifiedName~PluginSessionLogWorkflowAdapterTests` ExitCode 0, console Passed 24 Failed 0 Skipped 0 Total 24, Duration 6 m 5 s (dotnet-adapter-filter-rerun.log / .trx).
- TRX Counters.skipped attribute is missing. Console Skipped: 0; TRX notExecuted=0.
- Homemade HMAC Match=false. Plugin Test-MarkerSignature true.
- PythonProcessCount 1 is Google Cloud SDK gcloud.py auth login (PID 69564). This validator did not invoke python/python3/py.
- workflow.* result envelopes include deprecated: true. Treated as success metadata, not wrapper failure.
- all-trx-summary.json Cline Count 2 is a regex overlap with ClineV2. Rerun parser with hostKind: Cline) counts Cline 1 and ClineV2 1. p11Count/p12Count/p13Count remain 8.
- TEST-MCP-PLUGININT-001 AC3 AiTheory, AC4 PLUGIN_ROOT_OVERRIDE, AC5 native suite, and TR AC4 failsafe cleanup are later slices (P14-P19). This gate does not require those greens.
- workflow.requirements getFr/getTr/getTest createdAt stamps look like query time. Do not treat that stamp as a recreate. AC text is present. isSatisfied remains false; this gate does not claim store AC complete.
- PLAN remaining note already says next is C-green-P11-P13 hostile. Combined PLAN task "C P7 red + hostile then P8-P10; P11 red + hostile then P12-P13." remains done:false. This review does not change goal state.
- git status --porcelain shows ?? tests/McpServer.PluginIntegration.Tests/ and ?? docs/plans/PLAN-PLUGINHANDOFF-001.md. Untracked does not by itself block a green gate, and does not repair the persist shortcut.
- Concurrent D2-green collector `_hv-d2g-20260822T032428Z` was running Pester/C# tests. Not a P11-P13 FAIL.

## Claims reviewed

### A Requested

#### A1. Prior C-red-P11 AGREE exists at docs/receipts/hostile-validator-20260822T023808Z.md. Filter then Failed 8 Passed 0. P12/P13 absent.

Verdict: PASS

Evidence: File exists Length 17108 LastWriteTimeUtc 2026-08-22T02:45:04Z. OverallVerdict AGREE. JSON Phase C-red-P11, FilterFailed 8, FilterPassed 0, P12NamedTestsPresent false, P13NamedTestsPresent false, ExecuteCanonicalTurnAsyncThrowsNotImplemented true. Markdown states ExecuteCanonicalTurnAsync still threw and grepped P12/P13 absent.

#### A2a. PluginSessionLogWorkflowAdapter.ExecuteCanonicalTurnAsync no longer throws not implemented. It launches PluginHostProcessAdapter, then OpenSession/BeginTurn/AppendDialog/CompleteTurn on fixture.CreateTrustedClient(), returns AdapterOperational true plus session/request ids, cache path under .mcpServer/{cacheFolder}, 64-char SHA.

Verdict: PASS (as a description of the current implementation and of what the green tests assert)

Evidence: Adapter.cs no longer contains the not-implemented throw (adapter-scan AdapterThrowExact false). Lines 57-73 construct PluginHostProcessAdapter and call LaunchAsync. Lines 81-138 call fixture.CreateTrustedClient() then OpenSessionAsync, BeginTurnAsync, AppendDialogAsync, CompleteTurnAsync. Return sets AdapterOperational true, SessionId, RequestId, Status completed, CachePath, SourceSha from SHA256.HashData of entrypoint bytes (64 hex chars). Independent full suite and filter-rerun TRX notImplementedCount 0. Full suite Passed 54 including all eight P11 rows.

#### A2-persist. Persist is via plugin production tools, not a typed client shortcut.

Verdict: FAIL

Evidence: adapter-scan.json AdapterHasCreateTrustedClient true, AdapterDiscardsLaunchResult true (`_ = await processAdapter.LaunchAsync`), AdapterFabricatesSessionId true, AdapterCreatesCacheDirectory true, AdapterHashesEntrypointBytes true, AdapterCallsWorkflowSessionlog false, AdapterCallsInvokeMcpPlugin false, AdapterStdinFakeJson true, AdapterLaunchTimeoutSeconds5 true. Launch stdin is `{"method":"sessionlog_begin_turn","hostKind":"..."}`. Extra PowerShell args `-Command Status` are appended after `-File entrypoint`, so they are script parameters, not a pwsh -Command invocation. MCP-PLUGININT-001 technicalDetails: "Each plugin is exercised through its declared production entrypoint; shared library bypasses are prohibited." Parent asked this attack explicitly.

#### A3. Named tests exist without Skip: Theory_EachScenario_FailsUntilAdapterOperational (now expected green), Theory_Agent_BootstrapBeginAppendComplete_CapturesIdsCacheShaReceipt, Theory_Agent_ServerQuery_SourceTypeIdsActionDialogCompletedWorkspace. File: PluginSessionLogWorkflowAdapterTests.cs. Eight InlineData host kinds each.

Verdict: PASS

Evidence: File read this review. Three `[Theory(Timeout = 120000)]` methods at lines 25, 65, 100. Each has eight `[InlineData(PluginHostKind.*)]` rows: Codex, ClaudeCode, ClaudeCowork, Copilot, Grok, Cline, ClineV2, OpenCode. TestsSkipAttribute false, TestsAssertSkip false, SkipHitsInTestsFile empty. P14 method absent in this file.

#### A4. Independent re-run of the required filter and full PluginIntegration.Tests. Failed 0 Skipped 0 required for AGREE on this green gate.

Verdict: PASS (counts). Does not override A2-persist FAIL.

Evidence: Full command `dotnet test tests/McpServer.PluginIntegration.Tests -c Debug` ExitCode 0, console Passed! Failed 0 Passed 54 Skipped 0 Total 54, Duration 8 m 37 s (dotnet-pluginintegration-all.log / .trx; all-trx-summary executed 54 passed 54 failed 0 notExecuted 0; p11Passed 8 p12Passed 8 p13Passed 8 p14Count 0 skippedNames empty). Required filter recovered after wait RemainingHostCount 0: `dotnet test tests/McpServer.PluginIntegration.Tests -c Debug --no-build --filter FullyQualifiedName~PluginSessionLogWorkflowAdapterTests` ExitCode 0, console Passed! Failed 0 Passed 24 Skipped 0 Total 24, Duration 6 m 5 s (dotnet-adapter-filter-rerun.log / .trx; filter-rerun-trx-summary executed 24 passed 24 failed 0 notExecuted 0; eight hosts each for P11/P12/P13; p14Count 0; failedNames empty; skippedNames empty). First filter abort is residual, not the cited receipt.

#### A5. P14 Theory_Agent_PluginRootOverride_WritesOnlyWorkspaceMcpServerAgent_PoisonEmpty is absent. This AGREE does not authorize P14.

Verdict: PASS

Evidence: adapter-scan TestsP14Present false. P14HitsTestsSrcCount 0. Filter-rerun and full TRX p14Count 0. This review wrote no P14 tests.

#### A6. PLAN-PLUGINHANDOFF-001 and MCP-PLUGININT-001 remain done:false.

Verdict: PASS

Evidence: live workflow.todo.get and client.Todo.GetAsync. PLAN-PLUGINHANDOFF-001 done: false. Combined PLAN task "C P7 red + hostile then P8-P10; P11 red + hostile then P12-P13." done false. MCP-PLUGININT-001 done: false. P11/P12/P13/P14 implementationTasks done false. This review wrote no todo_update.

### B Workspace rules

#### B1. Honesty and receipts. Every done/green claim re-verified with machine evidence.

Verdict: PASS

Evidence: Independent dotnet test output and TRX under docs/receipts/_hv-c-green-p11-p13/. Parent described CreateTrustedClient persist honestly; this review still failed the production-entrypoint contract rather than treating honesty as AC coverage. Result XML docs say "Session id returned by the plugin workflow" while the adapter fabricates ids; that overclaim is cited under A2-persist, not a second B1 FAIL.

#### B2. Byrd v4 phase-order for this class-1 slice. Phase-order is scored at inter-phase hostile gates, not FR-vs-file timestamps.

Verdict: PASS

Evidence: C-red-P11 hostile AGREE exists (docs/receipts/hostile-validator-20260822T023808Z.md). Adapter LastWriteTimeUtc 2026-08-22T02:51:04Z and tests LastWriteTimeUtc 2026-08-22T02:59:38Z after that AGREE. This review does not FAIL B2 from FR createdAt versus file mtimes.

#### B3. MCP-only TODO/session/requirements storage.

Verdict: PASS

Evidence: Live todo_get/client.Todo.GetAsync and requirements get/listMappings via Invoke-McpPlugin.ps1. Session bootstrap/open/begin via client.SessionLog.* wrapper. This validator did not read or write TODO.yaml or session-log store files.

#### B4. PowerShell-only / no Python lab rule.

Verdict: PASS

Evidence: All collector, bootstrap, test, and session commands used pwsh.exe -NoProfile -NonInteractive. no-python-hits.json PythonProcessCount 1 is unrelated gcloud.py. No python.exe invocation by this review.

#### B5. Look-before-delete.

Verdict: PASS

Evidence: This review deleted no product files. Collector only removed prior TRX in the collector folder before a fresh independent test run.

#### B6. No em-dash / en-dash in adapter product/test files.

Verdict: PASS

Evidence: adapter-scan.json EmDashTests false, EnDashTests false, EmDashAdapter false, EnDashAdapter false.

### C Requirements

#### C1. FR-MCP-PLUGININT-001 exists with structured AC.

Verdict: PASS

Evidence: workflow.requirements.getFr id FR-MCP-PLUGININT-001. Five AC rows ac-1 through ac-5 present. status pending. isSatisfied false on all AC (expected; not closeout). AC1: every supported plugin bootstraps, begins, appends action/dialog, completes, and verifies durable server-visible content.

#### C2. TEST-MCP-PLUGININT-001 AC1 exists and is testable: exactly eight scenario rows covering the eight hosts.

Verdict: PASS

Evidence: workflow.requirements.getTest id TEST-MCP-PLUGININT-001 ac-1 text: "Exactly eight scenario rows cover Codex, Claude Code, Claude Cowork, Copilot, Grok, Cline, Cline v2, and OpenCode." Eight InlineData host kinds match. Filter-rerun p11/p12/p13 each Count 1 Passed 1 for those eight hosts.

#### C3. Named P12/P13 tests cover TEST-MCP-PLUGININT-001 AC2 (bootstrap, begin, append action/dialog, complete, durable query, workspace cache isolation) as the real plugin workflow. Suite green is not treated as substitute coverage.

Verdict: FAIL

Evidence: TEST AC2 text: "Every deterministic row proves bootstrap, begin, append action/dialog, complete, durable query, and workspace cache isolation." Description: "exercise the real Session Log workflow for all supported agent plugins." P12 body only asserts adapter result fields after ExecuteCanonicalTurnAsync. P13 queries fixture.CreateTrustedClient(). Neither inspects plugin stdout, workflow.sessionlog receipts, nor production tool names. Cache isolation is Path.Contains(.mcpServer/cacheFolder) on a directory the adapter created itself. Durable query proves the typed client write, not plugin persist. FR AC1 requires plugin surfaces to complete the canonical workflow.

#### C4. FR/TR/TEST mappings exist.

Verdict: PASS

Evidence: workflow.requirements.listMappings frId FR-MCP-PLUGININT-001 returned two items: FR-MCP-PLUGININT-001 -> TR-MCP-PLUGININT-001 and FR-MCP-PLUGININT-001 -> TEST-MCP-PLUGININT-001 (req-map.txt totalCount 2).

#### C5. TR-MCP-PLUGININT-001 AC3 production entrypoints, not raw REST.

Verdict: FAIL

Evidence: Live getTr ac-3: "PowerShell/hook plugins and Node SDK plugins are invoked through their supported production entrypoints, not by bypassing them with raw REST." McpServer.Client is the REST typed client. LaunchAsync is fire-and-forget with a 5s timeout and discarded PluginProcessLaunchResult. That is a bypass with a decorative process start. P13 is allowed to query via McpServer.Client. P12 persist is not.

### D Current plan holistically

#### D1. Implementer claimed C-green-P11-P13 greens exist, not PLAN complete, and not P14.

Verdict: PASS

Evidence: PLAN-PLUGINHANDOFF-001 done false. MCP-PLUGININT-001 done false. Parent brief forbids P14 and forbids marking those TODOs done. This review does not mark plan/TODO done.

#### D2. P14 greens or reds are not mixed into this gate.

Verdict: PASS

Evidence: Named P14 theory absent from tests/src. Full TRX p14Count 0. Filter-rerun p14Count 0.

#### D3. Master TODOs were not claimed done.

Verdict: PASS

Evidence: Live todo_get topDone false for both PLAN-PLUGINHANDOFF-001 and MCP-PLUGININT-001. Combined C P7-P13 PLAN task done false. P11/P12/P13 implementationTasks done false.

#### D4. Plan DoD for this slice: after C-red-P11 AGREE, P12 workflow half bootstraps the plugin and captures ids/cache/sha; P13 durable query via McpServer.Client; P11 eight rows now green; Failed 0 Skipped 0; P14 not in this gate.

Verdict: FAIL

Evidence: Tests are Failed 0 Skipped 0 for PluginIntegration.Tests (54) and the adapter filter (24). P13 query-via-client matches the plan sentence. P12 does not bootstrap plugin workflow: MCP-PLUGININT-001 P12 task text is "bootstrap the plugin, begin a canonical turn, append one action and one decision dialog, complete the turn"; technicalDetails prohibit shared library bypasses. ExecuteCanonicalTurnAsync uses the shared client after discarding the production launch. A green local shortcut does not complete the plan DoD for P12. OverallVerdict remains DISAGREE even though the numeric test gate is green.

## Accuracy and completeness

Accuracy: 96. Independent full-suite and recovered filter counts, adapter persist path, live TODO flags, and prior C-red-P11 AGREE were re-verified. First filter abort was recovered rather than hidden.

Completeness: 95. Surfaces A+B+C+D evaluated. P14, AiTheory, failsafe P15, and native-suite P19 correctly out of scope. Filter command was re-run after a concurrent testhost drained.

This review does not implement P14 and does not set any TODO done:true.
