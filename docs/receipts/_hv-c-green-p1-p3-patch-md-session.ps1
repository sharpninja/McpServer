#Requires -Version 7.0
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
$path = 'F:\GitHub\McpServer\docs\receipts\hostile-validator-20260821T234422Z.md'
$text = Get-Content -LiteralPath $path -Raw
$old = 'Persisted. SessionId GrokSubagentHostile-20260821T234007Z-c-green-p1-p3. Turn requestId req-20260821T234007Z-001-hostile-c-green-p1-p3. client.SessionLog.OpenSessionAsync created=true. BeginTurnAsync turnId 42804. Proof files: docs/receipts/_hv-c-green-p1-p3/sl-open.txt, sl-begin.txt, plus sl-dialog.txt / sl-patch.txt / sl-complete.txt / sl-query-*.txt written after this receipt.'
$new = 'Persisted. SessionId GrokSubagentHostile-20260821T234007Z-c-green-p1-p3. Turn requestId req-20260821T234007Z-001-hostile-c-green-p1-p3. client.SessionLog.OpenSessionAsync created=true. BeginTurnAsync turnId 42804. AppendDialogAsync totalDialogCount 3. CompleteTurnAsync same turnId. client.SessionLog.QueryAsync agent=GrokSubagentHostile totalCount=21; this session is first item; turn status completed; 6 actions with unquoted integer order; 3 processingDialog items; 2 designDecisions. Proof: docs/receipts/_hv-c-green-p1-p3/sl-open.txt, sl-begin.txt, sl-dialog.txt, sl-patch.txt, sl-complete.txt, sl-query-agent.txt, sl-query-sid.txt, sl-query-todo.txt.'
if ($text.IndexOf($old) -lt 0) { throw 'session proof paragraph not found' }
$text = $text.Replace($old, $new)
Set-Content -LiteralPath $path -Value $text -Encoding utf8 -NoNewline
Write-Output 'MD_SESSION_PROOF_PATCHED'
