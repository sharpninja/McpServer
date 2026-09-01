#Requires -Version 7.0
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
$stamp = [datetime]::UtcNow.ToString('yyyyMMddTHHmmssZ')
$ids = @(
    'STAMP=' + $stamp
    'SESSION_ID=GrokCode-' + $stamp + '-h5-done-products'
    'REQUEST_ID=req-' + $stamp + '-001-hostile-h5-done-products'
)
$path = 'F:\GitHub\McpServer\docs\receipts\_hv-h5-done-ids.txt'
$ids | Set-Content -LiteralPath $path -Encoding utf8
Get-Content -LiteralPath $path
Write-Output ('UTC_NOW=' + [datetime]::UtcNow.ToString('o'))
