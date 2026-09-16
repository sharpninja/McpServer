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

$turn = @{
    requestId = $requestId
    queryTitle = 'Hostile C-red-P15 failsafe pending isolation red gate'
    status = 'completed'
    response = 'Hostile C-red-P15 review DISAGREE. Receipt docs/receipts/hostile-validator-20260822T060633Z.md. Independent P15 filter Passed 24 Failed 0 Skipped 0. Peer 053539Z AGREEd C-red Failed 24 at 05:35:39 then green rewrite 05:40:18. This duplicate session does not re-AGREE red. PLAN and PLUGININT remain done false. Does not authorize P16.'
    interpretation = 'Duplicate C-red-P15 hostile after peer 053539Z AGREE. Current filter is green. Review only. Do not mark TODOs done.'
    tags = @('hostile-validator','PLAN-PLUGINHANDOFF-001','MCP-PLUGININT-001','C-red-P15')
    filesModified = @('docs/receipts/hostile-validator-20260822T060633Z.md','docs/receipts/hostile-validator-20260822T060633Z.json')
}

Invoke-Plugin -Method 'client.SessionLog.CompleteTurnAsync' -Params @{
    agent = $agent
    sessionId = $sessionId
    requestId = $requestId
    turn = $turn
} -Name 'sl-complete-turn'

Invoke-Plugin -Method 'client.SessionLog.QueryAsync' -Params @{
    agent = $agent
    sessionId = $sessionId
    limit = 3
} -Name 'sl-query-final'

Write-Output 'COMPLETE_TURN_DONE'
