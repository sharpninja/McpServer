#Requires -Version 7.0
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$mdPath = 'F:\GitHub\McpServer\docs\receipts\hostile-validator-20260819T174750Z.md'
$jsonPath = 'F:\GitHub\McpServer\docs\receipts\hostile-validator-20260819T174750Z.json'
$completed = '2026-08-19T17:51:27Z'

$proof = @'
sessionlog_complete_turn success turnId=42056 status=completed.
Persistence proved by sessionlog_query workspacePath=F:\GitHub\McpServer agent=GrokCode sessionId=GrokCode-20260819T174750Z-hostile-s0-leftover todoId=PLAN-TRIAGELEFTOVER-001 from=2026-08-19T17:40:00Z limit=10. totalCount=1. First item: sessionId GrokCode-20260819T174750Z-hostile-s0-leftover, sourceType GrokCode, turnCount=1, requestId req-20260819T174750Z-001-hostile-s0-leftover-triage, turn status=completed, planFile=docs/plans/triage-cluster-002.md, todoId=PLAN-TRIAGELEFTOVER-001, response starts with OverallVerdict DISAGREE, 8 actions (order integers 1-8, including design_decision), 4 dialog items (one category=decision), designDecisions present. Session-level status remains in_progress (expected; session not closed). Saved docs/receipts/_hv-s0-leftover/session-query-proof.json
'@

$md = [System.IO.File]::ReadAllText($mdPath)
if (-not $md.Contains('ActualCompletedUtc: pending-complete')) { throw 'md completed placeholder missing' }
if (-not $md.Contains('pending-complete; patched after sessionlog_complete_turn and sessionlog_query.')) { throw 'md proof placeholder missing' }
$md = $md.Replace('ActualCompletedUtc: pending-complete', 'ActualCompletedUtc: ' + $completed)
$md = $md.Replace('pending-complete; patched after sessionlog_complete_turn and sessionlog_query.', $proof.Trim())
[System.IO.File]::WriteAllText($mdPath, $md)

$json = Get-Content -LiteralPath $jsonPath -Raw -Encoding utf8 | ConvertFrom-Json
$json.ActualCompletedUtc = $completed
$json | Add-Member -NotePropertyName PersistenceProof -NotePropertyValue ([ordered]@{
    completeSuccess = $true
    turnId = 42056
    status = 'completed'
    queryTotalCount = 1
    sessionId = 'GrokCode-20260819T174750Z-hostile-s0-leftover'
    requestId = 'req-20260819T174750Z-001-hostile-s0-leftover-triage'
    planFile = 'docs/plans/triage-cluster-002.md'
    todoId = 'PLAN-TRIAGELEFTOVER-001'
    queryPath = 'docs/receipts/_hv-s0-leftover/session-query-proof.json'
}) -Force
$json | ConvertTo-Json -Depth 12 | Set-Content -LiteralPath $jsonPath -Encoding utf8

Write-Output 'PATCHED'
Write-Output ('MD_HAS_COMPLETED=' + ([System.IO.File]::ReadAllText($mdPath).Contains('ActualCompletedUtc: 2026-08-19T17:51:27Z')))
Write-Output ('MD_HAS_PROOF=' + ([System.IO.File]::ReadAllText($mdPath).Contains('sessionlog_query')))
Write-Output ('JSON_COMPLETED=' + ((Get-Content -LiteralPath $jsonPath -Raw | ConvertFrom-Json).ActualCompletedUtc))
