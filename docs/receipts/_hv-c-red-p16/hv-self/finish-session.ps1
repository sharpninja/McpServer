#Requires -Version 7.0
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$out = 'F:\GitHub\McpServer\docs\receipts\_hv-c-red-p16\hv-self'
$cacheRoot = Join-Path $out 'plugin-cache'
New-Item -ItemType Directory -Force -Path $cacheRoot | Out-Null
$plugin = 'F:\GitHub\mcpserver-grok-plugin\lib\Invoke-McpPlugin.ps1'
$agent = 'GrokSubagentHostile'
$sessionId = 'GrokSubagentHostile-20260822T072417Z-c-red-p16'
$requestId = 'req-20260822T072417Z-001-hostile-c-red-p16'
$env:PLUGIN_AGENT_NAME = $agent
$env:MCP_AGENT_NAME = $agent
$env:GROK_PLUGIN_ROOT = 'F:\GitHub\mcpserver-grok-plugin'
$env:MCP_PLUGIN_ROOT = 'F:\GitHub\mcpserver-grok-plugin'
$env:MCP_WORKSPACE_PATH = 'F:\GitHub\McpServer'
$env:MCPSERVER_WORKSPACE_PATH = 'F:\GitHub\McpServer'

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

Invoke-Plugin -Method 'workflow.todo.get' -Params @{ id = 'PLAN-PLUGINHANDOFF-001' } -Name 'todo-plan-final'
Invoke-Plugin -Method 'client.Todo.GetAsync' -Params @{ id = 'PLAN-PLUGINHANDOFF-001' } -Name 'todo-plan-client-final'
Invoke-Plugin -Method 'workflow.todo.get' -Params @{ id = 'MCP-PLUGININT-001' } -Name 'todo-pluginint-final'
Invoke-Plugin -Method 'client.Todo.GetAsync' -Params @{ id = 'MCP-PLUGININT-001' } -Name 'todo-pluginint-client-final'

$now = [DateTime]::UtcNow.ToString('o')
Invoke-Plugin -Method 'client.SessionLog.AppendDialogAsync' -Params @{
    agent = $agent
    sessionId = $sessionId
    requestId = $requestId
    items = @(
        @{ timestamp = $now; role = 'model'; category = 'observation'; content = 'Independent hv-self filter: FilterExitCode 1, DurationMs from hv-tests-done.json. TRX parse next. Sibling collector overwrote shared _hv-c-red-p16 session-id.txt with GrokSubagentHostile-20260822T072515Z-pluginhandoff-p16red. This turn remains 20260822T072417Z.' }
        @{ timestamp = $now; role = 'model'; category = 'decision'; content = 'Decision: score C-red-P16 from hv-self unique ResultsDirectory TRX, not the sibling results-20260822T072653Z files. Consequence: AGREE only if owned TRX is Failed 8 Passed 0 Skipped/notExecuted 0 with unimplemented EvaluateAsync on all eight hosts. Alternatives: trust sibling TRX. Rejected: hostile must re-run independently after collector collision.' }
    )
} -Name 'sl-dialog-end'

Invoke-Plugin -Method 'client.SessionLog.PatchTurnAsync' -Params @{
    agent = $agent
    sessionId = $sessionId
    requestId = $requestId
    interpretation = 'Hostile C-red-P16 review of PLAN-PLUGINHANDOFF-001 AiTheory companion rows. Independent rerun after sibling collector collision on shared _hv-c-red-p16 path.'
    response = 'Independent hv-self filter completed. Parsing TRX and live todo_get before overall verdict.'
    tags = @('hostile', 'C-red-P16', 'PLAN-PLUGINHANDOFF-001', 'TEST-MCP-PLUGININT-001', 'FR-MCP-PLUGININT-001', 'TR-MCP-PLUGININT-001')
} -Name 'sl-patch'

Invoke-Plugin -Method 'client.SessionLog.QueryAsync' -Params @{
    agent = $agent
    sessionId = $sessionId
    limit = 5
} -Name 'sl-query-sid'

Invoke-Plugin -Method 'workflow.sessionlog.queryHistory' -Params @{
    agent = $agent
    limit = 5
    offset = 0
} -Name 'sl-query-history'

Write-Output 'FINISH_MID_DONE'
