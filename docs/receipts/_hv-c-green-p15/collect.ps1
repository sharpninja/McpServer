#Requires -Version 7.0
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Continue'

$out = 'F:\GitHub\McpServer\docs\receipts\_hv-c-green-p15'
New-Item -ItemType Directory -Force -Path $out | Out-Null
Set-Location -LiteralPath 'F:\GitHub\McpServer'

$priorAgree = 'F:\GitHub\McpServer\docs\receipts\hostile-validator-20260822T053539Z.md'
$priorAgreeJson = 'F:\GitHub\McpServer\docs\receipts\hostile-validator-20260822T053539Z.json'
$adapter = 'F:\GitHub\McpServer\tests\McpServer.PluginIntegration.Tests\PluginSessionLogWorkflowAdapter.cs'
$tests = 'F:\GitHub\McpServer\tests\McpServer.PluginIntegration.Tests\PluginSessionLogWorkflowAdapterTests.cs'
$resultType = 'F:\GitHub\McpServer\tests\McpServer.PluginIntegration.Tests\PluginSessionLogWorkflowResult.cs'
$plan = 'F:\GitHub\McpServer\docs\plans\PLAN-PLUGINHANDOFF-001.md'
$failsafePs1 = 'F:\GitHub\McpServer\plugins\core\lib-ps\resolve-cache-dir.ps1'

$paths = @(
    $priorAgree
    $priorAgreeJson
    $adapter
    $tests
    $resultType
    $plan
    $failsafePs1
    'F:\GitHub\McpServer\tests\McpServer.PluginIntegration.Tests\PluginHostProcessAdapter.cs'
    'F:\GitHub\McpServer\tests\McpServer.PluginIntegration.Tests\PluginIntegrationServerFixture.cs'
    'F:\GitHub\McpServer\docs\receipts\hostile-validator-20260822T050748Z.md'
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
    $obj = [ordered]@{ Exists = (Test-Path -LiteralPath $MdPath) }
    if (Test-Path -LiteralPath $MdPath) {
        $agreeText = Get-Content -LiteralPath $MdPath -Raw
        $item = Get-Item -LiteralPath $MdPath
        $obj.Length = $item.Length
        $obj.LastWriteTimeUtc = $item.LastWriteTimeUtc.ToString('o')
        $obj.OverallVerdictLineMatchAgree = [bool]($agreeText -match '(?m)^OverallVerdict:\s*AGREE\s*$')
        $obj.OverallVerdictLineMatchDisagree = [bool]($agreeText -match '(?m)^OverallVerdict:\s*DISAGREE\s*$')
        $obj.MentionsCRedP15 = ($agreeText -match 'C-red-P15')
        $obj.MentionsFailed24 = ($agreeText -match 'Failed:\s*24' -or $agreeText -match 'Failed 24')
        $obj.MentionsPassed0 = ($agreeText -match 'Passed:\s*0' -or $agreeText -match 'Passed 0')
        $obj.MentionsSkipped0 = ($agreeText -match 'Skipped:\s*0' -or $agreeText -match 'Skipped 0')
        $obj.EmDash = $agreeText.Contains([char]0x2014)
        $obj.EnDash = $agreeText.Contains([char]0x2013)
    }
    if (Test-Path -LiteralPath $JsonPath) {
        $j = Get-Content -LiteralPath $JsonPath -Raw | ConvertFrom-Json
        $obj.JsonOverallVerdict = [string]$j.OverallVerdict
        $obj.JsonFailCount = $j.FailCount
        $obj.JsonPhase = [string]$j.Phase
        $obj.JsonFilterPassed = $j.TestResults.FilterConsolePassed
        $obj.JsonFilterFailed = $j.TestResults.FilterConsoleFailed
        $obj.JsonFilterSkipped = $j.TestResults.FilterConsoleSkipped
        $obj.JsonP15Present = $j.TestResults.P15NamedTestsPresent
        $obj.JsonP16Present = $j.TestResults.P16NamedTestsPresent
    }
    $obj | ConvertTo-Json -Depth 6 | Set-Content -LiteralPath $Dest -Encoding utf8
}

Get-ReceiptExtract -MdPath $priorAgree -JsonPath $priorAgreeJson -Dest (Join-Path $out 'prior-c-red-p15-agree.json')

$adapterText = Get-Content -LiteralPath $adapter -Raw
$testsText = Get-Content -LiteralPath $tests -Raw
$resultText = Get-Content -LiteralPath $resultType -Raw
$failsafeText = Get-Content -LiteralPath $failsafePs1 -Raw

$successBlock = [regex]::Match($testsText, 'public async Task Theory_Agent_Success_NoPendingFailsafe[\s\S]*?public async Task Theory_Agent_FailedSubmit')
$failedBlock = [regex]::Match($testsText, 'public async Task Theory_Agent_FailedSubmit_RetainsRootIdPending[\s\S]*?public async Task Theory_Agent_RetrySuccess')
$retryBlock = [regex]::Match($testsText, 'public async Task Theory_Agent_RetrySuccess_DeletesOnlyMatchingPending[\s\S]*?private static string ResolveFailsafePendingDirectory')
$canonicalFn = [regex]::Match($adapterText, '(?s)public async Task<PluginSessionLogWorkflowResult> ExecuteCanonicalTurnAsync\s*\(.*?\n    \{.*?\n    \}')
$execFailedFn = [regex]::Match($adapterText, '(?s)public async Task<PluginSessionLogWorkflowResult> ExecuteFailedSubmitAsync\s*\(.*?\n    \{.*?\n    \}')
$retryFn = [regex]::Match($adapterText, '(?s)public async Task<PluginSessionLogWorkflowResult> RetryFailedSubmitAsync\s*\(.*?\n    \{.*?\n    \}')
$getPendingFn = [regex]::Match($adapterText, '(?s)public static string GetFailsafePendingDirectory\s*\(.*?\n    \{.*?\n    \}')

function Get-InlineDataHosts([string]$methodName, [string]$text) {
    $before = [regex]::Match($text, "(?s)((?:\[InlineData\(PluginHostKind\.[A-Za-z0-9]+\)\]\s*){1,16})public async Task $methodName")
    $hosts = @()
    if ($before.Success) {
        $hosts = @([regex]::Matches($before.Groups[1].Value, 'PluginHostKind\.([A-Za-z0-9]+)') | ForEach-Object { $_.Groups[1].Value })
    }
    return $hosts
}

$expectedHosts = @('Codex', 'ClaudeCode', 'ClaudeCowork', 'Copilot', 'Grok', 'Cline', 'ClineV2', 'OpenCode')
$successHosts = @(Get-InlineDataHosts 'Theory_Agent_Success_NoPendingFailsafe' $testsText)
$failedHosts = @(Get-InlineDataHosts 'Theory_Agent_FailedSubmit_RetainsRootIdPending' $testsText)
$retryHosts = @(Get-InlineDataHosts 'Theory_Agent_RetrySuccess_DeletesOnlyMatchingPending' $testsText)

[ordered]@{
    TestsNamedSuccess = ($testsText -match 'Theory_Agent_Success_NoPendingFailsafe')
    TestsNamedFailedSubmit = ($testsText -match 'Theory_Agent_FailedSubmit_RetainsRootIdPending')
    TestsNamedRetry = ($testsText -match 'Theory_Agent_RetrySuccess_DeletesOnlyMatchingPending')
    TestsSkipAttribute = ($testsText -match 'Skip\s*=')
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
    SuccessHostsMatchExpected = (@(Compare-Object $expectedHosts $successHosts -SyncWindow 0).Count -eq 0)
    FailedHostsMatchExpected = (@(Compare-Object $expectedHosts $failedHosts -SyncWindow 0).Count -eq 0)
    RetryHostsMatchExpected = (@(Compare-Object $expectedHosts $retryHosts -SyncWindow 0).Count -eq 0)
    SuccessAssertsFailsafePathVerified = ($successBlock.Value -match 'Assert\.True\(\s*result\.FailsafePathVerified')
    SuccessAssertsEmptyPendingIfExists = ($successBlock.Value -match 'Assert\.Empty\(Directory\.GetFiles\(pending')
    FailedSubmitCallsExecuteFailedSubmitAsync = ($failedBlock.Value -match 'ExecuteFailedSubmitAsync')
    FailedSubmitAssertsFailsafePathVerified = ($failedBlock.Value -match 'Assert\.True\(\s*result\.FailsafePathVerified')
    FailedSubmitAssertsNotEmpty = ($failedBlock.Value -match 'Assert\.NotEmpty\(Directory\.GetFiles\(pending')
    FailedSubmitAssertsRootIdFilename = ($failedBlock.Value -match 'sessionlog-' -or $failedBlock.Value -match 'result\.SessionId')
    RetryCallsRetryFailedSubmitAsync = ($retryBlock.Value -match 'RetryFailedSubmitAsync')
    RetryAssertsMatchingGone = ($retryBlock.Value -match 'DoesNotContain' -and $retryBlock.Value -match 'SessionId')
    RetryAssertsSiblingRetained = ($retryBlock.Value -match 'Contains\("sibling"')
    CanonicalSetsFailsafeTrue = ($canonicalFn.Value -match 'FailsafePathVerified\s*=\s*true')
    CanonicalThrowsOnMatchingLeftover = ($canonicalFn.Value -match 'sessionlog-" \+ SanitizePathSegment\(sessionId\)' -and $canonicalFn.Value -match 'leftovers\.Length > 0')
    FailedSubmitWritesSessionlogSessionIdPendingYaml = ($execFailedFn.Value -match 'sessionlog-" \+ SanitizePathSegment\(sessionId\)' -and $execFailedFn.Value -match '-pending\.yaml')
    FailedSubmitWritesTypeRequest = ($execFailedFn.Value -match 'type: request')
    FailedSubmitCallsSessionLog = ($execFailedFn.Value -match 'SessionLog\.')
    RetryWritesSiblingUnrelated = ($retryFn.Value -match 'sessionlog-sibling-unrelated\.yaml')
    RetryDeletesMatchingGlob = ($retryFn.Value -match 'sessionlog-" \+ SanitizePathSegment\(failed\.SessionId\) \+ "-\*"')
    RetryCallsSessionLog = ($retryFn.Value -match 'SessionLog\.')
    GetPendingUsesV4Tree = ($getPendingFn.Value -match '\.mcpServer' -and $getPendingFn.Value -match 'failsafe' -and $getPendingFn.Value -match 'workspaces' -and $getPendingFn.Value -match 'pending')
    GetPendingBase64UrlUtf8 = ($getPendingFn.Value -match "Replace\('\+', '-'\)" -and $getPendingFn.Value -match "Replace\('/', '_'\)" -and $getPendingFn.Value -match "TrimEnd\('='\)" -and $getPendingFn.Value -match 'Encoding\.UTF8\.GetBytes')
    ResultFailsafePropertyExists = ($resultText -match 'public bool FailsafePathVerified')
    CoreV4PathComment = ($failsafeText -match '\{workspace\}/\.mcpServer/failsafe/\{agent\}/workspaces/\{workspace-key\}/pending/')
} | ConvertTo-Json -Depth 6 | Set-Content -LiteralPath (Join-Path $out 'adapter-scan.json') -Encoding utf8

$p15Hits = @(Select-String -Path 'F:\GitHub\McpServer\tests\McpServer.PluginIntegration.Tests\*.cs' -Pattern 'Success_NoPendingFailsafe|FailedSubmit_RetainsRootIdPending|RetrySuccess_DeletesOnlyMatchingPending' -ErrorAction SilentlyContinue)
$p16Hits = @(Select-String -Path 'F:\GitHub\McpServer\tests\McpServer.PluginIntegration.Tests\*.cs' -Pattern 'AiTheory_' -ErrorAction SilentlyContinue)
$skipHits = @(Select-String -Path 'F:\GitHub\McpServer\tests\McpServer.PluginIntegration.Tests\*.cs' -Pattern 'Skip\s*=|Fact\(Skip|Theory\(Skip|Assert\.Skip' -ErrorAction SilentlyContinue)
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
