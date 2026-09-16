#Requires -Version 7.0
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Continue'

$out = 'F:\GitHub\McpServer\docs\receipts\_hv-c-green-p14'
$sleepSec = 0
if ($args.Count -ge 1) { $sleepSec = [int]$args[0] }
if ($sleepSec -gt 0) {
    Start-Sleep -Seconds $sleepSec
}

Write-Output ('NOW=' + [DateTime]::UtcNow.ToString('o'))
$pidFile = Join-Path $out 'hv-tests-run.pid.json'
if (Test-Path -LiteralPath $pidFile) {
    Write-Output '---PID-FILE---'
    Get-Content -LiteralPath $pidFile -Raw
    $j = Get-Content -LiteralPath $pidFile -Raw | ConvertFrom-Json
    $id = $null
    if ($j.PSObject.Properties.Name -contains 'ProcessId') { $id = [int]$j.ProcessId }
    elseif ($j.PSObject.Properties.Name -contains 'Pid') { $id = [int]$j.Pid }
    if ($null -ne $id) {
        $proc = Get-Process -Id $id -ErrorAction SilentlyContinue
        if ($proc) { Write-Output ('PID_ALIVE=true Id=' + $id + ' Name=' + $proc.ProcessName) } else { Write-Output ('PID_ALIVE=false Id=' + $id) }
    }
}
$transcript = Join-Path $out 'hv-tests-run.transcript.log'
if (Test-Path -LiteralPath $transcript) {
    Write-Output '---TRANSCRIPT-TAIL---'
    Get-Content -LiteralPath $transcript -Tail 20
}
foreach ($name in @('hv-dotnet-list-tests-exit.json','hv-dotnet-p14-filter-exit.json','hv-dotnet-pluginint-all-exit.json')) {
    $p = Join-Path $out $name
    if (Test-Path -LiteralPath $p) {
        Write-Output ('---' + $name + '---')
        Get-Content -LiteralPath $p -Raw
    } else {
        Write-Output ('MISSING ' + $name)
    }
}
foreach ($pair in @(
    @{ n = 'FILTER_LOG'; p = (Join-Path $out 'hv-dotnet-p14-filter.log') },
    @{ n = 'FULL_LOG'; p = (Join-Path $out 'hv-dotnet-pluginint-all.log') }
)) {
    if (Test-Path -LiteralPath $pair.p) {
        $item = Get-Item -LiteralPath $pair.p
        Write-Output ($pair.n + ' len=' + $item.Length + ' utc=' + $item.LastWriteTimeUtc.ToString('o'))
        $t = Get-Content -LiteralPath $pair.p -Raw
        $lines = @(($t -split "`n") | Where-Object { $_ -match 'Passed!|Failed!' } | ForEach-Object { $_.Trim() })
        if ($lines.Count -gt 0) {
            Write-Output ($pair.n + '_SUMMARY:')
            $lines | ForEach-Object { Write-Output $_ }
        }
    }
}
