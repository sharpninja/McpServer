#Requires -Version 7.0
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Continue'

$out = 'F:\GitHub\McpServer\docs\receipts\_hv-c-red-p19'
New-Item -ItemType Directory -Force -Path $out | Out-Null
Set-Location -LiteralPath 'F:\GitHub\McpServer'
$transcript = Join-Path $out 'hv-wait-then-run.transcript.log'
try { Stop-Transcript | Out-Null } catch { }
Start-Transcript -Path $transcript -Force | Out-Null

Write-Output ("START_WAIT=" + [DateTime]::UtcNow.ToString('o'))
$waitStart = [DateTime]::UtcNow
$clear = $false
for ($i = 0; $i -lt 48; $i++) {
    $busy = @(Get-CimInstance Win32_Process -ErrorAction SilentlyContinue |
        Where-Object { $_.Name -match 'testhost|vstest' -and $_.CommandLine -match 'PluginIntegration' })
    [ordered]@{
        TimestampUtc = [DateTime]::UtcNow.ToString('o')
        Iteration = $i
        BusyCount = $busy.Count
        Pids = @($busy | ForEach-Object { $_.ProcessId })
    } | ConvertTo-Json | Add-Content -LiteralPath (Join-Path $out 'wait-leftover-r2.jsonl') -Encoding utf8
    Write-Output ("WAIT i=$i busy=$($busy.Count)")
    if ($busy.Count -eq 0) {
        $clear = $true
        break
    }
    Start-Sleep -Seconds 15
}
[ordered]@{
    TimestampUtc = [DateTime]::UtcNow.ToString('o')
    WaitedSeconds = ([DateTime]::UtcNow - $waitStart).TotalSeconds
    MachineClear = $clear
} | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $out 'wait-clear.json') -Encoding utf8
Write-Output ("MACHINE_CLEAR=$clear WAIT_S=" + ([DateTime]::UtcNow - $waitStart).TotalSeconds)

if (-not $clear) {
    Write-Output 'STILL_BUSY_RUNNING_FILTER_ANYWAY'
}

& (Join-Path $out 'tests-run-nowait.ps1')
Write-Output ("END_WAIT_THEN_RUN=" + [DateTime]::UtcNow.ToString('o'))
try { Stop-Transcript | Out-Null } catch { }
