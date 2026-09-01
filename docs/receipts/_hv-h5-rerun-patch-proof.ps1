#Requires -Version 7.0
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$path = 'F:\GitHub\McpServer\docs\receipts\hostile-validator-20260818T165609Z.md'
$text = Get-Content -LiteralPath $path -Raw
$old = '- sessionlog_dialog / replace_section / complete_turn and sessionlog_query proof recorded in docs/receipts/_hv-h5-rerun-query-proof.json after this receipt is written'
$new = @'
- sessionlog_dialog success totalDialogItems=4 (one category=decision)
- sessionlog_replace_section actions replaced=true (8 actions)
- sessionlog_complete_turn success turnId=41797 status=completed
- Persistence proved by sessionlog_query workspacePath=F:\GitHub\McpServer agent=GrokCode todoId=MCP-PRODUCTS-001 from=2026-08-18T16:50:00Z limit=10. totalCount=1. First item: sessionId GrokCode-20260818T165022Z-h5-done-rerun-products, sourceType GrokCode, turnCount=1, requestId req-20260818T165022Z-001-hostile-h5-done-rerun, queryTitle Hostile H5-done rerun after handoff lock, turn status=completed, response starts with OverallVerdict AGREE, 8 actions, 4 dialog items (one category=decision), designDecisions present. Session-level status remains in_progress (expected; session not closed). Saved docs/receipts/_hv-h5-rerun-query-proof.json
'@
if (-not $text.Contains($old)) {
    throw 'old proof line not found'
}
$text = $text.Replace($old, $new)
Set-Content -LiteralPath $path -Value $text -Encoding utf8 -NoNewline
Write-Output 'MD_UPDATED'
Write-Output ((Select-String -Path $path -Pattern 'sessionlog_query workspacePath').Line)
