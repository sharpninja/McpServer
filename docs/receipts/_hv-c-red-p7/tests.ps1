#Requires -Version 7.0
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Continue'

$out = 'F:\GitHub\McpServer\docs\receipts\_hv-c-red-p7'
New-Item -ItemType Directory -Force -Path $out | Out-Null
Set-Location -LiteralPath 'F:\GitHub\McpServer'

$filterLog = Join-Path $out 'dotnet-adapter-filter.log'
$filterTrx = Join-Path $out 'dotnet-adapter-filter.trx'
$allLog = Join-Path $out 'dotnet-pluginintegration-all.log'
$allTrx = Join-Path $out 'dotnet-pluginintegration-all.trx'

if (Test-Path -LiteralPath $filterTrx) { Remove-Item -LiteralPath $filterTrx -Force }
if (Test-Path -LiteralPath $allTrx) { Remove-Item -LiteralPath $allTrx -Force }

$filterArgs = @(
    'test', 'tests/McpServer.PluginIntegration.Tests', '-c', 'Debug',
    '--filter', 'FullyQualifiedName~Adapter_CapturesExecutableArgsStdinEnvCwdTimeoutExitStdoutStderr',
    '--logger', "trx;LogFileName=$filterTrx"
)
$allArgs = @(
    'test', 'tests/McpServer.PluginIntegration.Tests', '-c', 'Debug',
    '--logger', "trx;LogFileName=$allTrx"
)

Write-Output 'START_FILTER_TEST'
$sw = [System.Diagnostics.Stopwatch]::StartNew()
& dotnet @filterArgs *>&1 | Tee-Object -FilePath $filterLog
$filterExit = $LASTEXITCODE
$sw.Stop()
[ordered]@{
    Command = 'dotnet test tests/McpServer.PluginIntegration.Tests -c Debug --filter FullyQualifiedName~Adapter_CapturesExecutableArgsStdinEnvCwdTimeoutExitStdoutStderr'
    ExitCode = $filterExit
    DurationMs = $sw.ElapsedMilliseconds
    Log = $filterLog
    Trx = $filterTrx
} | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $out 'dotnet-adapter-filter-exit.json') -Encoding utf8
Write-Output ("FILTER_EXIT=$filterExit")

Write-Output 'START_ALL_TEST'
$sw2 = [System.Diagnostics.Stopwatch]::StartNew()
& dotnet @allArgs *>&1 | Tee-Object -FilePath $allLog
$allExit = $LASTEXITCODE
$sw2.Stop()
[ordered]@{
    Command = 'dotnet test tests/McpServer.PluginIntegration.Tests -c Debug'
    ExitCode = $allExit
    DurationMs = $sw2.ElapsedMilliseconds
    Log = $allLog
    Trx = $allTrx
} | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $out 'dotnet-pluginintegration-all-exit.json') -Encoding utf8
Write-Output ("ALL_EXIT=$allExit")
Write-Output 'TESTS_DONE'
