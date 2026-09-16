#Requires -Version 7.0
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Continue'
$out = 'F:\GitHub\McpServer\docs\receipts\_hv-c-green-p15\full-rerun'
$deadline = [DateTime]::UtcNow.AddMinutes(25)
while ([DateTime]::UtcNow -lt $deadline) {
    $exitFile = Join-Path $out 'hv-dotnet-pluginint-all-exit.json'
    $trx = Join-Path $out 'trx-all-hv\pluginint-all.trx'
    $log = Join-Path $out 'hv-dotnet-pluginint-all.log'
    $transcript = Join-Path $out 'hv-tests-run.transcript.log'
    $alive = $false
    $pidFile = Join-Path $out 'hv-tests-run.pid.json'
    $wmiPid = $null
    if (Test-Path -LiteralPath $pidFile) {
        $j = Get-Content -LiteralPath $pidFile -Raw | ConvertFrom-Json
        $wmiPid = [int]$j.ProcessId
        $alive = $null -ne (Get-Process -Id $wmiPid -ErrorAction SilentlyContinue)
    }
    $len = 0
    $lw = $null
    if (Test-Path -LiteralPath $log) {
        $i = Get-Item -LiteralPath $log
        $len = $i.Length
        $lw = $i.LastWriteTimeUtc.ToString('o')
    }
    $done = $false
    if (Test-Path -LiteralPath $transcript) {
        $t = Get-Content -LiteralPath $transcript -Raw
        if ($t -match 'HV_FULL_DONE') { $done = $true }
    }
    Write-Output ("WAIT alive=$alive pid=$wmiPid logLen=$len logUtc=$lw done=$done now=" + [DateTime]::UtcNow.ToString('o'))
    if ($done -and (Test-Path -LiteralPath $exitFile) -and (Test-Path -LiteralPath $trx)) {
        Write-Output ("FULL_READY " + [DateTime]::UtcNow.ToString('o'))
        Get-Content -LiteralPath $exitFile
        exit 0
    }
    Start-Sleep -Seconds 20
}
Write-Output 'FULL_NOT_READY'
exit 2
