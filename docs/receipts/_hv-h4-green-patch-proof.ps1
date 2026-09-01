#Requires -Version 7.0
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$mdPath = 'F:\GitHub\McpServer\docs\receipts\hostile-validator-20260818T160833Z.md'
$jsonPath = 'F:\GitHub\McpServer\docs\receipts\hostile-validator-20260818T160833Z.json'
$utf8 = New-Object System.Text.UTF8Encoding $false

$obj = Get-Content -LiteralPath $jsonPath -Raw | ConvertFrom-Json
$obj.sessionProof = [pscustomobject]@{
    transport = 'POST http://PAYTON-LEGION2:7147/mcp-transport'
    toolsUnique = 106
    initializeHttp = 200
    openCreated = $true
    sessionId = 'GrokCode-20260818T160502Z-h4-green-products'
    requestId = 'req-20260818T160502Z-001-hostile-h4-green-products'
    turnId = 41784
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
$json = $obj | ConvertTo-Json -Depth 10
[System.IO.File]::WriteAllText($jsonPath, $json + "`n", $utf8)

$md = Get-Content -LiteralPath $mdPath -Raw
$old = @'
## Session-log persistence proof

Native MCP tools over POST http://PAYTON-LEGION2:7147/mcp-transport (initialize, tools/list, tools/call). Agent GrokCode, workspace F:\GitHub\McpServer. tools/list unique name count 106 including sessionlog_open, sessionlog_begin_turn, sessionlog_dialog, sessionlog_complete_turn, sessionlog_query, todo_get, requirements_list.

- initialize protocolVersion 2025-03-26 HTTP 200
- sessionlog_open GrokCode-20260818T160502Z-h4-green-products created=true
- sessionlog_begin_turn requestId req-20260818T160502Z-001-hostile-h4-green-products turnId=41784 status=in_progress
- sessionlog_dialog / sessionlog_replace_section / sessionlog_complete_turn and query proof are appended after this file is written.
'@
$new = @'
## Session-log persistence proof

Native MCP tools over POST http://PAYTON-LEGION2:7147/mcp-transport (initialize, tools/list, tools/call). Agent GrokCode, workspace F:\GitHub\McpServer. tools/list unique name count 106 including sessionlog_open, sessionlog_begin_turn, sessionlog_dialog, sessionlog_complete_turn, sessionlog_query, todo_get, requirements_list.

- initialize protocolVersion 2025-03-26 HTTP 200
- sessionlog_open GrokCode-20260818T160502Z-h4-green-products created=true
- sessionlog_begin_turn requestId req-20260818T160502Z-001-hostile-h4-green-products turnId=41784 status=in_progress
- sessionlog_dialog success totalDialogItems=4
- sessionlog_replace_section actions replaced=true (7 actions)
- sessionlog_complete_turn success turnId=41784 status=completed
- Persistence proved by sessionlog_query workspacePath=F:\GitHub\McpServer agent=GrokCode todoId=MCP-PRODUCTS-001 from=2026-08-18T16:04:00Z limit=10. totalCount=1. First item: sessionId GrokCode-20260818T160502Z-h4-green-products, sourceType GrokCode, turnCount=1, requestId req-20260818T160502Z-001-hostile-h4-green-products, turn status=completed, response starts with OverallVerdict AGREE, 7 actions, 4 dialog items (one category=decision), designDecisions present. Session-level status remains in_progress (expected; session not closed).
'@
if ($md.IndexOf($old) -lt 0) {
    throw 'MD persistence section not found for replace'
}
$md2 = $md.Replace($old, $new)
[System.IO.File]::WriteAllText($mdPath, $md2, $utf8)

Write-Output ('MD_HAS_QUERY_PROOF=' + [bool]((Get-Content -LiteralPath $mdPath -Raw) -match 'Persistence proved by sessionlog_query'))
$re = Get-Content -LiteralPath $jsonPath -Raw | ConvertFrom-Json
Write-Output ('JSON_QUERY_TOTAL=' + $re.sessionProof.queryTotalCount)
Write-Output ('JSON_QUERY_STATUS=' + $re.sessionProof.queryTurnStatus)
Write-Output ('JSON_COMPLETE=' + $re.sessionProof.completeStatus)
Write-Output ('MD_SHA256=' + (Get-FileHash -LiteralPath $mdPath -Algorithm SHA256).Hash)
Write-Output ('JSON_SHA256=' + (Get-FileHash -LiteralPath $jsonPath -Algorithm SHA256).Hash)
Write-Output ('MD_EMDASH=' + [bool]((Get-Content -LiteralPath $mdPath -Raw) -match [char]0x2014))
Write-Output ('JSON_VERDICT=' + $re.OverallVerdict)
Write-Output ('JSON_FAILS=' + @($re.failList).Count)
