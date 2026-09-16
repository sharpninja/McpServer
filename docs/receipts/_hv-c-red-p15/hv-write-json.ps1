#Requires -Version 7.0
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$jsonPath = 'F:\GitHub\McpServer\docs\receipts\hostile-validator-20260822T053539Z.json'
$claims = @(
    @{ id = 'A1'; surface = 'A'; verdict = 'PASS' }
    @{ id = 'A2'; surface = 'A'; verdict = 'PASS' }
    @{ id = 'A3'; surface = 'A'; verdict = 'PASS' }
    @{ id = 'A4'; surface = 'A'; verdict = 'PASS' }
    @{ id = 'A5'; surface = 'A'; verdict = 'PASS' }
    @{ id = 'B1'; surface = 'B'; verdict = 'PASS' }
    @{ id = 'B2'; surface = 'B'; verdict = 'PASS' }
    @{ id = 'B3'; surface = 'B'; verdict = 'PASS' }
    @{ id = 'B4'; surface = 'B'; verdict = 'PASS' }
    @{ id = 'B5'; surface = 'B'; verdict = 'PASS' }
    @{ id = 'C1'; surface = 'C'; verdict = 'PASS' }
    @{ id = 'C2'; surface = 'C'; verdict = 'PASS' }
    @{ id = 'C3'; surface = 'C'; verdict = 'PASS' }
    @{ id = 'C4'; surface = 'C'; verdict = 'PASS' }
    @{ id = 'D1'; surface = 'D'; verdict = 'PASS' }
    @{ id = 'D2'; surface = 'D'; verdict = 'PASS' }
    @{ id = 'D3'; surface = 'D'; verdict = 'PASS' }
)
$obj = [ordered]@{
    TimestampUtc = '2026-08-22T05:35:39Z'
    ValidatorIdentity = 'GrokSubagentHostile'
    Workspace = 'F:\GitHub\McpServer'
    WorkClass = 1
    Phase = 'C-red-P15'
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
    PriorReceipt = 'docs/receipts/hostile-validator-20260822T050748Z.md'
    Collector = 'docs/receipts/_hv-c-red-p15/'
    SessionId = 'GrokSubagentHostile-20260822T052803Z-c-red-p15'
    RequestId = 'req-20260822T052803Z-001-hostile-c-red-p15'
    HealthNonce = 'nonce-hv-20260822052804-85089'
    HealthNonceMatch = $true
    PluginMarkerSignature = $true
    HomemadeMarkerSignature = $false
    ToolSearchExactNamePresent = $true
    OverallVerdict = 'AGREE'
    FailCount = 0
    UnknownCount = 0
    PassCount = 17
    Accuracy = 96
    Completeness = 96
    ExplicitFailList = @()
    MandatorySurfacesUnevaluated = @()
    TestResults = [ordered]@{
        ListTestsCommand = 'dotnet test tests/McpServer.PluginIntegration.Tests -c Debug --list-tests'
        ListTestsExitCode = 0
        ListTestsUniqueLineCount = 88
        ListP14 = 8
        ListP15Success = 8
        ListP15FailedSubmit = 8
        ListP15Retry = 8
        ListAiTheory = 0
        FilterCommand = 'dotnet test tests/McpServer.PluginIntegration.Tests -c Debug --filter "FullyQualifiedName~Theory_Agent_Success_NoPendingFailsafe|FullyQualifiedName~Theory_Agent_FailedSubmit_RetainsRootIdPending|FullyQualifiedName~Theory_Agent_RetrySuccess_DeletesOnlyMatchingPending"'
        FilterExitCode = 1
        FilterConsolePassed = 0
        FilterConsoleFailed = 24
        FilterConsoleSkipped = 0
        FilterConsoleTotal = 24
        FilterConsoleDuration = '4 m 20 s'
        FilterDurationMs = 279170
        FilterTrxExecuted = 24
        FilterTrxPassed = 0
        FilterTrxFailed = 24
        FilterTrxNotExecuted = 0
        FilterTrx = 'docs/receipts/_hv-c-red-p15/trx-p15-hv/p15-filter.trx'
        FilterLog = 'docs/receipts/_hv-c-red-p15/hv-dotnet-p15-filter.log'
        FailsafeVerifiedFalseMessageCount = 8
        ExecuteFailedNotImplementedCount = 8
        RetryNotImplementedCount = 8
        FailsafePathVerifiedImplemented = $false
        ExecuteFailedSubmitImplemented = $false
        RetryFailedSubmitImplemented = $false
        P15NamedTestsPresent = $true
        P15InlineDataCountPerTheory = 8
        P15Passed = 0
        P15Failed = 24
        P15Skipped = 0
        P16NamedTestsPresent = $false
        V4FailsafePathVerifiedInTests = $true
        WorkspaceKeyUtf8Base64Url = 'RjpcR2l0SHViXE1jcFNlcnZlcg'
    }
    TodoFlags = [ordered]@{
        'PLAN-PLUGINHANDOFF-001' = $false
        'MCP-PLUGININT-001' = $false
        'MCP-PLUGININT-001-P15' = $false
        'PLAN-combined-C-P14-P15' = $false
    }
    Claims = $claims
}
($obj | ConvertTo-Json -Depth 8) | Set-Content -LiteralPath $jsonPath -Encoding utf8
Write-Output ('WROTE=' + $jsonPath)
Write-Output ('LEN=' + (Get-Item -LiteralPath $jsonPath).Length)
