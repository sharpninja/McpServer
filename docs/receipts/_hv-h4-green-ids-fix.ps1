#Requires -Version 7.0
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$outDir = 'F:\GitHub\McpServer\docs\receipts'
$utcStamp = [datetime]::UtcNow.ToString('yyyyMMddTHHmmssZ')
$sessionId = "GrokCode-$utcStamp-h4-green-products"
$requestId = "req-$utcStamp-001-hostile-h4-green-products"
@(
    'STAMP=' + $utcStamp
    'SESSION_ID=' + $sessionId
    'REQUEST_ID=' + $requestId
) | Set-Content -LiteralPath (Join-Path $outDir '_hv-h4-green-ids.txt') -Encoding utf8
Write-Output ('UTC_STAMP=' + $utcStamp)
Write-Output ('SESSION_ID=' + $sessionId)
Write-Output ('REQUEST_ID=' + $requestId)
Write-Output ('SESSION_REGEX=' + [bool]($sessionId -match '^[A-Z][A-Za-z0-9]*-\d{8}T\d{6}Z-[a-z0-9]+(?:-[a-z0-9]+)*$'))
Write-Output ('REQUEST_REGEX=' + [bool]($requestId -match '^req-\d{8}T\d{6}Z-[a-z0-9]+(?:-[a-z0-9]+)*$'))
