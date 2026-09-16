#Requires -Version 7.0
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Continue'

$out = 'F:\GitHub\McpServer\docs\receipts\_hv-c-green-p15\run2'
New-Item -ItemType Directory -Force -Path $out | Out-Null
Set-Location -LiteralPath 'F:\GitHub\McpServer'
$transcript = Join-Path $out 'hv-tests-run.transcript.log'
try { Stop-Transcript | Out-Null } catch { }
Start-Transcript -Path $transcript -Force | Out-Null

Write-Output ("START_TESTS=" + [DateTime]::UtcNow.ToString('o'))

$listLog = Join-Path $out 'hv-dotnet-list-tests.log'
$swList = [System.Diagnostics.Stopwatch]::StartNew()
& dotnet test tests/McpServer.PluginIntegration.Tests -c Debug --list-tests *>&1 | Tee-Object -FilePath $listLog
$listExit = $LASTEXITCODE
$swList.Stop()
[ordered]@{
    Command = 'dotnet test tests/McpServer.PluginIntegration.Tests -c Debug --list-tests'
    ExitCode = $listExit
    DurationMs = $swList.ElapsedMilliseconds
    Log = $listLog
    FinishedUtc = [DateTime]::UtcNow.ToString('o')
} | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $out 'hv-dotnet-list-tests-exit.json') -Encoding utf8
Write-Output ("LIST_EXIT=$listExit")

$stamp = [DateTime]::UtcNow.ToString('yyyyMMddTHHmmssZ')
$resultsDir = Join-Path $out ("results-" + $stamp)
New-Item -ItemType Directory -Force -Path $resultsDir | Out-Null
$filterLog = Join-Path $out 'hv-dotnet-p15-filter.log'
$filterExpr = 'FullyQualifiedName~Theory_Agent_Success_NoPendingFailsafe|FullyQualifiedName~Theory_Agent_FailedSubmit_RetainsRootIdPending|FullyQualifiedName~Theory_Agent_RetrySuccess_DeletesOnlyMatchingPending'
$swFilter = [System.Diagnostics.Stopwatch]::StartNew()
$filterArgs = @(
    'test', 'tests/McpServer.PluginIntegration.Tests', '-c', 'Debug',
    '--filter', $filterExpr,
    '--logger', 'trx',
    '--logger', 'console',
    '--results-directory', $resultsDir
)
& dotnet @filterArgs *>&1 | Tee-Object -FilePath $filterLog
$filterExit = $LASTEXITCODE
$swFilter.Stop()
$trxFiles = @(Get-ChildItem -LiteralPath $resultsDir -Filter '*.trx' -Recurse -ErrorAction SilentlyContinue)
[ordered]@{
    Command = "dotnet test tests/McpServer.PluginIntegration.Tests -c Debug --filter `"$filterExpr`" --logger trx --logger console --results-directory $resultsDir"
    ExitCode = $filterExit
    DurationMs = $swFilter.ElapsedMilliseconds
    Log = $filterLog
    ResultsDirectory = $resultsDir
    TrxFiles = @($trxFiles | ForEach-Object { $_.FullName })
    TrxExists = ($trxFiles.Count -gt 0)
    FinishedUtc = [DateTime]::UtcNow.ToString('o')
} | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $out 'hv-dotnet-p15-filter-exit.json') -Encoding utf8
Write-Output ("FILTER_EXIT=$filterExit")
Write-Output ("RESULTS_DIR=$resultsDir")
Write-Output ("TRX_COUNT=" + $trxFiles.Count)
Write-Output ("END_UTC=" + [DateTime]::UtcNow.ToString('o'))
Write-Output 'HV_TESTS_DONE'
try { Stop-Transcript | Out-Null } catch { }
