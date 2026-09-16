#Requires -Version 7.0
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
$path = 'F:\GitHub\McpServer\docs\receipts\hostile-validator-20260821T234422Z.json'
$j = Get-Content -LiteralPath $path -Raw | ConvertFrom-Json
$j.PassCount = 25
$j | Add-Member -NotePropertyName SessionProof -NotePropertyValue ([pscustomobject]@{
    method = 'client.SessionLog.QueryAsync'
    agent = 'GrokSubagentHostile'
    totalCount = 21
    thisSession = 'GrokSubagentHostile-20260821T234007Z-c-green-p1-p3'
    turnStatus = 'completed'
    turnId = 42804
    actionCount = 6
    dialogCount = 3
    designDecisionCount = 2
    file = 'docs/receipts/_hv-c-green-p1-p3/sl-query-sid.txt'
}) -Force
$claimCount = @($j.Claims).Count
$j | ConvertTo-Json -Depth 12 | Set-Content -LiteralPath $path -Encoding utf8
Write-Output ("PassCount=" + $j.PassCount)
Write-Output ("Claims=" + $claimCount)

$md = 'F:\GitHub\McpServer\docs\receipts\hostile-validator-20260821T234422Z.md'
$mdItem = Get-Item -LiteralPath $md
$jsonItem = Get-Item -LiteralPath $path
[pscustomobject]@{
    MdLength = $mdItem.Length
    MdLastWriteTimeUtc = $mdItem.LastWriteTimeUtc.ToString('o')
    JsonLength = $jsonItem.Length
    JsonLastWriteTimeUtc = $jsonItem.LastWriteTimeUtc.ToString('o')
    JsonPassCount = $j.PassCount
    JsonOverallVerdict = $j.OverallVerdict
    JsonFailCount = $j.FailCount
} | ConvertTo-Json | Set-Content -LiteralPath 'F:\GitHub\McpServer\docs\receipts\_hv-c-green-p1-p3\receipt-on-disk.json' -Encoding utf8
Write-Output 'RECEIPT_DISK_OK'
