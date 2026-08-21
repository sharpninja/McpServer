#Requires -Version 7.0
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Continue'
Set-Location -LiteralPath 'F:\GitHub\McpServer'
$out = 'F:\GitHub\McpServer\docs\receipts\_hv-s0-h0-reattack'
$utc = (Get-Date).ToUniversalTime().ToString('yyyy-MM-ddTHH:mm:ssZ')
$stamp = (Get-Date).ToUniversalTime().ToString('yyyyMMddTHHmmssZ')
Set-Content -LiteralPath (Join-Path $out 'receipt-utc.txt') -Value $utc -Encoding utf8
Set-Content -LiteralPath (Join-Path $out 'receipt-stamp.txt') -Value $stamp -Encoding utf8
& ./build.ps1 ValidateTraceability *>&1 | Set-Content -LiteralPath (Join-Path $out 'validate-traceability.txt') -Encoding utf8
Write-Output $utc
Write-Output $stamp
Get-Content -LiteralPath (Join-Path $out 'validate-traceability.txt') | Select-Object -Last 25
