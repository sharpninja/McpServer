#Requires -Version 7.0
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
$path = 'F:\GitHub\McpServer\docs\receipts\hostile-validator-20260822T014442Z.json'
$obj = Get-Content -LiteralPath $path -Raw | ConvertFrom-Json
$obj.PassCount = @($obj.Claims).Count
$obj | ConvertTo-Json -Depth 10 | Set-Content -LiteralPath $path -Encoding utf8
Write-Output ("PASSCOUNT=" + $obj.PassCount)
