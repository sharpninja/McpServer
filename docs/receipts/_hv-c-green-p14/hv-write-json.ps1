#Requires -Version 7.0
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$jsonPath = 'F:\GitHub\McpServer\docs\receipts\hostile-validator-20260822T050748Z.json'
$obj = [ordered]@{
    TimestampUtc = '2026-08-22T05:07:48Z'
    ValidatorIdentity = 'GrokSubagentHostile'
    Workspace = 'F:\GitHub\McpServer'
    WorkClass = 1
    Phase = 'C-green-P14'
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
    PriorReceipt = 'docs/receipts/hostile-validator-20260822T041327Z.md'
    Collector = 'docs/receipts/_hv-c-green-p14/'
    SessionId = 'GrokSubagentHostile-20260822T042653Z-c-green-p14'
    RequestId = 'req-20260822T042653Z-001-hostile-c-green-p14'
    HealthNonce = 'nonce-hv-20260822044014-79279'
    HealthNonceMatch = $true
    PluginMarkerSignature = $true
    ToolSearchExactNamePresent = $true
    OverallVerdict = 'AGREE'
    FailCount = 0
    UnknownCount = 0
    PassCount = 20
    Accuracy = 97
    Completeness = 97
    ExplicitFailList = @()
    MandatorySurfacesUnevaluated = @()
    TestResults = [ordered]@{
        ListTestsCommand = 'dotnet test tests/McpServer.PluginIntegration.Tests -c Debug --list-tests'
        ListTestsExitCode = 0
        ListTestsFqnTotal = 62
        ListP14 = 8
        ListP15Success = 0
        ListP15FailedSubmit = 0
        ListP15Retry = 0
        ListAiTheory = 0
        FilterCommand = 'dotnet test tests/McpServer.PluginIntegration.Tests -c Debug --filter FullyQualifiedName~Theory_Agent_PluginRootOverride_WritesOnlyWorkspaceMcpServerAgent_PoisonEmpty'
        FilterExitCode = 0
        FilterConsolePassed = 8
        FilterConsoleFailed = 0
        FilterConsoleSkipped = 0
        FilterConsoleTotal = 8
        FilterConsoleDuration = '2 m 10 s'
        FilterTrxExecuted = 8
        FilterTrxPassed = 8
        FilterTrxFailed = 0
        FilterTrxNotExecuted = 0
        FilterTrx = 'docs/receipts/_hv-c-green-p14/trx-p14-hv/p14-filter.trx'
        FilterLog = 'docs/receipts/_hv-c-green-p14/hv-dotnet-p14-filter.log'
        FullCommand = 'dotnet test tests/McpServer.PluginIntegration.Tests -c Debug'
        FullExitCode = 0
        FullConsolePassed = 62
        FullConsoleFailed = 0
        FullConsoleSkipped = 0
        FullConsoleTotal = 62
        FullConsoleDuration = '10 m 7 s'
        FullTrxExecuted = 62
        FullTrxPassed = 62
        FullTrxFailed = 0
        FullTrxNotExecuted = 0
        FullTrx = 'docs/receipts/_hv-c-green-p14/trx-all-hv/pluginint-all.trx'
        FullLog = 'docs/receipts/_hv-c-green-p14/hv-dotnet-pluginint-all.log'
        PluginRootOverrideRejectedImplemented = $true
        P14NamedTestsPresent = $true
        P14InlineDataCount = 8
        P14Passed = 8
        P14Failed = 0
        P14Skipped = 0
        P15NamedTestsPresent = $false
        P16NamedTestsPresent = $false
    }
    TodoFlags = [ordered]@{
        'PLAN-PLUGINHANDOFF-001' = $false
        'MCP-PLUGININT-001' = $false
        'MCP-PLUGININT-001-P14' = $false
        'PLAN-combined-C-P14-P15' = $false
    }
    Claims = @(
        [ordered]@{ id = 'A1'; surface = 'A'; verdict = 'PASS' }
        [ordered]@{ id = 'A2'; surface = 'A'; verdict = 'PASS' }
        [ordered]@{ id = 'A3'; surface = 'A'; verdict = 'PASS' }
        [ordered]@{ id = 'A4'; surface = 'A'; verdict = 'PASS' }
        [ordered]@{ id = 'A5'; surface = 'A'; verdict = 'PASS' }
        [ordered]@{ id = 'A6'; surface = 'A'; verdict = 'PASS' }
        [ordered]@{ id = 'B1'; surface = 'B'; verdict = 'PASS' }
        [ordered]@{ id = 'B2'; surface = 'B'; verdict = 'PASS' }
        [ordered]@{ id = 'B3'; surface = 'B'; verdict = 'PASS' }
        [ordered]@{ id = 'B4'; surface = 'B'; verdict = 'PASS' }
        [ordered]@{ id = 'B5'; surface = 'B'; verdict = 'PASS' }
        [ordered]@{ id = 'C1'; surface = 'C'; verdict = 'PASS' }
        [ordered]@{ id = 'C2'; surface = 'C'; verdict = 'PASS' }
        [ordered]@{ id = 'C3'; surface = 'C'; verdict = 'PASS' }
        [ordered]@{ id = 'C4'; surface = 'C'; verdict = 'PASS' }
        [ordered]@{ id = 'C5'; surface = 'C'; verdict = 'PASS' }
        [ordered]@{ id = 'D1'; surface = 'D'; verdict = 'PASS' }
        [ordered]@{ id = 'D2'; surface = 'D'; verdict = 'PASS' }
        [ordered]@{ id = 'D3'; surface = 'D'; verdict = 'PASS' }
        [ordered]@{ id = 'D4'; surface = 'D'; verdict = 'PASS' }
    )
}
($obj | ConvertTo-Json -Depth 8) | Set-Content -LiteralPath $jsonPath -Encoding utf8
$item = Get-Item -LiteralPath $jsonPath
Write-Output ('JSON_LEN=' + $item.Length)
Write-Output ('JSON_UTC=' + $item.LastWriteTimeUtc.ToString('o'))
$md = Get-Content -LiteralPath 'F:\GitHub\McpServer\docs\receipts\hostile-validator-20260822T050748Z.md' -Raw
Write-Output ('MD_AGREE=' + [bool]($md -match '(?m)^OverallVerdict:\s*AGREE\s*$'))
Write-Output ('MD_EMDASH=' + $md.Contains([char]0x2014))
Write-Output ('MD_ENDASH=' + $md.Contains([char]0x2013))
$j = Get-Content -LiteralPath $jsonPath -Raw | ConvertFrom-Json
Write-Output ('JSON_VERDICT=' + $j.OverallVerdict)
Write-Output ('JSON_FAIL=' + $j.FailCount)
Write-Output ('JSON_PASS=' + $j.PassCount)
Write-Output ('JSON_CLAIMS=' + @($j.Claims).Count)
