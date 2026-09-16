#Requires -Version 7.0
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Continue'

$out = 'F:\GitHub\McpServer\docs\receipts\_hv-c-red-p16\hv-self'
$done = Join-Path $out 'hv-tests-done.json'
$deadline = (Get-Date).AddMinutes(6)
$pidInfo = $null
$pidPath = Join-Path $out 'hv-tests-run.pid.json'
if (Test-Path $pidPath) {
    $pidInfo = Get-Content -LiteralPath $pidPath -Raw | ConvertFrom-Json
}

while ((Get-Date) -lt $deadline) {
    if (Test-Path -LiteralPath $done) {
        Write-Output 'WAIT_FOUND'
        Get-Content -LiteralPath $done
        exit 0
    }
    $alive = $false
    if ($null -ne $pidInfo -and $pidInfo.ProcessId) {
        $alive = $null -ne (Get-Process -Id ([int]$pidInfo.ProcessId) -ErrorAction SilentlyContinue)
    }
    Write-Output ("WAIT_TICK alive=$alive utc=" + [DateTime]::UtcNow.ToString('o'))
    Start-Sleep -Seconds 3
}

Write-Output 'WAIT_TIMEOUT'
exit 1
