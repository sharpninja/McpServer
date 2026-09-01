#Requires -Version 7.0
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
$md = 'F:\GitHub\McpServer\docs\receipts\hostile-validator-20260818T163120Z.md'
$text = [System.IO.File]::ReadAllText($md)
$old = '- sessionlog_dialog / replace_section / complete_turn and query proof are appended after this receipt write'
$new = @'
- sessionlog_dialog success totalDialogItems=4 (one category=decision)
- sessionlog_replace_section actions replaced=true (8 actions)
- sessionlog_complete_turn success turnId=41787 status=completed
- Persistence proved by sessionlog_query workspacePath=F:\GitHub\McpServer agent=GrokCode todoId=MCP-PRODUCTS-001 from=2026-08-18T16:24:00Z limit=10. totalCount=1. First item: sessionId GrokCode-20260818T162441Z-h5-done-products, sourceType GrokCode, turnCount=1, requestId req-20260818T162441Z-001-hostile-h5-done-products, turn status=completed, response starts with OverallVerdict DISAGREE, 8 actions, 4 dialog items (one category=decision), designDecisions present. Session-level status remains in_progress (expected; session not closed). Saved docs/receipts/_hv-h5-done-query-proof.json
'@
if (-not $text.Contains($old)) { throw 'proof placeholder not found' }
$text = $text.Replace($old, $new)
[System.IO.File]::WriteAllText($md, $text)
Write-Output 'MD_PROOF_PATCHED'
