#Requires -Version 7.0
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Continue'

$out = 'F:\GitHub\McpServer\docs\receipts\_hv-c-green-p11-p13'
New-Item -ItemType Directory -Force -Path $out | Out-Null
Set-Location -LiteralPath 'F:\GitHub\McpServer'

function Get-PluginIntHosts {
    @(Get-CimInstance Win32_Process | Where-Object {
        $cmd = [string]$_.CommandLine
        $cmd -match 'McpServer\.PluginIntegration\.Tests' -or $cmd -match 'pluginintegration-all\.trx' -or $cmd -match 'trx-all-20260822T034054Z'
    })
}

$waitStarted = [DateTime]::UtcNow
$deadline = $waitStarted.AddMinutes(15)
while ([DateTime]::UtcNow -lt $deadline) {
    $hosts = Get-PluginIntHosts
    $count = @($hosts).Count
    $line = ([DateTime]::UtcNow.ToString('o')) + " HOSTS=$count"
    Add-Content -LiteralPath (Join-Path $out 'wait-filter-hosts.log') -Value $line
    if ($count -eq 0) { break }
    Start-Sleep -Seconds 20
}

$remaining = @(Get-PluginIntHosts).Count
[ordered]@{
    WaitStartedUtc = $waitStarted.ToString('o')
    WaitEndedUtc = [DateTime]::UtcNow.ToString('o')
    RemainingHostCount = $remaining
} | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $out 'wait-filter-hosts.json') -Encoding utf8

if ($remaining -gt 0) {
    Write-Output "WAIT_TIMEOUT remaining=$remaining"
}

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
