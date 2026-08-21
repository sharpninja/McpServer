#Requires -Version 7.0
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$workspace = 'F:\GitHub\McpServer'
$pluginRoot = 'F:\GitHub\mcpserver-grok-plugin'
$invoke = Join-Path $pluginRoot 'lib\Invoke-McpPlugin.ps1'
$outDir = Join-Path $workspace 'docs\receipts\_hv-happly-hdone'
$cacheRoot = Join-Path $outDir 'plugin-cache'
$sessionId = 'GrokCode-20260820T125722Z-happly-hdone-align'
$requestId = 'req-20260820T125722Z-001-hostile-apply-done-align'

$env:PLUGIN_AGENT_NAME = 'GrokCode'
$env:MCP_AGENT_NAME = 'GrokCode'
$env:MCP_PLUGIN_HOST = 'grok'
$env:MCP_PLUGIN_ROOT = $pluginRoot
$env:MCP_WORKSPACE_PATH = $workspace
$env:GROK_WORKSPACE_PATH = $workspace
$env:GROK_PLUGIN_ROOT = $pluginRoot

function Invoke-PluginMethod {
    param([string]$Method, [string]$Params = '', [int]$TimeoutSeconds = 90)
    $argList = @(
        '-NoProfile','-NonInteractive','-File',$invoke,
        '-Command','Invoke','-Method',$Method,
        '-WorkspacePath',$workspace,'-PluginRoot',$pluginRoot,
        '-CacheRoot',$cacheRoot,'-TimeoutSeconds',[string]$TimeoutSeconds
    )
    if ($Params) { $argList += @('-Params', $Params) }
    $stdout = & pwsh.exe @argList 2>&1 | Out-String
    return [ordered]@{ method = $Method; exitCode = $LASTEXITCODE; stdout = $stdout; isError = ($stdout -match '(?m)^type: error'); has503 = ($stdout -match '503|backend_unavailable') }
}

$audit = Invoke-PluginMethod 'workflow.todo.get' 'id: PLAN-TODOAUDIT-001'
$audit.stdout | Set-Content -LiteralPath (Join-Path $outDir '12-todo-get-TR-AUDIT-001.txt') -Encoding utf8
$rem = ''
$m = [regex]::Match($audit.stdout, '(?ims)^\s+remaining:\s*(.+?)(?=\r?\n\s+(?:note|description|technicalDetails|implementationTasks|dependsOn|functionalRequirements|deprecated):|\z)')
if ($m.Success) { $rem = $m.Groups[1].Value.Trim() }
[ordered]@{
    exitCode = $audit.exitCode
    remainingLength = $rem.Length
    hasOrphanReason = $rem.Contains('OrphanReason')
    remaining = $rem
} | ConvertTo-Json -Depth 5 | Set-Content -LiteralPath (Join-Path $outDir '12-tr-audit-remaining.json') -Encoding utf8
Write-Output ('AUDIT_ORPHAN=' + $rem.Contains('OrphanReason'))
Write-Output ('AUDIT_LEN=' + $rem.Length)

$dialog = Invoke-PluginMethod 'workflow.sessionlog.appendDialog' @"
dialogItems:
  - timestamp: $([datetime]::UtcNow.ToString('o'))
    role: model
    content: 'Decision: AGREE H-apply/H-done only if live todo_list Remaining cites 2026-08-20T101500Z for all S0 IDs, leftover stays done, 160-163 stay open, Handoff FR stay in_progress, placeholder notes exist, generate timestamps match, ValidateTraceability findings=0, ALIGN still done false.'
    category: decision
"@
Write-Output ('DIALOG=' + $dialog.exitCode + ' 503=' + $dialog.has503)
$actions = Invoke-PluginMethod 'workflow.sessionlog.appendActions' @"
actions:
  - order: 1
    description: Live todo_list done=false count 41; S0 40 missingDate empty; leftover done true; ALIGN done false; Handoff FR in_progress; TR-02..14 noted; generate 2026-08-20T12:50:45.7597304Z.
    type: design_decision
    status: completed
    filePath: docs/receipts/_hv-happly-hdone/04-s0-remaining.json
"@
Write-Output ('ACTIONS=' + $actions.exitCode + ' 503=' + $actions.has503)
$complete = Invoke-PluginMethod 'workflow.sessionlog.completeTurn' @"
response: Hostile H-apply H-done PLAN-TODOALIGN-001 store hygiene review complete.
"@
Write-Output ('COMPLETE=' + $complete.exitCode + ' 503=' + $complete.has503 + ' ERR=' + $complete.isError)
$hist = Invoke-PluginMethod 'workflow.sessionlog.queryHistory' @"
agent: GrokCode
limit: 8
offset: 0
"@
Write-Output ('HIST=' + $hist.exitCode)
Write-Output ('HIST_HAS_SID=' + $hist.stdout.Contains($sessionId))
Write-Output ('HIST_HAS_TITLE=' + $hist.stdout.Contains('Hostile H-apply H-done'))
$hist.stdout | Set-Content -LiteralPath (Join-Path $outDir '12-queryHistory.txt') -Encoding utf8
[ordered]@{
    dialog = $dialog
    actions = $actions
    complete = $complete
    historyHasSessionId = $hist.stdout.Contains($sessionId)
    historyHasTitle = $hist.stdout.Contains('Hostile H-apply H-done')
    historySnippet = if ($hist.stdout.Length -gt 2500) { $hist.stdout.Substring(0,2500) } else { $hist.stdout }
} | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath (Join-Path $outDir '12-session-complete.json') -Encoding utf8
