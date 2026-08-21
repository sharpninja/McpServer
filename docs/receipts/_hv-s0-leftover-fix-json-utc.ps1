#Requires -Version 7.0
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$path = 'F:\GitHub\McpServer\docs\receipts\hostile-validator-20260819T174750Z.json'
$json = Get-Content -LiteralPath $path -Raw -Encoding utf8 | ConvertFrom-Json -AsHashtable
$json['ActualCompletedUtc'] = '2026-08-19T17:51:27Z'
$json['PersistenceProof'] = [ordered]@{
    completeSuccess = $true
    turnId = 42056
    status = 'completed'
    queryTotalCount = 1
    sessionId = 'GrokCode-20260819T174750Z-hostile-s0-leftover'
    requestId = 'req-20260819T174750Z-001-hostile-s0-leftover-triage'
    planFile = 'docs/plans/triage-cluster-002.md'
    todoId = 'PLAN-TRIAGELEFTOVER-001'
    queryPath = 'docs/receipts/_hv-s0-leftover/session-query-proof.json'
}
$json | ConvertTo-Json -Depth 12 | Set-Content -LiteralPath $path -Encoding utf8
$check = Get-Content -LiteralPath $path -Raw -Encoding utf8 | ConvertFrom-Json -AsHashtable
Write-Output ('UTC=' + $check['ActualCompletedUtc'])
Write-Output ('VERDICT=' + $check['OverallVerdict'])
Write-Output ('FAIL=' + $check['Counts']['FAIL'])
Write-Output ('TURN=' + $check['PersistenceProof']['turnId'])
