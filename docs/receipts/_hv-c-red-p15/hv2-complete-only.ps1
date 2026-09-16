#Requires -Version 7.0
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Continue'

$out = 'F:\GitHub\McpServer\docs\receipts\_hv-c-red-p15\hv2'
$plugin = 'F:\GitHub\mcpserver-grok-plugin\lib\Invoke-McpPlugin.ps1'
$cacheRoot = Join-Path $out 'plugin-cache'
Set-Location -LiteralPath 'F:\GitHub\McpServer'
$env:MCP_WORKSPACE_PATH = 'F:\GitHub\McpServer'
$env:MCPSERVER_WORKSPACE_PATH = 'F:\GitHub\McpServer'
$env:PLUGIN_AGENT_NAME = 'GrokSubagentHostile'
$env:MCP_AGENT_NAME = 'GrokSubagentHostile'
$env:GROK_PLUGIN_ROOT = 'F:\GitHub\mcpserver-grok-plugin'
$env:MCP_PLUGIN_ROOT = 'F:\GitHub\mcpserver-grok-plugin'

$agent = 'GrokSubagentHostile'
$sessionId = 'GrokSubagentHostile-20260822T052958Z-pluginhandoff-p15red'
$requestId = 'req-20260822T052958Z-001-hostile-c-red-p15-failsafe'

function Invoke-Plugin {
    param([string]$Method, [hashtable]$Params, [string]$Name)
    $outFile = Join-Path $out ($Name + '.txt')
    $errFile = Join-Path $out ($Name + '.err.txt')
    try {
        $result = & $plugin -Command Invoke -Method $Method -ParamsObject $Params -WorkspacePath 'F:\GitHub\McpServer' -CacheRoot $cacheRoot -TimeoutSeconds 180 2> $errFile
        if ($null -eq $result) { $result = '' }
        if ($result -isnot [string]) { $result = ($result | Out-String) }
        Set-Content -LiteralPath $outFile -Value $result -Encoding utf8
        Write-Output ('OK ' + $Name)
    } catch {
        Set-Content -LiteralPath $outFile -Value ('ERROR ' + $_.Exception.ToString()) -Encoding utf8
        Write-Output ('FAIL ' + $Name + ' ' + $_.Exception.Message)
    }
}

Invoke-Plugin -Method 'client.SessionLog.CompleteTurnAsync' -Params @{
    agent = $agent
    sessionId = $sessionId
    requestId = $requestId
    payload = @{
        requestId = $requestId
        status = 'completed'
        response = 'Hostile C-red-P15 review DISAGREE. Receipt docs/receipts/hostile-validator-20260822T060633Z.md. Independent P15 filter Passed 24 Failed 0 Skipped 0. Adapter/tests rewritten 05:40:18Z during this red gate. First isolated run Failed 22 then crash against throw-not-implemented. PLAN-PLUGINHANDOFF-001 and MCP-PLUGININT-001 remain done false. This DISAGREE does not authorize P15 green or P16 and is not plan closeout.'
        queryTitle = 'Hostile C-red-P15 failsafe pending isolation red gate'
        interpretation = 'Hostile review Phase C-red-P15 ONLY. Green-before-red. Review only. Do not mark TODOs done.'
        tags = @('hostile-validator','PLAN-PLUGINHANDOFF-001','MCP-PLUGININT-001','C-red-P15')
        filesModified = @('docs/receipts/hostile-validator-20260822T060633Z.md','docs/receipts/hostile-validator-20260822T060633Z.json')
    }
} -Name 'sl-complete'

Invoke-Plugin -Method 'workflow.sessionlog.completeTurn' -Params @{
    response = 'Hostile C-red-P15 review DISAGREE. Receipt docs/receipts/hostile-validator-20260822T060633Z.md. Independent P15 filter Passed 24 Failed 0 Skipped 0. Green-before-red. PLAN and PLUGININT remain done false.'
} -Name 'sl-complete-wf'

Invoke-Plugin -Method 'client.SessionLog.QueryAsync' -Params @{
    agent = $agent
    sessionId = $sessionId
    limit = 5
} -Name 'sl-query-after'

Invoke-Plugin -Method 'workflow.sessionlog.queryHistory' -Params @{
    agent = $agent
    sessionId = $sessionId
    limit = 5
    offset = 0
} -Name 'sl-query-history-after'

Write-Output 'COMPLETE_ONLY_DONE'
