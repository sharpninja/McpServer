#Requires -Version 7.0
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
$out = 'F:\GitHub\McpServer\docs\receipts\_hv-c-green-p15'
$md = 'F:\GitHub\McpServer\docs\receipts\hostile-validator-20260822T071202Z.md'
$jsonPath = 'F:\GitHub\McpServer\docs\receipts\hostile-validator-20260822T071202Z.json'
$mdText = Get-Content -LiteralPath $md -Raw
$mdItem = Get-Item -LiteralPath $md
$obj = [ordered]@{
    TimestampUtc = '2026-08-22T07:12:02Z'
    ValidatorIdentity = 'GrokSubagentHostile'
    Workspace = 'F:\GitHub\McpServer'
    WorkClass = 1
    Phase = 'C-green-P15'
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
    PriorReceipt = 'docs/receipts/hostile-validator-20260822T053539Z.md'
    Collector = 'docs/receipts/_hv-c-green-p15/'
    SessionId = 'GrokSubagentHostile-20260822T062054Z-c-green-p15'
    RequestId = 'req-20260822T062054Z-001-hostile-c-green-p15'
    HealthNonce = 'nonce-hv-20260822062055-15239'
    HealthNonceMatch = $true
    PluginMarkerSignature = $true
    HomemadeMarkerSignature = $false
    ToolSearchExactNamePresent = $true
    OverallVerdict = 'AGREE'
    FailCount = 0
    UnknownCount = 0
    PassCount = 18
    Accuracy = 95
    Completeness = 97
    ExplicitFailList = @()
    MandatorySurfacesUnevaluated = @()
    TestResults = [ordered]@{
        ListTestsCommand = 'dotnet test tests/McpServer.PluginIntegration.Tests -c Debug --list-tests'
        ListTestsExitCode = 0
        ListP15Success = 8
        ListP15FailedSubmit = 8
        ListP15Retry = 8
        ListAiTheory = 0
        FilterCommand = 'dotnet test tests/McpServer.PluginIntegration.Tests -c Debug --filter "FullyQualifiedName~Theory_Agent_Success_NoPendingFailsafe|FullyQualifiedName~Theory_Agent_FailedSubmit_RetainsRootIdPending|FullyQualifiedName~Theory_Agent_RetrySuccess_DeletesOnlyMatchingPending"'
        FilterExitCode = 0
        FilterConsolePassed = 24
        FilterConsoleFailed = 0
        FilterConsoleSkipped = 0
        FilterConsoleTotal = 24
        FilterConsoleDuration = '5 m 43 s'
        FilterDurationMs = 367101
        FilterTrxExecuted = 24
        FilterTrxPassed = 24
        FilterTrxFailed = 0
        FilterTrxNotExecuted = 0
        FilterTrx = 'docs/receipts/_hv-c-green-p15/trx-p15-hv/p15-filter.trx'
        FilterLog = 'docs/receipts/_hv-c-green-p15/hv-dotnet-p15-filter.log'
        FullCommand = 'dotnet test tests/McpServer.PluginIntegration.Tests -c Debug'
        FullExitCode = 0
        FullConsolePassed = 86
        FullConsoleFailed = 0
        FullConsoleSkipped = 0
        FullConsoleTotal = 86
        FullConsoleDuration = '16 m 24 s'
        FullDurationMs = 1006540
        FullTrxExecuted = 86
        FullTrxPassed = 86
        FullTrxFailed = 0
        FullTrxNotExecuted = 0
        FullTrx = 'docs/receipts/_hv-c-green-p15/full-rerun/trx-all-hv/pluginint-all.trx'
        FullLog = 'docs/receipts/_hv-c-green-p15/full-rerun/hv-dotnet-pluginint-all.log'
        FullWmiPid = 47568
        FirstFullSuiteKilledBySibling = $true
        SiblingKillUtc = '2026-08-22T06:32:46Z'
        FailsafePathVerifiedImplemented = $true
        ExecuteFailedSubmitImplemented = $true
        RetryFailedSubmitImplemented = $true
        RetryAssertsMatchingGoneAndSiblingRetained = $true
        P15NamedTestsPresent = $true
        P15InlineDataCountPerTheory = 8
        P15Passed = 24
        P15Failed = 0
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
    Claims = @(
        [ordered]@{ Id = 'A1'; Surface = 'A'; Verdict = 'PASS'; Text = 'Adapter failsafe V4 pending verify/write/retry sibling delete' }
        [ordered]@{ Id = 'A2'; Surface = 'A'; Verdict = 'PASS'; Text = 'Named P15 theories 24 rows pass; retry matching gone sibling retained' }
        [ordered]@{ Id = 'A3'; Surface = 'A'; Verdict = 'PASS'; Text = 'Independent filter 24/0/0 and full suite 86/0/0' }
        [ordered]@{ Id = 'A4'; Surface = 'A'; Verdict = 'PASS'; Text = 'P16 AiTheory_ absent' }
        [ordered]@{ Id = 'A5'; Surface = 'A'; Verdict = 'PASS'; Text = 'PLAN and MCP-PLUGININT remain done false' }
        [ordered]@{ Id = 'A6'; Surface = 'A'; Verdict = 'PASS'; Text = 'Prior C-red-P15 AGREE 20260822T053539Z Failed 24' }
        [ordered]@{ Id = 'B1'; Surface = 'B'; Verdict = 'PASS'; Text = 'Honesty and receipts' }
        [ordered]@{ Id = 'B2'; Surface = 'B'; Verdict = 'PASS'; Text = 'Byrd phase-order C-red then green' }
        [ordered]@{ Id = 'B3'; Surface = 'B'; Verdict = 'PASS'; Text = 'MCP-only storage' }
        [ordered]@{ Id = 'B4'; Surface = 'B'; Verdict = 'PASS'; Text = 'PowerShell-only no Python' }
        [ordered]@{ Id = 'B5'; Surface = 'B'; Verdict = 'PASS'; Text = 'No fabricated results' }
        [ordered]@{ Id = 'C1'; Surface = 'C'; Verdict = 'PASS'; Text = 'FR/TR/TEST identified' }
        [ordered]@{ Id = 'C2'; Surface = 'C'; Verdict = 'PASS'; Text = 'Structured AC exist' }
        [ordered]@{ Id = 'C3'; Surface = 'C'; Verdict = 'PASS'; Text = 'Tests cover P15 AC green' }
        [ordered]@{ Id = 'C4'; Surface = 'C'; Verdict = 'PASS'; Text = 'Not claiming requirements complete; mappings exist' }
        [ordered]@{ Id = 'D1'; Surface = 'D'; Verdict = 'PASS'; Text = 'C-green-P15 only not plan closeout' }
        [ordered]@{ Id = 'D2'; Surface = 'D'; Verdict = 'PASS'; Text = 'Plan exit named greens plus full suite Failed 0 Skipped 0' }
        [ordered]@{ Id = 'D3'; Surface = 'D'; Verdict = 'PASS'; Text = 'PLAN and PLUGININT remain open' }
    )
    ReceiptMd = [ordered]@{
        Path = $md
        Length = $mdItem.Length
        LastWriteTimeUtc = $mdItem.LastWriteTimeUtc.ToString('o')
        OverallVerdictLineMatchAgree = [bool]($mdText -match '(?m)^OverallVerdict:\s*AGREE\s*$')
        EmDash = $mdText.Contains([char]0x2014)
        EnDash = $mdText.Contains([char]0x2013)
    }
}
$obj | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath $jsonPath -Encoding utf8
Copy-Item -LiteralPath $jsonPath -Destination (Join-Path $out 'receipt-on-disk.json') -Force
Write-Output ('JSON_LEN=' + (Get-Item -LiteralPath $jsonPath).Length)
Write-Output ('MD_AGREE=' + $obj.ReceiptMd.OverallVerdictLineMatchAgree)
Write-Output ('MD_EMDASH=' + $obj.ReceiptMd.EmDash)
Write-Output ('MD_ENDASH=' + $obj.ReceiptMd.EnDash)
