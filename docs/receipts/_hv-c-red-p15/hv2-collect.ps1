#Requires -Version 7.0
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Continue'

$out = 'F:\GitHub\McpServer\docs\receipts\_hv-c-red-p15\hv2'
New-Item -ItemType Directory -Force -Path $out | Out-Null
Set-Location -LiteralPath 'F:\GitHub\McpServer'

$adapter = 'F:\GitHub\McpServer\tests\McpServer.PluginIntegration.Tests\PluginSessionLogWorkflowAdapter.cs'
$tests = 'F:\GitHub\McpServer\tests\McpServer.PluginIntegration.Tests\PluginSessionLogWorkflowAdapterTests.cs'
$resultType = 'F:\GitHub\McpServer\tests\McpServer.PluginIntegration.Tests\PluginSessionLogWorkflowResult.cs'
$plan = 'F:\GitHub\McpServer\docs\plans\PLAN-PLUGINHANDOFF-001.md'
$failsafePs1 = 'F:\GitHub\McpServer\plugins\core\lib-ps\resolve-cache-dir.ps1'
$fixture = 'F:\GitHub\McpServer\tests\McpServer.PluginIntegration.Tests\PluginIntegrationServerFixture.cs'
$prior1 = 'F:\GitHub\McpServer\docs\receipts\hostile-validator-20260822T044818Z.md'
$prior1j = 'F:\GitHub\McpServer\docs\receipts\hostile-validator-20260822T044818Z.json'
$prior2 = 'F:\GitHub\McpServer\docs\receipts\hostile-validator-20260822T050748Z.md'
$prior2j = 'F:\GitHub\McpServer\docs\receipts\hostile-validator-20260822T050748Z.json'

$paths = @(
    $prior1, $prior1j, $prior2, $prior2j,
    $adapter, $tests, $resultType, $plan, $failsafePs1, $fixture,
    'F:\GitHub\McpServer\tests\McpServer.PluginIntegration.Tests\PluginHostProcessAdapter.cs'
)
$meta = foreach ($p in $paths) {
    $exists = Test-Path -LiteralPath $p
    if ($exists) {
        $i = Get-Item -LiteralPath $p
        [pscustomobject]@{
            Path = $p
            Exists = $true
            LastWriteTimeUtc = $i.LastWriteTimeUtc.ToString('o')
            Length = $i.Length
        }
    } else {
        [pscustomobject]@{
            Path = $p
            Exists = $false
            LastWriteTimeUtc = $null
            Length = 0
        }
    }
}
$meta | ConvertTo-Json -Depth 5 | Set-Content -LiteralPath (Join-Path $out 'timestamps.json') -Encoding utf8

function Get-ReceiptExtract {
    param([string]$MdPath, [string]$JsonPath, [string]$Dest)
    $obj = [ordered]@{ Exists = (Test-Path -LiteralPath $MdPath); Path = $MdPath }
    if (Test-Path -LiteralPath $MdPath) {
        $agreeText = Get-Content -LiteralPath $MdPath -Raw
        $item = Get-Item -LiteralPath $MdPath
        $obj.Length = $item.Length
        $obj.LastWriteTimeUtc = $item.LastWriteTimeUtc.ToString('o')
        $obj.OverallVerdictLineMatchAgree = [bool]($agreeText -match '(?m)^OverallVerdict:\s*AGREE\s*$')
        $obj.OverallVerdictLineMatchDisagree = [bool]($agreeText -match '(?m)^OverallVerdict:\s*DISAGREE\s*$')
        $obj.MentionsCGreenP14 = ($agreeText -match 'C-green-P14')
        $obj.MentionsCRedP14 = ($agreeText -match 'C-red-P14')
        $obj.MentionsPassed8 = ($agreeText -match 'Passed:\s*8' -or $agreeText -match 'Passed 8')
        $obj.MentionsFailed0 = ($agreeText -match 'Failed:\s*0' -or $agreeText -match 'Failed 0')
        $obj.MentionsSkipped0 = ($agreeText -match 'Skipped:\s*0' -or $agreeText -match 'Skipped 0')
        $obj.MentionsP15Name = ($agreeText -match 'Theory_Agent_Success_NoPendingFailsafe|Theory_Agent_FailedSubmit_RetainsRootIdPending|Theory_Agent_RetrySuccess_DeletesOnlyMatchingPending')
        $obj.MentionsP15Absent = ($agreeText -match '(?i)P15' -and $agreeText -match '(?i)absent')
        $obj.P15WordCount = ([regex]::Matches($agreeText, 'P15')).Count
        $obj.EmDash = $agreeText.Contains([char]0x2014)
        $obj.EnDash = $agreeText.Contains([char]0x2013)
        $obj.ValidatorIdentity = if ($agreeText -match '(?m)^ValidatorIdentity:\s*(.+)$') { $Matches[1].Trim() } else { $null }
        $obj.WorkClass = if ($agreeText -match '(?m)^WorkClass:\s*(.+)$') { $Matches[1].Trim() } else { $null }
    }
    if (Test-Path -LiteralPath $JsonPath) {
        $j = Get-Content -LiteralPath $JsonPath -Raw | ConvertFrom-Json
        $obj.JsonExists = $true
        $obj.JsonOverallVerdict = [string]$j.OverallVerdict
        $obj.JsonFailCount = $j.FailCount
        $obj.JsonPhase = [string]$j.Phase
        if ($null -ne $j.TestResults) {
            $obj.JsonFilterPassed = $j.TestResults.FilterPassed
            $obj.JsonFilterFailed = $j.TestResults.FilterFailed
            $obj.JsonFilterSkipped = $j.TestResults.FilterSkipped
            $obj.JsonP15NamedTestsPresent = $j.TestResults.P15NamedTestsPresent
            $obj.JsonP16NamedTestsPresent = $j.TestResults.P16NamedTestsPresent
            $obj.JsonFullConsolePassed = $j.TestResults.FullConsolePassed
            $obj.JsonFullConsoleFailed = $j.TestResults.FullConsoleFailed
            $obj.JsonFullConsoleSkipped = $j.TestResults.FullConsoleSkipped
        }
    } else {
        $obj.JsonExists = $false
    }
    $obj | ConvertTo-Json -Depth 6 | Set-Content -LiteralPath $Dest -Encoding utf8
}

Get-ReceiptExtract -MdPath $prior1 -JsonPath $prior1j -Dest (Join-Path $out 'prior-044818Z.json')
Get-ReceiptExtract -MdPath $prior2 -JsonPath $prior2j -Dest (Join-Path $out 'prior-050748Z.json')

$adapterText = Get-Content -LiteralPath $adapter -Raw
$testsText = Get-Content -LiteralPath $tests -Raw
$resultText = Get-Content -LiteralPath $resultType -Raw
$failsafeText = Get-Content -LiteralPath $failsafePs1 -Raw
$fixtureText = Get-Content -LiteralPath $fixture -Raw

function Get-InlineDataHosts([string]$methodName, [string]$text) {
    $before = [regex]::Match($text, "(?s)((?:\[InlineData\(PluginHostKind\.[A-Za-z0-9]+\)\]\s*){1,16})public async Task $methodName")
    $hosts = @()
    if ($before.Success) {
        $hosts = @([regex]::Matches($before.Groups[1].Value, 'PluginHostKind\.([A-Za-z0-9]+)') | ForEach-Object { $_.Groups[1].Value })
    }
    return $hosts
}

function Get-MethodSkip([string]$methodName, [string]$text) {
    $m = [regex]::Match($text, "(?s)((?:\[[^\]]+\]\s*)*)public async Task $methodName")
    if (-not $m.Success) { return $null }
    return $m.Groups[1].Value
}

$expectedHosts = @('Codex', 'ClaudeCode', 'ClaudeCowork', 'Copilot', 'Grok', 'Cline', 'ClineV2', 'OpenCode')
$successHosts = @(Get-InlineDataHosts 'Theory_Agent_Success_NoPendingFailsafe' $testsText)
$failedHosts = @(Get-InlineDataHosts 'Theory_Agent_FailedSubmit_RetainsRootIdPending' $testsText)
$retryHosts = @(Get-InlineDataHosts 'Theory_Agent_RetrySuccess_DeletesOnlyMatchingPending' $testsText)
$successAttrs = Get-MethodSkip 'Theory_Agent_Success_NoPendingFailsafe' $testsText
$failedAttrs = Get-MethodSkip 'Theory_Agent_FailedSubmit_RetainsRootIdPending' $testsText
$retryAttrs = Get-MethodSkip 'Theory_Agent_RetrySuccess_DeletesOnlyMatchingPending' $testsText

$successBlock = [regex]::Match($testsText, '(?s)public async Task Theory_Agent_Success_NoPendingFailsafe\(PluginHostKind hostKind\)\s*\{.*?^\s{4}\}')
$failedBlock = [regex]::Match($testsText, '(?s)public async Task Theory_Agent_FailedSubmit_RetainsRootIdPending\(PluginHostKind hostKind\)\s*\{.*?^\s{4}\}')
$retryBlock = [regex]::Match($testsText, '(?s)public async Task Theory_Agent_RetrySuccess_DeletesOnlyMatchingPending\(PluginHostKind hostKind\)\s*\{.*?^\s{4}\}')

$execFailedFn = [regex]::Match($adapterText, '(?s)ExecuteFailedSubmitAsync\s*\([^)]*\)\s*\{.*?^\s{4}\}')
$retryFn = [regex]::Match($adapterText, '(?s)RetryFailedSubmitAsync\s*\([^)]*\)\s*\{.*?^\s{4}\}')
$canonicalFn = [regex]::Match($adapterText, '(?s)ExecuteCanonicalTurnAsync\s*\([^)]*\)\s*\{.*?^\s{4}\}')

[ordered]@{
    TestsNamedSuccess = ($testsText -match 'Theory_Agent_Success_NoPendingFailsafe')
    TestsNamedFailedSubmit = ($testsText -match 'Theory_Agent_FailedSubmit_RetainsRootIdPending')
    TestsNamedRetry = ($testsText -match 'Theory_Agent_RetrySuccess_DeletesOnlyMatchingPending')
    TestsSkipAttributeAnywhere = ($testsText -match 'Skip\s*=')
    TestsFactSkip = ($testsText -match 'Fact\(Skip')
    TestsTheorySkip = ($testsText -match 'Theory\(Skip')
    TestsAssertSkip = ($testsText -match 'Assert\.Skip')
    TestsAiTheory = ($testsText -match 'AiTheory_')
    SuccessInlineDataHosts = $successHosts
    FailedInlineDataHosts = $failedHosts
    RetryInlineDataHosts = $retryHosts
    SuccessHostCount = $successHosts.Count
    FailedHostCount = $failedHosts.Count
    RetryHostCount = $retryHosts.Count
    SuccessHostsMatchExpected = ((@($successHosts | Sort-Object) -join ',') -eq ((@($expectedHosts | Sort-Object) -join ',')))
    FailedHostsMatchExpected = ((@($failedHosts | Sort-Object) -join ',') -eq ((@($expectedHosts | Sort-Object) -join ',')))
    RetryHostsMatchExpected = ((@($retryHosts | Sort-Object) -join ',') -eq ((@($expectedHosts | Sort-Object) -join ',')))
    SuccessAttrHasSkip = ($successAttrs -match 'Skip')
    FailedAttrHasSkip = ($failedAttrs -match 'Skip')
    RetryAttrHasSkip = ($retryAttrs -match 'Skip')
    SuccessAssertsFailsafePathVerifiedTrue = ($successBlock.Value -match 'FailsafePathVerified' -and $successBlock.Value -match 'Assert\.True\(\s*result\.FailsafePathVerified')
    FailedSubmitCallsExecuteFailedSubmitAsync = ($failedBlock.Value -match 'ExecuteFailedSubmitAsync')
    RetryCallsRetryFailedSubmitAsync = ($retryBlock.Value -match 'RetryFailedSubmitAsync')
    ResolveUsesV4Layout = ($testsText -match '\.mcpServer' -and $testsText -match '"failsafe"' -and $testsText -match '"workspaces"' -and $testsText -match '"pending"')
    ResolveBase64Url = ($testsText -match "Replace\('\+', '-'\)" -and $testsText -match "Replace\('/', '_'\)" -and $testsText -match "TrimEnd\('='\)")
    AdapterFailsafePathVerifiedAssign = ($adapterText -match 'FailsafePathVerified\s*=')
    AdapterExecuteFailedThrowsNotImplemented = ($adapterText -match 'ExecuteFailedSubmitAsync is not implemented' -or ($execFailedFn.Success -and $execFailedFn.Value -match 'InvalidOperationException' -and $execFailedFn.Value -match 'not implemented'))
    AdapterRetryThrowsNotImplemented = ($adapterText -match 'RetryFailedSubmitAsync is not implemented' -or ($retryFn.Success -and $retryFn.Value -match 'InvalidOperationException' -and $retryFn.Value -match 'not implemented'))
    AdapterExecuteFailedSnippet = if ($execFailedFn.Success) { $execFailedFn.Value.Substring(0, [Math]::Min(800, $execFailedFn.Value.Length)) } else { $null }
    AdapterRetrySnippet = if ($retryFn.Success) { $retryFn.Value.Substring(0, [Math]::Min(800, $retryFn.Value.Length)) } else { $null }
    CanonicalSetsFailsafePathVerified = ($canonicalFn.Value -match 'FailsafePathVerified\s*=')
    ResultFailsafePropertyExists = ($resultText -match 'public bool FailsafePathVerified')
    ResultCommentSaysRedUntilImplemented = ($resultText -match 'C-red-P15 stays false' -or $resultText -match 'defaults to false')
    FixtureReserved7147 = ($fixtureText -match 'ReservedServicePort\s*=\s*7147')
    FixtureAllocateExcludes7147 = ($fixtureText -match 'port != ReservedServicePort')
    FixtureConnectionString7147Db = ($fixtureText -match '7147')
    TestsAssertPortNot7147 = ($testsText -match '7147')
    CoreV4PathComment = ($failsafeText -match '\{workspace\}/\.mcpServer/failsafe/\{agent\}/workspaces')
} | ConvertTo-Json -Depth 6 | Set-Content -LiteralPath (Join-Path $out 'adapter-scan.json') -Encoding utf8

$p15Hits = @(Select-String -Path 'F:\GitHub\McpServer\tests\McpServer.PluginIntegration.Tests\*.cs' -Pattern 'Success_NoPendingFailsafe|FailedSubmit_RetainsRootIdPending|RetrySuccess_DeletesOnlyMatchingPending')
$p16Hits = @(Select-String -Path 'F:\GitHub\McpServer\tests\McpServer.PluginIntegration.Tests\*.cs' -Pattern 'AiTheory_|P16')
$skipHits = @(Select-String -Path 'F:\GitHub\McpServer\tests\McpServer.PluginIntegration.Tests\*.cs' -Pattern 'Skip\s*=|Fact\(Skip|Theory\(Skip|Assert\.Skip')
[ordered]@{
    P15HitCount = $p15Hits.Count
    P15Hits = @($p15Hits | ForEach-Object { $_.Path + ':' + $_.LineNumber + ':' + $_.Line.Trim() })
    P16HitCount = $p16Hits.Count
    P16Hits = @($p16Hits | ForEach-Object { $_.Path + ':' + $_.LineNumber + ':' + $_.Line.Trim() })
    SkipHitCount = $skipHits.Count
    SkipHits = @($skipHits | ForEach-Object { $_.Path + ':' + $_.LineNumber + ':' + $_.Line.Trim() })
} | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $out 'p15-p16-skip-grep.json') -Encoding utf8

$py = @(Get-CimInstance Win32_Process | Where-Object { $_.Name -match 'python' } | ForEach-Object {
    $cmd = [string]$_.CommandLine
    if ($cmd.Length -gt 200) { $cmd = $cmd.Substring(0, 200) }
    [ordered]@{ ProcessId = $_.ProcessId; Name = $_.Name; CommandLine = $cmd }
})
[ordered]@{
    PythonProcessCount = $py.Count
    Processes = $py
} | ConvertTo-Json -Depth 5 | Set-Content -LiteralPath (Join-Path $out 'no-python-hits.json') -Encoding utf8

$todoYamlStatus = git -C 'F:\GitHub\McpServer' status --porcelain -- docs/Project/TODO.yaml docs/todo.yaml
$pluginintStatus = git -C 'F:\GitHub\McpServer' status --porcelain -- tests/McpServer.PluginIntegration.Tests
[ordered]@{
    TodoYamlPorcelain = [string]$todoYamlStatus
    PluginIntPorcelain = [string]$pluginintStatus
} | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $out 'todo-yaml-git-status.json') -Encoding utf8
Set-Content -LiteralPath (Join-Path $out 'git-status-pluginint.txt') -Value ([string]$pluginintStatus) -Encoding utf8

Write-Output 'COLLECT_DONE'
