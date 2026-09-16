#Requires -Version 7.0
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Continue'

$out = 'F:\GitHub\McpServer\docs\receipts\_hv-c-green-p16-p18-rerun-20260822T084835Z'
Set-Location -LiteralPath 'F:\GitHub\McpServer'
$dotnet = 'C:\Program Files\dotnet\dotnet.exe'
$utc = [DateTime]::UtcNow.ToString('yyyyMMddTHHmmssZ')

function Invoke-DotnetCapture {
    param(
        [string[]]$DotnetArgs,
        [string]$LogName,
        [string]$ExitName
    )
    $logPath = Join-Path $out $LogName
    $exitPath = Join-Path $out $ExitName
    $sw = [System.Diagnostics.Stopwatch]::StartNew()
    $output = & $dotnet @DotnetArgs 2>&1 | ForEach-Object { $_.ToString() }
    $code = $LASTEXITCODE
    $sw.Stop()
    $text = $output -join [Environment]::NewLine
    Set-Content -LiteralPath $logPath -Value $text -Encoding utf8
    $console = [ordered]@{
        ExitCode = $code
        DurationMs = $sw.ElapsedMilliseconds
        TotalTests = $null
        Passed = $null
        Failed = $null
        Skipped = $null
        TotalTime = $null
        OutcomeLine = $null
    }
    if ($text -match 'Total tests:\s+(\d+)') { $console.TotalTests = $Matches[1] }
    if ($text -match '(?m)^\s*Passed:\s+(\d+)') { $console.Passed = $Matches[1] }
    if ($text -match '(?m)^\s*Failed:\s+(\d+)') { $console.Failed = $Matches[1] }
    if ($text -match '(?m)^\s*Skipped:\s+(\d+)') { $console.Skipped = $Matches[1] }
    if ($text -match 'Total time:\s+(.+)') { $console.TotalTime = $Matches[1].Trim() }
    if ($text -match 'Test Run (Successful|Failed|Aborted)\.') { $console.OutcomeLine = $Matches[0] }
    $console | ConvertTo-Json | Set-Content -LiteralPath $exitPath -Encoding utf8
    Write-Output ("DONE " + $LogName + " exit=" + $code + " ms=" + $sw.ElapsedMilliseconds)
    return $code
}

function Get-TrxSummary {
    param([string]$TrxPath, [string]$Dest)
    if (-not (Test-Path -LiteralPath $TrxPath)) {
        [ordered]@{ exists = $false; path = $TrxPath } | ConvertTo-Json | Set-Content -LiteralPath $Dest -Encoding utf8
        return
    }
    [xml]$xml = Get-Content -LiteralPath $TrxPath
    $ns = New-Object System.Xml.XmlNamespaceManager($xml.NameTable)
    $ns.AddNamespace('t', 'http://microsoft.com/schemas/visualstudio/2010/test')
    $counters = $xml.SelectSingleNode('//t:Counters', $ns)
    $unitResults = @($xml.SelectNodes('//t:UnitTestResult', $ns))
    $names = @($unitResults | ForEach-Object { [string]$_.testName })
    $outcomes = @($unitResults | ForEach-Object {
        [pscustomobject]@{ name = [string]$_.testName; outcome = [string]$_.outcome; duration = [string]$_.duration }
    })
    $skippedNames = @($outcomes | Where-Object { $_.outcome -match 'Skipped|NotExecuted' } | ForEach-Object { $_.name })
    $failedNames = @($outcomes | Where-Object { $_.outcome -eq 'Failed' } | ForEach-Object { $_.name })
    $p16 = @($names | Where-Object { $_ -match 'AiTheory_Agent_RequiresValidJsonFields' })
    $p17 = @($names | Where-Object { $_ -match 'AiTheory_RejectsInvalidJson_DoesNotOverrideDeterministicFailure' })
    $p19 = @($names | Where-Object { $_ -match 'PluginNativeSuite_|PluginInt_P19_' })
    $passedP16 = @($outcomes | Where-Object { $_.name -match 'AiTheory_Agent_RequiresValidJsonFields' -and $_.outcome -eq 'Passed' }).Count
    $failedP16 = @($outcomes | Where-Object { $_.name -match 'AiTheory_Agent_RequiresValidJsonFields' -and $_.outcome -eq 'Failed' }).Count
    [ordered]@{
        exists = $true
        path = $TrxPath
        lastWriteTimeUtc = (Get-Item -LiteralPath $TrxPath).LastWriteTimeUtc.ToString('o')
        total = $counters.total
        executed = $counters.executed
        passed = $counters.passed
        failed = $counters.failed
        skipped = $counters.skipped
        notExecuted = $counters.notExecuted
        resultCount = $unitResults.Count
        skippedNames = $skippedNames
        failedNames = $failedNames
        p16Count = $p16.Count
        p16Passed = $passedP16
        p16Failed = $failedP16
        p17Count = $p17.Count
        p19Count = $p19.Count
        names = $names
    } | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath $Dest -Encoding utf8
}

$listArgs = @(
    'test', 'tests/McpServer.PluginIntegration.Tests', '-c', 'Debug', '--list-tests'
)
Invoke-DotnetCapture -DotnetArgs $listArgs -LogName 'hv-dotnet-list-tests.log' -ExitName 'hv-dotnet-list-tests-exit.json'
$listText = Get-Content -LiteralPath (Join-Path $out 'hv-dotnet-list-tests.log') -Raw
$listNames = @([regex]::Matches($listText, '(?m)^\s*(McpServer\.PluginIntegration\.Tests\.\S+)') | ForEach-Object { $_.Groups[1].Value })
[ordered]@{
    count = $listNames.Count
    p16 = @($listNames | Where-Object { $_ -match 'AiTheory_Agent_RequiresValidJsonFields' }).Count
    p17 = @($listNames | Where-Object { $_ -match 'AiTheory_RejectsInvalidJson_DoesNotOverrideDeterministicFailure' }).Count
    p19 = @($listNames | Where-Object { $_ -match 'PluginNativeSuite_|PluginInt_P19_' }).Count
    names = $listNames
} | ConvertTo-Json -Depth 6 | Set-Content -LiteralPath (Join-Path $out 'list-tests-summary.json') -Encoding utf8

$listAiArgs = @(
    'test', 'tests/McpServer.PluginIntegration.Tests', '-c', 'Debug', '--list-tests', '--filter', 'PluginInt=AI'
)
Invoke-DotnetCapture -DotnetArgs $listAiArgs -LogName 'hv-dotnet-list-ai.log' -ExitName 'hv-dotnet-list-ai-exit.json'

$listDetArgs = @(
    'test', 'tests/McpServer.PluginIntegration.Tests', '-c', 'Debug', '--list-tests', '--filter', 'PluginInt=Deterministic'
)
Invoke-DotnetCapture -DotnetArgs $listDetArgs -LogName 'hv-dotnet-list-det.log' -ExitName 'hv-dotnet-list-det-exit.json'

$nukeDir = Join-Path $out ("results-nuke-" + $utc)
New-Item -ItemType Directory -Force -Path $nukeDir | Out-Null
$nukeArgs = @(
    'test', 'tests/Build.Tests', '-c', 'Debug',
    '--filter', 'FullyQualifiedName~NukeTarget_SkipIsFailure',
    '--results-directory', $nukeDir,
    '--logger', 'trx;LogFileName=nuke-skip.trx'
)
Invoke-DotnetCapture -DotnetArgs $nukeArgs -LogName 'hv-dotnet-nuke-filter.log' -ExitName 'hv-dotnet-nuke-filter-exit.json'
$nukeTrx = Get-ChildItem -LiteralPath $nukeDir -Filter '*.trx' -Recurse | Select-Object -First 1
if ($nukeTrx) { Get-TrxSummary -TrxPath $nukeTrx.FullName -Dest (Join-Path $out 'nuke-trx-summary.json') }

$aiDir = Join-Path $out ("results-ai-theory-" + $utc)
New-Item -ItemType Directory -Force -Path $aiDir | Out-Null
$aiTheoryArgs = @(
    'test', 'tests/McpServer.PluginIntegration.Tests', '-c', 'Debug',
    '--filter', 'FullyQualifiedName~PluginSessionLogAiTheoryTests',
    '--results-directory', $aiDir,
    '--logger', 'trx;LogFileName=ai-theory.trx'
)
Invoke-DotnetCapture -DotnetArgs $aiTheoryArgs -LogName 'hv-dotnet-ai-theory-filter.log' -ExitName 'hv-dotnet-ai-theory-filter-exit.json'
$aiTheoryTrx = Get-ChildItem -LiteralPath $aiDir -Filter '*.trx' -Recurse | Select-Object -First 1
if ($aiTheoryTrx) { Get-TrxSummary -TrxPath $aiTheoryTrx.FullName -Dest (Join-Path $out 'ai-theory-trx-summary.json') }

$aiTraitDir = Join-Path $out ("results-ai-trait-" + $utc)
New-Item -ItemType Directory -Force -Path $aiTraitDir | Out-Null
$aiTraitArgs = @(
    'test', 'tests/McpServer.PluginIntegration.Tests', '-c', 'Debug',
    '--filter', 'PluginInt=AI',
    '--results-directory', $aiTraitDir,
    '--logger', 'trx;LogFileName=ai-trait.trx'
)
Invoke-DotnetCapture -DotnetArgs $aiTraitArgs -LogName 'hv-dotnet-ai-trait-filter.log' -ExitName 'hv-dotnet-ai-trait-filter-exit.json'
$aiTraitTrx = Get-ChildItem -LiteralPath $aiTraitDir -Filter '*.trx' -Recurse | Select-Object -First 1
if ($aiTraitTrx) { Get-TrxSummary -TrxPath $aiTraitTrx.FullName -Dest (Join-Path $out 'ai-trait-trx-summary.json') }

$fullDir = Join-Path $out ("results-full-" + $utc)
New-Item -ItemType Directory -Force -Path $fullDir | Out-Null
$fullArgs = @(
    'test', 'tests/McpServer.PluginIntegration.Tests', '-c', 'Debug',
    '--results-directory', $fullDir,
    '--logger', 'trx;LogFileName=pluginint-all.trx'
)
Invoke-DotnetCapture -DotnetArgs $fullArgs -LogName 'hv-dotnet-pluginint-all.log' -ExitName 'hv-dotnet-pluginint-all-exit.json'
$fullTrx = Get-ChildItem -LiteralPath $fullDir -Filter '*.trx' -Recurse | Select-Object -First 1
if ($fullTrx) { Get-TrxSummary -TrxPath $fullTrx.FullName -Dest (Join-Path $out 'full-trx-summary.json') }

Write-Output 'TESTS_RUN_DONE'
