#Requires -Version 7.0
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Continue'

$out = 'F:\GitHub\McpServer\docs\receipts\_hv-c-red-p11'
New-Item -ItemType Directory -Force -Path $out | Out-Null
Set-Location -LiteralPath 'F:\GitHub\McpServer'

$restLog = Join-Path $out 'dotnet-pluginintegration-rest-nobuild.log'
$restTrx = Join-Path $out 'dotnet-pluginintegration-rest-nobuild.trx'
if (Test-Path -LiteralPath $restTrx) { Remove-Item -LiteralPath $restTrx -Force }

$restArgs = @(
    'test', 'tests/McpServer.PluginIntegration.Tests', '-c', 'Debug',
    '--no-build',
    '--filter', 'FullyQualifiedName!~Theory_EachScenario_FailsUntilAdapterOperational',
    '--logger', "trx;LogFileName=$restTrx"
)

Write-Output 'START_REST_NOBUILD_TEST'
$sw2 = [System.Diagnostics.Stopwatch]::StartNew()
& dotnet @restArgs *>&1 | Tee-Object -FilePath $restLog
$restExit = $LASTEXITCODE
$sw2.Stop()
[ordered]@{
    Command = 'dotnet test tests/McpServer.PluginIntegration.Tests -c Debug --no-build --filter FullyQualifiedName!~Theory_EachScenario_FailsUntilAdapterOperational'
    ExitCode = $restExit
    DurationMs = $sw2.ElapsedMilliseconds
    Log = $restLog
    Trx = $restTrx
} | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $out 'dotnet-pluginintegration-rest-nobuild-exit.json') -Encoding utf8
Write-Output ("REST_NOBUILD_EXIT=$restExit DURATION_MS=$($sw2.ElapsedMilliseconds)")
Write-Output 'REST_NOBUILD_DONE'
