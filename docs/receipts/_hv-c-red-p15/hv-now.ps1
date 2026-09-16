#Requires -Version 7.0
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
$utc = [DateTime]::UtcNow
$trx = Get-Item -LiteralPath 'F:\GitHub\McpServer\docs\receipts\_hv-c-red-p15\trx-p15-hv\p15-filter.trx'
[ordered]@{
    Stamp = $utc.ToString('yyyyMMddTHHmmssZ')
    Iso = $utc.ToString('o')
    TrxUtc = $trx.LastWriteTimeUtc.ToString('o')
    TrxLength = $trx.Length
} | ConvertTo-Json
