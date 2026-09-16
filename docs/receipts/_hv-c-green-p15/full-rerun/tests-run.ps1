#Requires -Version 7.0
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Continue'

$out = 'F:\GitHub\McpServer\docs\receipts\_hv-c-green-p15\full-rerun'
New-Item -ItemType Directory -Force -Path $out | Out-Null
Set-Location -LiteralPath 'F:\GitHub\McpServer'
$transcript = Join-Path $out 'hv-tests-run.transcript.log'
try { Stop-Transcript | Out-Null } catch { }
Start-Transcript -Path $transcript -Force | Out-Null

Write-Output ("START_FULL=" + [DateTime]::UtcNow.ToString('o'))

$fullTrxDir = Join-Path $out 'trx-all-hv'
New-Item -ItemType Directory -Force -Path $fullTrxDir | Out-Null
$fullTrx = Join-Path $fullTrxDir 'pluginint-all.trx'
if (Test-Path -LiteralPath $fullTrx) { Remove-Item -LiteralPath $fullTrx -Force }
$fullLog = Join-Path $out 'hv-dotnet-pluginint-all.log'
$swFull = [System.Diagnostics.Stopwatch]::StartNew()
$fullArgs = @(
    'test', 'tests/McpServer.PluginIntegration.Tests', '-c', 'Debug',
    '--logger', "trx;LogFileName=$fullTrx"
)
& dotnet @fullArgs *>&1 | Tee-Object -FilePath $fullLog
$fullExit = $LASTEXITCODE
$swFull.Stop()
[ordered]@{
    Command = 'dotnet test tests/McpServer.PluginIntegration.Tests -c Debug'
    ExitCode = $fullExit
    DurationMs = $swFull.ElapsedMilliseconds
    Log = $fullLog
    Trx = $fullTrx
    TrxExists = (Test-Path -LiteralPath $fullTrx)
    FinishedUtc = [DateTime]::UtcNow.ToString('o')
} | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $out 'hv-dotnet-pluginint-all-exit.json') -Encoding utf8
Write-Output ("FULL_EXIT=$fullExit")
Write-Output ("END_UTC=" + [DateTime]::UtcNow.ToString('o'))
Write-Output 'HV_FULL_DONE'
try { Stop-Transcript | Out-Null } catch { }
