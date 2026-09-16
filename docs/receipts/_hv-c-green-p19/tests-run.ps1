#Requires -Version 7.0
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Continue'

$out = 'F:\GitHub\McpServer\docs\receipts\_hv-c-green-p19'
New-Item -ItemType Directory -Force -Path $out | Out-Null
Set-Location -LiteralPath 'F:\GitHub\McpServer'

$resultsDir = Join-Path $out 'results-p19-green'
New-Item -ItemType Directory -Force -Path $resultsDir | Out-Null
$trx = Join-Path $resultsDir 'p19-green.trx'
$consoleLog = Join-Path $out 'hv-dotnet-p19-filter.log'
$listLog = Join-Path $out 'hv-dotnet-list-p19.log'

$swList = [System.Diagnostics.Stopwatch]::StartNew()
& dotnet test 'tests/McpServer.PluginIntegration.Tests' -c Debug --list-tests --filter 'FullyQualifiedName~PluginNativeSuiteReceiptTests' *>&1 |
    Tee-Object -FilePath $listLog | Out-Null
$listExit = $LASTEXITCODE
$swList.Stop()
[ordered]@{
    command = 'dotnet test tests/McpServer.PluginIntegration.Tests -c Debug --list-tests --filter FullyQualifiedName~PluginNativeSuiteReceiptTests'
    exitCode = $listExit
    durationMs = $swList.ElapsedMilliseconds
} | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $out 'hv-dotnet-list-p19-exit.json') -Encoding utf8

$sw = [System.Diagnostics.Stopwatch]::StartNew()
& dotnet test 'tests/McpServer.PluginIntegration.Tests' -c Debug --filter 'FullyQualifiedName~PluginNativeSuiteReceiptTests' --logger "trx;LogFileName=p19-green.trx" --results-directory $resultsDir *>&1 |
    Tee-Object -FilePath $consoleLog | Out-Null
$exit = $LASTEXITCODE
$sw.Stop()

$console = Get-Content -LiteralPath $consoleLog -Raw
$total = $null; $failed = $null; $passed = $null; $skipped = $null; $totalTime = $null
if ($console -match 'Total tests:\s*(\d+)') { $total = [int]$Matches[1] }
if ($console -match 'Failed:\s*(\d+)') { $failed = [int]$Matches[1] }
if ($console -match 'Passed:\s*(\d+)') { $passed = [int]$Matches[1] }
if ($console -match 'Skipped:\s*(\d+)') { $skipped = [int]$Matches[1] }
if ($console -match 'Total time:\s*([\d.]+)\s*Seconds') { $totalTime = [double]$Matches[1] }

$trxSummary = $null
if (Test-Path -LiteralPath $trx) {
    [xml]$trxXml = Get-Content -LiteralPath $trx -Raw
    $ns = New-Object System.Xml.XmlNamespaceManager($trxXml.NameTable)
    $ns.AddNamespace('t', 'http://microsoft.com/schemas/VisualStudio/TeamTest/2010')
    $counters = $trxXml.SelectSingleNode('//t:ResultSummary/t:Counters', $ns)
    $names = @($trxXml.SelectNodes('//t:UnitTestResult', $ns) | ForEach-Object { $_.testName })
    $outcomes = @($trxXml.SelectNodes('//t:UnitTestResult', $ns) | ForEach-Object {
        [ordered]@{ name = $_.testName; outcome = $_.outcome }
    })
    $trxSummary = [ordered]@{
        total = $counters.total
        executed = $counters.executed
        passed = $counters.passed
        failed = $counters.failed
        notExecuted = $counters.notExecuted
        skippedAttr = $counters.skipped
        names = $names
        outcomes = $outcomes
        p20Count = @($names | Where-Object { $_ -match 'PluginSessionLogHarness_AgainstUpdateService_SanitizedFixtures_FailedZeroSkippedZero|PluginPromotion_StagingOrProduction_RequiresOperatorApprovalFlag' }).Count
    }
    $trxSummary | ConvertTo-Json -Depth 6 | Set-Content -LiteralPath (Join-Path $out 'p19-trx-summary.json') -Encoding utf8
}

[ordered]@{
    command = 'dotnet test tests/McpServer.PluginIntegration.Tests -c Debug --filter FullyQualifiedName~PluginNativeSuiteReceiptTests'
    exitCode = $exit
    durationMs = $sw.ElapsedMilliseconds
    consoleTotal = $total
    consoleFailed = $failed
    consolePassed = $passed
    consoleSkipped = $skipped
    consoleTotalTimeSeconds = $totalTime
    trxPath = $trx
    trxExists = (Test-Path -LiteralPath $trx)
} | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $out 'hv-dotnet-p19-filter-exit.json') -Encoding utf8

Write-Output ("TEST_EXIT=$exit TOTAL=$total FAILED=$failed PASSED=$passed SKIPPED=$skipped")
