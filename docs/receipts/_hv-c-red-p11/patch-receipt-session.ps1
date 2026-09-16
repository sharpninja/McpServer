#Requires -Version 7.0
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$mdPath = 'F:\GitHub\McpServer\docs\receipts\hostile-validator-20260822T023808Z.md'
$jsonPath = 'F:\GitHub\McpServer\docs\receipts\hostile-validator-20260822T023808Z.json'
$md = Get-Content -LiteralPath $mdPath -Raw

$old = 'SessionId GrokSubagentHostile-20260822T023011Z-c-red-p11. Turn requestId req-20260822T023011Z-001-hostile-c-red-p11. client.SessionLog.OpenSessionAsync created=true. BeginTurnAsync turnId 42861. Persistence proof is recorded after completeTurn in docs/receipts/_hv-c-red-p11/sl-*.txt.'
$new = 'SessionId GrokSubagentHostile-20260822T023011Z-c-red-p11. Turn requestId req-20260822T023011Z-001-hostile-c-red-p11. client.SessionLog.OpenSessionAsync created=true. BeginTurnAsync turnId 42861. client.SessionLog.CompleteTurnAsync from this process failed because the turn was already completed. Persistence is proven by client.SessionLog.QueryAsync and workflow.sessionlog.queryHistory: session exists, turn status completed, filesModified includes docs/receipts/hostile-validator-20260822T023808Z.md (docs/receipts/_hv-c-red-p11/sl-query-sid-after.txt, sl-query-history-after.txt).'
if (-not $md.Contains($old)) { throw 'session proof paragraph not found' }
$md = $md.Replace($old, $new)

$residual = '- Concurrent workspace dirt:'
$add = "- This process CompleteTurnAsync failed (turn already completed). QueryAsync still shows turn status completed. Parallel receipt docs/receipts/hostile-validator-20260822T023602Z.md exists on the same session; this review canonical pair is 20260822T023808Z with the required full dotnet test command.`r`n$residual"
if (-not $md.Contains('this review canonical pair')) {
    if (-not $md.Contains($residual)) { throw 'residual bullet not found' }
    $md = $md.Replace($residual, $add)
}

Set-Content -LiteralPath $mdPath -Value $md -Encoding utf8

$j = Get-Content -LiteralPath $jsonPath -Raw | ConvertFrom-Json
$j | Add-Member -NotePropertyName PersistenceProof -NotePropertyValue 'client.SessionLog.QueryAsync turn status completed; CompleteTurnAsync from this process failed because turn already completed' -Force
$j | Add-Member -NotePropertyName TurnStatus -NotePropertyValue 'completed' -Force
$j | ConvertTo-Json -Depth 12 | Set-Content -LiteralPath $jsonPath -Encoding utf8
Write-Output 'RECEIPT_PATCHED'
