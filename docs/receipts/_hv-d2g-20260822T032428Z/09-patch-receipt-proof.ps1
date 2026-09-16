#Requires -Version 7.0
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$mdPath = 'F:\GitHub\McpServer\docs\receipts\hostile-validator-20260822T033842Z.md'
$text = [System.IO.File]::ReadAllText($mdPath)
$old = 'Native MCP Streamable HTTP tools at http://PAYTON-LEGION2:7147/mcp-transport (not raw /mcpserver/sessionlog REST). Proof files under docs/receipts/_hv-d2g-20260822T032428Z/ (session-open.json, session-begin.json, plus complete/query after persist).'
$new = @'
Native MCP Streamable HTTP tools at http://PAYTON-LEGION2:7147/mcp-transport (not raw /mcpserver/sessionlog REST):

- sessionlog_complete_turn: success=true, turnId=42873, status=completed, requestId=req-20260822T032428Z-001-d2-green-pluginhandoff.
- sessionlog_query agent=GrokSubagentHostile from=2026-08-22T03:20:00Z: includes sessionId GrokSubagentHostile-20260822T032428Z-pluginhandoff-d2g.
- Turn status=completed. queryTitle=Hostile D2-green remaining-gap tests PLAN-PLUGINHANDOFF-001. planFile=docs/plans/PLAN-PLUGINHANDOFF-001.md. todoId=PLAN-PLUGINHANDOFF-001.
- 8 actions (orders 1-8 including design_decision). 5 processingDialog items (2 observation + 3 decision). 3 designDecisions. filesModified receipt md+json.
- Proof file: docs/receipts/_hv-d2g-20260822T032428Z/session-query-proof.json
'@
if (-not $text.Contains($old)) { throw 'receipt proof paragraph not found' }
$text = $text.Replace($old, $new)
$extra = @'

- docs/receipts/_hv-d2g-20260822T032428Z/session-complete.json
- docs/receipts/_hv-d2g-20260822T032428Z/session-query-agent.json
- docs/receipts/_hv-d2g-20260822T032428Z/session-query-proof.json
'@
if (-not $text.Contains('session-query-proof.json')) {
    $text = $text.TrimEnd() + "`r`n" + $extra.TrimStart() + "`r`n"
}
[System.IO.File]::WriteAllText($mdPath, $text)
Write-Output 'PATCHED_MD'
