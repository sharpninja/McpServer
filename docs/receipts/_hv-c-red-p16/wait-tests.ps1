#Requires -Version 7.0
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Continue'

$out = 'F:\GitHub\McpServer\docs\receipts\_hv-c-red-p16'
$done = Join-Path $out 'hv-tests-done.json'
$deadline = (Get-Date).AddMinutes(8)
$pidInfo = $null
$pidPath = Join-Path $out 'hv-tests-run.pid.json'
if (Test-Path $pidPath) {
    $pidInfo = Get-Content -LiteralPath $pidPath -Raw | ConvertFrom-Json
}

while ((Get-Date) -lt $deadline) {
    if (Test-Path -LiteralPath $done) {
        [ordered]@{
            TimestampUtc = [DateTime]::UtcNow.ToString('o')
            Found = $true
            Path = $done
        } | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $out 'wait-tests-status.json') -Encoding utf8
        Write-Output 'WAIT_FOUND'
        Get-Content -LiteralPath $done
        exit 0
    }
    $alive = $false
    if ($null -ne $pidInfo -and $pidInfo.ProcessId) {
        $alive = $null -ne (Get-Process -Id ([int]$pidInfo.ProcessId) -ErrorAction SilentlyContinue)
    }
    Start-Sleep -Seconds 2
}

[ordered]@{
    TimestampUtc = [DateTime]::UtcNow.ToString('o')
    Found = $false
    Path = $done
} | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $out 'wait-tests-status.json') -Encoding utf8
Write-Output 'WAIT_TIMEOUT'
exit 1
