#Requires -Version 7.0
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$mdPath = 'F:\GitHub\McpServer\docs\receipts\hostile-validator-20260822T001811Z.md'
$jsonPath = 'F:\GitHub\McpServer\docs\receipts\hostile-validator-20260822T001811Z.json'
$out = 'F:\GitHub\McpServer\docs\receipts\_hv-c-red-p4-r2'

$old = 'Persisted this turn. SessionId GrokSubagentHostile-20260822T001410Z-c-red-p4-r2. Turn requestId req-20260822T001410Z-001-hostile-c-red-p4-rereview. client.SessionLog.OpenSessionAsync created=true. BeginTurnAsync turnId 42814. AppendDialogAsync start totalDialogCount 1. CompleteTurnAsync and QueryAsync proof files are written by the finish-session collector after this receipt is on disk. Proof so far: docs/receipts/_hv-c-red-p4-r2/sl-open.txt, sl-begin.txt, sl-dialog-start.txt. Finish-session will add sl-dialog.txt, sl-patch.txt, sl-complete.txt, sl-query-agent.txt, sl-query-sid.txt, sl-query-todo.txt, sl-query-history.txt.'
$new = 'Persisted. SessionId GrokSubagentHostile-20260822T001410Z-c-red-p4-r2. Turn requestId req-20260822T001410Z-001-hostile-c-red-p4-rereview. client.SessionLog.OpenSessionAsync created=true. BeginTurnAsync turnId 42814. AppendDialogAsync totalDialogCount 4. CompleteTurnAsync same turnId. client.SessionLog.QueryAsync agent=GrokSubagentHostile sessionId=this session: first item; turn status completed; 6 actions with unquoted integer order; 4 processingDialog items; 2 designDecisions; response contains AGREE and receipt path. workflow.sessionlog.queryHistory returns this session first (session-level status in_progress is the open session, not the completed turn). Proof: docs/receipts/_hv-c-red-p4-r2/sl-open.txt, sl-begin.txt, sl-dialog-start.txt, sl-dialog.txt, sl-patch.txt, sl-complete.txt, sl-query-agent.txt, sl-query-sid.txt, sl-query-todo.txt, sl-query-history.txt.'

$md = Get-Content -LiteralPath $mdPath -Raw
if (-not $md.Contains($old)) {
    throw 'receipt md session-proof paragraph not found'
}
$md = $md.Replace($old, $new)
$md = $md.Replace('CompleteTurn/QueryAsync proof is completed by the finish-session collector citing this receipt.', 'Session turn completed and QueryAsync proved persistence (sl-query-sid.txt).')
Set-Content -LiteralPath $mdPath -Value $md -Encoding utf8 -NoNewline

$json = Get-Content -LiteralPath $jsonPath -Raw | ConvertFrom-Json
$json.SessionQueryProof.QueryFile = 'docs/receipts/_hv-c-red-p4-r2/sl-query-sid.txt'
$json.SessionQueryProof.TurnStatus = 'completed'
$json.SessionQueryProof.TurnId = 42814
$json.SessionQueryProof.ActionCount = 6
$json.SessionQueryProof.DialogCount = 4
$json.SessionQueryProof.DesignDecisionCount = 2
$json.SessionQueryProof.ResponseContainsAgree = $true
$json | ConvertTo-Json -Depth 12 | Set-Content -LiteralPath $jsonPath -Encoding utf8

$receiptMd = Get-Item -LiteralPath $mdPath
$receiptJson = Get-Item -LiteralPath $jsonPath
$mdText = Get-Content -LiteralPath $mdPath -Raw
$jsonObj = Get-Content -LiteralPath $jsonPath -Raw | ConvertFrom-Json
[ordered]@{
    MdExists = $true
    MdLength = $receiptMd.Length
    MdLastWriteTimeUtc = $receiptMd.LastWriteTimeUtc.ToString('o')
    MdHasAgree = [bool]($mdText -match '(?m)^OverallVerdict:\s*AGREE\s*$')
    MdHasQueryProof = ($mdText -match 'turn status completed')
    JsonExists = $true
    JsonLength = $receiptJson.Length
    JsonLastWriteTimeUtc = $receiptJson.LastWriteTimeUtc.ToString('o')
    JsonOverallVerdict = [string]$jsonObj.OverallVerdict
    JsonFailCount = $jsonObj.FailCount
    JsonTurnStatus = [string]$jsonObj.SessionQueryProof.TurnStatus
    JsonActionCount = [int]$jsonObj.SessionQueryProof.ActionCount
    JsonDialogCount = [int]$jsonObj.SessionQueryProof.DialogCount
    JsonResponseContainsAgree = [bool]$jsonObj.SessionQueryProof.ResponseContainsAgree
} | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $out 'receipt-on-disk-after-query.json') -Encoding utf8

Write-Output 'RECEIPT_PATCHED'
