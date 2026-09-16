#Requires -Version 7.0
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Continue'

$out = 'F:\GitHub\McpServer\docs\receipts\_hv-c-green-p11-p13'
New-Item -ItemType Directory -Force -Path $out | Out-Null
Set-Location -LiteralPath 'F:\GitHub\McpServer'

$filterLog = Join-Path $out 'dotnet-adapter-filter-rerun.log'
$filterTrx = Join-Path $out 'dotnet-adapter-filter-rerun.trx'
if (Test-Path -LiteralPath $filterTrx) { Remove-Item -LiteralPath $filterTrx -Force }

$filterArgs = @(
    'test', 'tests/McpServer.PluginIntegration.Tests', '-c', 'Debug',
    '--no-build',
    '--filter', 'FullyQualifiedName~PluginSessionLogWorkflowAdapterTests',
    '--logger', "trx;LogFileName=$filterTrx",
    '--nologo'
)

Write-Output ("START_UTC=" + [DateTime]::UtcNow.ToString('o'))
$sw = [System.Diagnostics.Stopwatch]::StartNew()
& dotnet @filterArgs > $filterLog 2>&1
$filterExit = $LASTEXITCODE
$sw.Stop()
[ordered]@{
    Command = 'dotnet test tests/McpServer.PluginIntegration.Tests -c Debug --no-build --filter FullyQualifiedName~PluginSessionLogWorkflowAdapterTests'
    ExitCode = $filterExit
    DurationMs = $sw.ElapsedMilliseconds
    Log = $filterLog
    Trx = $filterTrx
    TrxExists = (Test-Path -LiteralPath $filterTrx)
    FinishedUtc = [DateTime]::UtcNow.ToString('o')
} | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $out 'dotnet-adapter-filter-rerun-exit.json') -Encoding utf8
Write-Output ("FILTER_RERUN_EXIT=$filterExit")
Write-Output ("END_UTC=" + [DateTime]::UtcNow.ToString('o'))
