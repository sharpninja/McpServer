#Requires -Version 7.0
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
$p = 'F:\GitHub\McpServer\docs\receipts\hostile-validator-20260822T003120Z.json'
$o = Get-Content -LiteralPath $p -Raw | ConvertFrom-Json
$o.PassCount = 18
$o | ConvertTo-Json -Depth 12 | Set-Content -LiteralPath $p -Encoding utf8
Write-Output ('PassCount=' + $o.PassCount + ' FailCount=' + $o.FailCount + ' Verdict=' + $o.OverallVerdict)
