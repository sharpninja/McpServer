#Requires -Version 7.0
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Continue'

$out = 'F:\GitHub\McpServer\docs\receipts\_hv-c-red-p14'
Set-Location -LiteralPath 'F:\GitHub\McpServer'
$filterLog = Join-Path $out 'dotnet-p14-filter-rerun.log'
$filterTrxDir = Join-Path $out 'trx-p14-rerun'
New-Item -ItemType Directory -Force -Path $filterTrxDir | Out-Null
$filterTrx = Join-Path $filterTrxDir 'p14-filter-rerun.trx'
if (Test-Path -LiteralPath $filterTrx) { Remove-Item -LiteralPath $filterTrx -Force }

Write-Output ("START_UTC=" + [DateTime]::UtcNow.ToString('o'))
$sw = [System.Diagnostics.Stopwatch]::StartNew()
$filterArgs = @(
    'test', 'tests/McpServer.PluginIntegration.Tests', '-c', 'Debug', '--no-build',
    '--filter', 'FullyQualifiedName~Theory_Agent_PluginRootOverride_WritesOnlyWorkspaceMcpServerAgent_PoisonEmpty',
    '--logger', "trx;LogFileName=$filterTrx"
)
& dotnet @filterArgs *>&1 | Tee-Object -FilePath $filterLog
$filterExit = $LASTEXITCODE
$sw.Stop()
[ordered]@{
    Command = 'dotnet test tests/McpServer.PluginIntegration.Tests -c Debug --no-build --filter FullyQualifiedName~Theory_Agent_PluginRootOverride_WritesOnlyWorkspaceMcpServerAgent_PoisonEmpty'
    ExitCode = $filterExit
    DurationMs = $sw.ElapsedMilliseconds
    Log = $filterLog
    Trx = $filterTrx
    FinishedUtc = [DateTime]::UtcNow.ToString('o')
} | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $out 'dotnet-p14-filter-rerun-exit.json') -Encoding utf8
Write-Output ("FILTER_EXIT=$filterExit")
Write-Output ("END_UTC=" + [DateTime]::UtcNow.ToString('o'))
Write-Output 'TESTS_RERUN_DONE'
