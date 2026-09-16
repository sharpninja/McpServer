#Requires -Version 7.0
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
$out = 'F:\GitHub\McpServer\docs\receipts\_hv-c-green-p20'
$nonce = 'nonce-hv-retry-' + [DateTime]::UtcNow.ToString('yyyyMMddHHmmss') + '-' + (Get-Random -Maximum 99999)
$health = Invoke-RestMethod -Uri ("http://PAYTON-LEGION2:7147/health?nonce=$nonce") -Method Get -TimeoutSec 20
$obj = [ordered]@{
    nonceSent = $nonce
    nonceEcho = [string]$health.nonce
    status = [string]$health.status
    match = ($health.nonce -eq $nonce)
    storage = [string]$health.storage
    version = [string]$health.version
}
$obj | ConvertTo-Json -Depth 6 | Set-Content -LiteralPath (Join-Path $out 'health-nonce-retry.json') -Encoding utf8
Write-Output ("RETRY_MATCH=" + $obj.match + " STATUS=" + $obj.status + " NONCE=" + $nonce)
