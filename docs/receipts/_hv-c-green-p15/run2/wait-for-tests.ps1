#Requires -Version 7.0
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Continue'

$out = 'F:\GitHub\McpServer\docs\receipts\_hv-c-green-p15\run2'
$deadline = [DateTime]::UtcNow.AddMinutes(14)
$pidFile = Join-Path $out 'hv-tests-run.pid.json'
$exitFile = Join-Path $out 'hv-dotnet-p15-filter-exit.json'
$transcript = Join-Path $out 'hv-tests-run.transcript.log'
$filterLog = Join-Path $out 'hv-dotnet-p15-filter.log'

$wmiPid = $null
if (Test-Path -LiteralPath $pidFile) {
    $j = Get-Content -LiteralPath $pidFile -Raw | ConvertFrom-Json
    $wmiPid = [int]$j.ProcessId
}

while ([DateTime]::UtcNow -lt $deadline) {
    $alive = $false
    if ($wmiPid) { $alive = $null -ne (Get-Process -Id $wmiPid -ErrorAction SilentlyContinue) }
    $len = 0
    $lw = 'missing'
    if (Test-Path -LiteralPath $filterLog) {
        $i = Get-Item -LiteralPath $filterLog
        $len = $i.Length
        $lw = $i.LastWriteTimeUtc.ToString('o')
    }
    $done = $false
    if (Test-Path -LiteralPath $transcript) {
        $t = Get-Content -LiteralPath $transcript -Raw -ErrorAction SilentlyContinue
        if ($t -and $t -match 'HV_TESTS_DONE') { $done = $true }
    }
    Write-Output ('WAIT alive=' + $alive + ' exitFile=' + (Test-Path -LiteralPath $exitFile) + ' done=' + $done + ' logLen=' + $len + ' logUtc=' + $lw + ' now=' + [DateTime]::UtcNow.ToString('o'))
    if ($done -and (Test-Path -LiteralPath $exitFile)) { break }
    if ((-not $alive) -and (Test-Path -LiteralPath $exitFile)) { break }
    Start-Sleep -Seconds 20
}

if (Test-Path -LiteralPath $exitFile) {
    Write-Output '--- EXIT JSON ---'
    Get-Content -LiteralPath $exitFile -Raw
} else {
    Write-Output 'EXIT_JSON_MISSING'
}

if (Test-Path -LiteralPath $filterLog) {
    Write-Output '--- FILTER TAIL ---'
    Get-Content -LiteralPath $filterLog -Tail 20
}

& (Join-Path $out 'parse-trx.ps1')
Write-Output 'WAIT_FOR_TESTS_END'
