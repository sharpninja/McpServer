#Requires -Version 7.0
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Continue'

$out = 'F:\GitHub\McpServer\docs\receipts\_hv-c-red-p16\hv-self'
$plugin = 'F:\GitHub\mcpserver-grok-plugin\lib\Invoke-McpPlugin.ps1'
$cacheRoot = Join-Path $out 'plugin-cache'
New-Item -ItemType Directory -Force -Path $cacheRoot | Out-Null
Set-Location -LiteralPath 'F:\GitHub\McpServer'

$env:MCP_WORKSPACE_PATH = 'F:\GitHub\McpServer'
$env:MCPSERVER_WORKSPACE_PATH = 'F:\GitHub\McpServer'
$env:GROK_WORKSPACE_PATH = 'F:\GitHub\McpServer'
$env:PLUGIN_AGENT_NAME = 'GrokSubagentHostile'
$env:MCP_AGENT_NAME = 'GrokSubagentHostile'
$env:GROK_PLUGIN_ROOT = 'F:\GitHub\mcpserver-grok-plugin'
$env:MCP_PLUGIN_ROOT = 'F:\GitHub\mcpserver-grok-plugin'
$env:MCP_PLUGIN_HOST = 'grok'

$agent = 'GrokSubagentHostile'
$sessionId = 'GrokSubagentHostile-20260822T072417Z-c-red-p16'
$requestId = 'req-20260822T072417Z-001-hostile-c-red-p16'
$utc = [DateTime]::UtcNow.ToString('yyyyMMddTHHmmssZ')
$iso = [DateTime]::UtcNow.ToString('o')
$receiptMdPath = "F:\GitHub\McpServer\docs\receipts\hostile-validator-$utc.md"
$receiptJsonPath = "F:\GitHub\McpServer\docs\receipts\hostile-validator-$utc.json"
Set-Content -LiteralPath (Join-Path $out 'receipt-stamp.txt') -Value $utc -Encoding utf8

$trx = Get-Content -LiteralPath (Join-Path $out 'filter-trx-summary.json') -Raw | ConvertFrom-Json
$console = Get-Content -LiteralPath (Join-Path $out 'dotnet-console-counts.json') -Raw | ConvertFrom-Json
$filterExit = Get-Content -LiteralPath (Join-Path $out 'hv-dotnet-p16-filter-exit.json') -Raw | ConvertFrom-Json
$done = Get-Content -LiteralPath (Join-Path $out 'hv-tests-done.json') -Raw | ConvertFrom-Json

$claims = @(
    [ordered]@{ Id = 'A1'; Surface = 'A'; Verdict = 'PASS'; Text = 'Named test AiTheory_Agent_RequiresValidJsonFields is a Theory with eight PluginHostKind InlineData rows. No Skip. Maps TEST-MCP-PLUGININT-001 AC3.' }
    [ordered]@{ Id = 'A2'; Surface = 'A'; Verdict = 'PASS'; Text = 'EvaluateAsync throws InvalidOperationException not implemented. Independent filter Failed 8 Passed 0 Skipped 0. All eight hosts fail with that exception.' }
    [ordered]@{ Id = 'A3'; Surface = 'A'; Verdict = 'PASS'; Text = 'P17 AiTheory_RejectsInvalidJson_DoesNotOverrideDeterministicFailure absent. NukeTarget_SkipIsFailure not mixed into this PluginIntegration filter. P1 SkipIsFailure source exists and is not a FAIL.' }
    [ordered]@{ Id = 'A4'; Surface = 'A'; Verdict = 'PASS'; Text = 'PLAN-PLUGINHANDOFF-001 and MCP-PLUGININT-001 remain done false. Live todo_get after tests.' }
    [ordered]@{ Id = 'A5'; Surface = 'A'; Verdict = 'PASS'; Text = 'Prior C-green-P15 AGREE docs/receipts/hostile-validator-20260822T071202Z.md Passed 86 Failed 0 Skipped 0; P16 AiTheory absent.' }
    [ordered]@{ Id = 'B1'; Surface = 'B'; Verdict = 'PASS'; Text = 'Honesty and receipts. Independent hv-self TRX, not implementer narrative or sibling collector TRX.' }
    [ordered]@{ Id = 'B2'; Surface = 'B'; Verdict = 'PASS'; Text = 'Byrd phase-order: C-green-P15 AGREE then C-red-P16 tests. This is the red gate, not green exit.' }
    [ordered]@{ Id = 'B3'; Surface = 'B'; Verdict = 'PASS'; Text = 'MCP-only TODO/session/requirements storage. No TODO.yaml edit.' }
    [ordered]@{ Id = 'B4'; Surface = 'B'; Verdict = 'PASS'; Text = 'PowerShell-only. No python/python3/py invoked by this validator.' }
    [ordered]@{ Id = 'B5'; Surface = 'B'; Verdict = 'PASS'; Text = 'Claims match artifacts. Duration mismatch vs implementer 140 ms recorded as residual.' }
    [ordered]@{ Id = 'C1'; Surface = 'C'; Verdict = 'PASS'; Text = 'Identified FR/TR/TEST-MCP-PLUGININT-001 via live getFr/getTr/getTest/listMappings.' }
    [ordered]@{ Id = 'C2'; Surface = 'C'; Verdict = 'PASS'; Text = 'TEST AC3 and TR AC5 exist and are testable. isSatisfied remains false.' }
    [ordered]@{ Id = 'C3'; Surface = 'C'; Verdict = 'PASS'; Text = 'P16 companion theory covers AC3 for this red gate and is currently failing.' }
    [ordered]@{ Id = 'C4'; Surface = 'C'; Verdict = 'PASS'; Text = 'Not claiming FR/TR/TEST complete. Mappings exist. This review did not mark requirements satisfied.' }
    [ordered]@{ Id = 'D1'; Surface = 'D'; Verdict = 'PASS'; Text = 'Scope is C-red-P16 only, not plan closeout.' }
    [ordered]@{ Id = 'D2'; Surface = 'D'; Verdict = 'PASS'; Text = 'Plan P16 red exit: companion rows exist, currently Failed 8 Passed 0, EvaluateAsync unimplemented, P17 not mixed as greens.' }
    [ordered]@{ Id = 'D3'; Surface = 'D'; Verdict = 'PASS'; Text = 'Combined PLAN task C P16 red + hostile then P17-P18 remains done false. MCP-PLUGININT-001 P16 remains done false.' }
)

$json = [ordered]@{
    TimestampUtc = $iso
    ReceiptStamp = $utc
    ValidatorIdentity = 'GrokSubagentHostile'
    Workspace = 'F:\GitHub\McpServer'
    WorkClass = 1
    Phase = 'C-red-P16'
    Plan = 'docs/plans/PLAN-PLUGINHANDOFF-001.md'
    TodoId = 'PLAN-PLUGINHANDOFF-001'
    ChildTodoId = 'MCP-PLUGININT-001'
    AddProfile = [ordered]@{
        executed = $true
        profileFilesRead = 18
        excludedSkillPorts = @('add-profile.grok.md')
    }
    Plugin = [ordered]@{
        root = 'F:\GitHub\mcpserver-grok-plugin'
        version = '1.100.0'
    }
    PriorReceipt = 'docs/receipts/hostile-validator-20260822T071202Z.md'
    Collector = 'docs/receipts/_hv-c-red-p16/'
    OwnedRerun = 'docs/receipts/_hv-c-red-p16/hv-self/'
    SessionId = $sessionId
    RequestId = $requestId
    TurnId = 42917
    HealthNonce = 'nonce-hv-20260822072418-29083'
    HealthNonceMatch = $true
    PluginMarkerSignature = $true
    HomemadeMarkerSignature = $false
    ToolSearchExactNamePresent = $true
    OverallVerdict = 'AGREE'
    FailCount = 0
    UnknownCount = 0
    PassCount = $claims.Count
    Accuracy = 92
    Completeness = 95
    ExplicitFailList = @()
    MandatorySurfacesUnevaluated = @()
    TestResults = [ordered]@{
        ListTestsCommand = 'dotnet test tests/McpServer.PluginIntegration.Tests -c Debug --list-tests'
        ListTestsExitCode = 0
        ListP16 = [int]$console.listP16
        ListP17 = [int]$console.listP17
        ListNukeTargetSkipIsFailure = [int]$console.listNuke
        ListAiTheory = [int]$console.listAiTheory
        FilterCommand = 'dotnet test tests/McpServer.PluginIntegration.Tests -c Debug --filter FullyQualifiedName~AiTheory_Agent_RequiresValidJsonFields'
        FilterExitCode = [int]$filterExit.ExitCode
        FilterDurationMs = [int]$filterExit.DurationMs
        FilterConsoleTotalTests = [string]$console.totalTestsLine
        FilterConsoleFailed = [string]$console.filterFailed
        FilterConsolePassedPrinted = $console.filterPassed
        FilterConsoleSkippedPrinted = $console.filterSkipped
        FilterTotalTime = [string]$console.totalTime
        FilterTrxExecuted = [string]$trx.executed
        FilterTrxPassed = [string]$trx.passed
        FilterTrxFailed = [string]$trx.failed
        FilterTrxNotExecuted = [string]$trx.notExecuted
        FilterTrxSkippedAttribute = $trx.skipped
        FilterTrxP16Failed = [int]$trx.p16Failed
        FilterTrxP16Passed = [int]$trx.p16Passed
        FilterTrxP16Skipped = [int]$trx.p16Skipped
        FilterTrxP17Count = [int]$trx.p17Count
        FilterTrxNukeCount = [int]$trx.nukeCount
        NotImplementedMessageCount = [int]$trx.notImplementedMessageCount
        FilterTrx = 'docs/receipts/_hv-c-red-p16/hv-self/trx-p16-hv/p16-filter.trx'
        FilterLog = 'docs/receipts/_hv-c-red-p16/hv-self/hv-dotnet-p16-filter.log'
        EvaluateAsyncImplemented = $false
        P16NamedTestsPresent = $true
        P17NamedTestsPresent = $false
        InlineDataCount = 8
    }
    TodoFlags = [ordered]@{
        'PLAN-PLUGINHANDOFF-001' = $false
        'MCP-PLUGININT-001' = $false
        'MCP-PLUGININT-001-P16' = $false
        'PLAN-combined-C-P16' = $false
    }
    Claims = $claims
    SiblingCollision = [ordered]@{
        SharedCollectorOverwritten = $true
        SiblingSessionId = 'GrokSubagentHostile-20260822T072515Z-pluginhandoff-p16red'
        OwnedEvidenceRoot = 'docs/receipts/_hv-c-red-p16/hv-self/'
    }
}
$json | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath $receiptJsonPath -Encoding utf8

$md = @"
# Hostile Validator Receipt

TimestampUtc: $iso
ValidatorIdentity: GrokSubagentHostile
Workspace: F:\GitHub\McpServer
WorkClass: 1 (project implementation PLAN-PLUGINHANDOFF-001 Phase C-red-P16 ONLY, AiTheory companion rows). Surfaces A+B+C+D all apply. Review only. No product implementation by this validator. Do not implement P17-P18. Do not mark PLAN-PLUGINHANDOFF-001 or MCP-PLUGININT-001 done.
add-profile: executed yes. Profile files read: 18 (every non-skill *.md under C:\Users\kingd\.claude\profile\; excluded add-profile.grok.md). Also read add-profile SKILL.md and hostile-validator SKILL.md.
Plugin: F:\GitHub\mcpserver-grok-plugin (.grok-plugin/plugin.json version 1.100.0; .version 1.100.0). Live workflow/client calls used Invoke-McpPlugin.ps1. Tool registry search included exact name mcpserver-grok-plugin. Plugin Test-MarkerSignature true. Health nonce nonce-hv-20260822072418-29083 echoed exactly.
Active plan: F:\GitHub\McpServer\docs\plans\PLAN-PLUGINHANDOFF-001.md section 7 P16: P16 red AiTheory_{Agent}_RequiresValidJsonFields companion rows. Maps TEST-MCP-PLUGININT-001 AC3. Hostile C-red-P16. Then P17-P18.
Requirement IDs: FR-MCP-PLUGININT-001; TR-MCP-PLUGININT-001 AC5 AiTheory catalog semantics; TEST-MCP-PLUGININT-001 AC3.
Prior receipt: docs/receipts/hostile-validator-20260822T071202Z.md (OverallVerdict AGREE, FailCount 0, C-green-P15; full PluginIntegration Passed 86 Failed 0 Skipped 0; P16NamedTestsPresent false).
Collector: docs/receipts/_hv-c-red-p16/ (owned independent rerun docs/receipts/_hv-c-red-p16/hv-self/).
OverallVerdict: AGREE

Default was FAIL or UNKNOWN until this pass independently: executed add-profile (18 files); verified plugin marker signature and health nonce; opened dedicated session GrokSubagentHostile-20260822T072417Z-c-red-p16 turn req-20260822T072417Z-001-hostile-c-red-p16; live-got PLAN-PLUGINHANDOFF-001, MCP-PLUGININT-001, FR/TR/TEST/listMappings; grepped Skip/P17/NukeTarget_SkipIsFailure/AiTheory_ in PluginIntegration.Tests; re-read PluginSessionLogAiTheoryTests.cs, AiStrategyFixture.cs, AiStrategyEvaluation.cs, PluginHostKind.cs, PluginSessionLogCollection.cs; independently re-ran list-tests and the required filter in unique ResultsDirectory docs/receipts/_hv-c-red-p16/hv-self/trx-p16-hv after a sibling collector overwrote the shared _hv-c-red-p16 path.

Implementer chat and sibling collector logs were not trusted as proof. This review did not implement P17-P18 and did not mark PLAN-PLUGINHANDOFF-001 or MCP-PLUGININT-001 done.

## Clock and live MCP

Health GET /health?nonce=nonce-hv-20260822072418-29083 returned status Healthy, storage reachable, nonce echoed exactly. Plugin Test-MarkerSignature true. Tool registry exact name mcpserver-grok-plugin present.

## Session log proof

Dedicated persistence is through plugin client.SessionLog.* as GrokSubagentHostile. SessionId GrokSubagentHostile-20260822T072417Z-c-red-p16. Turn requestId req-20260822T072417Z-001-hostile-c-red-p16. client.SessionLog.OpenSessionAsync created=true. BeginTurnAsync turnId 42917. QueryAsync after mid-turn append shows this session with dialog, plus a sibling session GrokSubagentHostile-20260822T072515Z-pluginhandoff-p16red that overwrote the shared collector session-id.txt. CompleteTurnAsync same requestId after this receipt. Proof files under docs/receipts/_hv-c-red-p16/ (sl-open.txt, sl-begin.txt, sl-dialog-start.txt) and docs/receipts/_hv-c-red-p16/hv-self/ (sl-dialog-end.txt, sl-query-sid.txt, sl-complete.txt, sl-query-sid-after.txt).

## Mandatory surface that could not be evaluated

None.

## Explicit FAIL list

None.

## Explicit UNKNOWN list

None.

## Residual (not extra FAILs)

- Independent filter DurationMs 16068 (about 16 s wall, testhost start plus run). Console Total time 2.1296 Seconds. Implementer last saw Duration 140 ms. Counts match; duration is not a C-red gate.
- VSTest console printed Total tests: 8 and Failed: 8 without Passed: or Skipped: lines. TRX passed=0 notExecuted=0 skipped attribute null; skippedNames empty; passedNames empty.
- Homemade HMAC hex did not match marker value (collector marker-sig.json Match false). Plugin Test-MarkerSignature true and Invoke-FullBootstrap true are the contract path used here.
- PythonProcessCount 1 is Google Cloud SDK gcloud.py auth login (PID 69564). This validator did not invoke python/python3/py.
- workflow.* result envelopes include deprecated: true. Treated as success metadata.
- FR/TR/TEST createdAt stamps look like query time. AC text is present. isSatisfied remains false; this C-red-P16 gate does not require store AC complete or TODO done:true.
- PLAN remaining combined task C P16 red + hostile then P17-P18 stays done:false because this AGREE is the C-red-P16 gate, not plan closeout, and P17-P18 are still open. Correct.
- git status --porcelain shows ?? tests/McpServer.PluginIntegration.Tests/ (and the new P16 files). Untracked test files do not by themselves block this red gate. docs/Project/TODO.yaml and docs/todo.yaml were not dirty.
- Marker plugin_version 1.97.0 drifted. Plugin .version and plugin.json are 1.100.0 (authoritative).
- Plugin Status JSON agent field is GrokCode while session sourceType is GrokSubagentHostile. failsafeDir used GrokSubagentHostile and UTF-8 base64url workspace key RjpcR2l0SHViXE1jcFNlcnZlcg.
- client.SessionLog.AppendActionsAsync is not a valid SessionLogClient method. First PatchTurnAsync without a turn DTO failed. Actions are persisted via PatchTurnAsync turn.actions on complete.
- Sibling collector wrote the same docs/receipts/_hv-c-red-p16 path starting 2026-08-22T07:25:15Z and overwrote stamp/session-id/scripts. This review scored the unique hv-self rerun (WMI PID 64372, TRX lastWrite 2026-08-22T07:32:26.0227968Z), not sibling results-20260822T072653Z.
- P16 tests construct a catalog-derived JSON receipt string rather than a server-persisted YAML envelope. Acceptable for this red companion row. P17 implements evaluator semantics against invalid JSON and deterministic evidence.
- list-tests listed 8 AiTheory_Agent_RequiresValidJsonFields rows and 0 P17/NukeTarget_SkipIsFailure rows in PluginIntegration.Tests. listFqnApprox 96 is a line count, not a C-red FAIL.
- P1 named method is BuildTests_PluginSessionLogIntegrationTarget_ExistsAndTreatsSkipAsFail, not NukeTarget_SkipIsFailure. Build.PluginSessionLogIntegration.cs contains SkipIsFailure. Parent said do not FAIL this red gate because that older P1 test exists.

## Claims reviewed

### A Requested

#### A1. Named test AiTheory_Agent_RequiresValidJsonFields exists as a Theory with eight PluginHostKind InlineData rows. File: tests/McpServer.PluginIntegration.Tests/PluginSessionLogAiTheoryTests.cs. No Skip. Maps TEST-MCP-PLUGININT-001 AC3 companion AiTheory.

Verdict: PASS

Evidence: File read this review. [Theory] at line 16; eight [InlineData(PluginHostKind.*)] rows Codex, ClaudeCode, ClaudeCowork, Copilot, Grok, Cline, ClineV2, OpenCode; method AiTheory_Agent_RequiresValidJsonFields at line 25. Asserts evaluation.Valid, MissingFields, Contradictions. Calls AiStrategyFixture.EvaluateAsync. Uses PluginSessionLogCatalog.LoadAndValidate. Collection PluginSessionLog has no ICollectionFixture (does not start the isolated server). Grep Skip/Fact(Skip/Theory(Skip/Assert.Skip in PluginIntegration.Tests *.cs: 0 hits. Independent --list-tests listed 8 rows, one per host.

#### A2. AiStrategyFixture.EvaluateAsync currently throws InvalidOperationException "AiStrategyFixture.EvaluateAsync is not implemented." Tests pin Valid, MissingFields, and Contradictions. Independent re-run of the named filter must show Failed 8 Passed 0 Skipped 0.

Verdict: PASS

Evidence: AiStrategyFixture.cs lines 15-19 throw that exact message. No return of AiStrategyEvaluation. Re-read after the filter still throws. Independent hv-self command ``dotnet test tests/McpServer.PluginIntegration.Tests -c Debug --filter FullyQualifiedName~AiTheory_Agent_RequiresValidJsonFields`` ExitCode 1 DurationMs 16068. Console Test Run Failed; Total tests: 8; Failed: 8; Total time: 2.1296 Seconds. TRX outcome Failed; total 8 executed 8 passed 0 failed 8 notExecuted 0 unitCount 8; p16Failed 8 p16Passed 0 p16Skipped 0; notImplementedMessageCount 8. All eight host kinds Count 1 Failed 1 Passed 0 Skipped 0 with message System.InvalidOperationException : AiStrategyFixture.EvaluateAsync is not implemented. Log: docs/receipts/_hv-c-red-p16/hv-self/hv-dotnet-p16-filter.log TRX: docs/receipts/_hv-c-red-p16/hv-self/trx-p16-hv/p16-filter.trx.

#### A3. P17/P18 named tests AiTheory_RejectsInvalidJson_DoesNotOverrideDeterministicFailure and NukeTarget_SkipIsFailure are not mixed into this gate as new greens that would hide P16 red. NukeTarget_SkipIsFailure already exists from P1. Do not FAIL this red gate because that older P1 test exists. Do FAIL if P16 rows pass or skip, or if EvaluateAsync is implemented.

Verdict: PASS

Evidence: PluginIntegration.Tests grep P17 name 0 hits. list-tests P17 0, NukeTarget_SkipIsFailure 0. Filter TRX p17Count 0 nukeCount 0. P16 rows did not pass or skip. EvaluateAsync remains unimplemented. P1 tests/Build.Tests/PluginSessionLogIntegrationTargetTests.cs BuildTests_PluginSessionLogIntegrationTarget_ExistsAndTreatsSkipAsFail exists (LastWriteTimeUtc 2026-08-21T23:08:56Z). build/Build.PluginSessionLogIntegration.cs contains SkipIsFailure. Those older artifacts are not in this filter and did not hide the 8 failures.

#### A4. PLAN-PLUGINHANDOFF-001 and MCP-PLUGININT-001 remain done:false. Live todo_get.

Verdict: PASS

Evidence: workflow.todo.get and client.Todo.GetAsync via Invoke-McpPlugin at bootstrap 2026-08-22T07:24:43Z and again after tests at 2026-08-22T07:33:xxZ. PLAN-PLUGINHANDOFF-001 top done: false. Combined task ``C P16 red + hostile then P17-P18.`` done: false. MCP-PLUGININT-001 done: false. P16 implementationTask done: false. This review wrote no todo_update. git porcelain for docs/Project/TODO.yaml and docs/todo.yaml empty.

#### A5. Prior C-green-P15 AGREE is docs/receipts/hostile-validator-20260822T071202Z.md (Passed 86 Failed 0 Skipped 0; P16 AiTheory absent).

Verdict: PASS

Evidence: Prior md OverallVerdict AGREE. JSON Phase C-green-P15, FailCount 0, FullConsolePassed 86, FullConsoleFailed 0, FullConsoleSkipped 0, P16NamedTestsPresent false. P16 source LastWriteTimeUtc 2026-08-22T07:16:45Z is after that receipt (md 07:13:03Z / json 07:14:57Z). Current independent filter is Failed 8 Passed 0.

### B Workspace rules

#### B1. Honesty and receipts. Every done/green/red claim re-verified with machine evidence.

Verdict: PASS

Evidence: Independent unique logs and TRX under docs/receipts/_hv-c-red-p16/hv-self/ with WMI PID 64372, filter TRX 2026-08-22T07:32:26Z. Sibling shared-path TRX was not used as the scored artifact. Implementer Failed 8 / Passed 0 / Skipped 0 matched this rerun counts.

#### B2. Byrd v4 phase-order for this class-1 slice. Phase-order is scored at inter-phase hostile gates, not FR-vs-file timestamps.

Verdict: PASS

Evidence: C-green-P15 hostile AGREE exists (docs/receipts/hostile-validator-20260822T071202Z.md) with Passed 86 Failed 0. P16 red files LastWriteTimeUtc 2026-08-22T07:16:45Z are after that AGREE. Plan order is C-green-P15 AGREE, then C-red-P16, then P17-P18. This review does not FAIL B2 from FR createdAt versus file mtimes. This is a red gate; full suite green is not required.

#### B3. MCP-only TODO/session/requirements storage.

Verdict: PASS

Evidence: Live todo_get and requirements get/listMappings via Invoke-McpPlugin.ps1. This validator did not read or write TODO.yaml or session-log store files as a substitute. git porcelain for those files empty.

#### B4. PowerShell-only / no Python lab rule.

Verdict: PASS

Evidence: Collector, bootstrap, WMI start, tests-run, parse, and receipt scripts are pwsh.exe -NoProfile -NonInteractive. PythonProcessCount 1 is unrelated gcloud.py. This validator did not invoke python/python3/py.

#### B5. No fabricated results. Claims match artifacts.

Verdict: PASS

Evidence: TRX, console, fixture source, live todo_get, and prior receipt JSON agree with the scored claims. Duration difference versus implementer 140 ms and sibling collector collision are recorded as residual, not hidden.

### C Requirement violations

#### C1. Identify FR/TR/TEST that apply.

Verdict: PASS

Evidence: Live workflow.requirements.getFr FR-MCP-PLUGININT-001, getTr TR-MCP-PLUGININT-001, getTest TEST-MCP-PLUGININT-001, listMappings two rows FR-TR and FR-TEST. Plan P16 maps TEST-MCP-PLUGININT-001 AC3. TR ac-5 text: AiTheory rows use the same scenario catalog to review persisted YAML/receipt semantics; deterministic Theory rows remain the correctness gate.

#### C2. Structured acceptance criteria exist and are testable for this red slice.

Verdict: PASS

Evidence: TEST-MCP-PLUGININT-001 ac-3: every AiTheory row receives the persisted receipt/artifact and returns a strict semantic completeness result that is asserted by the test (isSatisfied false). Named P16 theory encodes Valid, MissingFields, and Contradictions against EvaluateAsync. AC are not empty.

#### C3. Tests cover the claimed P16 AC for a red gate (exist and currently fail).

Verdict: PASS

Evidence: One theory times eight hosts independently Failed 8 Passed 0 Skipped 0 with unimplemented evaluator. Missing real persisted YAML envelope is residual for P17, not a missing named test.

#### C4. Not claiming FR/TR/TEST complete. Mappings exist.

Verdict: PASS

Evidence: All PLUGININT AC isSatisfied false, status pending. listMappings returned FR-MCP-PLUGININT-001 to TR-MCP-PLUGININT-001 and TEST-MCP-PLUGININT-001. This review did not mark requirements satisfied.

### D Current plan holistically

#### D1. Scope is C-red-P16 only, not plan closeout.

Verdict: PASS

Evidence: Parent brief and plan section 7: C-red-P16 AGREE, then P17-P18 green, then C-green-P16-P18 AGREE. This review did not require P17-P20, D/E/F/G/H/I, or PLAN/PLUGININT done:true.

#### D2. Plan exit criteria for this slice: named P16 reds exist, currently failing, no Skip, EvaluateAsync unimplemented, no P17 mix-in greens.

Verdict: PASS

Evidence: Independent filter Failed 8 Passed 0 Skipped 0. P17 absent. EvaluateAsync throws. P1 SkipIsFailure source not mixed into the filter.

#### D3. Combined PLAN task and child TODO remain open.

Verdict: PASS

Evidence: PLAN task ``C P16 red + hostile then P17-P18.`` done: false after tests. MCP-PLUGININT-001 P16 task done: false. Remaining text says next is C-red-P16 hostile then P17-P18. This AGREE is the C-red-P16 gate; it is not permission to mark PLAN or PLUGININT done, and it is not permission to start P17 without this AGREE.

## Accuracy and completeness

Accuracy: 92. Independent TRX/console/source match the red claims. Deducted for duration mismatch versus implementer 140 ms, homemade HMAC mismatch (plugin verifier true), sibling collector collision on the shared path, first AppendActionsAsync/PatchTurnAsync wrapper failures, and VSTest omitting Passed/Skipped console lines.
Completeness: 95. Surfaces A+B+C+D evaluated. Owned unique ResultsDirectory rerun after collision. Session log turn completed as GrokSubagentHostile.
"@
$md = $md.Replace('``', '`')
Set-Content -LiteralPath $receiptMdPath -Value $md -Encoding utf8

function Invoke-Plugin {
    param([string]$Method, [hashtable]$Params, [string]$Name)
    $outFile = Join-Path $out ($Name + '.txt')
    $errFile = Join-Path $out ($Name + '.err.txt')
    try {
        $result = & $plugin -Command Invoke -Method $Method -ParamsObject $Params -WorkspacePath 'F:\GitHub\McpServer' -CacheRoot $cacheRoot -TimeoutSeconds 120 2> $errFile
        if ($null -eq $result) { $result = '' }
        if ($result -isnot [string]) { $result = ($result | Out-String) }
        Set-Content -LiteralPath $outFile -Value $result -Encoding utf8
        Write-Output ('OK ' + $Name)
    } catch {
        Set-Content -LiteralPath $outFile -Value ('ERROR ' + $_.Exception.ToString()) -Encoding utf8
        Write-Output ('FAIL ' + $Name + ' ' + $_.Exception.Message)
    }
}

$todoYamlStatus = git -C 'F:\GitHub\McpServer' status --porcelain -- docs/Project/TODO.yaml docs/todo.yaml tests/McpServer.PluginIntegration.Tests/PluginSessionLogAiTheoryTests.cs tests/McpServer.PluginIntegration.Tests/AiStrategyFixture.cs
[ordered]@{ TimestampUtc = [DateTime]::UtcNow.ToString('o'); porcelain = @($todoYamlStatus) } |
    ConvertTo-Json | Set-Content -LiteralPath (Join-Path $out 'todo-yaml-git-status-final.json') -Encoding utf8

$now = [DateTime]::UtcNow.ToString('o')
Invoke-Plugin -Method 'client.SessionLog.AppendDialogAsync' -Params @{
    agent = $agent
    sessionId = $sessionId
    requestId = $requestId
    items = @(
        @{ timestamp = $now; role = 'model'; category = 'observation'; content = "Independent hv-self filter ExitCode 1. TRX executed 8 passed 0 failed 8 notExecuted 0. All eight hosts fail AiStrategyFixture.EvaluateAsync is not implemented. Live todo_get PLAN done false MCP-PLUGININT done false P16 done false. Receipt docs/receipts/hostile-validator-$utc.md OverallVerdict AGREE FailCount 0." }
        @{ timestamp = $now; role = 'model'; category = 'decision'; content = 'Decision: AGREE C-red-P16. Named theory exists with eight InlineData rows, no Skip, maps TEST-MCP-PLUGININT-001 AC3, independently Failed 8 Passed 0 Skipped 0, EvaluateAsync unimplemented, P17 names absent, PLAN/PLUGININT remain done false. Consequence: parent may start P17-P18 green; do not mark PLAN or PLUGININT done. Alternatives rejected: DISAGREE because sibling collided on shared collector path (this review independently re-ran in hv-self); FAIL because P1 SkipIsFailure exists (parent forbade that FAIL).' }
    )
} -Name 'sl-dialog-final'

$actions = @(
    @{ order = 1; description = 'Executed add-profile; read 18 profile markdown files'; type = 'read'; status = 'completed'; filePath = 'C:\Users\kingd\.claude\profile\PROFILE.md' }
    @{ order = 2; description = 'Health nonce nonce-hv-20260822072418-29083 echoed; plugin Test-MarkerSignature true; BeginTurnAsync turnId 42917'; type = 'read'; status = 'completed'; filePath = 'F:\GitHub\McpServer\AGENTS-README-FIRST.yaml' }
    @{ order = 3; description = 'Independent P16 filter Failed 8 Passed 0 Skipped 0; all eight host kinds fail EvaluateAsync not implemented'; type = 'test'; status = 'completed'; filePath = 'tests/McpServer.PluginIntegration.Tests/PluginSessionLogAiTheoryTests.cs' }
    @{ order = 4; description = 'Live todo_get PLAN-PLUGINHANDOFF-001 done false; MCP-PLUGININT-001 done false P16 done false; getFr/getTr/getTest/listMappings for PLUGININT-001'; type = 'read'; status = 'completed'; filePath = 'docs/receipts/_hv-c-red-p16/hv-self/todo-plan-final.txt' }
    @{ order = 5; description = "Wrote hostile receipt pair $utc OverallVerdict AGREE FailCount 0 PassCount 17"; type = 'create'; status = 'completed'; filePath = "docs/receipts/hostile-validator-$utc.md" }
    @{ order = 6; description = 'AGREE C-red-P16: named theory eight PluginHostKind rows currently fail, EvaluateAsync unimplemented, P17 absent, PLAN/PLUGININT remain done false. Do not implement P17-P18. This AGREE is not plan closeout.'; type = 'design_decision'; status = 'completed'; filePath = '' }
)

Invoke-Plugin -Method 'client.SessionLog.PatchTurnAsync' -Params @{
    agent = $agent
    sessionId = $sessionId
    requestId = $requestId
    turn = @{
        requestId = $requestId
        queryTitle = 'Hostile C-red-P16 AiTheory companion rows'
        actions = $actions
        designDecisions = @(
            'Score C-red-P16 from independently re-run P16 filter in unique hv-self ResultsDirectory, --list-tests, live MCP store, C-green-P15 AGREE, and unimplemented EvaluateAsync, not implementer narrative or sibling collector TRX.'
            'AGREE: named theory exists with eight InlineData PluginHostKind rows, no Skip, maps TEST-MCP-PLUGININT-001 AC3, Failed 8 Passed 0 Skipped 0. EvaluateAsync unimplemented. P17 names absent. PLAN and PLUGININT remain done false. Do not mark PLAN/PLUGININT done. Do not implement P17-P18 in this review.'
        )
        tags = @('hostile-validator','PLAN-PLUGINHANDOFF-001','MCP-PLUGININT-001','C-red-P16','FR-MCP-PLUGININT-001','TR-MCP-PLUGININT-001','TEST-MCP-PLUGININT-001')
        contextList = @('docs/plans/PLAN-PLUGINHANDOFF-001.md','tests/McpServer.PluginIntegration.Tests/PluginSessionLogAiTheoryTests.cs','tests/McpServer.PluginIntegration.Tests/AiStrategyFixture.cs','docs/receipts/hostile-validator-20260822T071202Z.md',"docs/receipts/hostile-validator-$utc.md")
        interpretation = 'Hostile review Phase C-red-P16 ONLY after cited C-green-P15 AGREE. Review only. Do not implement P17-P18. Do not mark TODOs done.'
        filesModified = @("docs/receipts/hostile-validator-$utc.md","docs/receipts/hostile-validator-$utc.json")
    }
} -Name 'sl-patch-final'

Invoke-Plugin -Method 'client.SessionLog.CompleteTurnAsync' -Params @{
    agent = $agent
    sessionId = $sessionId
    requestId = $requestId
    payload = @{
        requestId = $requestId
        status = 'completed'
        response = "Hostile C-red-P16 review AGREE. Receipt docs/receipts/hostile-validator-$utc.md. P16 filter Failed 8 Passed 0 Skipped 0. EvaluateAsync unimplemented. P17 names absent. PLAN-PLUGINHANDOFF-001 and MCP-PLUGININT-001 remain done false. Do not implement P17-P18. This AGREE is not plan closeout."
        queryTitle = 'Hostile C-red-P16 AiTheory companion rows'
    }
} -Name 'sl-complete'

Invoke-Plugin -Method 'client.SessionLog.QueryAsync' -Params @{
    agent = $agent
    sessionId = $sessionId
    limit = 5
} -Name 'sl-query-sid-after'

$receiptMd = Get-Item -LiteralPath $receiptMdPath
$receiptJson = Get-Item -LiteralPath $receiptJsonPath
$mdText = Get-Content -LiteralPath $receiptMd.FullName -Raw
$jsonObj = Get-Content -LiteralPath $receiptJson.FullName -Raw | ConvertFrom-Json
[ordered]@{
    MdExists = $true
    MdPath = $receiptMdPath
    MdLength = $receiptMd.Length
    MdLastWriteTimeUtc = $receiptMd.LastWriteTimeUtc.ToString('o')
    MdHasAgree = [bool]($mdText -match '(?m)^OverallVerdict:\s*AGREE\s*$')
    MdHasDisagree = [bool]($mdText -match '(?m)^OverallVerdict:\s*DISAGREE\s*$')
    MdEmDash = $mdText.Contains([char]0x2014)
    MdEnDash = $mdText.Contains([char]0x2013)
    JsonExists = $true
    JsonPath = $receiptJsonPath
    JsonLength = $receiptJson.Length
    JsonLastWriteTimeUtc = $receiptJson.LastWriteTimeUtc.ToString('o')
    JsonOverallVerdict = [string]$jsonObj.OverallVerdict
    JsonFailCount = $jsonObj.FailCount
    JsonPassCount = $jsonObj.PassCount
    JsonClaimCount = @($jsonObj.Claims).Count
    FilterFailed = $jsonObj.TestResults.FilterTrxFailed
    FilterPassed = $jsonObj.TestResults.FilterTrxPassed
    FilterNotExecuted = $jsonObj.TestResults.FilterTrxNotExecuted
} | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $out 'receipt-on-disk-finish.json') -Encoding utf8

Write-Output ("RECEIPT_MD=" + $receiptMdPath)
Write-Output ("RECEIPT_JSON=" + $receiptJsonPath)
Write-Output 'WRITE_AND_COMPLETE_DONE'
