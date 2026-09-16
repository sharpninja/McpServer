#Requires -Version 7.0
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$path = 'F:\GitHub\McpServer\docs\receipts\hostile-validator-20260822T132620Z.md'
$text = Get-Content -LiteralPath $path -Raw

$oldIntro = 'Default was FAIL or UNKNOWN until this pass independently: executed add-profile (18 files); verified plugin marker signature and health nonce; continued dedicated session GrokSubagentHostile-20260822T131910Z-c-red-p20 turn req-20260822T131910Z-001-hostile-c-red-p20 BeginTurnAsync turnId 42975; live-got PLAN-PLUGINHANDOFF-001, MCP-PLUGININT-001, MCP-WORKSPACEHYGIENE-002, TEST-MCP-PLUGININT-001; grepped P20 and P19 names; confirmed pluginint-p20 dirs absent; rebuilt and re-ran FullyQualifiedName~PluginUpdateServiceHarnessTests.'
$newIntro = 'Default was FAIL or UNKNOWN until this pass independently: executed add-profile (18 files); verified plugin marker signature and health nonce; opened dedicated rebuild session GrokSubagentHostile-20260822T133042Z-c-red-p20-rebuild turn req-20260822T133042Z-001-hostile-c-red-p20-rebuild BeginTurnAsync turnId 42986; live-got PLAN-PLUGINHANDOFF-001, MCP-PLUGININT-001, MCP-WORKSPACEHYGIENE-002, TEST-MCP-PLUGININT-001; grepped P20 and P19 names; confirmed pluginint-p20 dirs absent; rebuilt and re-ran FullyQualifiedName~PluginUpdateServiceHarnessTests.'
if (-not $text.Contains($oldIntro)) { throw 'intro paragraph not found' }
$text = $text.Replace($oldIntro, $newIntro)

$oldProof = 'Dedicated persistence is through plugin client.SessionLog.* as GrokSubagentHostile. SessionId GrokSubagentHostile-20260822T131910Z-c-red-p20. Turn requestId req-20260822T131910Z-001-hostile-c-red-p20. BeginTurnAsync turnId 42975. QueryAsync after begin shows this session with observation plus decision dialog. CompleteTurnAsync same requestId after this receipt. Proof files under docs/receipts/_hv-c-red-p20/ (sl-open.txt, sl-begin.txt, sl-dialog-start.txt, sl-query-sid.txt, independent-sl-dialog-mid.txt, sl-complete.txt, sl-query-sid-after.txt).'
$newProof = 'Dedicated persistence is through plugin client.SessionLog.* as GrokSubagentHostile. SessionId GrokSubagentHostile-20260822T133042Z-c-red-p20-rebuild. Turn requestId req-20260822T133042Z-001-hostile-c-red-p20-rebuild. BeginTurnAsync/CompleteTurnAsync turnId 42986. QueryAsync after complete shows this session with completed turn, actions, and response citing this receipt. Proof files under docs/receipts/_hv-c-red-p20/ (own-sl-open.txt, own-sl-begin.txt, own-sl-dialog.txt, own-sl-patch.txt, own-sl-complete.txt, own-sl-query-sid-after.txt). A sibling collector session GrokSubagentHostile-20260822T131910Z-c-red-p20 turn 42975 completed earlier citing docs/receipts/hostile-validator-20260822T132413Z.md; this rebuild session is the persistence proof for 132620Z.'
if (-not $text.Contains($oldProof)) { throw 'proof paragraph not found' }
$text = $text.Replace($oldProof, $newProof)

$oldResidual = '- A first collector pass used `--no-build`. This review rebuilt (`McpServer.PluginIntegration.Tests ->` in independent-dotnet-p20-filter.log) and got the same two FileNotFoundException failures.'
$newResidual = '- A sibling collector session used `--no-build` and wrote docs/receipts/hostile-validator-20260822T132413Z.md. This review rebuilt (`McpServer.PluginIntegration.Tests ->` in independent-dotnet-p20-filter.log) and got the same two FileNotFoundException failures. 131910Z CompleteTurn from this rebuild instance failed because that turn was already completed; persistence for 132620Z is session 133042Z turn 42986.'
if (-not $text.Contains($oldResidual)) { throw 'residual bullet not found' }
$text = $text.Replace($oldResidual, $newResidual)

Set-Content -LiteralPath $path -Value $text -Encoding utf8
Write-Output 'PATCHED_MD'
