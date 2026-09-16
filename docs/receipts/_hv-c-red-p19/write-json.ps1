#Requires -Version 7.0
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$out = 'F:\GitHub\McpServer\docs\receipts\_hv-c-red-p19'
$stamp = '20260822T092424Z'
$obj = [ordered]@{
    TimestampUtc = '2026-08-22T09:24:24Z'
    ReceiptStamp = $stamp
    ValidatorIdentity = 'GrokSubagentHostile'
    Workspace = 'F:\GitHub\McpServer'
    WorkClass = 1
    Phase = 'C-red-P19'
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
    PriorReceipt = 'docs/receipts/hostile-validator-20260822T084649Z.md'
    Collector = 'docs/receipts/_hv-c-red-p19/'
    SessionId = 'GrokSubagentHostile-20260822T090257Z-c-red-p19'
    RequestId = 'req-20260822T090257Z-001-hostile-c-red-p19'
    TurnId = 42935
    HealthNonce = 'nonce-hv-20260822090258-672'
    HealthNonceMatch = $true
    PluginMarkerSignature = $true
    HomemadeMarkerSignature = $false
    ToolSearchExactNamePresent = $true
    OverallVerdict = 'AGREE'
    FailCount = 0
    UnknownCount = 0
    PassCount = 19
    Accuracy = 96
    Completeness = 95
    ExplicitFailList = @()
    MandatorySurfacesUnevaluated = @()
    TestResults = [ordered]@{
        ListTestsCommand = 'dotnet test tests/McpServer.PluginIntegration.Tests -c Debug --list-tests --filter FullyQualifiedName~PluginNativeSuiteReceiptTests'
        ListTestsExitCode = 0
        ListedCount = 3
        ListedNames = @(
            'McpServer.PluginIntegration.Tests.PluginNativeSuiteReceiptTests.PluginNativeSuite_EachOfficialPlugin_FailedZeroSkippedZero'
            'McpServer.PluginIntegration.Tests.PluginNativeSuiteReceiptTests.PluginNativeSuite_AfterSyncAgentPlugins_FailedZeroSkippedZero'
            'McpServer.PluginIntegration.Tests.PluginNativeSuiteReceiptTests.PluginInt_P19_RecordsBranchAndSha_NoUnrelatedCommit'
        )
        FilterCommand = 'dotnet test tests/McpServer.PluginIntegration.Tests -c Debug --filter FullyQualifiedName~PluginNativeSuiteReceiptTests'
        FilterExitCode = 1
        FilterDurationMs = 18165
        ConsoleTotal = 3
        ConsoleFailed = 3
        ConsolePassed = 0
        ConsoleSkipped = 0
        ConsoleTotalTimeSeconds = 2.4977
        TrxTotal = '3'
        TrxExecuted = '3'
        TrxPassed = '0'
        TrxFailed = '3'
        TrxNotExecuted = '0'
        TrxP19Failed = 3
        TrxP19Passed = 0
        TrxP19Skipped = 0
        TrxP20Count = 0
        FileNotFoundCount = 3
        FileNotFoundAllFailed = $true
        Trx = 'docs/receipts/_hv-c-red-p19/results-p19-20260822T092241Z/p19-red.trx'
        PluginIntP19DirectoryCountAfterTests = 0
        P19TestsLastWriteTimeUtc = '2026-08-22T08:53:43.7029903Z'
        MachineClearWaitSeconds = 510.025508
    }
    Claims = @(
        [ordered]@{ id = 'A1'; surface = 'A'; verdict = 'PASS'; title = 'Three named P19 tests exist, no Skip' }
        [ordered]@{ id = 'A2'; surface = 'A'; verdict = 'PASS'; title = 'Independent filter Failed 3 Passed 0 Skipped 0 FileNotFoundException' }
        [ordered]@{ id = 'A3'; surface = 'A'; verdict = 'PASS'; title = 'Tests pin AC; empty folder cannot green' }
        [ordered]@{ id = 'A4'; surface = 'A'; verdict = 'PASS'; title = 'P20 absent; P19 did not pass; no fake-green pluginint-p19 dirs' }
        [ordered]@{ id = 'A5'; surface = 'A'; verdict = 'PASS'; title = 'PLAN-PLUGINHANDOFF-001 and MCP-PLUGININT-001 done false after tests' }
        [ordered]@{ id = 'A6'; surface = 'A'; verdict = 'PASS'; title = 'Prior C-green-P16-P18 AGREE exists' }
        [ordered]@{ id = 'B1'; surface = 'B'; verdict = 'PASS'; title = 'Byrd C-green AGREE then P19 red tests currently red' }
        [ordered]@{ id = 'B2'; surface = 'B'; verdict = 'PASS'; title = 'Independent receipts re-run and re-read' }
        [ordered]@{ id = 'B3'; surface = 'B'; verdict = 'PASS'; title = 'MCP-only TODO/session/requirements storage' }
        [ordered]@{ id = 'B4'; surface = 'B'; verdict = 'PASS'; title = 'PowerShell only; validator did not invoke Python' }
        [ordered]@{ id = 'B5'; surface = 'B'; verdict = 'PASS'; title = 'Honesty: claims match artifacts' }
        [ordered]@{ id = 'C1'; surface = 'C'; verdict = 'PASS'; title = 'FR/TR/TEST PLUGININT-001 exist with AC and mappings' }
        [ordered]@{ id = 'C2'; surface = 'C'; verdict = 'PASS'; title = 'TEST AC5 native-suite half pinned by named P19 tests currently red' }
        [ordered]@{ id = 'C3'; surface = 'C'; verdict = 'PASS'; title = 'AC testable for this slice; isSatisfied not required' }
        [ordered]@{ id = 'C4'; surface = 'C'; verdict = 'PASS'; title = 'Did not demand native-suite green, P20, or closeout' }
        [ordered]@{ id = 'D1'; surface = 'D'; verdict = 'PASS'; title = 'C-red-P19 only; no P19 execute' }
        [ordered]@{ id = 'D2'; surface = 'D'; verdict = 'PASS'; title = 'C-red-P19 DoD: three named tests exist, red, P20 not mixed' }
        [ordered]@{ id = 'D3'; surface = 'D'; verdict = 'PASS'; title = 'Not plan closeout; TODOs remain done false' }
        [ordered]@{ id = 'D4'; surface = 'D'; verdict = 'PASS'; title = 'No pluginint-p19 evidence folder; green not started' }
    )
}

$json = $obj | ConvertTo-Json -Depth 8
$rootPath = "F:\GitHub\McpServer\docs\receipts\hostile-validator-$stamp.json"
Set-Content -LiteralPath $rootPath -Value $json -Encoding utf8
Copy-Item -LiteralPath $rootPath -Destination (Join-Path $out "hostile-validator-$stamp.json") -Force
Copy-Item -LiteralPath "F:\GitHub\McpServer\docs\receipts\hostile-validator-$stamp.md" -Destination (Join-Path $out "hostile-validator-$stamp.md") -Force
Write-Output ('WROTE ' + $rootPath)
Write-Output ('MD_EXISTS=' + (Test-Path -LiteralPath "F:\GitHub\McpServer\docs\receipts\hostile-validator-$stamp.md"))
Write-Output ('JSON_LENGTH=' + (Get-Item -LiteralPath $rootPath).Length)
