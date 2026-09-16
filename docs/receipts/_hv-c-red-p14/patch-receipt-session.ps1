#Requires -Version 7.0
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$path = 'F:\GitHub\McpServer\docs\receipts\hostile-validator-20260822T041327Z.md'
$text = Get-Content -LiteralPath $path -Raw
$old = 'Dedicated persistence is through plugin `client.SessionLog.*` as GrokSubagentHostile. SessionId GrokSubagentHostile-20260822T040409Z-c-red-p14. Turn requestId req-20260822T040409Z-001-hostile-c-red-p14. client.SessionLog.OpenSessionAsync created=true. BeginTurnAsync turnId 42878. CompleteTurnAsync proof is written by finish-session.ps1 after this receipt lands. QueryAsync/queryHistory proof files: docs/receipts/_hv-c-red-p14/sl-query-sid-after.txt and sl-query-history-after.txt.'
$new = 'Dedicated persistence is through plugin `client.SessionLog.*` as GrokSubagentHostile. SessionId GrokSubagentHostile-20260822T040409Z-c-red-p14. Turn requestId req-20260822T040409Z-001-hostile-c-red-p14. client.SessionLog.OpenSessionAsync created=true. BeginTurnAsync turnId 42878. CompleteTurnAsync same turnId. QueryAsync agent=GrokSubagentHostile sessionId match: turn status completed, filesModified includes docs/receipts/hostile-validator-20260822T041327Z.md and .json, 7 actions with integer order, 4 processingDialog items (observation + decision), 2 designDecisions. workflow.sessionlog.queryHistory lists this session first. Proof: docs/receipts/_hv-c-red-p14/sl-open.txt, sl-begin.txt, sl-dialog.txt, sl-patch.txt, sl-complete.txt, sl-query-sid-after.txt, sl-query-history-after.txt.'
if ($text.IndexOf($old) -lt 0) { throw 'session proof paragraph not found' }
$text = $text.Replace($old, $new)
Set-Content -LiteralPath $path -Value $text -Encoding utf8
Write-Output 'PATCH_DONE'
