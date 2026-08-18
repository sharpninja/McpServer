#Requires -Version 7.0
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
$mdPath = 'F:\GitHub\McpServer\docs\receipts\hostile-validator-20260818T005349Z.md'
$jsPath = 'F:\GitHub\McpServer\docs\receipts\hostile-validator-20260818T005349Z.json'
Get-Item -LiteralPath $mdPath, $jsPath | ForEach-Object {
    Write-Output ($_.Name + ' len=' + $_.Length + ' utc=' + $_.LastWriteTimeUtc.ToString('o'))
}
$md = Get-Content -LiteralPath $mdPath -Raw
$js = Get-Content -LiteralPath $jsPath -Raw
Write-Output ('MD_HAS_AGREE=' + $md.Contains('OverallVerdict: AGREE'))
Write-Output ('JSON_HAS_AGREE=' + $js.Contains('"OverallVerdict": "AGREE"'))
Write-Output ('MD_HAS_EMDASH=' + $md.Contains([char]0x2014))
Write-Output ('JSON_HAS_EMDASH=' + $js.Contains([char]0x2014))
$obj = $js | ConvertFrom-Json
Write-Output ('JSON_VERDICT=' + $obj.OverallVerdict)
Write-Output ('JSON_FAILS=' + $obj.FailList.Count)
Write-Output ('JSON_SESSION=' + $obj.SessionId)
Write-Output ('JSON_TURN=' + $obj.ServerTurnId)
