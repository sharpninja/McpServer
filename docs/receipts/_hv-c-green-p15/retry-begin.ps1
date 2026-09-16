#Requires -Version 7.0
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Continue'

$out = 'F:\GitHub\McpServer\docs\receipts\_hv-c-green-p15'
$plugin = 'F:\GitHub\mcpserver-grok-plugin\lib\Invoke-McpPlugin.ps1'
$cacheRoot = Join-Path $out 'plugin-cache'
Set-Location -LiteralPath 'F:\GitHub\McpServer'
$env:MCP_WORKSPACE_PATH = 'F:\GitHub\McpServer'
$env:MCPSERVER_WORKSPACE_PATH = 'F:\GitHub\McpServer'
$env:PLUGIN_AGENT_NAME = 'GrokSubagentHostile'
$env:MCP_AGENT_NAME = 'GrokSubagentHostile'
$env:GROK_PLUGIN_ROOT = 'F:\GitHub\mcpserver-grok-plugin'
$env:MCP_PLUGIN_ROOT = 'F:\GitHub\mcpserver-grok-plugin'
$env:MCP_PLUGIN_HOST = 'grok'

$agent = (Get-Content -LiteralPath (Join-Path $out 'session-id.txt') -Raw).Trim().Split('-')[0]
$sessionId = (Get-Content -LiteralPath (Join-Path $out 'session-id.txt') -Raw).Trim()
$requestId = (Get-Content -LiteralPath (Join-Path $out 'request-id.txt') -Raw).Trim()
Write-Output "SESSION=$sessionId"
Write-Output "REQUEST=$requestId"
Write-Output "AGENT=$agent"

function Invoke-Named {
    param([string]$Name, [string]$Method, [hashtable]$Params)
    $outFile = Join-Path $out ($Name + '.txt')
    $errFile = Join-Path $out ($Name + '.err.txt')
    try {
        $result = & $plugin -Command Invoke -Method $Method -ParamsObject $Params -WorkspacePath 'F:\GitHub\McpServer' -CacheRoot $cacheRoot -TimeoutSeconds 120 2>&1 | ForEach-Object { $_.ToString() }
        $text = ($result | Out-String)
        Set-Content -LiteralPath $outFile -Value $text -Encoding utf8
        if ($errFile) { Set-Content -LiteralPath $errFile -Value '' -Encoding utf8 }
        Write-Output ('OK ' + $Name)
    } catch {
        Set-Content -LiteralPath $outFile -Value ('ERROR ' + $_.Exception.ToString()) -Encoding utf8
        Set-Content -LiteralPath $errFile -Value ($_.ToString()) -Encoding utf8
        Write-Output ('FAIL ' + $Name + ' ' + $_.Exception.Message)
    }
}

Invoke-Named -Name 'sl-begin-retry' -Method 'client.SessionLog.BeginTurnAsync' -Params @{
    agent = $agent
    sessionId = $sessionId
    requestId = $requestId
    queryTitle = 'Hostile C-green-P15 failsafe pending isolation'
    queryText = 'CLASS 1 hostile C-green-P15 after C-red-P15 AGREE 20260822T053539Z. Review only. Do not implement P16. Do not mark TODOs done.'
    model = 'grok-build-subagent'
    planFile = 'docs/plans/PLAN-PLUGINHANDOFF-001.md'
    todoId = 'PLAN-PLUGINHANDOFF-001'
}

Invoke-Named -Name 'sl-begin-wf' -Method 'workflow.sessionlog.beginTurn' -Params @{
    requestId = $requestId
    queryTitle = 'Hostile C-green-P15 failsafe pending isolation'
    queryText = 'CLASS 1 hostile C-green-P15 after C-red-P15 AGREE 20260822T053539Z.'
}

$now = [DateTime]::UtcNow.ToString('o')
Invoke-Named -Name 'sl-dialog-retry' -Method 'client.SessionLog.AppendDialogAsync' -Params @{
    agent = $agent
    sessionId = $sessionId
    requestId = $requestId
    items = @(
        @{ timestamp = $now; role = 'model'; category = 'observation'; content = 'Retry begin after first BeginTurnAsync failed with wrapper exit 1. OpenSession created true. Independent tests running via WMI PID 86108. add-profile 18 files. PLAN and MCP-PLUGININT live todo_get done false.' }
        @{ timestamp = $now; role = 'model'; category = 'decision'; content = 'Decision: keep the same sessionId/requestId rather than opening a duplicate session. Consequence: completeTurn binds to req-20260822T062054Z-001-hostile-c-green-p15. Rejected: new session for the same review.' }
    )
}

Invoke-Named -Name 'sl-query-sid' -Method 'client.SessionLog.QueryAsync' -Params @{
    agent = $agent
    sessionId = $sessionId
    limit = 5
}

Write-Output 'RETRY_BEGIN_DONE'
