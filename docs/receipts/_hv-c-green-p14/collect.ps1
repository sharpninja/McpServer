#Requires -Version 7.0
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Continue'

$out = 'F:\GitHub\McpServer\docs\receipts\_hv-c-green-p14'
New-Item -ItemType Directory -Force -Path $out | Out-Null
Set-Location -LiteralPath 'F:\GitHub\McpServer'

$priorRed = 'F:\GitHub\McpServer\docs\receipts\hostile-validator-20260822T041327Z.md'
$priorRedJson = 'F:\GitHub\McpServer\docs\receipts\hostile-validator-20260822T041327Z.json'
$persistDisagree = 'F:\GitHub\McpServer\docs\receipts\hostile-validator-20260822T035559Z.md'
$persistDisagreeJson = 'F:\GitHub\McpServer\docs\receipts\hostile-validator-20260822T035559Z.json'
$adapter = 'F:\GitHub\McpServer\tests\McpServer.PluginIntegration.Tests\PluginSessionLogWorkflowAdapter.cs'
$tests = 'F:\GitHub\McpServer\tests\McpServer.PluginIntegration.Tests\PluginSessionLogWorkflowAdapterTests.cs'
$resultType = 'F:\GitHub\McpServer\tests\McpServer.PluginIntegration.Tests\PluginSessionLogWorkflowResult.cs'
$plan = 'F:\GitHub\McpServer\docs\plans\PLAN-PLUGINHANDOFF-001.md'
$fixture = 'F:\GitHub\McpServer\tests\McpServer.PluginIntegration.Tests\PluginIntegrationServerFixture.cs'
$processAdapter = 'F:\GitHub\McpServer\tests\McpServer.PluginIntegration.Tests\PluginHostProcessAdapter.cs'
$runner = 'F:\GitHub\McpServer\tests\McpServer.PluginIntegration.Tests\RealPluginProcessRunner.cs'

$paths = @(
    $priorRed
    $priorRedJson
    $persistDisagree
    $persistDisagreeJson
    $adapter
    $tests
    $resultType
    $plan
    $fixture
    $processAdapter
    $runner
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
        $obj.MentionsFailed8 = ($agreeText -match 'Failed:\s*8' -or $agreeText -match 'Failed 8')
        $obj.MentionsPassed0 = ($agreeText -match 'Passed:\s*0' -or $agreeText -match 'Passed 0')
        $obj.MentionsPluginRootOverrideRejected = ($agreeText -match 'PluginRootOverrideRejected')
        $obj.MentionsCreateTrustedClient = ($agreeText -match 'CreateTrustedClient')
        $obj.EmDash = $agreeText.Contains([char]0x2014)
        $obj.EnDash = $agreeText.Contains([char]0x2013)
    }
    if (Test-Path -LiteralPath $JsonPath) {
        $j = Get-Content -LiteralPath $JsonPath -Raw | ConvertFrom-Json
        $obj.JsonOverallVerdict = [string]$j.OverallVerdict
        $obj.JsonFailCount = $j.FailCount
        $obj.JsonPhase = [string]$j.Phase
    }
    $obj | ConvertTo-Json -Depth 6 | Set-Content -LiteralPath $Dest -Encoding utf8
}

Get-ReceiptExtract -MdPath $priorRed -JsonPath $priorRedJson -Dest (Join-Path $out 'prior-c-red-p14-agree.json')
Get-ReceiptExtract -MdPath $persistDisagree -JsonPath $persistDisagreeJson -Dest (Join-Path $out 'later-c-green-p11-p13-disagree.json')

$adapterText = Get-Content -LiteralPath $adapter -Raw
$testsText = Get-Content -LiteralPath $tests -Raw
$resultText = Get-Content -LiteralPath $resultType -Raw
$fixtureText = Get-Content -LiteralPath $fixture -Raw
$runnerText = Get-Content -LiteralPath $runner -Raw
$processText = Get-Content -LiteralPath $processAdapter -Raw

[ordered]@{
    AdapterHasPluginRootOverrideRejectedAssign = ($adapterText -match 'PluginRootOverrideRejected\s*=\s*pluginRootOverrideRejected')
    AdapterReadsPluginRootOverrideEnv = ($adapterText -match 'GetEnvironmentVariable\("PLUGIN_ROOT_OVERRIDE"\)')
    AdapterSetsExtraEnvEmptyOverride = ($adapterText -match '\["PLUGIN_ROOT_OVERRIDE"\]\s*=\s*string\.Empty')
    AdapterCachePathMcpServerCacheFolder = ($adapterText -match 'Path\.Combine\(fixture\.WorkspacePath,\s*"\.mcpServer",\s*scenario\.CacheFolder\)')
    AdapterHasCreateTrustedClient = ($adapterText -match 'CreateTrustedClient')
    AdapterDiscardsLaunchResult = ($adapterText -match '_\s*=\s*await processAdapter\.LaunchAsync')
    AdapterCallsWorkflowSessionlog = ($adapterText -match 'workflow\.sessionlog')
    AdapterCallsInvokeMcpPlugin = ($adapterText -match 'Invoke-McpPlugin')
    AdapterFabricatesSessionId = ($adapterText -match 'sessionId = scenario\.AgentSourceType \+ "-" \+ utc')
    ResultPropertyExists = ($resultText -match 'PluginRootOverrideRejected')
    ResultCommentStillSaysRed = ($resultText -match 'C-red-P14 stays false')
    TestsNamedMethod = ($testsText -match 'Theory_Agent_PluginRootOverride_WritesOnlyWorkspaceMcpServerAgent_PoisonEmpty')
    TestsAssertRejected = ($testsText -match 'must reject PLUGIN_ROOT_OVERRIDE')
    TestsAssertPoisonEmpty = ($testsText -match 'Assert\.Empty\(Directory\.GetFileSystemEntries\(poison\)\)')
    TestsAssertCacheContainsMcpServer = ($testsText -match 'Assert\.Contains\(Path\.Combine\("\.mcpServer", scenario\.CacheFolder\)')
    TestsAssertCacheContainsWorkspace = ($testsText -match 'Assert\.Contains\(fixture\.WorkspacePath, result\.CachePath')
    TestsAssertSiblingCache = ($testsText -match 'sibling')
    TestsSkipAttribute = ($testsText -match 'Skip\s*=')
    TestsAssertSkip = ($testsText -match 'Assert\.Skip')
    TestsP15Success = ($testsText -match 'Success_NoPendingFailsafe')
    TestsP15FailedSubmit = ($testsText -match 'FailedSubmit_RetainsRootIdPending')
    TestsP15Retry = ($testsText -match 'RetrySuccess_DeletesOnlyMatchingPending')
    TestsAssertPortNot7147 = ($testsText -match '7147')
    FixtureReserved7147 = ($fixtureText -match 'ReservedServicePort = 7147')
    FixtureAllocateExcludes7147 = ($fixtureText -match 'port != ReservedServicePort')
    RunnerRemovesEmptyEnv = ($runnerText -match 'startInfo\.Environment\.Remove\(pair\.Key\)')
    ProcessOverlayExtraEnv = ($processText -match 'environment\[pair\.Key\] = pair\.Value')
    InlineDataHostCount = ([regex]::Matches($testsText, '\[InlineData\(PluginHostKind\.')).Count
} | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $out 'adapter-scan.json') -Encoding utf8

$p15Hits = @(Select-String -Path 'F:\GitHub\McpServer\tests\McpServer.PluginIntegration.Tests\*.cs' -Pattern 'Success_NoPendingFailsafe|FailedSubmit_RetainsRootIdPending|RetrySuccess_DeletesOnlyMatchingPending' -SimpleMatch:$false -ErrorAction SilentlyContinue)
$skipHits = @(Select-String -Path 'F:\GitHub\McpServer\tests\McpServer.PluginIntegration.Tests\*.cs' -Pattern 'Skip\s*=|Fact\(Skip|Theory\(Skip|Assert\.Skip' -ErrorAction SilentlyContinue)
[ordered]@{
    P15HitCount = $p15Hits.Count
    P15Hits = @($p15Hits | ForEach-Object { $_.Path + ':' + $_.LineNumber + ':' + $_.Line.Trim() })
    SkipHitCount = $skipHits.Count
    SkipHits = @($skipHits | ForEach-Object { $_.Path + ':' + $_.LineNumber + ':' + $_.Line.Trim() })
} | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $out 'p15-skip-grep.json') -Encoding utf8

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
