#Requires -Version 7.0
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
$path = 'F:\GitHub\McpServer\docs\receipts\hostile-validator-20260818T185500Z.json'
$obj = Get-Content -LiteralPath $path -Raw | ConvertFrom-Json
$obj | Add-Member -NotePropertyName SessionProof -NotePropertyValue ([pscustomobject]@{
    file = 'docs/receipts/_hv-wrapup-query-proof2.json'
    sessionId = 'GrokCode-20260818T184548Z-hostile-wrapup'
    requestId = 'req-20260818T184548Z-001-hostile-wrap-up-review'
    turnStatus = 'completed'
    actionCount = 10
    dialogCount = 3
}) -Force
$obj | ConvertTo-Json -Depth 12 | Set-Content -LiteralPath $path -Encoding utf8
Write-Output 'JSON_UPDATED'
