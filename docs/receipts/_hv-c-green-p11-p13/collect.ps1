#Requires -Version 7.0
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Continue'

$out = 'F:\GitHub\McpServer\docs\receipts\_hv-c-green-p11-p13'
New-Item -ItemType Directory -Force -Path $out | Out-Null
Set-Location -LiteralPath 'F:\GitHub\McpServer'

$priorAgree = 'F:\GitHub\McpServer\docs\receipts\hostile-validator-20260822T023808Z.md'
$priorAgreeJson = 'F:\GitHub\McpServer\docs\receipts\hostile-validator-20260822T023808Z.json'
$adapter = 'F:\GitHub\McpServer\tests\McpServer.PluginIntegration.Tests\PluginSessionLogWorkflowAdapter.cs'
$tests = 'F:\GitHub\McpServer\tests\McpServer.PluginIntegration.Tests\PluginSessionLogWorkflowAdapterTests.cs'

$paths = @(
    $priorAgree
    $priorAgreeJson
    $adapter
    $tests
    'F:\GitHub\McpServer\tests\McpServer.PluginIntegration.Tests\PluginSessionLogWorkflowResult.cs'
    'F:\GitHub\McpServer\tests\McpServer.PluginIntegration.Tests\PluginHostProcessAdapter.cs'
    'F:\GitHub\McpServer\tests\McpServer.PluginIntegration.Tests\PluginIntegrationServerFixture.cs'
    'F:\GitHub\McpServer\docs\plans\PLAN-PLUGINHANDOFF-001.md'
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

if (Test-Path -LiteralPath $priorAgree) {
    $agreeText = Get-Content -LiteralPath $priorAgree -Raw
    $agreeObj = [ordered]@{
        Exists = $true
        Length = (Get-Item -LiteralPath $priorAgree).Length
        LastWriteTimeUtc = (Get-Item -LiteralPath $priorAgree).LastWriteTimeUtc.ToString('o')
        OverallVerdictLineMatch = [bool]($agreeText -match '(?m)^OverallVerdict:\s*AGREE\s*$')
        MentionsCRedP11 = ($agreeText -match 'C-red-P11')
        MentionsFailed8 = ($agreeText -match 'Failed:\s*8' -or $agreeText -match 'Failed 8')
        MentionsPassed0 = ($agreeText -match 'Passed:\s*0' -or $agreeText -match 'Passed 0')
        MentionsP12P13Absent = ($agreeText -match 'P12/P13' -and $agreeText -match 'absent')
        MentionsNotImplemented = ($agreeText -match 'ExecuteCanonicalTurnAsync is not implemented')
        EmDash = $agreeText.Contains([char]0x2014)
        EnDash = $agreeText.Contains([char]0x2013)
    }
    if (Test-Path -LiteralPath $priorAgreeJson) {
        $j = Get-Content -LiteralPath $priorAgreeJson -Raw | ConvertFrom-Json
        $agreeObj.JsonOverallVerdict = [string]$j.OverallVerdict
        $agreeObj.JsonFailCount = $j.FailCount
        $agreeObj.JsonPhase = [string]$j.Phase
        $agreeObj.JsonFilterFailed = $j.TestResults.FilterFailed
        $agreeObj.JsonFilterPassed = $j.TestResults.FilterPassed
        $agreeObj.JsonP12Present = $j.TestResults.P12NamedTestsPresent
        $agreeObj.JsonP13Present = $j.TestResults.P13NamedTestsPresent
        $agreeObj.JsonThrowsNotImplemented = $j.TestResults.ExecuteCanonicalTurnAsyncThrowsNotImplemented
    }
    $agreeObj | ConvertTo-Json -Depth 6 | Set-Content -LiteralPath (Join-Path $out 'prior-c-red-p11-agree.json') -Encoding utf8
}

$adapterText = if (Test-Path $adapter) { Get-Content -LiteralPath $adapter -Raw } else { '' }
$testsText = if (Test-Path $tests) { Get-Content -LiteralPath $tests -Raw } else { '' }

$scan = [ordered]@{
    AdapterExists = (Test-Path $adapter)
    TestsExists = (Test-Path $tests)
    AdapterThrowExact = ($adapterText -match 'PluginSessionLogWorkflowAdapter\.ExecuteCanonicalTurnAsync is not implemented\.')
    AdapterHasCreateTrustedClient = ($adapterText -match 'CreateTrustedClient')
    AdapterHasOpenSession = ($adapterText -match 'OpenSessionAsync')
    AdapterHasBeginTurn = ($adapterText -match 'BeginTurnAsync')
    AdapterHasAppendDialog = ($adapterText -match 'AppendDialogAsync')
    AdapterHasCompleteTurn = ($adapterText -match 'CompleteTurnAsync')
    AdapterHasPluginHostProcessAdapter = ($adapterText -match 'PluginHostProcessAdapter')
    AdapterDiscardsLaunchResult = ($adapterText -match '_\s*=\s*await processAdapter\.LaunchAsync')
    AdapterFabricatesSessionId = ($adapterText -match 'sessionId = scenario\.AgentSourceType \+ "-"')
    AdapterCreatesCacheDirectory = ($adapterText -match 'Directory\.CreateDirectory\(cachePath\)')
    AdapterHashesEntrypointBytes = ($adapterText -match 'SHA256\.HashData')
    AdapterCallsWorkflowSessionlog = ($adapterText -match 'workflow\.sessionlog')
    AdapterCallsInvokeMcpPlugin = ($adapterText -match 'Invoke-McpPlugin|Invoke-CodexMcpPlugin')
    AdapterStdinFakeJson = ($adapterText -match 'sessionlog_begin_turn')
    AdapterLaunchTimeoutSeconds5 = ($adapterText -match 'TimeSpan\.FromSeconds\(5\)')
    TestsP11Present = ($testsText -match 'Theory_EachScenario_FailsUntilAdapterOperational')
    TestsP12Present = ($testsText -match 'Theory_Agent_BootstrapBeginAppendComplete_CapturesIdsCacheShaReceipt')
    TestsP13Present = ($testsText -match 'Theory_Agent_ServerQuery_SourceTypeIdsActionDialogCompletedWorkspace')
    TestsP14Present = ($testsText -match 'PluginRootOverride_WritesOnlyWorkspaceMcpServerAgent_PoisonEmpty')
    TestsSkipAttribute = ($testsText -match '\[Skip')
    TestsAssertSkip = ($testsText -match 'Assert\.Skip')
    P11InlineDataCount = ([regex]::Matches($testsText, '\[InlineData\(PluginHostKind\.')).Count
    EmDashAdapter = $adapterText.Contains([char]0x2014)
    EnDashAdapter = $adapterText.Contains([char]0x2013)
    EmDashTests = $testsText.Contains([char]0x2014)
    EnDashTests = $testsText.Contains([char]0x2013)
}

$repoRoot = 'F:\GitHub\McpServer'
$p14Hits = @(Select-String -Path (Join-Path $repoRoot 'tests\**\*.cs'), (Join-Path $repoRoot 'src\**\*.cs') -Pattern 'PluginRootOverride_WritesOnlyWorkspaceMcpServerAgent_PoisonEmpty' -SimpleMatch -ErrorAction SilentlyContinue)
$scan.P14HitsTestsSrcCount = @($p14Hits).Count
$skipHits = @(Select-String -Path $tests -Pattern 'Skip' -ErrorAction SilentlyContinue)
$scan.SkipHitsInTestsFile = @($skipHits | ForEach-Object { "$($_.LineNumber):$($_.Line.Trim())" })
$clientHits = @(Select-String -Path $adapter -Pattern 'CreateTrustedClient|OpenSessionAsync|LaunchAsync|_ =' -ErrorAction SilentlyContinue)
$scan.AdapterPersistAnchors = @($clientHits | ForEach-Object { "$($_.LineNumber):$($_.Line.Trim())" })
$scan | ConvertTo-Json -Depth 6 | Set-Content -LiteralPath (Join-Path $out 'adapter-scan.json') -Encoding utf8

git -C $repoRoot status --porcelain tests/McpServer.PluginIntegration.Tests docs/plans/PLAN-PLUGINHANDOFF-001.md docs/receipts 2>$null |
    Set-Content -LiteralPath (Join-Path $out 'git-status-pluginint.txt') -Encoding utf8
git -C $repoRoot status --porcelain -- docs/Project/TODO.yaml docs/todo.yaml 2>$null |
    Set-Content -LiteralPath (Join-Path $out 'todo-yaml-git-status.txt') -Encoding utf8
git -C $repoRoot log -n 5 --oneline -- tests/McpServer.PluginIntegration.Tests/PluginSessionLogWorkflowAdapter.cs tests/McpServer.PluginIntegration.Tests/PluginSessionLogWorkflowAdapterTests.cs 2>$null |
    Set-Content -LiteralPath (Join-Path $out 'git-log-adapter.txt') -Encoding utf8

$pythonHits = @(Get-CimInstance Win32_Process -ErrorAction SilentlyContinue | Where-Object {
    $_.Name -match '^(python|python3|py)\.exe$'
} | Select-Object ProcessId, Name, CommandLine)
[ordered]@{
    TimestampUtc = [DateTime]::UtcNow.ToString('o')
    PythonProcessCount = @($pythonHits).Count
    PythonProcesses = @($pythonHits)
} | ConvertTo-Json -Depth 5 | Set-Content -LiteralPath (Join-Path $out 'no-python-hits.json') -Encoding utf8

Write-Output 'COLLECT_DONE'
