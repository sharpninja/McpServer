#Requires -Version 7.0
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Continue'

$out = 'F:\GitHub\McpServer\docs\receipts\_hv-c-red-p11'
New-Item -ItemType Directory -Force -Path $out | Out-Null
Set-Location -LiteralPath 'F:\GitHub\McpServer'

$priorAgree = 'F:\GitHub\McpServer\docs\receipts\hostile-validator-20260822T020756Z.md'
$priorAgreeJson = 'F:\GitHub\McpServer\docs\receipts\hostile-validator-20260822T020756Z.json'
$priorAgreeCutoff = [DateTime]::Parse('2026-08-22T02:07:56Z').ToUniversalTime()

$paths = @(
    $priorAgree
    $priorAgreeJson
    'F:\GitHub\McpServer\tests\McpServer.PluginIntegration.Tests\PluginSessionLogWorkflowAdapter.cs'
    'F:\GitHub\McpServer\tests\McpServer.PluginIntegration.Tests\PluginSessionLogWorkflowAdapterTests.cs'
    'F:\GitHub\McpServer\tests\McpServer.PluginIntegration.Tests\PluginSessionLogWorkflowResult.cs'
    'F:\GitHub\McpServer\tests\McpServer.PluginIntegration.Tests\PluginSessionLogCollection.cs'
    'F:\GitHub\McpServer\tests\McpServer.PluginIntegration.Tests\PluginSessionLogCatalog.cs'
    'F:\GitHub\McpServer\tests\McpServer.PluginIntegration.Tests\PluginSessionLogScenario.cs'
    'F:\GitHub\McpServer\tests\McpServer.PluginIntegration.Tests\PluginHostKind.cs'
    'F:\GitHub\McpServer\tests\McpServer.PluginIntegration.Tests\PluginHostProcessAdapter.cs'
    'F:\GitHub\McpServer\tests\McpServer.PluginIntegration.Tests\PluginHostProcessAdapterTests.cs'
    'F:\GitHub\McpServer\tests\McpServer.PluginIntegration.Tests\PluginIntegrationServerFixture.cs'
    'F:\GitHub\McpServer\tests\McpServer.PluginIntegration.Tests\PluginIntegrationServerFixtureTests.cs'
    'F:\GitHub\McpServer\tests\McpServer.PluginIntegration.Tests\PluginIntegrationProjectTests.cs'
    'F:\GitHub\McpServer\tests\McpServer.PluginIntegration.Tests\scenarios\plugin-sessionlog-scenarios.json'
    'F:\GitHub\McpServer\docs\plans\PLAN-PLUGINHANDOFF-001.md'
)
$meta = foreach ($p in $paths) {
    $exists = Test-Path -LiteralPath $p
    if ($exists) {
        $i = Get-Item -LiteralPath $p
        $lw = $i.LastWriteTimeUtc
        [pscustomobject]@{
            Path = $p
            Exists = $true
            LastWriteTimeUtc = $lw.ToString('o')
            Length = $i.Length
            AfterCGreenP7P10Agree = ($lw -gt $priorAgreeCutoff)
        }
    } else {
        [pscustomobject]@{
            Path = $p
            Exists = $false
            LastWriteTimeUtc = $null
            Length = 0
            AfterCGreenP7P10Agree = $null
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
        MentionsCGreenP7P10 = ($agreeText -match 'C-green-P7-P10')
        MentionsTheoryEachScenario = ($agreeText -match 'Theory_EachScenario_FailsUntilAdapterOperational')
        MentionsAbsent = ($agreeText -match 'absent')
        MentionsNoMatchLiteral = ($agreeText -match 'NO_MATCH')
        MentionsP11MatchCountZero = ($agreeText -match 'P11MatchCountTestsSrc 0')
        MentionsP11NamedTestsPresentFalse = ($agreeText -match 'P11NamedTestsPresent')
        MentionsDoNotRequireP11 = ($agreeText -match 'Do not require P11')
        MentionsGrepTestsSrc = ($agreeText -match 'grepped Theory_EachScenario_FailsUntilAdapterOperational in tests/src \(absent\)')
        EmDash = $agreeText.Contains([char]0x2014)
        EnDash = $agreeText.Contains([char]0x2013)
    }
    if (Test-Path -LiteralPath $priorAgreeJson) {
        $j = Get-Content -LiteralPath $priorAgreeJson -Raw | ConvertFrom-Json
        $agreeObj.JsonOverallVerdict = [string]$j.OverallVerdict
        $agreeObj.JsonFailCount = $j.FailCount
        $agreeObj.JsonPassCount = $j.PassCount
        $agreeObj.JsonP11NamedTestsPresent = $j.TestResults.P11NamedTestsPresent
        $agreeObj.JsonPhase = [string]$j.Phase
    }
    $agreeObj | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $out 'prior-cgreen-p7-p10-agree.json') -Encoding utf8
} else {
    [ordered]@{ Exists = $false } | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $out 'prior-cgreen-p7-p10-agree.json') -Encoding utf8
}

$testsCs = 'F:\GitHub\McpServer\tests\McpServer.PluginIntegration.Tests\PluginSessionLogWorkflowAdapterTests.cs'
$adapterCs = 'F:\GitHub\McpServer\tests\McpServer.PluginIntegration.Tests\PluginSessionLogWorkflowAdapter.cs'
$testsSrc = if (Test-Path $testsCs) { Get-Content -LiteralPath $testsCs -Raw } else { '' }
$adapterSrc = if (Test-Path $adapterCs) { Get-Content -LiteralPath $adapterCs -Raw } else { '' }

$hostKinds = @('Codex', 'ClaudeCode', 'ClaudeCowork', 'Copilot', 'Grok', 'Cline', 'ClineV2', 'OpenCode')
$inlineHits = foreach ($h in $hostKinds) {
    [pscustomobject]@{
        HostKind = $h
        InlineDataPresent = [bool]($testsSrc -match ("\[InlineData\(PluginHostKind\.$h\)\]"))
    }
}

$p11Name = 'Theory_EachScenario_FailsUntilAdapterOperational'
$p12Name = 'BootstrapBeginAppendComplete_CapturesIdsCacheShaReceipt'
$p13Name = 'ServerQuery_SourceTypeIdsActionDialogCompletedWorkspace'

$csFiles = @(Get-ChildItem -Path 'F:\GitHub\McpServer\tests','F:\GitHub\McpServer\src' -Recurse -Include *.cs,*.csproj -File -ErrorAction SilentlyContinue)
$p11Hits = @($csFiles | Select-String -Pattern $p11Name -SimpleMatch)
$p12Hits = @($csFiles | Select-String -Pattern $p12Name -SimpleMatch)
$p13Hits = @($csFiles | Select-String -Pattern $p13Name -SimpleMatch)
$p12TheoryHits = @($csFiles | Select-String -Pattern 'Theory_\{Agent\}_BootstrapBeginAppendComplete_CapturesIdsCacheShaReceipt' -SimpleMatch)
$p13TheoryHits = @($csFiles | Select-String -Pattern 'Theory_\{Agent\}_ServerQuery_SourceTypeIdsActionDialogCompletedWorkspace' -SimpleMatch)

$skipTheory = [bool]($testsSrc -match '\[Theory[^\]]*Skip\s*=')
$skipFact = [bool]($testsSrc -match '\[Fact[^\]]*Skip\s*=')
$assertSkip = [bool]($testsSrc -match 'Assert\.Skip|Skip\.If|Skip\.Unless')
$timeoutMatch = [regex]::Match($testsSrc, '\[Theory\(Timeout\s*=\s*(\d+)\)\]')
$timeoutValue = if ($timeoutMatch.Success) { [int]$timeoutMatch.Groups[1].Value } else { $null }

$scan = [ordered]@{
    TestsFileExists = (Test-Path -LiteralPath $testsCs)
    AdapterFileExists = (Test-Path -LiteralPath $adapterCs)
    P11MethodPresent = [bool]($testsSrc -match ('public async Task ' + $p11Name + '\(PluginHostKind hostKind\)'))
    P11InlineDataCount = @([regex]::Matches($testsSrc, '\[InlineData\(PluginHostKind\.\w+\)\]')).Count
    InlineDataHostKinds = $inlineHits
    AllEightInlinePresent = (@($inlineHits | Where-Object { -not $_.InlineDataPresent }).Count -eq 0)
    TheoryTimeoutMs = $timeoutValue
    TheoryTimeoutIs120000 = ($timeoutValue -eq 120000)
    SkipAttributeOnTheory = $skipTheory
    SkipAttributeOnFact = $skipFact
    AssertSkipPresent = $assertSkip
    StartsFixture = [bool]($testsSrc -match 'new PluginIntegrationServerFixture\(\)')
    AwaitsStartAsync = [bool]($testsSrc -match 'await fixture\.StartAsync')
    AssertsPortNot7147 = [bool]($testsSrc -match 'Assert\.NotEqual\(7147,\s*fixture\.Port\)')
    CallsExecuteCanonicalTurnAsync = [bool]($testsSrc -match 'ExecuteCanonicalTurnAsync\(scenario,\s*fixture')
    CollectionPluginSessionLog = [bool]($testsSrc -match '\[Collection\("PluginSessionLog"\)\]')
    AdapterThrowsInvalidOperation = [bool]($adapterSrc -match 'throw new InvalidOperationException')
    AdapterThrowContainsNotImplemented = [bool]($adapterSrc -match 'not implemented')
    AdapterThrowExact = [bool]($adapterSrc -match 'PluginSessionLogWorkflowAdapter\.ExecuteCanonicalTurnAsync is not implemented\.')
    AdapterHasLaunchLogic = [bool]($adapterSrc -match 'ProcessStartInfo|HttpClient|IPluginProcessRunner|RunAsync')
    AdapterLineCount = @($adapterSrc -split "`n").Count
    P11HitsTestsSrcCount = $p11Hits.Count
    P11HitPaths = @($p11Hits | ForEach-Object { $_.Path })
    P12HitsTestsSrcCount = $p12Hits.Count
    P12HitPaths = @($p12Hits | ForEach-Object { $_.Path + ':' + $_.LineNumber })
    P13HitsTestsSrcCount = $p13Hits.Count
    P13HitPaths = @($p13Hits | ForEach-Object { $_.Path + ':' + $_.LineNumber })
    P12TheoryBraceHits = $p12TheoryHits.Count
    P13TheoryBraceHits = $p13TheoryHits.Count
    EmDashTests = $testsSrc.Contains([char]0x2014)
    EnDashTests = $testsSrc.Contains([char]0x2013)
    EmDashAdapter = $adapterSrc.Contains([char]0x2014)
    EnDashAdapter = $adapterSrc.Contains([char]0x2013)
}
$scan | ConvertTo-Json -Depth 6 | Set-Content -LiteralPath (Join-Path $out 'adapter-scan.json') -Encoding utf8

$gitStatus = git -C 'F:\GitHub\McpServer' status --porcelain -- tests/McpServer.PluginIntegration.Tests docs/plans/PLAN-PLUGINHANDOFF-001.md
Set-Content -LiteralPath (Join-Path $out 'git-status-pluginint.txt') -Value ([string]$gitStatus) -Encoding utf8
$gitLog = git -C 'F:\GitHub\McpServer' log -n 8 --format='%h %ci %s' -- tests/McpServer.PluginIntegration.Tests/PluginSessionLogWorkflowAdapter.cs tests/McpServer.PluginIntegration.Tests/PluginSessionLogWorkflowAdapterTests.cs
Set-Content -LiteralPath (Join-Path $out 'git-log-adapter.txt') -Value ([string]$gitLog) -Encoding utf8

$todoYamlStatus = git -C 'F:\GitHub\McpServer' status --porcelain -- docs/Project/TODO.yaml
[ordered]@{ TodoYamlPorcelain = [string]$todoYamlStatus } | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $out 'todo-yaml-git-status.json') -Encoding utf8

Write-Output 'COLLECT_DONE'
Write-Output ("P11_METHOD=" + $scan.P11MethodPresent)
Write-Output ("INLINE_COUNT=" + $scan.P11InlineDataCount)
Write-Output ("TIMEOUT=" + $scan.TheoryTimeoutMs)
Write-Output ("THROW_EXACT=" + $scan.AdapterThrowExact)
Write-Output ("P12_HITS=" + $scan.P12HitsTestsSrcCount)
Write-Output ("P13_HITS=" + $scan.P13HitsTestsSrcCount)
Write-Output ("PRIOR_NO_MATCH=" + $agreeObj.MentionsNoMatchLiteral)
Write-Output ("PRIOR_ABSENT=" + $agreeObj.MentionsAbsent)
