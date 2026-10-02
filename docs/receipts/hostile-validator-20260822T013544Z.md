# Hostile Validator Receipt

TimestampUtc: 2026-08-22T01:35:44Z
ValidatorIdentity: GrokSubagentHostile
Workspace: F:\GitHub\McpServer
WorkClass: 1 (project implementation PLAN-PLUGINHANDOFF-001 Phase C-green-P5-P6 after C-red-P5 AGREE). Surfaces A+B+C+D all apply. Review only. No product implementation by this validator. Do not treat this as P7. PLAN-PLUGINHANDOFF-001 and MCP-PLUGININT-001 must remain not done.
add-profile: executed yes. Profile files read: 18 (every non-skill *.md under C:\Users\kingd\.claude\profile\; excluded add-profile.grok.md). Also read add-profile SKILL.md and hostile-validator SKILL.md.
Plugin: F:\GitHub\mcpserver-grok-plugin (.grok-plugin/plugin.json version 1.99.0; .version 1.99.0). Live workflow/client calls used Invoke-McpPlugin.ps1. Tool registry search included exact name mcpserver-grok-plugin.
Active plan: F:\GitHub\McpServer\docs\plans\PLAN-PLUGINHANDOFF-001.md section 7 P5 then P6.
Requirement IDs: FR-MCP-PLUGININT-001, TR-MCP-PLUGININT-001, TEST-MCP-PLUGININT-001.
Prior receipt: docs/receipts/hostile-validator-20260822T005108Z.md (OverallVerdict AGREE, FailCount 0, C-red-P5; StartAsync still threw; HealthReady absent).
Collector: docs/receipts/_hv-c-green-p5-p6-20260822T012151Z/
OverallVerdict: AGREE

Default was FAIL or UNKNOWN until this pass independently re-ran `dotnet test tests/McpServer.PluginIntegration.Tests -c Debug --filter FullyQualifiedName~PluginIntegrationServerFixtureTests`, re-ran the full PluginIntegration.Tests project, re-read PluginIntegrationServerFixture.cs and PluginIntegrationServerFixtureTests.cs on disk, live-got PLAN-PLUGINHANDOFF-001 and MCP-PLUGININT-001, live-got FR/TR/TEST/listMappings, proved 7147 still belongs to developer pid 16936, and opened a dedicated GrokSubagentHostile session. Implementer chat and parent collector logs were not trusted as proof.

## Clock and live MCP

Plugin Test-MarkerSignature true (docs/receipts/_hv-c-green-p5-p6-20260822T012151Z/plugin-marker-sig.json). Homemade HMAC Match=false is a payload-construction false-negative; plugin Test-MarkerSignature is the authoritative check. Health GET http://127.0.0.1:7147/health?nonce=42804f263bf84a25a9f1523539a4b563 returned status Healthy, storage reachable, nonce echoed exactly (health-nonce-retry.json). Plugin Status available. Tool registry search includes exact name mcpserver-grok-plugin (TOOL_SEARCH_EXACT=True, 8 names). Developer Windows service McpServer Running PathName `C:\ProgramData\McpServer\McpServer.Support.Mcp.exe --urls http://+:7147`. Listeners7147 OwningProcess 16936 before and after the independent test run.

## Session log proof

SessionId GrokSubagentHostile-20260822T012646Z-c-green-p5-p6. Turn requestId req-20260822T012646Z-001-hostile-c-green-p5-p6. client.SessionLog.OpenSessionAsync created=true. BeginTurnAsync turnId 42839. Persistence proof is recorded after completeTurn in docs/receipts/_hv-c-green-p5-p6-20260822T012151Z/sl-*.txt.

## Independent test rerun (not parent collector)

Filter command: `dotnet test tests/McpServer.PluginIntegration.Tests -c Debug --filter FullyQualifiedName~PluginIntegrationServerFixtureTests`. Console: Passed! Failed: 0, Passed: 7, Skipped: 0, Total: 7, Duration: 1 m 35 s. FILTER_EXIT=0. TRX Counters total=7 executed=7 passed=7 failed=0 notExecuted=0. All seven named methods outcome=Passed. StartupTimeout duration 00:00:01.1022886 (cancel path, not a full 13s start). Other fixture tests 11s to 25s.

Full project: Passed! Failed: 0, Passed: 14, Skipped: 0, Total: 14, Duration: 1 m 18 s. ALL_EXIT=0. TRX Counters total=14 executed=14 passed=14 failed=0 notExecuted=0. The extra seven are Catalog_* plus PluginIntegrationProject_HasXmlDocsAndNonparallelCollection.

## Isolation from developer 7147 database

- AllocateFreePort rejects ReservedServicePort=7147. Grep of PluginIntegrationServerFixture.cs 7147 hits: only `private const int ReservedServicePort = 7147` and `port != ReservedServicePort`. No `Port = 7147` assignment. Parent collector FixtureHardcodes7147PortAssignment true is a regex false positive against that constant.
- Tests assert NotEqual 7147, DoesNotContain 7147 in DatabasePath, and workspace is not F:\GitHub\McpServer.
- Isolated hosts logged WebRootPath under C:\Users\kingd\AppData\Local\Temp\mcp-pluginint-* during the independent run. After tests, temp mcp-pluginint-* directory count is 0. Only leftover Support.Mcp process is pid 16936.
- Repo sqlite stamps unchanged across the run: F:\GitHub\McpServer\mcp.db 4096 bytes LastWriteTimeUtc 2026-07-11T23:26:43Z; src\McpServer.Support.Mcp\mcp.db 1146880 bytes 2026-08-08T09:08:39Z.
- Live service DataFolder is C:\ProgramData\McpServer-Data. mcp.db there 19329024 bytes LastWriteTimeUtc 2026-05-22T13:50:24Z (unchanged). Developer 7147 health after tests: Healthy.
- Fixture StartAsync writes temp appsettings with Mcp:Port from AllocateFreePort, Mcp:DataSource under the temp data path, MCP_SQLITE_DATA_SOURCE, MCP_WORKSPACE_PATH, and waits for marker signature/apiKey plus health nonce echo before _started=true. DisposeAsync Kill(entireProcessTree: true) then Directory.Delete(_rootPath).

## Mandatory surface that could not be evaluated

None. Diagnostic /mcpserver/diagnostic/execution-path is 404 on this Production service (controller is Debug/Staging only). Isolation was proven from service PathName, pid 16936, temp roots, TRX, and DataFolder sqlite stamps instead.

## Explicit FAIL list

None.

## Residual (not extra FAILs)

- Production CreateBuilder path uses UseUrls http://+:{port} when ASPNETCORE_ENVIRONMENT=Production. Fixture sets Production. Client BaseUrl and AllocateFreePort are loopback; the child host is not ListenLocalhost-only. TR-MCP-PLUGININT-001 AC2 requires isolated workspace, not exclusive 127.0.0.1 bind.
- Isolated hosts write Serilog files into tests/McpServer.PluginIntegration.Tests/bin/Debug/net10.0/logs because ContentRoot is the copied Support.Mcp.dll directory. That is test-output log leak, not the developer sqlite.
- TodoBootstrapImporter errors on isolated TODO.yaml `sections: []`. HealthReady still QueryAsync NotNull.
- ServerFixture_SignatureAndNonceTrust asserts marker text contains signature:/apiKey: and health nonce echo. It does not recompute HMAC-SHA256.
- ServerFixture_HealthReady_ExposesTrustedMarkerAndClient does not itself assert Port != 7147. SelectsFreePort does. CreateTrustedClient binds fixture.BaseUrl and ApiKey.
- MCP-PLUGININT-001 P5 task still names PluginSessionLogServerFixture. On-disk type is PluginIntegrationServerFixture. Plan P5 names the six methods, which match.
- workflow.requirements.getMapping is unregistered (schema_validation_failed). listMappings returned FR-MCP-PLUGININT-001 to TR-MCP-PLUGININT-001 and TEST-MCP-PLUGININT-001.
- workflow.requirements getFr/getTr/getTest createdAt stamps look like query time. Do not treat that stamp as a recreate. AC text matches the prior C-red-P5 live get.
- Homemade HMAC Match=false. Plugin Test-MarkerSignature true.
- Fixture files remain untracked (`git status --porcelain` shows ?? on csproj, PluginIntegrationServerFixture.cs, PluginIntegrationServerFixtureTests.cs). git log for those paths is empty.
- P1-P4 MCP-PLUGININT implementationTasks remain done:false. Correct until those tasks are separately marked after their AGREE. This review does not change goal state. P5, P6, and P7 tasks are also done:false.
- PLAN implementation task "C P5 red + hostile then P6" remains done:false. Correct until this AGREE is cited; this review does not mark it done.
- Parent collector docs/receipts/_hv-c-green-p5-p6/ existed and was incomplete at dispatch time. This review used a new collector and re-ran tests.

## Claims reviewed

### A Requested

#### A1. C-red-P5 AGREE exists at docs/receipts/hostile-validator-20260822T005108Z.md. At that gate StartAsync still threw not implemented and HealthReady was absent.

Verdict: PASS

Evidence: File exists. OverallVerdict: AGREE. Body states StartAsync is not implemented and ServerFixture_HealthReady_ExposesTrustedMarkerAndClient is absent (P6). Filter TRX at that gate failed 6 passed 0. Fixture LastWriteTimeUtc now 2026-08-22T01:05:30Z and tests 2026-08-22T01:02:10Z, both after 2026-08-22T00:51:08Z.

#### A2. After that AGREE, PluginIntegrationServerFixture.StartAsync starts a Process with the Support.Mcp dll, isolated temp workspace/database, marker wait, health nonce wait, AllocateFreePort never 7147. Dispose kills the tree and deletes the temp root.

Verdict: PASS

Evidence: PluginIntegrationServerFixture.cs StartProcess FileName=dotnet ArgumentList Support.Mcp.dll (copied output or src bin). WorkspacePath/DatabasePath under Path.GetTempPath()/mcp-pluginint-{guid}. WaitForMarkerApiKeyAsync requires signature: and apiKey. WaitForHealthAsync requires nonce echo. AllocateFreePort retries until port != ReservedServicePort. DisposeAsync Kill(entireProcessTree: true) then Directory.Delete(_rootPath, recursive: true). Csproj ProjectReference McpServer.Support.Mcp.csproj. StartAsyncThrowsNotImplemented is now false.

#### A3. Seven tests exist and currently pass (not skip). Parent collector may be incomplete; this review re-ran the named filter and the full PluginIntegration.Tests project.

Verdict: PASS

Evidence: PluginIntegrationServerFixtureTests.cs seven [Fact] methods, none Skip=. Independent filter: Passed 7 Failed 0 Skipped 0 Total 7 exit 0. Independent full project: Passed 14 Failed 0 Skipped 0 Total 14 exit 0. TRX notExecuted=0. Logs: docs/receipts/_hv-c-green-p5-p6-20260822T012151Z/dotnet-fixture-filter.log and dotnet-pluginintegration-all.log plus trx-filter and trx-all.

#### A4. Never uses developer 7147 database. Attack FixtureHardcodes7147PortAssignment true: constant ReservedServicePort=7147 as exclusion is allowed only if Port is never assigned 7147.

Verdict: PASS

Evidence: Isolation section. Port property is assigned only from AllocateFreePort(). Two 7147 source hits are the exclusion constant and the inequality. Developer pid 16936 still owns 7147 after tests. Isolated temp roots in Support.Mcp test-output log. Temp leftover count 0. Repo and ProgramData McpServer-Data sqlite LastWriteTimeUtc not moved into the test window.

#### A5. P7 Adapter_CapturesExecutableArgsStdinEnvCwdTimeoutExitStdoutStderr is not implemented. Do not treat this receipt as P7.

Verdict: PASS

Evidence: Select-String over tests/McpServer.PluginIntegration.Tests/*.cs has no Adapter_CapturesExecutable. Plan P7 remains the next red. This receipt does not claim P7.

#### A6. PLAN-PLUGINHANDOFF-001 and MCP-PLUGININT-001 remain done:false.

Verdict: PASS

Evidence: live workflow.todo.get and client.Todo.GetAsync. PLAN done: false. MCP-PLUGININT-001 done: false. P5, P6, P7 tasks done: false. PLAN task "C P5 red + hostile then P6" done: false. remaining text still says PLAN done=false.

### B Workspace rules

#### B1. Byrd v4 for this green-phase gate: C-red-P5 AGREE exists, then P6 implementation, then this C-green-P5-P6 review. Full executed PluginIntegration.Tests scope Failed 0 Skipped 0.

Verdict: PASS

Evidence: Inter-phase C-red-P5 AGREE 2026-08-22T00:51:08Z. Fixture implementation LastWrite after that AGREE. Independent suite green. Did not FAIL B1 from FR createdAt vs file mtimes.

#### B2. Always bring the receipts.

Verdict: PASS

Evidence: this review re-ran both the named filter and the full PluginIntegration.Tests project, re-read fixture source, live todo_get, live requirements, developer service PathName/pid, and wrote collector artifacts under docs/receipts/_hv-c-green-p5-p6-20260822T012151Z/.

#### B3. MCP-only TODO/session/requirements storage.

Verdict: PASS

Evidence: todo_get and session lifecycle used Invoke-McpPlugin.ps1 (workflow.todo.get, client.Todo.GetAsync, workflow.sessionlog.bootstrap, client.SessionLog.*). No direct edit of todo.yaml or session-log files.

#### B4. PowerShell-only / no Python.

Verdict: PASS

Evidence: collector, bootstrap, tests, TRX inspect, process/service inspect, and plugin invokes used pwsh.exe -NoProfile -NonInteractive. No python/python3/py.

#### B5. Honesty: claims match artifacts.

Verdict: PASS

Evidence: attacks (still throwing not implemented, skipped tests, 7147 Port assignment, developer sqlite reuse, P7 mixed in, PLAN marked done) were not sustained. Implementer claims match independently re-run TRX and source.

### C Requirements

#### C1. Applicable FR/TR/TEST exist with structured AC.

Verdict: PASS

Evidence: live getFr FR-MCP-PLUGININT-001 five AC isSatisfied false. getTr TR-MCP-PLUGININT-001 six AC including ac-2 fixture isolated workspace and marker trust, isSatisfied false. getTest TEST-MCP-PLUGININT-001 five AC isSatisfied false. Status pending. This gate does not claim AC satisfied.

#### C2. Mapping FR to TR and TEST exists.

Verdict: PASS

Evidence: workflow.requirements.listMappings frId FR-MCP-PLUGININT-001 returned two items: trId TR-MCP-PLUGININT-001 and testId TEST-MCP-PLUGININT-001 (req-map-list.txt).

#### C3. P5/P6 named tests cover TR-MCP-PLUGININT-001 AC2 fixture slice. Remaining FR/TEST AC for eight-agent Theory are later P7-P20.

Verdict: PASS

Evidence: plan P5/P6 names the seven tests. Tests assert free port not 7147, isolated temp workspace/database, startup timeout, marker path, signature/nonce health echo, deterministic cleanup, trusted client QueryAsync. MCP-PLUGININT-001 P5/P6 task text matches those behaviors. This brief forbids requiring P7.

### D Current plan holistically

#### D1. C-green-P5-P6 exit is the six P5 tests plus HealthReady green, compiled Support.Mcp fixture, never-developer-DB, after C-red-P5 AGREE. Not PLAN done. Not P7.

Verdict: PASS

Evidence: plan section 7 order: C-red-P5 AGREE, then P6 green, then C-green-P5-P6 AGREE. Independent tests match that exit. Implementer did not mark plan steps complete.

#### D2. P7 Adapter_Captures is absent. This receipt is not P7.

Verdict: PASS

Evidence: A5. Plan next line is P7 red after this AGREE.

#### D3. Attacks against this green gate (still stub, skips, 7147 DB reuse, P7 mixed in, PLAN done) are not sustained.

Verdict: PASS

Evidence: A2, A3, A4, A5, A6.

## Attacks (parent brief)

- StartAsync still throws not implemented: not sustained (Process.Start of Support.Mcp.dll, 11s-25s tests).
- HealthReady absent: not sustained (method present, Passed, 13.23s).
- tests skipped: not sustained (Skipped 0, TRX notExecuted 0).
- parent collector incomplete so tests were not green: not sustained (this review re-ran; filter 7/7 and full 14/14).
- FixtureHardcodes7147PortAssignment means Port is assigned 7147: not sustained (exclusion constant only).
- developer 7147 database used: not sustained (temp mcp-pluginint roots, 7147 still pid 16936, sqlite stamps outside the test window).
- P7 implemented: not sustained (no Adapter_Captures method).
- PLAN/PLUGININT done true: not sustained (live todo_get done false).

## Accuracy and completeness

Accuracy: 96. Independently re-ran the exact filter and the full project, re-read fixture source, live todo_get both IDs, live FR/TR/TEST/listMappings, marker trust and health nonce, developer service PathName and pid 16936, temp leftover count 0.
Completeness: 95. Diagnostic execution-path 404 on Production. Live sqlite mtime on C:\ProgramData\McpServer-Data\mcp.db is old (May 22) with no WAL; isolation still proven from temp roots, port owner, and test assertions. Did not require P7 or mark TODOs done.
