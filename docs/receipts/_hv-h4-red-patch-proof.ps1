#Requires -Version 7.0
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$outDir = 'F:\GitHub\McpServer\docs\receipts'
$mdPath = Join-Path $outDir 'hostile-validator-20260818T155200Z.md'
$jsonPath = Join-Path $outDir 'hostile-validator-20260818T155200Z.json'

$jsonObj = Get-Content -LiteralPath $jsonPath -Raw | ConvertFrom-Json
$sessionProof = [ordered]@{
    transport = 'POST http://PAYTON-LEGION2:7147/mcp-transport'
    toolsUnique = 106
    initializeHttp = 200
    openCreated = $true
    sessionId = 'GrokCode-20260818T154849Z-h4-red-products'
    requestId = 'req-20260818T154849Z-001-hostile-h4-red-products'
    turnId = 41778
    beginStatus = 'in_progress'
    dialogSuccess = $true
    dialogItems = 4
    actionsReplaced = $true
    actionCount = 7
    completeStatus = 'completed'
    queryTotalCount = 1
    querySourceType = 'GrokCode'
    queryTurnStatus = 'completed'
    queryResponseStartsWith = 'OverallVerdict AGREE'
    sessionStatusRemains = 'in_progress'
}
$jsonObj | Add-Member -NotePropertyName 'sessionProof' -NotePropertyValue $sessionProof -Force
$jsonOut = $jsonObj | ConvertTo-Json -Depth 12
Set-Content -LiteralPath $jsonPath -Value $jsonOut -Encoding utf8

$md = Get-Content -LiteralPath $mdPath -Raw
$old = '- sessionlog_dialog / replace_section / complete_turn and query proof are appended after this receipt write (see collector _hv-h4-red-mcp3.ps1 / _hv-h4-red-query-proof.json)'
$new = @'
- sessionlog_dialog success totalDialogItems=4
- sessionlog_replace_section actions replaced=true (7 actions)
- sessionlog_complete_turn success turnId=41778 status=completed
- Persistence proved by sessionlog_query workspacePath=F:\GitHub\McpServer agent=GrokCode todoId=MCP-PRODUCTS-001 from=2026-08-18T15:48:00Z limit=10. totalCount=1. First item: sessionId GrokCode-20260818T154849Z-h4-red-products, sourceType GrokCode, turnCount=1, requestId req-20260818T154849Z-001-hostile-h4-red-products, turn status=completed, response starts with OverallVerdict AGREE, 7 actions, 4 dialog items (one category=decision), designDecisions present. Session-level status remains in_progress (expected; session not closed).
'@
if ($md.Contains($old)) {
    $md = $md.Replace($old, $new)
} else {
    throw 'Placeholder persistence line not found'
}
Set-Content -LiteralPath $mdPath -Value $md -Encoding utf8

$mdItem = Get-Item -LiteralPath $mdPath
$jsonItem = Get-Item -LiteralPath $jsonPath
Write-Output ('MD_EXISTS=' + $mdItem.FullName)
Write-Output ('MD_BYTES=' + $mdItem.Length)
Write-Output ('MD_MTIME=' + $mdItem.LastWriteTimeUtc.ToString('o'))
Write-Output ('MD_SHA256=' + (Get-FileHash -LiteralPath $mdPath -Algorithm SHA256).Hash)
Write-Output ('JSON_EXISTS=' + $jsonItem.FullName)
Write-Output ('JSON_BYTES=' + $jsonItem.Length)
Write-Output ('JSON_MTIME=' + $jsonItem.LastWriteTimeUtc.ToString('o'))
Write-Output ('JSON_SHA256=' + (Get-FileHash -LiteralPath $jsonPath -Algorithm SHA256).Hash)
$verdict = Select-String -LiteralPath $mdPath -Pattern 'OverallVerdict: AGREE'
Write-Output ('MD_VERDICT=' + $verdict.Line.Trim())
$failNone = Select-String -LiteralPath $mdPath -Pattern '^None\.$'
Write-Output ('MD_FAIL_NONE=' + [bool]$failNone)
