#Requires -Version 7.0
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Continue'

$out = 'F:\GitHub\McpServer\docs\receipts\_hv-c-red-p20'
$root = 'F:\GitHub\McpServer'
New-Item -ItemType Directory -Force -Path $out | Out-Null
Set-Location -LiteralPath $root

$resultsDir = Join-Path $out 'results-p20-red'
New-Item -ItemType Directory -Force -Path $resultsDir | Out-Null
$trx = Join-Path $resultsDir 'p20-red.trx'
$listLog = Join-Path $out 'hv-dotnet-list-p20.log'
$consoleLog = Join-Path $out 'hv-dotnet-p20-filter.log'
$filter = 'FullyQualifiedName~PluginUpdateServiceHarnessTests'

function Parse-ConsoleCounts {
    param([string]$Console)
    $failed = $null; $passed = $null; $skipped = $null; $total = $null
    if ($Console -match '(?m)Failed!\s+Failed:\s+(\d+),\s+Passed:\s+(\d+),\s+Skipped:\s+(\d+),\s+Total:\s+(\d+)') {
        $failed = [int]$Matches[1]; $passed = [int]$Matches[2]; $skipped = [int]$Matches[3]; $total = [int]$Matches[4]
    } elseif ($Console -match '(?m)Passed!\s+Failed:\s+(\d+),\s+Passed:\s+(\d+),\s+Skipped:\s+(\d+),\s+Total:\s+(\d+)') {
        $failed = [int]$Matches[1]; $passed = [int]$Matches[2]; $skipped = [int]$Matches[3]; $total = [int]$Matches[4]
    }
    return [ordered]@{ failed = $failed; passed = $passed; skipped = $skipped; total = $total }
}

function Parse-Trx {
    param([string]$Path)
    if (-not (Test-Path -LiteralPath $Path)) { return $null }
    [xml]$trxXml = Get-Content -LiteralPath $Path -Raw
    $ns = New-Object System.Xml.XmlNamespaceManager($trxXml.NameTable)
    $ns.AddNamespace('t', 'http://microsoft.com/schemas/VisualStudio/TeamTest/2010')
    $counters = $trxXml.SelectSingleNode('//t:ResultSummary/t:Counters', $ns)
    $results = @($trxXml.SelectNodes('//t:UnitTestResult', $ns) | ForEach-Object {
        $messageNode = $_.SelectSingleNode('t:Output/t:ErrorInfo/t:Message', $ns)
        $stackNode = $_.SelectSingleNode('t:Output/t:ErrorInfo/t:StackTrace', $ns)
        [ordered]@{
            name = $_.testName
            outcome = $_.outcome
            duration = $_.duration
            message = if ($messageNode) { $messageNode.InnerText } else { $null }
            stackTrace = if ($stackNode) { $stackNode.InnerText } else { $null }
        }
    })
    return [ordered]@{
        total = $counters.total
        executed = $counters.executed
        passed = $counters.passed
        failed = $counters.failed
        notExecuted = $counters.notExecuted
        skippedAttr = $counters.skipped
        outcomes = $results
    }
}

$dllJson = Join-Path $out 'dll-vs-source.json'
$useNoBuild = $false
if (Test-Path -LiteralPath $dllJson) {
    $dllState = Get-Content -LiteralPath $dllJson -Raw | ConvertFrom-Json
    $useNoBuild = [bool]$dllState.dllNewerOrEqual
}

$listArgs = @('test', 'tests/McpServer.PluginIntegration.Tests', '-c', 'Debug', '--list-tests', '--filter', $filter)
if ($useNoBuild) { $listArgs += '--no-build' } else { $listArgs += '-m:1' }

$swList = [System.Diagnostics.Stopwatch]::StartNew()
& dotnet @listArgs *>&1 | Tee-Object -FilePath $listLog | Out-Null
$listExit = $LASTEXITCODE
$swList.Stop()
$listText = Get-Content -LiteralPath $listLog -Raw
$listed = @([regex]::Matches($listText, 'PluginUpdateServiceHarnessTests\.\S+') | ForEach-Object { $_.Value } | Select-Object -Unique)
[ordered]@{
    command = ('dotnet ' + ($listArgs -join ' '))
    exitCode = $listExit
    durationMs = $swList.ElapsedMilliseconds
    listedTests = $listed
    listedCount = @($listed).Count
    usedNoBuild = $useNoBuild
} | ConvertTo-Json -Depth 5 | Set-Content -LiteralPath (Join-Path $out 'hv-dotnet-list-p20-exit.json') -Encoding utf8

$runArgs = @('test', 'tests/McpServer.PluginIntegration.Tests', '-c', 'Debug', '--filter', $filter, '--logger', 'trx;LogFileName=p20-red.trx', '--results-directory', $resultsDir)
if ($useNoBuild) { $runArgs += '--no-build' } else { $runArgs += '-m:1' }

$sw = [System.Diagnostics.Stopwatch]::StartNew()
& dotnet @runArgs *>&1 | Tee-Object -FilePath $consoleLog | Out-Null
$exit = $LASTEXITCODE
$sw.Stop()
$console = Get-Content -LiteralPath $consoleLog -Raw
$counts = Parse-ConsoleCounts -Console $console
$trxSummary = Parse-Trx -Path $trx
if ($trxSummary) {
    $trxSummary | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath (Join-Path $out 'p20-trx-summary.json') -Encoding utf8
}

[ordered]@{
    command = ('dotnet ' + ($runArgs -join ' '))
    exitCode = $exit
    durationMs = $sw.ElapsedMilliseconds
    usedNoBuild = $useNoBuild
    consoleFailed = $counts.failed
    consolePassed = $counts.passed
    consoleSkipped = $counts.skipped
    consoleTotal = $counts.total
    trxPath = $trx
    trxExists = (Test-Path -LiteralPath $trx)
} | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $out 'hv-dotnet-p20-filter-exit.json') -Encoding utf8

Write-Output ("TEST_EXIT=$exit FAILED=$($counts.failed) PASSED=$($counts.passed) SKIPPED=$($counts.skipped) TOTAL=$($counts.total) NOBUILD=$useNoBuild")
