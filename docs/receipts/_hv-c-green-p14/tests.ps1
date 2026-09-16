#Requires -Version 7.0
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Continue'

$out = 'F:\GitHub\McpServer\docs\receipts\_hv-c-green-p14'
New-Item -ItemType Directory -Force -Path $out | Out-Null
Set-Location -LiteralPath 'F:\GitHub\McpServer'

$listLog = Join-Path $out 'dotnet-list-tests.log'
$filterLog = Join-Path $out 'dotnet-p14-filter.log'
$filterTrxDir = Join-Path $out 'trx-p14'
New-Item -ItemType Directory -Force -Path $filterTrxDir | Out-Null
$filterTrx = Join-Path $filterTrxDir 'p14-filter.trx'

if (Test-Path -LiteralPath $filterTrx) { Remove-Item -LiteralPath $filterTrx -Force }

Write-Output ("START_UTC=" + [DateTime]::UtcNow.ToString('o'))
Write-Output 'START_LIST_TESTS'
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
} | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $out 'dotnet-list-tests-exit.json') -Encoding utf8
Write-Output ("LIST_EXIT=$listExit")

Write-Output 'START_P14_FILTER'
$sw = [System.Diagnostics.Stopwatch]::StartNew()
$filterArgs = @(
    'test', 'tests/McpServer.PluginIntegration.Tests', '-c', 'Debug',
    '--filter', 'FullyQualifiedName~Theory_Agent_PluginRootOverride_WritesOnlyWorkspaceMcpServerAgent_PoisonEmpty',
    '--logger', "trx;LogFileName=$filterTrx"
)
& dotnet @filterArgs *>&1 | Tee-Object -FilePath $filterLog
$filterExit = $LASTEXITCODE
$sw.Stop()
[ordered]@{
    Command = 'dotnet test tests/McpServer.PluginIntegration.Tests -c Debug --filter FullyQualifiedName~Theory_Agent_PluginRootOverride_WritesOnlyWorkspaceMcpServerAgent_PoisonEmpty'
    ExitCode = $filterExit
    DurationMs = $sw.ElapsedMilliseconds
    Log = $filterLog
    Trx = $filterTrx
    TrxExists = (Test-Path -LiteralPath $filterTrx)
    FinishedUtc = [DateTime]::UtcNow.ToString('o')
} | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $out 'dotnet-p14-filter-exit.json') -Encoding utf8
Write-Output ("FILTER_EXIT=$filterExit")
Write-Output ("END_UTC=" + [DateTime]::UtcNow.ToString('o'))
Write-Output 'TESTS_DONE'
