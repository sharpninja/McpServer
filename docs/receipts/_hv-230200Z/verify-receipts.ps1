Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
Get-Item -LiteralPath 'F:\GitHub\McpServer\docs\receipts\hostile-validator-20260818T230200Z.md','F:\GitHub\McpServer\docs\receipts\hostile-validator-20260818T230200Z.json' |
    ForEach-Object { Write-Output ($_.Name + ' length=' + $_.Length + ' utc=' + $_.LastWriteTimeUtc.ToString('o')) }
$j = Get-Content -LiteralPath 'F:\GitHub\McpServer\docs\receipts\hostile-validator-20260818T230200Z.json' -Raw | ConvertFrom-Json
Write-Output ('JSON_VERDICT=' + $j.OverallVerdict)
Write-Output ('JSON_PASS=' + $j.Counts.PASS + ' FAIL=' + $j.Counts.FAIL + ' UNKNOWN=' + $j.Counts.UNKNOWN)
Write-Output ('JSON_SESSION=' + $j.SessionId)
Write-Output ('JSON_TURN=' + $j.TurnRequestId)
Write-Output ('JSON_COMPLETE=' + $j.SessionPersistence.completeStatus)
Write-Output ('JSON_QUERY=' + $j.SessionPersistence.queryTotalCount)
Write-Output ('JSON_ADDPROFILE=' + $j.AddProfile.profileFileCount)
Select-String -LiteralPath 'F:\GitHub\McpServer\docs\receipts\hostile-validator-20260818T230200Z.md' -Pattern 'OverallVerdict|DISAGREE|profileFileCount: 18' |
    ForEach-Object { Write-Output ('MD=' + $_.Line.Trim()) }
