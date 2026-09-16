#Requires -Version 7.0
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
$now = [DateTime]::UtcNow
$utc = $now.ToString('yyyyMMddTHHmmssZ')
$iso = $now.ToString('yyyy-MM-ddTHH:mm:ssZ')
Set-Content -LiteralPath 'F:\GitHub\McpServer\docs\receipts\_hv-c-red-p20\receipt-stamp.txt' -Value $utc -Encoding utf8
Set-Content -LiteralPath 'F:\GitHub\McpServer\docs\receipts\_hv-c-red-p20\receipt-stamp-iso.txt' -Value $iso -Encoding utf8
Write-Output $utc
Write-Output $iso
