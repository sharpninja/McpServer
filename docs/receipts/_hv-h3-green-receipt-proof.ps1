#Requires -Version 7.0
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$md = 'F:\GitHub\McpServer\docs\receipts\hostile-validator-20260818T154000Z.md'
$js = 'F:\GitHub\McpServer\docs\receipts\hostile-validator-20260818T154000Z.json'
$mdi = Get-Item -LiteralPath $md
$jsi = Get-Item -LiteralPath $js
Write-Output ('MD_EXISTS=' + $mdi.Exists + ' BYTES=' + $mdi.Length + ' UTC=' + $mdi.LastWriteTimeUtc.ToString('o'))
Write-Output ('JSON_EXISTS=' + $jsi.Exists + ' BYTES=' + $jsi.Length + ' UTC=' + $jsi.LastWriteTimeUtc.ToString('o'))
$obj = Get-Content -LiteralPath $js -Raw | ConvertFrom-Json
Write-Output ('JSON_VERDICT=' + $obj.OverallVerdict)
Write-Output ('JSON_IDENTITY=' + $obj.ValidatorIdentity)
Write-Output ('JSON_TURN=' + $obj.turnId)
Write-Output ('JSON_TODO_DONE=' + $obj.todoDone)
Write-Output ('JSON_IPRODUCT=' + $obj.iProductServiceCsCount)
Write-Output ('JSON_SUPPORT=' + [string]$obj.supportFilter.passed + '/' + [string]$obj.supportFilter.failed + '/' + [string]$obj.supportFilter.skipped)
Write-Output ('JSON_FAILCOUNT=' + $obj.failCount)
$mdHits = Select-String -LiteralPath $md -Pattern 'OverallVerdict: AGREE|ValidatorIdentity: GrokSubagentHostile|turnId: 41775|IPRODUCTSERVICE_CS_COUNT=0|Failed 0 Passed 38'
foreach ($h in $mdHits) {
    Write-Output ('MD ' + $h.LineNumber + ':' + $h.Line.Trim())
}
