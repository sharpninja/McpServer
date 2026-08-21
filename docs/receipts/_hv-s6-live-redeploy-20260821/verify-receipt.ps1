#Requires -Version 7.0
Set-StrictMode -Version Latest
Get-Item -LiteralPath @(
    'F:\GitHub\McpServer\docs\receipts\hostile-validator-20260821T103620Z.md'
    'F:\GitHub\McpServer\docs\receipts\hostile-validator-20260821T103620Z.json'
) | ForEach-Object {
    Write-Output ($_.FullName + ' Len=' + $_.Length + ' Utc=' + $_.LastWriteTimeUtc.ToString('o'))
}
$j = Get-Content -LiteralPath 'F:\GitHub\McpServer\docs\receipts\hostile-validator-20260821T103620Z.json' -Raw | ConvertFrom-Json
Write-Output ('OverallVerdict=' + $j.OverallVerdict)
Write-Output ('FailCount=' + $j.FailCount)
Write-Output ('PassCount=' + $j.PassCount)
Write-Output ('UnknownCount=' + $j.UnknownCount)
Write-Output ('ExplicitFailListCount=' + @($j.ExplicitFailList).Count)
$md = Select-String -LiteralPath 'F:\GitHub\McpServer\docs\receipts\hostile-validator-20260821T103620Z.md' -Pattern 'OverallVerdict'
$md | ForEach-Object { Write-Output ('MD:' + $_.Line) }
