#Requires -Version 7.0
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Continue'

$out = 'F:\GitHub\McpServer\docs\receipts\_hv-c-red-p15'
$doneMarker = Join-Path $out 'hv-dotnet-p15-filter-exit.json'
$transcript = Join-Path $out 'hv-tests-run.transcript.log'
$pidFile = Join-Path $out 'hv-tests-run.pid.json'
$deadline = [DateTime]::UtcNow.AddMinutes(18)
$pid = $null
if (Test-Path -LiteralPath $pidFile) {
    $pidObj = Get-Content -LiteralPath $pidFile -Raw | ConvertFrom-Json
    $pid = [int]$pidObj.ProcessId
}

while ([DateTime]::UtcNow -lt $deadline) {
    $alive = $false
    if ($pid) {
        $alive = $null -ne (Get-Process -Id $pid -ErrorAction SilentlyContinue)
    }
    $hasExit = Test-Path -LiteralPath $doneMarker
    $hasDone = $false
    if (Test-Path -LiteralPath $transcript) {
        $t = Get-Content -LiteralPath $transcript -Raw -ErrorAction SilentlyContinue
        $hasDone = ($t -match 'HV_TESTS_DONE')
    }
    if ($hasExit -and $hasDone) {
        Write-Output 'WAIT_DONE_FILES_PRESENT'
        Write-Output ("ALIVE=$alive")
        break
    }
    if (-not $alive -and $hasExit) {
        Write-Output 'WAIT_PROCESS_EXITED_WITH_FILTER_EXIT'
        break
    }
    Start-Sleep -Seconds 15
}

if (-not (Test-Path -LiteralPath $doneMarker)) {
    Write-Output 'WAIT_TIMEOUT_NO_FILTER_EXIT'
    [ordered]@{
        TimestampUtc = [DateTime]::UtcNow.ToString('o')
        Pid = $pid
        Alive = $(if ($pid) { $null -ne (Get-Process -Id $pid -ErrorAction SilentlyContinue) } else { $null })
        HasFilterExit = $false
    } | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $out 'wait-tests-status.json') -Encoding utf8
    exit 2
}

[ordered]@{
    TimestampUtc = [DateTime]::UtcNow.ToString('o')
    Pid = $pid
    Alive = $(if ($pid) { $null -ne (Get-Process -Id $pid -ErrorAction SilentlyContinue) } else { $null })
    HasFilterExit = $true
    TranscriptHasDone = (Test-Path $transcript) -and ((Get-Content -LiteralPath $transcript -Raw) -match 'HV_TESTS_DONE')
} | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $out 'wait-tests-status.json') -Encoding utf8
Write-Output 'WAIT_OK'
