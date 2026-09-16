#Requires -Version 7.0
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Continue'

$out = 'F:\GitHub\McpServer\docs\receipts\_hv-c-red-p15\hv2'
New-Item -ItemType Directory -Force -Path $out | Out-Null
Set-Location -LiteralPath 'F:\GitHub\McpServer'
Write-Output ("RUN3_START=" + [DateTime]::UtcNow.ToString('o'))

$filterTrxDir = Join-Path $out 'trx-p15-run3'
if (Test-Path -LiteralPath $filterTrxDir) { Remove-Item -LiteralPath $filterTrxDir -Recurse -Force }
New-Item -ItemType Directory -Force -Path $filterTrxDir | Out-Null
$filterLog = Join-Path $out 'dotnet-p15-filter-run3.log'
$filterExpr = 'FullyQualifiedName~Theory_Agent_Success_NoPendingFailsafe|FullyQualifiedName~Theory_Agent_FailedSubmit_RetainsRootIdPending|FullyQualifiedName~Theory_Agent_RetrySuccess_DeletesOnlyMatchingPending'
$swFilter = [System.Diagnostics.Stopwatch]::StartNew()
$filterArgs = @(
    'test', 'tests/McpServer.PluginIntegration.Tests', '-c', 'Debug',
    '--filter', $filterExpr,
    '--logger', 'trx',
    '--logger', 'console',
    '--results-directory', $filterTrxDir
)
& dotnet @filterArgs *>&1 | Tee-Object -FilePath $filterLog
$filterExit = $LASTEXITCODE
$swFilter.Stop()
[ordered]@{
    Command = "dotnet test tests/McpServer.PluginIntegration.Tests -c Debug --filter `"$filterExpr`""
    ExitCode = $filterExit
    DurationMs = $swFilter.ElapsedMilliseconds
    Log = $filterLog
    ResultsDirectory = $filterTrxDir
    FinishedUtc = [DateTime]::UtcNow.ToString('o')
} | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $out 'dotnet-p15-filter-run3-exit.json') -Encoding utf8
Write-Output ("RUN3_EXIT=$filterExit")
Write-Output ("RUN3_END=" + [DateTime]::UtcNow.ToString('o'))
