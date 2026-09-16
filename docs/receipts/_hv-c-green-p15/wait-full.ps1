#Requires -Version 7.0
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Continue'
$out = 'F:\GitHub\McpServer\docs\receipts\_hv-c-green-p15'
$deadline = [DateTime]::UtcNow.AddMinutes(22)
while ([DateTime]::UtcNow -lt $deadline) {
    $exitFile = Join-Path $out 'hv-dotnet-pluginint-all-exit.json'
    $trx = Join-Path $out 'trx-all-hv\pluginint-all.trx'
    $log = Join-Path $out 'hv-dotnet-pluginint-all.log'
    $transcript = Join-Path $out 'hv-tests-run.transcript.log'
    if ((Test-Path -LiteralPath $exitFile) -and (Test-Path -LiteralPath $trx)) {
        $t = Get-Content -LiteralPath $transcript -Raw -ErrorAction SilentlyContinue
        if ($t -and $t -match 'HV_TESTS_DONE') {
            Write-Output ("FULL_READY " + [DateTime]::UtcNow.ToString('o'))
            Get-Content -LiteralPath $exitFile
            break
        }
        Write-Output ("FULL_TRX_PRESENT_WAIT_DONE_MARKER " + [DateTime]::UtcNow.ToString('o'))
    }
    $len = 0
    $lw = $null
    if (Test-Path -LiteralPath $log) {
        $i = Get-Item -LiteralPath $log
        $len = $i.Length
        $lw = $i.LastWriteTimeUtc.ToString('o')
    }
    $alive = $false
    $pidFile = Join-Path $out 'hv-tests-run.pid.json'
    if (Test-Path -LiteralPath $pidFile) {
        $j = Get-Content -LiteralPath $pidFile -Raw | ConvertFrom-Json
        $alive = $null -ne (Get-Process -Id ([int]$j.ProcessId) -ErrorAction SilentlyContinue)
    }
    Write-Output ("WAIT full alive=$alive logLen=$len logUtc=$lw now=" + [DateTime]::UtcNow.ToString('o'))
    Start-Sleep -Seconds 30
}
if (-not (Test-Path -LiteralPath (Join-Path $out 'hv-dotnet-pluginint-all-exit.json'))) {
    Write-Output 'FULL_NOT_READY'
    exit 2
}
if (-not (Test-Path -LiteralPath (Join-Path $out 'trx-all-hv\pluginint-all.trx'))) {
    Write-Output 'FULL_TRX_MISSING'
    exit 3
}
