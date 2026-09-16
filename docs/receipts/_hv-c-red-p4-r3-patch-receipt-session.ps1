#Requires -Version 7.0
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$mdPath = 'F:\GitHub\McpServer\docs\receipts\hostile-validator-20260822T003120Z.md'
$jsonPath = 'F:\GitHub\McpServer\docs\receipts\hostile-validator-20260822T003120Z.json'

$old = @'
Opened this review session before independent tests. SessionId GrokSubagentHostile-20260822T002701Z-c-red-p4-r3. Turn requestId req-20260822T002701Z-001-hostile-c-red-p4-r3. client.SessionLog.OpenSessionAsync created. BeginTurnAsync turnId 42820. AppendDialogAsync start written. CompleteTurnAsync and QueryAsync proof files are written by the finish-session collector after this receipt pair is on disk. Proof so far: docs/receipts/_hv-c-red-p4-r3/sl-open.txt, sl-begin.txt, sl-dialog-start.txt.
'@
$new = @'
Persisted. SessionId GrokSubagentHostile-20260822T002701Z-c-red-p4-r3. Turn requestId req-20260822T002701Z-001-hostile-c-red-p4-r3. client.SessionLog.OpenSessionAsync created. BeginTurnAsync turnId 42820. AppendDialogAsync totalDialogCount 4. CompleteTurnAsync same turnId. client.SessionLog.QueryAsync agent=GrokSubagentHostile sessionId=this session: first item; turn status completed; 6 actions with unquoted integer order; 4 processingDialog items; 2 designDecisions; response contains DISAGREE and receipt path. workflow.sessionlog.queryHistory and QueryAsync totalCount 25 for this agent. Session-level status in_progress is the open session, not the completed turn. Proof: docs/receipts/_hv-c-red-p4-r3/sl-open.txt, sl-begin.txt, sl-dialog-start.txt, sl-dialog.txt, sl-patch.txt, sl-complete.txt, sl-query-agent.txt, sl-query-sid.txt, sl-query-todo.txt, sl-query-history.txt.
'@

$md = Get-Content -LiteralPath $mdPath -Raw
if (-not $md.Contains($old)) { throw 'session proof paragraph not found' }
$md = $md.Replace($old, $new)
$md = $md.Replace(
    'Completeness: 95. Surfaces A+B+C+D scored. Session turn opened (turnId 42820). Finish-session will complete the turn and QueryAsync after this file.',
    'Completeness: 95. Surfaces A+B+C+D scored. Session turn completed and QueryAsync proved persistence (sl-query-sid.txt).'
)
Set-Content -LiteralPath $mdPath -Value $md.TrimEnd() -Encoding utf8

$json = Get-Content -LiteralPath $jsonPath -Raw | ConvertFrom-Json
# ConvertFrom-Json PSCustomObject is fine; rewrite Completeness already in json as number
$json | ConvertTo-Json -Depth 10 | Set-Content -LiteralPath $jsonPath -Encoding utf8

$out = 'F:\GitHub\McpServer\docs\receipts\_hv-c-red-p4-r3'
[ordered]@{
    MdHasDisagree = [bool]((Get-Content -LiteralPath $mdPath -Raw) -match '(?m)^OverallVerdict:\s*DISAGREE\s*$')
    MdHasPersistedProof = [bool]((Get-Content -LiteralPath $mdPath -Raw).Contains('turn status completed'))
    JsonOverallVerdict = [string](Get-Content -LiteralPath $jsonPath -Raw | ConvertFrom-Json).OverallVerdict
    JsonFailCount = (Get-Content -LiteralPath $jsonPath -Raw | ConvertFrom-Json).FailCount
} | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $out 'receipt-on-disk-after-query.json') -Encoding utf8
Write-Output 'PATCH_RECEIPT_DONE'
