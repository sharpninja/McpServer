#Requires -Version 7.0
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$obj = [ordered]@{
    TimestampUtc = '2026-08-22T08:46:49Z'
    ReceiptStamp = '20260822T084649Z'
    ValidatorIdentity = 'GrokSubagentHostile'
    Workspace = 'F:\GitHub\McpServer'
    WorkClass = 1
    Phase = 'C-green-P16-P18-r2'
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
    PriorReceipt = 'docs/receipts/hostile-validator-20260822T081750Z.md'
    PriorCRedReceipt = 'docs/receipts/hostile-validator-20260822T073737Z.md'
    Collector = 'docs/receipts/_hv-c-green-p16-p18-r2/'
    SessionId = 'GrokSubagentHostile-20260822T083324Z-c-green-p16-p18-r2'
    RequestId = 'req-20260822T083324Z-001-hostile-c-green-p16-p18-r2'
    TurnId = 42930
    HealthNonce = 'nonce-hv-20260822083325-84692'
    HealthNonceMatch = $true
    PluginMarkerSignature = $true
    HomemadeMarkerSignature = $false
    ToolSearchExactNamePresent = $true
    OverallVerdict = 'AGREE'
    FailCount = 0
    UnknownCount = 0
    PassCount = 20
    Accuracy = 96
    Completeness = 94
    ExplicitFailList = @()
    MandatorySurfacesUnevaluated = @()
    TestResults = [ordered]@{
        ListTestsCommand = 'dotnet test tests/McpServer.PluginIntegration.Tests -c Debug --list-tests'
        ListTestsExitCode = 0
        ListedAllApprox = 95
        ListedAiApprox = 9
        ListedDetApprox = 86
        ListP16 = 8
        ListP17 = 1
        ListP19 = 0
        AiFilterCommand = 'dotnet test tests/McpServer.PluginIntegration.Tests -c Debug --filter PluginInt=AI'
        AiFilterExitCode = 0
        AiFilterDurationMs = 21097
        AiConsoleTotal = 9
        AiConsolePassed = 9
        AiConsoleFailed = 0
        AiConsoleSkipped = 0
        AiTrxTotal = '9'
        AiTrxPassed = '9'
        AiTrxFailed = '0'
        AiTrxNotExecuted = '0'
        AiTrxP16Passed = 8
        AiTrxP17Passed = 1
        AiTrxP19Count = 0
        AiTrx = 'docs/receipts/_hv-c-green-p16-p18-r2/results-ai-20260822T084254Z/ai-theory.trx'
        NukeFilterCommand = 'dotnet test tests/Build.Tests -c Debug --filter FullyQualifiedName~NukeTarget_SkipIsFailure'
        NukeFilterExitCode = 0
        NukeFilterDurationMs = 4119
        NukeConsoleTotal = 1
        NukeConsolePassed = 1
        NukeConsoleFailed = 0
        NukeConsoleSkipped = 0
        NukeTrxPassed = '1'
        NukeTrxFailed = '0'
        NukeTrx = 'docs/receipts/_hv-c-green-p16-p18-r2/results-nuke-reread-20260822T084513Z/nuke-skip-reread.trx'
        NukeBuildSourceUtc = '2026-08-22T08:44:07.2351641Z'
        NukeTestSourceUtc = '2026-08-22T08:43:22.1103753Z'
        FullSuiteIndependentlyRerun = $false
        TraitUnionEqualsAll = $true
        EvaluateAsyncImplemented = $true
        P17NamedTestsPresent = $true
        PreflightPresent = $true
        DeterministicFilterPresent = $true
        AiFilterPresent = $true
        FailIfSkippedBothTrx = $true
        P19NamedTestsPresent = $false
        PlanTopDone = $false
        PluginIntTopDone = $false
        PlanCombinedC16Done = $false
        PluginIntP18Done = $false
    }
    ClaimVerdicts = [ordered]@{
        A1 = 'PASS'
        A2 = 'PASS'
        A3 = 'PASS'
        A4 = 'PASS'
        A5 = 'PASS'
        A6 = 'PASS'
        B1 = 'PASS'
        B2 = 'PASS'
        B3 = 'PASS'
        B4 = 'PASS'
        B5 = 'PASS'
        C1 = 'PASS'
        C2 = 'PASS'
        C3 = 'PASS'
        C4 = 'PASS'
        C5 = 'PASS'
        D1 = 'PASS'
        D2 = 'PASS'
        D3 = 'PASS'
        D4 = 'PASS'
    }
}

$jsonPath = 'F:\GitHub\McpServer\docs\receipts\hostile-validator-20260822T084649Z.json'
$obj | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath $jsonPath -Encoding utf8
Copy-Item -LiteralPath $jsonPath -Destination 'F:\GitHub\McpServer\docs\receipts\_hv-c-green-p16-p18-r2\hostile-validator-20260822T084649Z.json' -Force
Write-Output 'JSON_WRITTEN'
