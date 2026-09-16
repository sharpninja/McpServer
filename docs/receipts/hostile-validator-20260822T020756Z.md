# Hostile Validator Receipt

TimestampUtc: 2026-08-22T02:07:56Z
ValidatorIdentity: GrokSubagentHostile
Workspace: F:\GitHub\McpServer
WorkClass: 1 (project implementation PLAN-PLUGINHANDOFF-001 Phase C-green-P7-P10 ONLY, plugin Session Log process-adapter greens). Surfaces A+B+C+D all apply. Review only. No product implementation by this validator. Do not require P11-P20. PLAN-PLUGINHANDOFF-001 and MCP-PLUGININT-001 must remain not done.
add-profile: executed yes. Profile files read: 18 (every non-skill *.md under C:\Users\kingd\.claude\profile\; excluded add-profile.grok.md). Also read add-profile SKILL.md and hostile-validator SKILL.md.
Plugin: F:\GitHub\mcpserver-grok-plugin (.grok-plugin/plugin.json version 1.99.0; .version 1.99.0). Live workflow/client calls used Invoke-McpPlugin.ps1. Tool registry search included exact name mcpserver-grok-plugin.
Active plan: F:\GitHub\McpServer\docs\plans\PLAN-PLUGINHANDOFF-001.md section 7 P7-P10.
Requirement IDs: FR-MCP-PLUGININT-001, TR-MCP-PLUGININT-001 AC3 (production entrypoints via process I/O, not raw REST), TEST-MCP-PLUGININT-001.
Prior receipt: docs/receipts/hostile-validator-20260822T014442Z.md (OverallVerdict AGREE, FailCount 0, C-red-P7; LaunchAsync threw not implemented; Failed 8 Passed 14 Skipped 0).
Collector: docs/receipts/_hv-c-green-p7-p10/
OverallVerdict: AGREE

Default was FAIL or UNKNOWN until this pass independently re-ran `dotnet test tests/McpServer.PluginIntegration.Tests -c Debug --filter FullyQualifiedName~PluginHostProcessAdapterTests`, re-ran the full PluginIntegration.Tests project, re-read PluginHostProcessAdapter.cs and PluginHostProcessAdapterTests.cs on disk, grepped Theory_EachScenario_FailsUntilAdapterOperational in tests/src (absent), live-got PLAN-PLUGINHANDOFF-001 and MCP-PLUGININT-001, live-got FR/TR/TEST/listMappings, and opened a dedicated GrokSubagentHostile session. Implementer chat and implementer logs were not trusted as proof.

## Clock and live MCP

Health GET /health?nonce=77858f1f53cf4072b553e65e24861976 returned status Healthy, storage reachable, nonce echoed exactly (docs/receipts/_hv-c-green-p7-p10/health-nonce.json). Plugin Test-MarkerSignature true; Invoke-FullBootstrap true (docs/receipts/_hv-c-green-p7-p10/plugin-marker-sig.json). Homemade HMAC in marker-sig.json Match=false is a payload-construction false-negative; plugin Test-MarkerSignature is the authoritative check. Plugin Status available after wrapper invoke. Tool registry search includes exact name mcpserver-grok-plugin (TOOL_SEARCH_EXACT=True).

## Session log proof

SessionId GrokSubagentHostile-20260822T020204Z-c-green-p7-p10. Turn requestId req-20260822T020204Z-001-hostile-c-green-p7-p10. client.SessionLog.OpenSessionAsync created=true. BeginTurnAsync turnId 42850. Persistence proof is recorded after completeTurn in docs/receipts/_hv-c-green-p7-p10/sl-*.txt.

## Mandatory surface that could not be evaluated

None.

## Explicit FAIL list

None.

## Residual (not extra FAILs)

- TRX Counters.skipped attribute is missing. Console output Skipped: 0; TRX notExecuted=0, executed=16/30, failed=0, passed=16/30.
- Homemade HMAC Match=false. Plugin Test-MarkerSignature true.
- Isolated CacheRoot Status snapshot before OpenSession reported no-session and agent GrokCode; persistence is the client.SessionLog created=true / turnId 42850 path, not that Status snapshot.
- Collector python scanner matched the substring python inside filenames no-python-scan.json and no-python-hits.json. No python/python3/py.exe invocation. Residual of the scanner pattern, not a lab Python use.
- Copilot sibling repo contains Invoke-CopilotMcpPlugin.ps1, but the locked catalog row declares lib/Invoke-McpPlugin.ps1. P8 is catalog-declared entrypoints. Adapter and P8 tests follow the catalog.
- Tests require IPluginProcessRunner.LastRequest and unique fake exit/stdout/stderr. They do not also assert that no HttpClient REST call occurred. Adapter.cs HttpClient hits 0 and LaunchAsync returns _runner.RunAsync only. Residual, not a C-green-P7-P10 FAIL.
- MCP-PLUGININT-001 P1-P20 implementationTasks remain done:false except P0. PLAN task "C P7 red + hostile then P8-P10; P11 red + hostile then P12-P13." remains done:false. This review does not change goal state.
- workflow.requirements getFr/getTr/getTest createdAt stamps look like query time. Do not treat that stamp as a recreate. AC text is present. isSatisfied remains false; this gate does not claim store AC complete.
- Node theory rows assert entrypoint in args and executable node; they do not forbid extra PowerShell flags on the node argument list. Adapter node branch adds only the entrypoint path.
- git status --porcelain shows ?? tests/McpServer.PluginIntegration.Tests/. Untracked does not block this green gate.
- workflow.* result envelopes include deprecated: true. Treated as success metadata, not wrapper failure.
- TEST-MCP-PLUGININT-001 AC3 is AiTheory semantic completeness (P16). This gate covers TR-MCP-PLUGININT-001 AC3 production entrypoints via process I/O, not TEST AC3.

## Claims reviewed

### A Requested

#### A1. PluginHostProcessAdapter.LaunchAsync is implemented (no longer throws "not implemented"). It maps PowerShell hosts (Codex, ClaudeCode, ClaudeCowork, Copilot, Grok) to pwsh.exe with -NoProfile -NonInteractive -File plus catalog entrypoint path; Node hosts (Cline, ClineV2, OpenCode) to node plus catalog entrypoint; forwards stdin, env (required vars, PLUGIN_AGENT_NAME=AgentSourceType, *_PLUGIN_ROOT=plugin root), cwd=plugin root, timeout; returns fake runner exit/stdout/stderr. File: tests/McpServer.PluginIntegration.Tests/PluginHostProcessAdapter.cs.

Verdict: PASS

Evidence: File read this review. LaunchAsyncThrowsNotImplemented false. NotImplementedHitCount 0. RunnerRunAsyncHitCount 1 at line 103 `return _runner.RunAsync(request, cancellationToken);`. DummyLaunchResultCtorCount 0. AdapterHttpClientHits 0. AdapterMapsPwshFile true. AdapterMapsNode true. AdapterSetsPluginAgentName true. AdapterSetsPluginRootEnv true. AdapterWorkingDirectoryPluginRoot true. AdapterTimeoutForwarded true. AdapterStdinForwarded true. IsPowerShellHost covers Codex, ClaudeCode, ClaudeCowork, Copilot, Grok. Else branch executable node plus entrypointPath. Environment maps *_PLUGIN_ROOT to pluginRoot and PLUGIN_AGENT_NAME to scenario.AgentSourceType.

#### A2. Named tests exist and are not skipped: Adapter_CapturesExecutableArgsStdinEnvCwdTimeoutExitStdoutStderr (eight PluginHostKind theory rows); Adapter_CodexAndPowerShellHook_UseDeclaredEntrypoint (Codex + four PowerShell-hook kinds); Adapter_ClineV1_StdioSessionLogTools; Adapter_ClineV2AndOpenCode_UseProductionExport (ClineV2, OpenCode). File: PluginHostProcessAdapterTests.cs.

Verdict: PASS

Evidence: adapter-scan.json NamedTests MatchCount 1 each in PluginHostProcessAdapterTests.cs. P7IsTheory true, P7InlineDataCount 8, all eight HostKind InlineDataPresent true. P8IsTheory true, P8Inline all five PowerShell-hook kinds true. P9IsFact true. P10IsTheory true. SkipAttributeOnAdapterTests false. SkipLiteralFactSkip false. SkipLiteralTheorySkip false. SkipIfOrAssertSkipCount 0. Filter TRX p7Count 8 p8Count 5 p9Count 1 p10Count 2, skippedNames empty.

#### A3. Independent re-run required. Filter FullyQualifiedName~PluginHostProcessAdapterTests Failed 0 Skipped 0 (implementer last saw Passed 16 Failed 0 Skipped 0 Duration 190 ms). Full `dotnet test tests/McpServer.PluginIntegration.Tests -c Debug` Failed 0 Skipped 0 (implementer last saw Passed 30 Failed 0 Skipped 0 Duration 1 m 14 s).

Verdict: PASS

Evidence: Independent filter command exit 0, console Passed! Failed: 0, Passed: 16, Skipped: 0, Total: 16, Duration 196 ms (dotnet-adapter-filter.log, filter-trx-summary.json executed 16 passed 16 failed 0 notExecuted 0). Full project command exit 0, console Passed! Failed: 0, Passed: 30, Skipped: 0, Total: 30, Duration 1 m 29 s (dotnet-pluginintegration-all.log, all-trx-summary.json executed 30 passed 30 failed 0 notExecuted 0). Filter wall-clock 40337 ms includes restore/build; test body 196 ms. Full wall-clock 106164 ms. Counts match implementer. Duration difference is not a FAIL.

#### A4. P11 reds are NOT mixed in: Theory_EachScenario_FailsUntilAdapterOperational must be absent from tests/src. This gate does not require live plugin bootstrap/begin/append/complete against the fixture (that is P11-P13). Adapter unit tests using FakePluginProcessRunner are the P8-P10 scope.

Verdict: PASS

Evidence: adapter-scan.json P11MatchCountTestsSrc 0, P11Paths empty. Grep of tests/ and src/ *.cs *.csproj for Theory_EachScenario_FailsUntilAdapterOperational returned no product/test hits (plan markdown and prior receipts only). Filter TRX p11Count 0. Full TRX p11Count 0. FakePluginProcessRunner LastRequest private set assigned only inside RunAsync line 24.

#### A5. PLAN-PLUGINHANDOFF-001 and MCP-PLUGININT-001 remain done:false. Live todo_get required. This AGREE is not plan closeout.

Verdict: PASS

Evidence: live workflow.todo.get and client.Todo.GetAsync. PLAN-PLUGINHANDOFF-001 topDone false (todo-plan-done-extract.json). Combined PLAN task "C P7 red + hostile then P8-P10; P11 red + hostile then P12-P13." done false. MCP-PLUGININT-001 topDone false. P7/P8/P9/P10/P11 implementationTasks done false. This review wrote no todo_update.

#### A6. Prior C-red-P7 AGREE is docs/receipts/hostile-validator-20260822T014442Z.md (Failed 8 Passed 14 Skipped 0, LaunchAsync not implemented). Confirm LaunchAsync actually calls IPluginProcessRunner.RunAsync rather than returning dummy results that skip the fake.

Verdict: PASS

Evidence: prior-cred-p7-agree.json OverallVerdictLineMatch true, JsonOverallVerdict AGREE, JsonFailCount 0, MentionsNotImplemented true, MentionsFailed8 true, MentionsPassed14 true, LaunchAsyncThrowsNotImplemented true, P8NamedTestsPresent false, FilterFailed 8, AllPassed 14, AllFailed 8. Prior md LastWriteTimeUtc 2026-08-22T01:46:20Z. Adapter.cs LastWriteTimeUtc 2026-08-22T01:50:26Z after that AGREE. Current LaunchAsync returns `_runner.RunAsync(request, cancellationToken)` (line 103). DummyLaunchResultCtorCount 0. Fake LastRequest is private set and assigned only in RunAsync. P7 test asserts fake.LastRequest not null and result.ExitCode/StandardOutput/StandardError equal the unique NextResult values (100+(int)hostKind, stdout-hostKind, stderr-hostKind). A dummy return that skipped the fake would leave LastRequest null and fail all eight P7 rows. Independent rerun Passed 8/8 on that theory.

### B Workspace rules

#### B1. Honesty and receipts. Every done/green claim re-verified with machine evidence.

Verdict: PASS

Evidence: Independent dotnet test output and TRX on disk under docs/receipts/_hv-c-green-p7-p10/. Implementer 16/30 counts matched. LaunchAsync implementation confirmed by file read, not by implementer narrative.

#### B2. Byrd v4 phase-order for this class-1 slice. Phase-order is scored at inter-phase hostile gates, not FR-vs-file timestamps.

Verdict: PASS

Evidence: C-red-P7 hostile AGREE exists (docs/receipts/hostile-validator-20260822T014442Z.md). P8-P10 green followed that AGREE. Adapter implementation LastWriteTime after the C-red-P7 receipt. This review does not FAIL B2 from FR createdAt versus file mtimes. Full PluginIntegration.Tests Failed 0 Skipped 0 is the green-phase suite for this project slice.

#### B3. MCP-only TODO/session/requirements storage.

Verdict: PASS

Evidence: Live todo_get/client.Todo.GetAsync and requirements get/listMappings via Invoke-McpPlugin.ps1. Session bootstrap/open/begin via client.SessionLog.* wrapper. git status --porcelain for docs/Project/TODO.yaml was empty (todo-yaml-git-status.json). This validator did not read or write TODO.yaml or session-log files.

#### B4. PowerShell-only / no Python lab rule.

Verdict: PASS

Evidence: All collector, bootstrap, test, and session commands used pwsh.exe -NoProfile -NonInteractive. no-python-hits.json two hits are the scanner matching the substring python inside no-python-scan.json and no-python-hits.json path strings. No python.exe / python3 / py.exe invocation.

#### B5. Look-before-delete.

Verdict: PASS

Evidence: This review deleted no product files. Collector only removed prior TRX in the collector folder before a fresh independent test run.

#### B6. No em-dash / en-dash in adapter product/test files.

Verdict: PASS

Evidence: adapter-scan.json EmDashAdapter false, EmDashTests false.

### C Requirements

#### C1. FR-MCP-PLUGININT-001 exists with structured AC.

Verdict: PASS

Evidence: workflow.requirements.getFr id FR-MCP-PLUGININT-001. Five AC rows ac-1 through ac-5 present. status pending. isSatisfied false on all AC (expected; not closeout).

#### C2. TR-MCP-PLUGININT-001 AC3 exists and is testable for this slice: production entrypoints via process I/O, not raw REST.

Verdict: PASS

Evidence: workflow.requirements.getTr id TR-MCP-PLUGININT-001 ac-3 text: "PowerShell/hook plugins and Node SDK plugins are invoked through their supported production entrypoints, not by bypassing them with raw REST." isSatisfied false (expected). AC is testable: catalog entrypoint, executable, args, no repl-invoke, process runner seam.

#### C3. Named P7-P10 tests cover the AC3 slice in the test body, not comments. Suite green is not treated as substitute coverage.

Verdict: PASS

Evidence: P7 asserts executable pwsh.exe vs node, catalog entrypoint in args, -NoProfile -NonInteractive -File for PowerShell hosts, stdin, cwd=plugin root, timeout, required env, PLUGIN_AGENT_NAME, *_PLUGIN_ROOT, and fake exit/stdout/stderr. P8 asserts pwsh.exe -File catalog path, Codex ends with Invoke-CodexMcpPlugin.ps1, other PowerShell-hook kinds contain Invoke-McpPlugin.ps1, arguments do not contain repl-invoke.ps1. P9 asserts node, dist/index.js, sessionlog stdin, CLINE_PLUGIN_ROOT, no repl-invoke.ps1. P10 asserts node, dist/index.js, no repl-invoke.ps1, no Invoke-McpPlugin.ps1. Sibling probe: all eight catalog entrypoints exist and IsReplInvoke false. Adapter.cs has zero HttpClient hits.

#### C4. FR/TR/TEST mappings exist.

Verdict: PASS

Evidence: workflow.requirements.listMappings frId FR-MCP-PLUGININT-001 returned two items: FR-MCP-PLUGININT-001 -> TR-MCP-PLUGININT-001 and FR-MCP-PLUGININT-001 -> TEST-MCP-PLUGININT-001 (req-map.txt totalCount 2).

#### C5. TEST-MCP-PLUGININT-001 exists. This gate does not require TEST AC2 live bootstrap or TEST AC3 AiTheory.

Verdict: PASS

Evidence: workflow.requirements.getTest id TEST-MCP-PLUGININT-001. AC1 eight scenarios; AC2 bootstrap/begin/append/complete (P11-P13); AC3 AiTheory (P16); AC4 PLUGIN_ROOT_OVERRIDE (P14); AC5 native suite (P19). Parent brief forbids requiring P11-P20. Store isSatisfied remains false. Not a FAIL.

### D Current plan holistically

#### D1. Implementer claimed C-green-P7-P10 only, not PLAN closeout.

Verdict: PASS

Evidence: PLAN-PLUGINHANDOFF-001 done false. MCP-PLUGININT-001 done false. Plan section 7 order is C-red-P7 AGREE, then P8-P10 green, then C-green-P7-P10 AGREE. This review does not mark plan/TODO done.

#### D2. P11 reds are not mixed into this green gate.

Verdict: PASS

Evidence: Theory_EachScenario_FailsUntilAdapterOperational absent from tests/src. No live fixture bootstrap/begin/append/complete tests added. P11 implementationTask done false.

#### D3. Master TODOs were not claimed done.

Verdict: PASS

Evidence: Live todo_get topDone false for both PLAN-PLUGINHANDOFF-001 and MCP-PLUGININT-001. Combined C P7-P13 PLAN task done false.

#### D4. Plan DoD for this slice: P8 catalog PowerShell entrypoints, P9 Cline v1 dist/index.js, P10 Cline v2 and OpenCode production export not raw repl-invoke.ps1, after C-red-P7 AGREE.

Verdict: PASS

Evidence: Named tests and independent green rerun cover those P8-P10 pins. Catalog rows match. Prior C-red-P7 AGREE on disk. Full PluginIntegration.Tests Failed 0 Skipped 0 keeps P1-P6 greens plus P7-P10 greens.

## Accuracy and completeness

Accuracy: 97. Independent test counts, LaunchAsync runner call, live TODO flags, and prior C-red-P7 AGREE were re-verified. Residual scanner false-positive and Copilot extra wrapper file do not change the verdict.

Completeness: 96. Surfaces A+B+C+D evaluated. Live plugin bootstrap/begin/append/complete against the fixture was correctly out of scope (P11-P13). TEST AC3 AiTheory out of scope (P16).
