#Requires -Version 7.0
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$mdPath = 'F:\GitHub\McpServer\docs\receipts\hostile-validator-20260818T152430Z.md'
$jsonPath = 'F:\GitHub\McpServer\docs\receipts\hostile-validator-20260818T152430Z.json'

$md = Get-Content -LiteralPath $mdPath -Raw
$old = @'
- sessionlog_dialog / replace_section / complete_turn recorded in the MCP3 script output
- Persistence proved by sessionlog_query in the MCP3 script (see receipt JSON sessionProof)
'@
$new = @'
- sessionlog_dialog success totalDialogItems=4
- sessionlog_replace_section actions replaced=true (9 actions)
- sessionlog_complete_turn success turnId=41766 status=completed
- Persistence proved by sessionlog_query workspacePath=F:\GitHub\McpServer agent=GrokCode todoId=MCP-PRODUCTS-001 from=2026-08-18T15:20:00Z limit=10. totalCount=1. First item: sessionId GrokCode-20260818T152309Z-h3-red-products, sourceType GrokCode, turnCount=1, requestId req-20260818T152309Z-001-hostile-h3-red-products, turn status=completed, response starts with OverallVerdict AGREE, 9 actions, 4 dialog items (one category=decision), designDecisions present. Session-level status remains in_progress (expected; session not closed).
'@
if ($md.Contains($old) -eq $false) {
    throw 'MD persistence block not found'
}
$md = $md.Replace($old, $new)
Set-Content -LiteralPath $mdPath -Value $md.TrimEnd() -Encoding utf8
Write-Output 'MD_PATCHED'

$json = Get-Content -LiteralPath $jsonPath -Raw | ConvertFrom-Json
$json | Add-Member -NotePropertyName sessionProof -NotePropertyValue ([pscustomobject]@{
    queryFrom = '2026-08-18T15:20:00Z'
    totalCount = 1
    sessionId = 'GrokCode-20260818T152309Z-h3-red-products'
    sourceType = 'GrokCode'
    turnCount = 1
    requestId = 'req-20260818T152309Z-001-hostile-h3-red-products'
    turnStatus = 'completed'
    sessionStatus = 'in_progress'
    actionCount = 9
    dialogCount = 4
    hasDecisionDialog = $true
    responseStartsWith = 'OverallVerdict AGREE'
    turnId = 41766
}) -Force
$json | ConvertTo-Json -Depth 12 | Set-Content -LiteralPath $jsonPath -Encoding utf8
Write-Output 'JSON_PATCHED'

$mdItem = Get-Item -LiteralPath $mdPath
$jsonItem = Get-Item -LiteralPath $jsonPath
Write-Output ('MD_EXISTS Len=' + $mdItem.Length + ' LastWriteUtc=' + $mdItem.LastWriteTimeUtc.ToString('o'))
Write-Output ('JSON_EXISTS Len=' + $jsonItem.Length + ' LastWriteUtc=' + $jsonItem.LastWriteTimeUtc.ToString('o'))
if ((Select-String -LiteralPath $mdPath -Pattern 'OverallVerdict: AGREE' -SimpleMatch)) {
    Write-Output 'MD_HAS_AGREE'
}
if ((Select-String -LiteralPath $jsonPath -Pattern '"OverallVerdict": "AGREE"' -SimpleMatch)) {
    Write-Output 'JSON_HAS_AGREE'
}
