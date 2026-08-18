#Requires -Version 7.0
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$mdPath = 'F:\GitHub\McpServer\docs\receipts\hostile-validator-20260817T232829Z.md'
$jsonPath = 'F:\GitHub\McpServer\docs\receipts\hostile-validator-20260817T232829Z.json'

$proof = @'
## Session log proof

workflow.sessionlog.bootstrap: initialized true.
workflow.sessionlog.openSession / beginTurn / appendDialog / appendActions / completeTurn: EXIT_OK for session GrokCode-20260817T232250Z-hostile-effort and request req-20260817T232250Z-001-hostile-validate-effort.

workflow.sessionlog.queryHistory (agent GrokCode, limit 10) listed sessionId GrokCode-20260817T232250Z-hostile-effort first. tags included hostile-validator, agent-help, effort, class-2, AGREE. turnCount was 1 at query time. Session-level status remained in_progress.

client.SessionLog.QueryAsync (sessionlog_query backend) with text "Hostile validate Agent Help effort-high claims" returned totalCount 1, sessionId GrokCode-20260817T232250Z-hostile-effort. Turn req-20260817T232250Z-001-hostile-validate-effort status=completed. Response contains OverallVerdict AGREE and receipt path docs/receipts/hostile-validator-20260817T232829Z.md. Four actions (orders 1-4: design_decision, web_reference, web_reference, design_decision). Three processingDialog items including category decision. Tags include hostile-validator and AGREE.

A later hook turn req-20260817T233034Z-prompt-81dc landed on the same session after this review completed its turn. That later turn is not part of this verdict.
'@

$md = [System.IO.File]::ReadAllText($mdPath)
$old = @'
## Session log proof

Created after claim checks. Persistence proof is appended in this file after workflow.sessionlog.queryHistory and client.SessionLog.QueryAsync (native sessionlog_query backend) return. If those queries fail, this receipt is incomplete.
'@
if (-not $md.Contains($old)) { throw 'Expected session-proof placeholder not found.' }
$md = $md.Replace($old, $proof.TrimEnd() + [Environment]::NewLine)
$md = $md.Replace('Completeness: 95.', 'Completeness: 98. Source, live YAML, official reasoning docs, grok --help, deployed exe, git SHA, and persisted session-log turn were checked.')
[System.IO.File]::WriteAllText($mdPath, $md)

$doc = Get-Content -LiteralPath $jsonPath -Raw | ConvertFrom-Json
$doc.Completeness = 98
$doc | Add-Member -NotePropertyName SessionLogProof -NotePropertyValue ([ordered]@{
    BootstrapInitialized = $true
    SessionId = 'GrokCode-20260817T232250Z-hostile-effort'
    RequestId = 'req-20260817T232250Z-001-hostile-validate-effort'
    TurnStatus = 'completed'
    QueryHistoryListedSession = $true
    QueryAsyncMatchedTitle = $true
    ActionCount = 4
    DialogCount = 3
    QueryMethod = 'client.SessionLog.QueryAsync'
    NativeToolEquivalent = 'sessionlog_query'
}) -Force
$json = $doc | ConvertTo-Json -Depth 8
[System.IO.File]::WriteAllText($jsonPath, $json)
Write-Output 'RECEIPT_PATCHED'
