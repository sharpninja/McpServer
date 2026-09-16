#Requires -Version 7.0
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$mdPath = 'F:\GitHub\McpServer\docs\receipts\hostile-validator-20260821T235922Z.md'
$jsonPath = 'F:\GitHub\McpServer\docs\receipts\hostile-validator-20260821T235922Z.json'
$out = 'F:\GitHub\McpServer\docs\receipts\_hv-c-red-p4'

$md = Get-Content -LiteralPath $mdPath -Raw
$old = @'
## Session log proof

Persisted. SessionId GrokSubagentHostile-20260821T235600Z-c-red-p4. Turn requestId req-20260821T235600Z-001-hostile-c-red-p4. client.SessionLog.OpenSessionAsync created=true. BeginTurnAsync turnId 42807. Dialog/actions/complete/query proof files are written after this receipt pair (docs/receipts/_hv-c-red-p4/sl-open.txt, sl-begin.txt, then sl-dialog.txt, sl-patch.txt, sl-complete.txt, sl-query-*.txt).
'@
$new = @'
## Session log proof

Persisted. SessionId GrokSubagentHostile-20260821T235600Z-c-red-p4. Turn requestId req-20260821T235600Z-001-hostile-c-red-p4. client.SessionLog.OpenSessionAsync created=true. BeginTurnAsync turnId 42807. AppendDialogAsync totalDialogCount 3. CompleteTurnAsync same turnId. client.SessionLog.QueryAsync agent=GrokSubagentHostile sessionId=this session: first item; turn status completed; 6 actions with unquoted integer order; 3 processingDialog items; 2 designDecisions; response contains DISAGREE and receipt path. workflow.sessionlog.queryHistory returns this session first (session-level status in_progress is the open session, not the completed turn). Proof: docs/receipts/_hv-c-red-p4/sl-open.txt, sl-begin.txt, sl-dialog.txt, sl-patch.txt, sl-complete.txt, sl-query-agent.txt, sl-query-sid.txt, sl-query-todo.txt, sl-query-history.txt.
'@
if ($md.Contains($old) -eq $false) { throw 'session proof block not found' }
$md = $md.Replace($old, $new)
$md = $md.Replace('Completeness: 93. Surfaces A+B+C+D scored. Session turn completed after this file with query proof in the collector.', 'Completeness: 95. Surfaces A+B+C+D scored. Session turn completed and QueryAsync proved persistence (sl-query-sid.txt).')
Set-Content -LiteralPath $mdPath -Value $md -Encoding utf8 -NoNewline

$json = Get-Content -LiteralPath $jsonPath -Raw | ConvertFrom-Json
$json.Completeness = 95
$json | Add-Member -NotePropertyName SessionQueryProof -NotePropertyValue ([pscustomobject]@{
    QueryFile = 'docs/receipts/_hv-c-red-p4/sl-query-sid.txt'
    TurnStatus = 'completed'
    TurnId = 42807
    ActionCount = 6
    DialogCount = 3
    DesignDecisionCount = 2
    ResponseContainsDisagree = $true
}) -Force
$json | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath $jsonPath -Encoding utf8

$mdItem = Get-Item -LiteralPath $mdPath
$jsonItem = Get-Item -LiteralPath $jsonPath
[ordered]@{
    MdLength = $mdItem.Length
    MdLastWriteTimeUtc = $mdItem.LastWriteTimeUtc.ToString('o')
    MdHasDisagree = ((Get-Content -LiteralPath $mdPath -Raw) -match '(?m)^OverallVerdict:\s*DISAGREE\s*$')
    MdHasQueryProof = ((Get-Content -LiteralPath $mdPath -Raw) -match 'QueryAsync')
    JsonLength = $jsonItem.Length
    JsonLastWriteTimeUtc = $jsonItem.LastWriteTimeUtc.ToString('o')
    JsonOverallVerdict = [string](Get-Content -LiteralPath $jsonPath -Raw | ConvertFrom-Json).OverallVerdict
    JsonFailCount = (Get-Content -LiteralPath $jsonPath -Raw | ConvertFrom-Json).FailCount
} | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $out 'receipt-on-disk-after-query.json') -Encoding utf8
Write-Output 'PATCH_RECEIPT_DONE'
