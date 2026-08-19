#Requires -Version 7.0
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
$md = 'F:\GitHub\McpServer\docs\receipts\hostile-validator-20260818T234800Z.md'
$js = 'F:\GitHub\McpServer\docs\receipts\hostile-validator-20260818T234800Z.json'
$mdi = Get-Item -LiteralPath $md
$jsi = Get-Item -LiteralPath $js
$obj = Get-Content -LiteralPath $js -Raw | ConvertFrom-Json
$mdText = Get-Content -LiteralPath $md -Raw
Write-Output ('UTC=' + [DateTime]::UtcNow.ToString('yyyy-MM-ddTHH:mm:ssZ'))
Write-Output ('MD_LEN=' + $mdi.Length)
Write-Output ('MD_WRITE=' + $mdi.LastWriteTimeUtc.ToString('o'))
Write-Output ('JS_LEN=' + $jsi.Length)
Write-Output ('JS_WRITE=' + $jsi.LastWriteTimeUtc.ToString('o'))
Write-Output ('JSON_VERDICT=' + $obj.OverallVerdict)
Write-Output ('JSON_PASS=' + $obj.Counts.PASS)
Write-Output ('JSON_FAIL=' + $obj.Counts.FAIL)
Write-Output ('JSON_UNK=' + $obj.Counts.UNKNOWN)
Write-Output ('JSON_NA=' + $obj.Counts.NA)
Write-Output ('JSON_RESP_AGREE=' + $obj.SessionPersistence.queryResponseContainsOverallVerdictAgree)
Write-Output ('MD_HAS_AGREE=' + [bool]($mdText -match 'OverallVerdict\s+AGREE'))
Write-Output ('MD_HAS_EMDASH=' + $mdText.Contains([char]0x2014))
Write-Output ('TODOS_DONE_FLAG=' + $obj.DidNotMarkTodosDone)
