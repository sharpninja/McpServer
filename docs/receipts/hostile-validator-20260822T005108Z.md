# Hostile Validator Receipt

TimestampUtc: 2026-08-22T00:51:08Z
ValidatorIdentity: GrokSubagentHostile
Workspace: F:\GitHub\McpServer
WorkClass: 1 (project implementation PLAN-PLUGINHANDOFF-001 Phase C-red-P5 only, isolated server fixture reds). Surfaces A+B+C+D all apply. Review only. No product implementation by this validator. Do not require P5 green. PLAN-PLUGINHANDOFF-001 must remain not done.
add-profile: executed yes. Profile files read: 18 (every non-skill *.md under C:\Users\kingd\.claude\profile\; excluded add-profile.grok.md). Also read add-profile SKILL.md and hostile-validator SKILL.md.
Plugin: F:\GitHub\mcpserver-grok-plugin (.grok-plugin/plugin.json version 1.99.0; .version 1.99.0). Live workflow/client calls used Invoke-McpPlugin.ps1. Tool registry search included exact name mcpserver-grok-plugin.
Active plan: F:\GitHub\McpServer\docs\plans\PLAN-PLUGINHANDOFF-001.md section 7 P5.
Requirement IDs: FR-MCP-PLUGININT-001, TR-MCP-PLUGININT-001, TEST-MCP-PLUGININT-001.
Prior receipt: docs/receipts/hostile-validator-20260822T003817Z.md (OverallVerdict AGREE, FailCount 0, C-green-P4; PluginIntegration.Tests Passed 7 Failed 0 Skipped 0; no ServerFixture_ mention).
Collector: docs/receipts/_hv-c-red-p5/
OverallVerdict: AGREE

Default was FAIL or UNKNOWN until this pass independently re-ran `dotnet test tests/McpServer.PluginIntegration.Tests -c Debug --filter FullyQualifiedName~PluginIntegrationServerFixtureTests`, re-ran the full PluginIntegration.Tests project, re-read PluginIntegrationServerFixture.cs and PluginIntegrationServerFixtureTests.cs on disk, live-got PLAN-PLUGINHANDOFF-001 and MCP-PLUGININT-001, live-got FR/TR/TEST/listMappings, and opened a dedicated GrokSubagentHostile session. Implementer chat was not trusted.

## Clock and live MCP

Health GET /health?nonce=22f296d079bc456491d751e4e67d0861 returned status Healthy, storage reachable, nonce echoed exactly (docs/receipts/_hv-c-red-p5/health-nonce.json). Plugin Test-MarkerSignature true; Invoke-FullBootstrap true (docs/receipts/_hv-c-red-p5/plugin-marker-sig.json). Homemade HMAC in marker-sig.json Match=false is a payload-construction false-negative; plugin Test-MarkerSignature is the authoritative check. Plugin Status available. Tool registry search includes exact name mcpserver-grok-plugin (TOOL_SEARCH_EXACT=True).

## Session log proof

SessionId GrokSubagentHostile-20260822T004733Z-c-red-p5. Turn requestId req-20260822T004733Z-001-hostile-c-red-p5-fixture. client.SessionLog.OpenSessionAsync created=true. BeginTurnAsync turnId 42827. Persistence proof is recorded after completeTurn in docs/receipts/_hv-c-red-p5/sl-*.txt.

## Mandatory surface that could not be evaluated

None.

## Explicit FAIL list

None.

## Residual (not extra FAILs)

- ServerFixture_StartupTimeout swallows only OperationCanceledException. If StartAsync later succeeds while ignoring the 1s CTS, InRange(port) still passes. Residual test-quality hole for P6, not a current-red miss: StartAsync throws now.
- ServerFixture_SignatureAndNonceTrust asserts marker text contains "signature:" and "apiKey:" plus health nonce echo. It does not recompute HMAC-SHA256. Residual for P6, not a C-red-P5 FAIL.
- MCP-PLUGININT-001 P5 task names PluginSessionLogServerFixture. On-disk type is PluginIntegrationServerFixture. Plan P5 names the six test methods, which match. Residual naming drift only.
- XML docs on the six facts are short P5-red summaries. They do not enumerate fixture data or TEST-MCP-PLUGININT-001 in every method. Matches prior C-red XML style; residual, not a FAIL against the named-test plan gate.
- TRX Counters.skipped attribute is missing. Console output Skipped: 0; TRX notExecuted=0, executed=6, passed=0, failed=6 for the filter; full project executed=13 passed=7 failed=6 notExecuted=0.
- workflow.requirements.getMapping is unregistered (schema_validation_failed). listMappings returned FR-MCP-PLUGININT-001 to TR-MCP-PLUGININT-001 and TEST-MCP-PLUGININT-001.
- workflow.requirements getFr/getTr/getTest createdAt stamps look like query time. Do not treat that stamp as a recreate. AC text matches the markdown projection.
- Homemade HMAC Match=false. Plugin Test-MarkerSignature true.
- Fixture files are untracked (`git status --porcelain` shows ?? PluginIntegrationServerFixture.cs and PluginIntegrationServerFixtureTests.cs). git log for those paths is empty. That supports new-red, not a FAIL.
- P1-P4 MCP implementationTasks remain done:false. Correct until those tasks are separately marked after their AGREE. This review does not change goal state. P5 and P6 tasks are also done:false.
- Full PluginIntegration.Tests Passed 7 Failed 6 Skipped 0. The seven passes are prior P2/P4 catalog and xml-docs tests, not P5 greens.

## Claims reviewed

### A Requested

#### A1. Six named P5 tests exist: ServerFixture_SelectsFreePort, ServerFixture_CreatesIsolatedTempWorkspaceAndDatabase, ServerFixture_StartupTimeout, ServerFixture_CreatesMarker, ServerFixture_SignatureAndNonceTrust, ServerFixture_DeterministicCleanup.

Verdict: PASS

Evidence: PluginIntegrationServerFixtureTests.cs lines 13, 23, 35, 53, 63, 79. fixture-scan.json NamedPresentCount 6; each FactNearby true, SkipNearby false, TraitSkip false. Plan P5 names these six methods (docs/plans/PLAN-PLUGINHANDOFF-001.md line 362). Filter TRX failedNames contains all six.

#### A2. PluginIntegrationServerFixture.StartAsync throws not implemented. Tests currently fail (Failed 6 Skipped 0). Never uses developer 7147 DB in the stub.

Verdict: PASS

Evidence: PluginIntegrationServerFixture.cs lines 26-28 throw InvalidOperationException "PluginIntegrationServerFixture.StartAsync is not implemented." DisposeAsync throws the matching message only after WorkspacePath is set; StartAsync never sets it. Independent filter run: Failed! Failed: 6, Passed: 0, Skipped: 0, Total: 6, Duration: 74 ms, exit 1 (docs/receipts/_hv-c-red-p5/dotnet-fixture-filter.log). TRX total=6 executed=6 passed=0 failed=6 notExecuted=0. All six error messages are that StartAsync throw. fixture-scan: StartAsyncHasWebApplication false, Kestrel false, CreateBuilder false, ListenLocalhost false, DotnetRun false, FixtureHardcodes7147Port false. DatabasePath defaults empty. Tests assert NotEqual 7147 and DoesNotContain 7147 in DatabasePath. Csproj references McpServer.Client only, not McpServer.Support.Mcp. Durations are 1-17 ms; no host start.

#### A3. These are new reds, not relabeled P4 greens.

Verdict: PASS

Evidence: Prior C-green-P4 AGREE docs/receipts/hostile-validator-20260822T003817Z.md MentionsServerFixture false, PluginAllPassed 7 with Catalog_* plus PluginIntegrationProject_HasXmlDocsAndNonparallelCollection only. Fixture tests LastWriteTimeUtc 2026-08-22T00:42:25.5806890Z and fixture source 2026-08-22T00:43:19.5225186Z, both after C-green-P4 AGREE 2026-08-22T00:38:17Z. Catalog tests remain 2026-08-22T00:06:51.8604136Z (before that AGREE). git status porcelain ?? on both fixture files; git log empty. Full project still passes those same seven P4/P2 names and fails the six new ServerFixture_ names. ServerFixture_HealthReady_ExposesTrustedMarkerAndClient is absent (P6).

#### A4. PLAN-PLUGINHANDOFF-001 and MCP-PLUGININT-001 still done:false.

Verdict: PASS

Evidence: live workflow.todo.get and client.Todo.GetAsync. PLAN-PLUGINHANDOFF-001 done: false. MCP-PLUGININT-001 done: false. MCP-PLUGININT-001 P5 task done: false. P6 task done: false. PLAN task "C P1 red + hostile, P2-P3 green + hostile; P4 red + hostile then green; P5 red + hostile then P6." done: false (docs/receipts/_hv-c-red-p5/todo-plan.txt, todo-pluginint.txt, todo-plan-client.txt, todo-pluginint-client.txt).

### B Workspace rules

#### B1. Byrd v4 for this red-phase gate: AC-covering tests exist and are shown red. No P5 green mixed in. Prior C-green-P4 AGREE exists.

Verdict: PASS

Evidence: C-green-P4 AGREE receipt OverallVerdict AGREE FailCount 0. This review is the C-red-P5 inter-phase gate. Six named reds fail. P6 HealthReady test is not present. StartAsync is a throw stub, not a compiled host. Did not FAIL B1 from FR createdAt vs file mtimes.

#### B2. Always bring the receipts.

Verdict: PASS

Evidence: this review re-ran both the named filter and the full PluginIntegration.Tests project, re-read fixture source, live todo_get, live requirements, and wrote collector artifacts under docs/receipts/_hv-c-red-p5/.

#### B3. MCP-only TODO/session/requirements storage.

Verdict: PASS

Evidence: todo_get and session lifecycle used Invoke-McpPlugin.ps1 (workflow.todo.get, client.Todo.GetAsync, workflow.sessionlog.bootstrap, client.SessionLog.*). No direct edit of todo.yaml or session-log files.

#### B4. PowerShell-only / no Python.

Verdict: PASS

Evidence: collector, bootstrap, tests, TRX parse, and plugin invokes used pwsh.exe -NoProfile -NonInteractive. No python/python3/py.

#### B5. Honesty: claims match artifacts.

Verdict: PASS

Evidence: attacks (real host, skipped tests, P5 green mixed in, 7147 reused) were not sustained. Implementer claims match the independently re-run TRX and source.

### C Requirements

#### C1. Applicable FR/TR/TEST exist with structured AC.

Verdict: PASS

Evidence: live getFr FR-MCP-PLUGININT-001 five AC isSatisfied false. getTr TR-MCP-PLUGININT-001 six AC including ac-2 "The fixture starts or binds to a real MCP Server on an isolated workspace and verifies marker trust before invoking plugins." isSatisfied false. getTest TEST-MCP-PLUGININT-001 five AC isSatisfied false. Status pending. docs/Project markdown matches.

#### C2. Mapping FR to TR and TEST exists.

Verdict: PASS

Evidence: workflow.requirements.listMappings frId FR-MCP-PLUGININT-001 returned two items: trId TR-MCP-PLUGININT-001 and testId TEST-MCP-PLUGININT-001 (docs/receipts/_hv-c-red-p5/req-map-pluginint-001.txt). docs/Project/TR-per-FR-Mapping.md has the same triple.

#### C3. P5 named tests cover TR-MCP-PLUGININT-001 AC2 fixture slice (isolated workspace, marker trust, no developer DB). This red gate does not claim AC satisfied.

Verdict: PASS

Evidence: plan P5 names the six tests and "Never the developer service DB." Tests assert free port not 7147, isolated temp workspace/database, startup timeout, marker path, signature/nonce health echo, deterministic cleanup. MCP-PLUGININT-001 P5 task text matches those behaviors. Remaining FR/TEST AC for eight-agent Theory are later P7-P20; this brief forbids requiring them.

### D Current plan holistically

#### D1. C-red-P5 exit is the six named failing tests plus never-developer-DB, not PLAN done and not P5 green.

Verdict: PASS

Evidence: plan section 7: "P5 red: [six names]. Never the developer service DB. Hostile C-red-P5. Then P6." Order: C-red-P5 AGREE, then P6 green, then C-green-P5-P6 AGREE. Implementer did not mark plan steps complete. PLAN and MCP-PLUGININT remain done false.

#### D2. Attacks against this red gate (real host already started, tests skipped, P5 green mixed in, 7147 reused) are not sustained.

Verdict: PASS

Evidence: A2 and A3. Full suite 7 prior greens + 6 new reds. Filter scope is Failed 6 Skipped 0. No HealthReady test. Stub does not bind 7147 or open the developer database.

## Attacks (parent brief)

- fixture already starts a real host: not sustained (throw stub, no Kestrel/WebApplication/Process.Start, 1-17 ms fails).
- tests skipped: not sustained (Skipped 0, TRX notExecuted 0).
- P5 green mixed in: not sustained (zero P5 passes; P6 test absent; seven passes are prior catalog/xml-docs).
- 7147 reused: not sustained (no Port=7147, tests assert not 7147, no Support.Mcp project reference, empty DatabasePath).

## Accuracy and completeness

Accuracy: 97. Independently re-ran the exact filter and the full project, re-read fixture source, live todo_get both IDs, live FR/TR/TEST/listMappings, marker trust and health nonce.
Completeness: 96. Surfaces A+B+C+D scored. Residuals recorded. Mapping recovered via listMappings after getMapping schema miss.
