# Hostile Validator Receipt

TimestampUtc: 2026-08-22T01:44:42Z
ValidatorIdentity: GrokSubagentHostile
Workspace: F:\GitHub\McpServer
WorkClass: 1 (project implementation PLAN-PLUGINHANDOFF-001 Phase C-red-P7 ONLY, plugin Session Log process-adapter contract reds). Surfaces A+B+C+D all apply. Review only. No product implementation by this validator. Do not require P8-P10 green. PLAN-PLUGINHANDOFF-001 and MCP-PLUGININT-001 must remain not done.
add-profile: executed yes. Profile files read: 18 (every non-skill *.md under C:\Users\kingd\.claude\profile\; excluded add-profile.grok.md). Also read add-profile SKILL.md and hostile-validator SKILL.md.
Plugin: F:\GitHub\mcpserver-grok-plugin (.grok-plugin/plugin.json version 1.99.0; .version 1.99.0). Live workflow/client calls used Invoke-McpPlugin.ps1. Tool registry search included exact name mcpserver-grok-plugin.
Active plan: F:\GitHub\McpServer\docs\plans\PLAN-PLUGINHANDOFF-001.md section 7 P7.
Requirement IDs: FR-MCP-PLUGININT-001, TR-MCP-PLUGININT-001 (AC3 process-capture), TEST-MCP-PLUGININT-001.
Prior receipt: docs/receipts/hostile-validator-20260822T012355Z.md (OverallVerdict AGREE, FailCount 0, C-green-P5-P6; grepped Adapter_CapturesExecutable as NO_MATCH).
Collector: docs/receipts/_hv-c-red-p7/
OverallVerdict: AGREE

Default was FAIL or UNKNOWN until this pass independently re-ran `dotnet test tests/McpServer.PluginIntegration.Tests -c Debug --filter FullyQualifiedName~Adapter_CapturesExecutableArgsStdinEnvCwdTimeoutExitStdoutStderr`, re-ran the full PluginIntegration.Tests project, re-read PluginHostProcessAdapter.cs and PluginHostProcessAdapterTests.cs on disk, grepped P8-P10 named tests (absent), live-got PLAN-PLUGINHANDOFF-001 and MCP-PLUGININT-001, live-got FR/TR/TEST/listMappings, and opened a dedicated GrokSubagentHostile session. Implementer chat and implementer logs were not trusted as proof.

## Clock and live MCP

Health GET /health?nonce=ff129a213c5344a0a2c83fe2837f1e7a returned status Healthy, storage reachable, nonce echoed exactly (docs/receipts/_hv-c-red-p7/health-nonce.json). Plugin Test-MarkerSignature true; Invoke-FullBootstrap true (docs/receipts/_hv-c-red-p7/plugin-marker-sig.json). Homemade HMAC in marker-sig.json Match=false is a payload-construction false-negative; plugin Test-MarkerSignature is the authoritative check. Plugin Status available after wrapper invoke. Tool registry search includes exact name mcpserver-grok-plugin (TOOL_SEARCH_EXACT=True).

## Session log proof

SessionId GrokSubagentHostile-20260822T010850Z-c-red-p7. Turn requestId req-20260822T010850Z-001-hostile-c-red-p7-adapter. client.SessionLog.OpenSessionAsync created=true. BeginTurnAsync turnId 42845. Persistence proof is recorded after completeTurn in docs/receipts/_hv-c-red-p7/sl-*.txt.

## Mandatory surface that could not be evaluated

None.

## Explicit FAIL list

None.

## Residual (not extra FAILs)

- TRX Counters.skipped attribute is missing. Console output Skipped: 0; TRX notExecuted=0, executed=8/22, failed=8, passed=0/14.
- Homemade HMAC Match=false. Plugin Test-MarkerSignature true.
- Isolated CacheRoot Status snapshot before OpenSession reported no-session; persistence is the client.SessionLog created=true / turnId 42845 path, not that Status snapshot.
- SessionId timestamp used format HHMM (MM=month) so 010850Z rather than wall-clock minutes around 0139Z. Identifier still unique and persisted.
- MCP-PLUGININT-001 P1-P6 and P7-P20 implementationTasks remain done:false. PLAN task "C P7 red + hostile then P8-P10; P11 red + hostile then P12-P13." remains done:false. This review does not change goal state.
- workflow.requirements getFr/getTr/getTest createdAt stamps look like query time. Do not treat that stamp as a recreate. AC text is present. isSatisfied remains false; this gate does not claim store AC complete.
- PluginHostProcessAdapter constructor does not store the runner field. LaunchAsync validates then throws. That is the red stub, not a hidden green.
- Node theory rows assert entrypoint in args and executable node; they do not forbid extra PowerShell flags on the node argument list.
- Tests require IPluginProcessRunner.LastRequest; they do not also assert that no HttpClient REST call occurred. A green that both launches a process and calls REST would still be process I/O. Residual, not a C-red-P7 FAIL.
- git status --porcelain shows ?? tests/McpServer.PluginIntegration.Tests/. Untracked does not block this red gate.
- workflow.* result envelopes include deprecated: true. Treated as success metadata, not wrapper failure.

## Claims reviewed

### A Requested

#### A1. PLAN-PLUGINHANDOFF-001 C-red-P7 named test exists: Adapter_CapturesExecutableArgsStdinEnvCwdTimeoutExitStdoutStderr as a Theory with eight InlineData PluginHostKind rows: Codex, ClaudeCode, ClaudeCowork, Copilot, Grok, Cline, ClineV2, OpenCode. File: tests/McpServer.PluginIntegration.Tests/PluginHostProcessAdapterTests.cs. No [Skip].

Verdict: PASS

Evidence: File read this review. [Theory] at line 18, eight [InlineData(PluginHostKind.*)] rows lines 19-26, method line 27. adapter-scan.json NamedTestPresent true, IsTheory true, InlineDataCount 8, all eight HostKind InlineDataPresent true, SkipAttributeOnTheory false, SkipLiteralFactSkip false, SkipLiteralTheorySkip false. Csproj Skip hits 0. Filter TRX executed 8, skippedNames empty, notExecuted 0.

#### A2. PluginHostProcessAdapter.LaunchAsync currently throws InvalidOperationException "PluginHostProcessAdapter.LaunchAsync is not implemented." Tests currently fail. Independent re-run required. Filter must show Failed 8 Passed 0 Skipped 0. Full project must keep prior P1-P6 greens and only the eight P7 rows failing (implementer last saw Failed 8 Passed 14 Skipped 0 Total 22).

Verdict: PASS

Evidence: PluginHostProcessAdapter.cs line 36 throw new InvalidOperationException("PluginHostProcessAdapter.LaunchAsync is not implemented."); LaunchAsyncHasOtherReturn false. Independent filter command exit 1, console Failed: 8, Passed: 0, Skipped: 0, Total: 8, Duration 176 ms (dotnet-adapter-filter.log, filter-trx-summary.json, p7NotImplementedCount 8). Full project command exit 1, console Failed: 8, Passed: 14, Skipped: 0, Total: 22, Duration 1 m 16 s (dotnet-pluginintegration-all.log, all-trx-summary.json). Failed names are only the eight hostKind rows. Passed names are the 6 catalog Facts, 1 XMLDocs project Fact, and 7 ServerFixture Facts. Every failed message is PluginHostProcessAdapter.LaunchAsync is not implemented. Stack points at Adapter.cs:36 and Tests.cs:53 (the LaunchAsync call), not catalog load.

#### A3. These are new reds, not relabeled P5/P6 greens. Prior C-green-P5-P6 AGREE is docs/receipts/hostile-validator-20260822T012355Z.md which grepped Adapter_CapturesExecutable as NO_MATCH. P8-P10 named tests are absent.

Verdict: PASS

Evidence: prior-cgreen-p5-p6-agree.json OverallVerdictLineMatch true, JsonOverallVerdict AGREE, JsonFailCount 0, MentionsNoMatchLiteral true, MentionsAdapterNoMatch true. Prior md LastWriteTimeUtc 2026-08-22T01:25:09Z. Adapter test files LastWriteTimeUtc 2026-08-22T01:29:52Z and adapter stub 2026-08-22T01:30:59Z, after that AGREE. P5/P6 ServerFixture_* methods still exist and Passed in the full run. adapter-scan ForbiddenNamedTests MatchCount 0 for Adapter_CodexAndPowerShellHook_UseDeclaredEntrypoint, Adapter_ClineV1_StdioSessionLogTools, Adapter_ClineV2AndOpenCode_UseProductionExport. all-trx p8Count 0. P7 name exists only in the two adapter files.

#### A4. Tests pin TR-MCP-PLUGININT-001 AC3 process-capture contract so a trivial no-op would not green all eight. Pins must be in the test body, not comments.

Verdict: PASS

Evidence: PluginHostProcessAdapterTests.cs assertions (not comments): Assert.NotNull(fake.LastRequest); Assert.Equal(ExpectedExecutable(hostKind), fake.LastRequest.Executable) where ExpectedExecutable is pwsh.exe for Codex/ClaudeCode/ClaudeCowork/Copilot/Grok else node; Assert.Contains(entrypointPath, fake.LastRequest.Arguments); PowerShell hosts also Assert.Contains -NoProfile, -NonInteractive, -File; Assert.Equal(stdin, fake.LastRequest.StandardInput); Assert.Equal(pluginRoot, fake.LastRequest.WorkingDirectory) with pluginRoot = sibling Directory.GetParent(repoRoot)+RepositoryName; Assert.Equal(timeout, fake.LastRequest.Timeout) with timeout unique per hostKind (20+(int)hostKind seconds); Assert.Equal expectedExit/stdout/stderr from FakePluginProcessRunner.NextResult; Assert.All required catalog env names present and non-whitespace; Assert.Equal(scenario.AgentSourceType, PLUGIN_AGENT_NAME); *_PLUGIN_ROOT equals pluginRoot. FakePluginProcessRunner records LastRequest only inside RunAsync. C-red-P4 lesson attack: a trivial return of an empty PluginProcessLaunchResult leaves LastRequest null and fails all eight. A hardcoded single executable fails the node vs pwsh split. Unique timeout/exit/stdout per hostKind blocks a one-constant stub. Catalog LoadAndValidate succeeded (failures are unimplemented, not missing sibling roots). sibling-plugin-probe.json RootExists and EntrypointExists true for all eight.

#### A5. PLAN-PLUGINHANDOFF-001 and MCP-PLUGININT-001 remain done:false. Live todo_get required. This gate does not close those TODOs.

Verdict: PASS

Evidence: live workflow.todo.get and client.Todo.GetAsync. PLAN-PLUGINHANDOFF-001 done: false (todo-plan-done-extract.json topDone false). MCP-PLUGININT-001 done: false. P7/P8/P9/P10 implementationTasks done: false. PLAN combined task "C P7 red + hostile then P8-P10; P11 red + hostile then P12-P13." done: false. This review wrote no todo_update.

#### A6. C-red-P7 is red-phase only: adapter mapping unimplemented on purpose. Do not FAIL this gate for missing P8-P10 implementation. Do FAIL if LaunchAsync is actually implemented and the eight rows pass, or if P8-P10 greens are mixed in, or if any of the eight theory rows is skipped.

Verdict: PASS (attack not sustained)

Evidence: LaunchAsync throws not implemented; filter Passed 0; full p7Passed 0 p7Skipped 0 p8Count 0. No theory Skip. This review does not require P8-P10 greens.

### B Workspace rules

#### B1. Honesty: implementer claims match independently re-verified artifacts.

Verdict: PASS

Evidence: Filter and full counts match the claimed Failed 8 Passed 14 Skipped 0 Total 22. Exception text matches. Eight host kinds match. No fabricated green.

#### B2. Always bring the receipts: this review re-ran commands and re-read files. Byrd phase-order is this inter-phase red gate, not FR createdAt vs file mtime.

Verdict: PASS

Evidence: Collector logs and TRX under docs/receipts/_hv-c-red-p7/. Adapter tests LastWriteTime after C-green-P5-P6 AGREE write. Not scored from FR createdAt.

#### B3. MCP-only storage: no direct edit of todo.yaml / session logs / requirements store.

Verdict: PASS

Evidence: Store reads via Invoke-McpPlugin.ps1 workflow.todo.get, client.Todo.GetAsync, workflow.requirements.*, client.SessionLog.*. git status porcelain for the test project is untracked tests only, not docs/todo.yaml. This review did not write TODO or session-log storage files.

#### B4. Lab PowerShell / no Python.

Verdict: PASS

Evidence: All collector/test/session work used pwsh.exe -NoProfile -NonInteractive. no-python-scan.json CollectorPythonHits 0. No python/python3/py invoked.

#### B5. Byrd v4 for this class-1 slice: red tests first, implementation not yet, prior C-green-P5-P6 AGREE exists before these reds.

Verdict: PASS

Evidence: LaunchAsync unimplemented. Tests fail. Prior AGREE receipt exists. Adapter files after that receipt. Plan section 7: C-red-P7 AGREE, then P8-P10. This gate is the red review, not the green.

#### B6. Look-before-delete.

Verdict: PASS

Evidence: No deletes of operator data. Collector New-Item -Force on docs/receipts/_hv-c-red-p7 only.

### C Requirements

#### C1. FR-MCP-PLUGININT-001, TR-MCP-PLUGININT-001, TEST-MCP-PLUGININT-001 exist with structured AC.

Verdict: PASS

Evidence: live getFr/getTr/getTest. FR has five AC, isSatisfied false. TR has six AC including AC3 "PowerShell/hook plugins and Node SDK plugins are invoked through their supported production entrypoints, not by bypassing them with raw REST." TEST has five AC. status pending.

#### C2. Mappings FR to TR and FR to TEST exist.

Verdict: PASS

Evidence: workflow.requirements.listMappings frId FR-MCP-PLUGININT-001 returned items FR-MCP-PLUGININT-001/TR-MCP-PLUGININT-001 and FR-MCP-PLUGININT-001/TEST-MCP-PLUGININT-001, totalCount 2 (req-map.txt).

#### C3. This red slice's tests cover TR-MCP-PLUGININT-001 AC3 process-capture (executable/args/stdin/env/cwd/timeout/exit/stdout/stderr via fake process runner). Do not require P8-P10 production invocation greens.

Verdict: PASS

Evidence: Test XML summary and body cite TR-MCP-PLUGININT-001 AC3. Assertions listed in A4. IPluginProcessRunner / FakePluginProcessRunner / PluginProcessLaunchRequest/Result exist. Full eight-agent Theory bootstrap is P11+, AiTheory is TEST AC3 / P16. This brief forbids requiring those here.

#### C4. Store AC isSatisfied remains false. Suite-green is not used as AC coverage.

Verdict: PASS

Evidence: All PLUGININT AC isSatisfied false. Executed scope is Failed 8, not claimed green.

### D Plan

#### D1. Implementer claims C-red-P7 red tests exist and are currently failing, not that PLAN-PLUGINHANDOFF-001 is complete. AGREE on this red gate is not plan closeout.

Verdict: PASS

Evidence: Plan section 7 P7 text matches the named test. PLAN top done false. Combined C P7 task done false. This receipt OverallVerdict AGREE does not authorize done:true.

#### D2. Did not mix P8-P10 greens into this red gate.

Verdict: PASS

Evidence: P8-P10 named tests absent. Full project failures are only the eight P7 rows. Prior P1-P6 greens remaining passing is required prior-scope, not mixed new greens.

#### D3. Plan forbids mixing red and green in one hostile gate. This gate evaluated only C-red-P7.

Verdict: PASS

Evidence: Scope lock honored. P8-P10 implementation not required and not present as passing named tests.

## Independent test commands

Filter: `dotnet test tests/McpServer.PluginIntegration.Tests -c Debug --filter FullyQualifiedName~Adapter_CapturesExecutableArgsStdinEnvCwdTimeoutExitStdoutStderr`

Filter exit 1. Failed 8 Passed 0 Skipped 0 Total 8.

Full: `dotnet test tests/McpServer.PluginIntegration.Tests -c Debug`

Full exit 1. Failed 8 Passed 14 Skipped 0 Total 22.

## Accuracy and completeness

Accuracy: 97. Independently re-ran the exact filter and the full project, re-read adapter source, grepped P8-P10 names, live todo_get both IDs, live FR/TR/TEST/listMappings, marker trust and health nonce.

Completeness: 96. Surfaces A+B+C+D all scored. Residuals recorded. Session turn opened; completeTurn/query proof follows in collector sl-complete/sl-query files.

## Verdict

OverallVerdict: AGREE
FailCount: 0
UnknownCount: 0
This AGREE is the C-red-P7 inter-phase gate only. It is not PLAN-PLUGINHANDOFF-001 closeout and not MCP-PLUGININT-001 done.
