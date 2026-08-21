$ErrorActionPreference = 'Stop'
$md = 'F:\GitHub\McpServer\docs\receipts\hostile-validator-20260819T184746Z.md'
$js = 'F:\GitHub\McpServer\docs\receipts\hostile-validator-20260819T184746Z.json'
Get-Item -LiteralPath $md, $js | ForEach-Object {
    Write-Output ($_.Name + ' len=' + $_.Length + ' utc=' + $_.LastWriteTimeUtc.ToString('o'))
}
$j = Get-Content -LiteralPath $js -Raw | ConvertFrom-Json
Write-Output ('json.OverallVerdict=' + $j.OverallVerdict)
Write-Output ('json.FailCount=' + $j.FailCount)
Write-Output ('json.PassCount=' + $j.PassCount)
Write-Output ('json.UnknownCount=' + $j.UnknownCount)
Select-String -LiteralPath $md -Pattern 'OverallVerdict','FAIL list','PASS:','UNKNOWN:' | ForEach-Object { $_.Line }
