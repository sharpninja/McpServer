# Hostile Validator Receipt

TimestampUtc: 2026-08-22T01:23:55Z
ValidatorIdentity: GrokSubagentHostile
Workspace: F:\GitHub\McpServer
WorkClass: 1 (project implementation PLAN-PLUGINHANDOFF-001 Phase C-green-P5-P6 only, isolated compiled server fixture greens). Surfaces A+B+C+D all apply. Review only. No product implementation by this validator. Do not require P7-P20. PLAN-PLUGINHANDOFF-001 must remain not done.
add-profile: executed yes. Profile files read: 18 (every non-skill *.md under C:\Users\kingd\.claude\profile\; excluded add-profile.grok.md). Also read add-profile SKILL.md and hostile-validator SKILL.md.
Plugin: F:\GitHub\mcpserver-grok-plugin (.grok-plugin/plugin.json version 1.99.0; .version 1.99.0). Live workflow/client calls used Invoke-McpPlugin.ps1. Tool registry search included exact name mcpserver-grok-plugin.
Active plan: F:\GitHub\McpServer\docs\plans\PLAN-PLUGINHANDOFF-001.md section 7 P5/P6.
Requirement IDs: FR-MCP-PLUGININT-001, TR-MCP-PLUGININT-001, TEST-MCP-PLUGININT-001.
Prior receipt: docs/receipts/hostile-validator-20260822T005108Z.md (OverallVerdict AGREE, FailCount 0, C-red-P5; PluginIntegrationServerFixtureTests Failed 6 Passed 0 Skipped 0; StartAsync throws not implemented).
Collector: docs/receipts/_hv-c-green-p5-p6/
OverallVerdict: AGREE

Default was FAIL or UNKNOWN until this pass independently re-ran `dotnet test tests/McpServer.PluginIntegration.Tests -c Debug --filter FullyQualifiedName~PluginIntegrationServerFixtureTests`, re-ran the full PluginIntegration.Tests project, re-read PluginIntegrationServerFixture.cs and PluginIntegrationServerFixtureTests.cs on disk, grepped Adapter_CapturesExecutable (absent in tests/src *.cs and *.csproj), live-got PLAN-PLUGINHANDOFF-001 and MCP-PLUGININT-001, live-got FR/TR/TEST/listMappings, and opened a dedicated GrokSubagentHostile session. Implementer chat and implementer log were not trusted as proof.

## Clock and live MCP

Health GET /health?nonce=aa3c36eb250446bda62d0c8fb6ae9905 returned status Healthy, storage reachable, nonce echoed exactly (docs/receipts/_hv-c-green-p5-p6/health-nonce.json). Plugin Test-MarkerSignature true; Invoke-FullBootstrap true (docs/receipts/_hv-c-green-p5-p6/plugin-marker-sig.json). Homemade HMAC in marker-sig.json Match=false is a payload-construction false-negative; plugin Test-MarkerSignature is the authoritative check. Plugin Status available. Tool registry search includes exact name mcpserver-grok-plugin (TOOL_SEARCH_EXACT=True).

## Session log proof

SessionId GrokSubagentHostile-20260822T011806Z-c-green-p5-p6. Turn requestId req-20260822T011806Z-001-hostile-c-green-p5-p6. client.SessionLog.OpenSessionAsync created=true. BeginTurnAsync turnId 42836. Persistence proof is recorded after completeTurn in docs/receipts/_hv-c-green-p5-p6/sl-*.txt.

## Mandatory surface that could not be evaluated

None.

## Explicit FAIL list

None.

## Residual (not extra FAILs)

- ServerFixture_SignatureAndNonceTrust still asserts marker text contains "signature:" and "apiKey:" plus health nonce echo. It does not recompute HMAC-SHA256. HealthReady uses the live marker apiKey through McpServerClient.Todo.QueryAsync, which is operational trust of the isolated host. Residual, not a C-green-P5-P6 FAIL against the named tests.
- ServerFixture_StartupTimeout still accepts either OperationCanceledException or a successful port. Independent TRX duration 00:00:01.0963710 for that method matches the 1s CancellationTokenSource path (cancel honored; host did not fully start in 1s). Residual test-quality hole remains, not a FAIL.
- Collector regex FixtureHardcodes7147PortAssignment matched `ReservedServicePort = 7147` as a substring. AllocateFreePort rejects 7147. Tests assert NotEqual 7147. Developer service is live on 7147 while fixture tests pass health on fixture.Port in 11-30s. Not a 7147 reuse.
- TRX Counters.skipped attribute is missing. Console output Skipped: 0; TRX notExecuted=0, executed=7/14, failed=0.
- workflow.requirements.getMapping is unregistered (schema_validation_failed). listMappings returned FR-MCP-PLUGININT-001 to TR-MCP-PLUGININT-001 and TEST-MCP-PLUGININT-001.
- workflow.requirements getFr/getTr/getTest createdAt stamps look like query time. Do not treat that stamp as a recreate. AC text matches the markdown projection. isSatisfied remains false; this gate does not claim store AC complete.
- Homemade HMAC Match=false. Plugin Test-MarkerSignature true.
- Fixture files remain untracked (`git status --porcelain` shows ?? tests/McpServer.PluginIntegration.Tests/). That does not block this green gate.
- MCP-PLUGININT-001 P1-P4 and P5-P20 implementationTasks remain done:false. PLAN task "C P1 red + hostile, P2-P3 green + hostile; P4 red + hostile then green; P5 red + hostile then P6." remains done:false. This review does not change goal state.
- TR-MCP-PLUGININT-001 AC3-AC6 and FR/TEST eight-agent Theory AC remain for P7-P20. This brief forbids requiring them.

## Claims reviewed

### A Requested

#### A1. PluginIntegrationServerFixture.StartAsync launches compiled McpServer.Support.Mcp (dotnet + Support.Mcp.dll), not WebApplicationFactory, on a free loopback port that is never 7147.

Verdict: PASS

Evidence: PluginIntegrationServerFixture.cs StartProcess FileName="dotnet", ArgumentList adds McpServer.Support.Mcp.dll (ResolveSupportAssemblyPath), `_process.Start()`. No WebApplication, WebApplicationFactory, Kestrel, or CreateBuilder (fixture-scan.json). AllocateFreePort uses TcpListener loopback port 0 and rejects ReservedServicePort 7147. Tests assert InRange and NotEqual 7147. Independent filter TRX ServerFixture_SelectsFreePort Passed duration 00:00:28.7223462 (real host, not the 1-17 ms C-red-P5 throw stub). Csproj now references McpServer.Support.Mcp.csproj. Copied dll exists at tests/McpServer.PluginIntegration.Tests/bin/Debug/net10.0/McpServer.Support.Mcp.dll.

#### A2. Isolated temp workspace and sqlite database; workspace is not F:\GitHub\McpServer; database path does not contain 7147.

Verdict: PASS

Evidence: Fixture creates `_rootPath = Path.Combine(Path.GetTempPath(), "mcp-pluginint-" + Guid)` with WorkspacePath workspace subdirectory and DatabasePath data/mcp.db. ServerFixture_CreatesIsolatedTempWorkspaceAndDatabase asserts Directory.Exists workspace, database file or parent directory exists, DoesNotContain 7147 in DatabasePath, and workspace is not F:\GitHub\McpServer. Independent TRX that method Passed duration 00:00:12.6153134. MCP_SQLITE_DATA_SOURCE and appsettings DataSource point at the isolated mcp.db, not the developer service database.

#### A3. Fixture writes AGENTS-README-FIRST.yaml with signature: and apiKey:. Health GET /health?nonce= echoes the nonce.

Verdict: PASS

Evidence: WaitForMarkerApiKeyAsync requires MarkerPath text to contain "signature:" and a non-empty apiKey scalar. WaitForHealthAsync GETs /health?nonce= and requires the body to contain the nonce. ServerFixture_CreatesMarker and ServerFixture_SignatureAndNonceTrust passed (durations 00:00:13.8073741 and 00:00:17.2230040). Tests read the on-disk marker and GET health on 127.0.0.1:{fixture.Port}.

#### A4. DisposeAsync kills the process and deletes the isolated workspace directory.

Verdict: PASS

Evidence: TryStopProcessAsync calls Kill(entireProcessTree: true) then WaitForExitAsync. DisposeAsync then Directory.Delete(_rootPath, recursive: true) with retries. ServerFixture_DeterministicCleanup starts, captures workspace, DisposeAsync, asserts Directory.Exists(workspace) is false. Independent TRX Passed duration 00:00:30.0940635. A live process holding sqlite would typically block recursive delete; the directory-gone assertion plus process-tree kill is enough for this gate.

#### A5. ServerFixture_StartupTimeout honors a 1s CancellationToken (OperationCanceledException or successful port).

Verdict: PASS

Evidence: Test uses CancellationTokenSource(TimeSpan.FromSeconds(1)), catches OperationCanceledException or asserts InRange(port). StartAsync calls ThrowIfCancellationRequested and WaitForMarker/WaitForHealth throw OperationCanceledException when the token fires. Independent TRX duration 00:00:01.0963710 (cancel path, not a full host start). Filter Passed.

#### A6. ServerFixture_HealthReady_ExposesTrustedMarkerAndClient exists and creates a trusted McpServerClient that can QueryAsync todos.

Verdict: PASS

Evidence: PluginIntegrationServerFixtureTests.cs method present with [Fact(Timeout = 120000)], no Skip. CreateTrustedClient uses McpServerClientFactory.Create with BaseUrl, ApiKey, WorkspacePath. Test asserts marker signature:/apiKey:, non-empty ApiKey, then client.Todo.QueryAsync NotNull. Independent TRX Passed duration 00:00:14.8524343.

#### A7. Independent: dotnet test tests/McpServer.PluginIntegration.Tests -c Debug --filter FullyQualifiedName~PluginIntegrationServerFixtureTests Failed 0 Passed 7 Skipped 0.

Verdict: PASS

Evidence: This review re-ran that exact filter. Console: Passed! Failed: 0, Passed: 7, Skipped: 0, Total: 7, Duration: 1 m 58 s. FILTER_EXIT=0 DurationMs=141111. TRX total=7 executed=7 passed=7 failed=0 notExecuted=0. Passed names are the six P5 methods plus ServerFixture_HealthReady_ExposesTrustedMarkerAndClient. Implementer log at C:\Users\kingd\AppData\Local\Temp\grok-goal-678ba5b2f579\implementer\p5-p6-fixture-tests.log also says Passed 7 Failed 0 Skipped 0 at 2026-08-22T01:09:29Z (after C-red-P5 AGREE); it was not used as proof. Full project extra: Passed 14 Failed 0 Skipped 0 ALL_EXIT=0.

#### A8. PLAN-PLUGINHANDOFF-001 and MCP-PLUGININT-001 still done:false. No P7 adapter tests were added in this slice.

Verdict: PASS

Evidence: live workflow.todo.get and client.Todo.GetAsync. PLAN-PLUGINHANDOFF-001 done: false. MCP-PLUGININT-001 done: false. P5, P6, and P7 implementationTasks done: false. PLAN C task including "P5 red + hostile then P6." done: false (todo-done-extract.json, todo-plan.txt, todo-pluginint.txt). Select-String Adapter_CapturesExecutable over tests/ and src/ *.cs and *.csproj: NO_MATCH. Filter and full TRX adapterNames []. Full project 14 tests are 7 catalog/xml-docs plus 7 fixture names.

### B Workspace rules

#### B1. Byrd v4 for this green-phase gate: C-red-P5 AGREE exists; P6 implementation after that AGREE; current-plus-prior PluginIntegration.Tests Failed 0 Skipped 0.

Verdict: PASS

Evidence: C-red-P5 AGREE docs/receipts/hostile-validator-20260822T005108Z.md OverallVerdict AGREE FailCount 0. Fixture tests LastWriteTimeUtc 2026-08-22T01:02:10.4371979Z and fixture source 2026-08-22T01:05:30.8859739Z, both after C-red-P5 AGREE 2026-08-22T00:51:08Z. Catalog tests remain 2026-08-22T00:06:51.8604136Z. Independent filter Failed 0 Passed 7 Skipped 0. Full project Failed 0 Passed 14 Skipped 0. Did not FAIL B1 from FR createdAt vs file mtimes.

#### B2. Always bring the receipts.

Verdict: PASS

Evidence: this review re-ran both the named filter and the full PluginIntegration.Tests project, re-read fixture source, grepped Adapter_CapturesExecutable, live todo_get, live requirements, and wrote collector artifacts under docs/receipts/_hv-c-green-p5-p6/.

#### B3. MCP-only TODO/session/requirements storage.

Verdict: PASS

Evidence: todo_get and session lifecycle used Invoke-McpPlugin.ps1 (workflow.todo.get, client.Todo.GetAsync, workflow.sessionlog.bootstrap, client.SessionLog.*). No direct edit of todo.yaml or session-log files.

#### B4. PowerShell-only / no Python.

Verdict: PASS

Evidence: collector, bootstrap, tests, TRX parse, and plugin invokes used pwsh.exe -NoProfile -NonInteractive. No python/python3/py.

#### B5. Honesty: claims match artifacts.

Verdict: PASS

Evidence: attacks (still throws not implemented; uses 7147; uses developer DB; fake marker without process; skipped tests; P7 mixed in; PLAN done) were not sustained. Implementer claims match the independently re-run TRX and source.

### C Requirements

#### C1. Applicable FR/TR/TEST exist with structured AC.

Verdict: PASS

Evidence: live getFr FR-MCP-PLUGININT-001 five AC isSatisfied false. getTr TR-MCP-PLUGININT-001 six AC including ac-2 "The fixture starts or binds to a real MCP Server on an isolated workspace and verifies marker trust before invoking plugins." isSatisfied false. getTest TEST-MCP-PLUGININT-001 five AC isSatisfied false. Status pending. This green gate does not claim store AC satisfied.

#### C2. Mapping FR to TR and TEST exists.

Verdict: PASS

Evidence: workflow.requirements.listMappings frId FR-MCP-PLUGININT-001 returned two items: trId TR-MCP-PLUGININT-001 and testId TEST-MCP-PLUGININT-001 (docs/receipts/_hv-c-green-p5-p6/req-map-list.txt). docs/Project/TR-per-FR-Mapping.md has the same triple.

#### C3. P5/P6 named tests cover TR-MCP-PLUGININT-001 AC2 fixture slice (isolated workspace, compiled host, marker trust, no developer DB). Later eight-agent AC are P7-P20.

Verdict: PASS

Evidence: plan P5 names the six tests and "Never the developer service DB." P6 names compiled McpServer.Support.Mcp on loopback plus ServerFixture_HealthReady_ExposesTrustedMarkerAndClient. Tests now pass against a real isolated host. Remaining FR/TEST AC for eight-agent Theory are later P7-P20; this brief forbids requiring them.

### D Current plan holistically

#### D1. C-green-P5-P6 exit is the named fixture tests green plus compiled isolated host, not PLAN done and not P7.

Verdict: PASS

Evidence: plan section 7: "C-red-P5 AGREE, then P6 green, then C-green-P5-P6 AGREE". Order observed. Implementer did not mark PLAN or PLUGININT done. P7 Adapter_CapturesExecutable is absent. This review does not require PLAN done.

#### D2. Attacks against this green gate (still throws not implemented; 7147 reused; developer DB; fake marker without process; skipped tests; P7 mixed in; PLAN done) are not sustained.

Verdict: PASS

Evidence: A1-A8. StartAsync no longer throws not implemented. Filter 7 greens in 1 m 58 s. Full suite 14 greens, 0 adapter names. PLAN and PLUGININT done false.

## Attacks (parent brief)

- still throws not implemented: not sustained (StartAsyncThrowsNotImplemented false; 11-30 s passing host tests).
- uses 7147: not sustained (AllocateFreePort rejects 7147; tests assert NotEqual 7147; developer 7147 remains the live marker service while fixture tests hit other ports).
- uses developer DB: not sustained (temp mcp-pluginint-* / data/mcp.db; DoesNotContain 7147; workspace not F:\GitHub\McpServer).
- fake marker without process: not sustained (dotnet + Support.Mcp.dll process; marker wait plus health nonce; QueryAsync via trusted client).
- skipped tests: not sustained (console Skipped: 0; TRX notExecuted 0; no [Fact Skip]).
- P7 mixed in: not sustained (Adapter_CapturesExecutable NO_MATCH; TRX adapterNames empty).
- PLAN done: not sustained (live todo_get done: false).

## Accuracy and completeness

Accuracy: 97. Independently re-ran the exact filter and the full project, re-read fixture source, grepped Adapter_CapturesExecutable, live todo_get both IDs, live FR/TR/TEST/listMappings, marker trust and health nonce.
Completeness: 96. Surfaces A+B+C+D scored. Residuals recorded. Mapping recovered via listMappings after getMapping schema miss.
