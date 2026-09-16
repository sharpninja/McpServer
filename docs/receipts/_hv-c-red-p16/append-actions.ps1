#Requires -Version 7.0
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$out = 'F:\GitHub\McpServer\docs\receipts\_hv-c-red-p16'
$plugin = 'F:\GitHub\mcpserver-grok-plugin\lib\Invoke-McpPlugin.ps1'
$cacheRoot = Join-Path $out 'plugin-cache'
$sessionId = (Get-Content -LiteralPath (Join-Path $out 'session-id.txt') -Raw).Trim()
$requestId = (Get-Content -LiteralPath (Join-Path $out 'request-id.txt') -Raw).Trim()
$agent = 'GrokSubagentHostile'
$env:PLUGIN_AGENT_NAME = $agent
$env:MCP_AGENT_NAME = $agent
$env:GROK_PLUGIN_ROOT = 'F:\GitHub\mcpserver-grok-plugin'
$env:MCP_PLUGIN_ROOT = 'F:\GitHub\mcpserver-grok-plugin'
$env:MCP_WORKSPACE_PATH = 'F:\GitHub\McpServer'

function Invoke-Plugin {
    param([string]$Method, [hashtable]$Params, [string]$Name)
    $outFile = Join-Path $out ($Name + '.txt')
    $errFile = Join-Path $out ($Name + '.err.txt')
    try {
        $result = & $plugin -Command Invoke -Method $Method -ParamsObject $Params -WorkspacePath 'F:\GitHub\McpServer' -CacheRoot $cacheRoot -TimeoutSeconds 120 2> $errFile
        if ($null -eq $result) { $result = '' }
        if ($result -isnot [string]) { $result = ($result | Out-String) }
        Set-Content -LiteralPath $outFile -Value $result -Encoding utf8
        Write-Output ('OK ' + $Name)
    } catch {
        Set-Content -LiteralPath $outFile -Value ('ERROR ' + $_.Exception.ToString()) -Encoding utf8
        Write-Output ('FAIL ' + $Name + ' ' + $_.Exception.Message)
    }
}

Invoke-Plugin -Method 'workflow.sessionlog.appendActions' -Params @{
    actions = @(
        @{
            order = 1
            type = 'design_decision'
            status = 'completed'
            filePath = 'docs/plans/PLAN-PLUGINHANDOFF-001.md'
            description = 'CLASS 1 C-red-P16 only. AGREE requires Failed 8 Passed 0 Skipped 0 on AiTheory_Agent_RequiresValidJsonFields and unimplemented EvaluateAsync. Not plan closeout.'
        }
        @{
            order = 2
            type = 'edit'
            status = 'completed'
            filePath = 'docs/receipts/_hv-c-red-p16'
            description = 'Collector bootstrap, file scans, and independent P16 filter rerun under docs/receipts/_hv-c-red-p16.'
        }
    )
} -Name 'sl-actions-wf'

$now = [DateTime]::UtcNow.ToString('o')
Invoke-Plugin -Method 'client.SessionLog.AppendDialogAsync' -Params @{
    agent = $agent
    sessionId = $sessionId
    requestId = $requestId
    items = @(
        @{ timestamp = $now; role = 'model'; category = 'observation'; content = 'client.SessionLog.AppendActionsAsync is not a valid SessionLogClient method. Using workflow.sessionlog.appendActions plus PatchTurnAsync for action persistence.' }
        @{ timestamp = $now; role = 'model'; category = 'decision'; content = 'Decision: persist actions through workflow.sessionlog.appendActions because AppendActionsAsync is unknown on SessionLogClient. Consequence: session-log completeness uses the documented workflow method. Alternatives: PatchTurnAsync only. Rejected as sole path because workflow.appendActions is the skill contract for actions.' }
    )
} -Name 'sl-dialog-actions'

Write-Output 'APPEND_ACTIONS_DONE'
