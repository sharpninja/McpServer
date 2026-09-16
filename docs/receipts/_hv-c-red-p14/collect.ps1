#Requires -Version 7.0
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Continue'

$out = 'F:\GitHub\McpServer\docs\receipts\_hv-c-red-p14'
New-Item -ItemType Directory -Force -Path $out | Out-Null
Set-Location -LiteralPath 'F:\GitHub\McpServer'

$priorAgree = 'F:\GitHub\McpServer\docs\receipts\hostile-validator-20260822T034932Z.md'
$priorAgreeJson = 'F:\GitHub\McpServer\docs\receipts\hostile-validator-20260822T034932Z.json'
$laterDisagree = 'F:\GitHub\McpServer\docs\receipts\hostile-validator-20260822T035559Z.md'
$laterDisagreeJson = 'F:\GitHub\McpServer\docs\receipts\hostile-validator-20260822T035559Z.json'
$adapter = 'F:\GitHub\McpServer\tests\McpServer.PluginIntegration.Tests\PluginSessionLogWorkflowAdapter.cs'
$tests = 'F:\GitHub\McpServer\tests\McpServer.PluginIntegration.Tests\PluginSessionLogWorkflowAdapterTests.cs'
$resultType = 'F:\GitHub\McpServer\tests\McpServer.PluginIntegration.Tests\PluginSessionLogWorkflowResult.cs'
$plan = 'F:\GitHub\McpServer\docs\plans\PLAN-PLUGINHANDOFF-001.md'

$paths = @(
    $priorAgree
    $priorAgreeJson
    $laterDisagree
    $laterDisagreeJson
    $adapter
    $tests
    $resultType
    $plan
    'F:\GitHub\McpServer\tests\McpServer.PluginIntegration.Tests\PluginHostProcessAdapter.cs'
    'F:\GitHub\McpServer\tests\McpServer.PluginIntegration.Tests\PluginIntegrationServerFixture.cs'
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
        $obj.MentionsCGreenP11P13 = ($agreeText -match 'C-green-P11-P13')
        $obj.MentionsPassed54 = ($agreeText -match 'Passed:\s*54' -or $agreeText -match 'Passed 54')
        $obj.MentionsFailed0 = ($agreeText -match 'Failed:\s*0' -or $agreeText -match 'Failed 0')
        $obj.MentionsSkipped0 = ($agreeText -match 'Skipped:\s*0' -or $agreeText -match 'Skipped 0')
        $obj.MentionsP14Absent = ($agreeText -match 'P14' -and $agreeText -match 'absent')
        $obj.EmDash = $agreeText.Contains([char]0x2014)
        $obj.EnDash = $agreeText.Contains([char]0x2013)
    }
    if (Test-Path -LiteralPath $JsonPath) {
        $j = Get-Content -LiteralPath $JsonPath -Raw | ConvertFrom-Json
        $obj.JsonOverallVerdict = [string]$j.OverallVerdict
        $obj.JsonFailCount = $j.FailCount
        $obj.JsonPhase = [string]$j.Phase
        $obj.JsonAllPassed = $j.TestResults.AllPassed
        $obj.JsonAllFailed = $j.TestResults.AllFailed
        $obj.JsonAllTotal = $j.TestResults.AllTotal
        $obj.JsonP14Present = $j.TestResults.P14NamedTestsPresent
        $obj.JsonAllSkippedConsole = $j.TestResults.AllSkippedConsole
    }
    $obj | ConvertTo-Json -Depth 6 | Set-Content -LiteralPath $Dest -Encoding utf8
}

Get-ReceiptExtract -MdPath $priorAgree -JsonPath $priorAgreeJson -Dest (Join-Path $out 'prior-c-green-p11-p13-agree.json')
Get-ReceiptExtract -MdPath $laterDisagree -JsonPath $laterDisagreeJson -Dest (Join-Path $out 'later-c-green-p11-p13-disagree.json')

$adapterText = if (Test-Path $adapter) { Get-Content -LiteralPath $adapter -Raw } else { '' }
$testsText = if (Test-Path $tests) { Get-Content -LiteralPath $tests -Raw } else { '' }
$resultText = if (Test-Path $resultType) { Get-Content -LiteralPath $resultType -Raw } else { '' }

$inlineMatches = [regex]::Matches($testsText, '(?s)public async Task Theory_Agent_PluginRootOverride_WritesOnlyWorkspaceMcpServerAgent_PoisonEmpty\(PluginHostKind hostKind\).*?FindRepositoryRoot')
$p14Block = if ($inlineMatches.Count -gt 0) { $inlineMatches[0].Value } else { '' }
$p14Inline = [regex]::Matches($p14Block, '\[InlineData\(PluginHostKind\.(?<k>[A-Za-z0-9]+)\)')
$p14TheoryAttr = [regex]::Match($testsText, '\[Theory[^\]]*\]\s*(?:\r?\n\s*\[InlineData[^\]]*\]\s*){8}public async Task Theory_Agent_PluginRootOverride_WritesOnlyWorkspaceMcpServerAgent_PoisonEmpty')

$scan = [ordered]@{
    AdapterExists = (Test-Path $adapter)
    TestsExists = (Test-Path $tests)
    ResultTypeExists = (Test-Path $resultType)
    AdapterSetsPluginRootOverrideRejectedTrue = ($adapterText -match 'PluginRootOverrideRejected\s*=\s*true')
    AdapterMentionsPluginRootOverride = ($adapterText -match 'PLUGIN_ROOT_OVERRIDE|PluginRootOverride')
    AdapterReturnOmitsPluginRootOverrideRejected = ($adapterText -match 'return new PluginSessionLogWorkflowResult' -and $adapterText -notmatch 'PluginRootOverrideRejected\s*=')
    ResultPropertyExists = ($resultText -match 'public bool PluginRootOverrideRejected')
    TestsP14MethodPresent = ($testsText -match 'Theory_Agent_PluginRootOverride_WritesOnlyWorkspaceMcpServerAgent_PoisonEmpty')
    TestsP14IsTheory = $p14TheoryAttr.Success
    TestsP14InlineHostKinds = @($p14Inline | ForEach-Object { $_.Groups['k'].Value })
    TestsP14InlineCount = $p14Inline.Count
    TestsP15SuccessNoPending = ($testsText -match 'Success_NoPendingFailsafe')
    TestsP15FailedSubmit = ($testsText -match 'FailedSubmit_RetainsRootIdPending')
    TestsP15RetrySuccess = ($testsText -match 'RetrySuccess_DeletesOnlyMatchingPending')
    TestsP16AiTheory = ($testsText -match 'AiTheory_')
    TestsSkipAttribute = ($testsText -match '\[Skip')
    TestsFactSkip = ($testsText -match 'Fact\s*\(\s*Skip')
    TestsTheorySkip = ($testsText -match 'Theory\s*\(\s*Skip')
    TestsAssertSkip = ($testsText -match 'Assert\.Skip')
    TestsSetsPluginRootOverrideEnv = ($testsText -match 'PLUGIN_ROOT_OVERRIDE')
    TestsStartsIsolatedFixture = ($testsText -match 'new PluginIntegrationServerFixture')
    TestsAssertsNot7147InP14 = ($p14Block -match '7147')
    TestsCallsExecuteCanonicalTurnAsync = ($testsText -match 'ExecuteCanonicalTurnAsync')
    TestsAssertsPluginRootOverrideRejected = ($testsText -match 'result\.PluginRootOverrideRejected')
    TestsAssertsPoisonEmpty = ($testsText -match 'Assert\.Empty\(Directory\.GetFileSystemEntries\(poison\)\)')
    TestsXmlMapsAc4 = ($testsText -match 'Maps TEST-MCP-PLUGININT-001 AC4')
    EmDashAdapter = $adapterText.Contains([char]0x2014)
    EnDashAdapter = $adapterText.Contains([char]0x2013)
    EmDashTests = $testsText.Contains([char]0x2014)
    EnDashTests = $testsText.Contains([char]0x2013)
}

$repoRoot = 'F:\GitHub\McpServer'
$p14Hits = @(Select-String -Path (Join-Path $repoRoot 'tests\**\*.cs'), (Join-Path $repoRoot 'src\**\*.cs') -Pattern 'PluginRootOverride_WritesOnlyWorkspaceMcpServerAgent_PoisonEmpty' -SimpleMatch -ErrorAction SilentlyContinue)
$scan.P14HitsTestsSrc = @($p14Hits | ForEach-Object { "$($_.Path):$($_.LineNumber)" })
$scan.P14HitsTestsSrcCount = @($p14Hits).Count

$p15Hits = @(Select-String -Path (Join-Path $repoRoot 'tests\McpServer.PluginIntegration.Tests\**\*.cs') -Pattern 'Success_NoPendingFailsafe|FailedSubmit_RetainsRootIdPending|RetrySuccess_DeletesOnlyMatchingPending|AiTheory_' -ErrorAction SilentlyContinue)
$scan.P15P16HitsPluginIntegration = @($p15Hits | ForEach-Object { "$($_.Path):$($_.LineNumber):$($_.Line.Trim())" })
$scan.P15P16HitsPluginIntegrationCount = @($p15Hits).Count

$skipHits = @(Select-String -Path (Join-Path $repoRoot 'tests\McpServer.PluginIntegration.Tests\**\*.cs') -Pattern 'Skip' -ErrorAction SilentlyContinue)
$scan.SkipHitsPluginIntegration = @($skipHits | ForEach-Object { "$($_.Filename):$($_.LineNumber):$($_.Line.Trim())" })

$adapterAnchors = @(Select-String -Path $adapter -Pattern 'PluginRootOverrideRejected|PLUGIN_ROOT_OVERRIDE|return new PluginSessionLogWorkflowResult' -ErrorAction SilentlyContinue)
$scan.AdapterAnchors = @($adapterAnchors | ForEach-Object { "$($_.LineNumber):$($_.Line.Trim())" })
$testAnchors = @(Select-String -Path $tests -Pattern 'PluginRootOverride|PoisonEmpty|PLUGIN_ROOT_OVERRIDE|InlineData\(PluginHostKind' -ErrorAction SilentlyContinue)
$scan.TestAnchors = @($testAnchors | ForEach-Object { "$($_.LineNumber):$($_.Line.Trim())" })
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
    PythonProcesses = @($pythonHits | ForEach-Object {
        [ordered]@{
            ProcessId = $_.ProcessId
            Name = $_.Name
            CommandLine = [string]$_.CommandLine
        }
    })
} | ConvertTo-Json -Depth 5 | Set-Content -LiteralPath (Join-Path $out 'no-python-hits.json') -Encoding utf8

$testhosts = @(Get-CimInstance Win32_Process -ErrorAction SilentlyContinue | Where-Object {
    $_.Name -match 'testhost|dotnet' -and [string]$_.CommandLine -match 'PluginIntegration'
} | Select-Object ProcessId, Name, CommandLine)
[ordered]@{
    TimestampUtc = [DateTime]::UtcNow.ToString('o')
    Count = @($testhosts).Count
    Processes = @($testhosts | ForEach-Object {
        [ordered]@{
            ProcessId = $_.ProcessId
            Name = $_.Name
            CommandLine = [string]$_.CommandLine
        }
    })
} | ConvertTo-Json -Depth 5 | Set-Content -LiteralPath (Join-Path $out 'dotnet-procs.json') -Encoding utf8

Write-Output 'COLLECT_DONE'
