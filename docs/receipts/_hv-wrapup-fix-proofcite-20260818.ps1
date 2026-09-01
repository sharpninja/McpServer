#Requires -Version 7.0
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
$md = 'F:\GitHub\McpServer\docs\receipts\hostile-validator-20260818T185500Z.md'
$text = [System.IO.File]::ReadAllText($md)
$old = 'Query proof after completeTurn is written to `docs/receipts/_hv-wrapup-query-proof.json`.'
$new = 'Query proof after completeTurn is `docs/receipts/_hv-wrapup-query-proof2.json`: session GrokCode-20260818T184548Z-hostile-wrapup turn req-20260818T184548Z-001-hostile-wrap-up-review status=completed, 10 actions, 3 dialog items including category=decision, response starts with OverallVerdict AGREE.'
if (-not $text.Contains($old)) { throw 'cite line not found' }
$text = $text.Replace($old, $new)
[System.IO.File]::WriteAllText($md, $text)
Get-Item -LiteralPath $md, 'F:\GitHub\McpServer\docs\receipts\hostile-validator-20260818T185500Z.json' |
    ForEach-Object { Write-Output ($_.FullName + ' len=' + $_.Length + ' utc=' + $_.LastWriteTimeUtc.ToString('o')) }
Write-Output ('UTC=' + (Get-Date -AsUTC -Format o))
