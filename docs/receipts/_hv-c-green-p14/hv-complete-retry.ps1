#Requires -Version 7.0
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Continue'

$out = 'F:\GitHub\McpServer\docs\receipts\_hv-c-green-p14'
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

$agent = 'GrokSubagentHostile'
$sessionId = 'GrokSubagentHostile-20260822T042653Z-c-green-p14'
$requestId = 'req-20260822T042653Z-001-hostile-c-green-p14'
$response = 'Hostile C-green-P14 review AGREE. Receipt docs/receipts/hostile-validator-20260822T050748Z.md. P14 filter Passed 8 Failed 0 Skipped 0. Full PluginIntegration.Tests Passed 62 Failed 0 Skipped 0. P15/P16 absent. PLAN-PLUGINHANDOFF-001 and MCP-PLUGININT-001 remain done false. This AGREE is not plan closeout.'

function Save-Result {
    param([string]$Name, $Value)
    if ($null -eq $Value) { $Value = '' }
    if ($Value -isnot [string]) { $Value = ($Value | Out-String) }
    Set-Content -LiteralPath (Join-Path $out ($Name + '.txt')) -Value $Value -Encoding utf8
}

try {
    $r1 = & $plugin -Command Invoke -Method 'workflow.sessionlog.completeTurn' -ParamsObject @{
        response = $response
    } -WorkspacePath 'F:\GitHub\McpServer' -CacheRoot $cacheRoot -TimeoutSeconds 120 2> (Join-Path $out 'sl-complete-wf.err.txt')
    Save-Result 'sl-complete-wf' $r1
    Write-Output 'OK sl-complete-wf'
} catch {
    Save-Result 'sl-complete-wf' ('ERROR ' + $_.Exception.ToString())
    Write-Output ('FAIL sl-complete-wf ' + $_.Exception.Message)
}

try {
    $r2 = & $plugin -Command CompleteTurn -Response $response -WorkspacePath 'F:\GitHub\McpServer' -CacheRoot $cacheRoot -TimeoutSeconds 120 2> (Join-Path $out 'sl-complete-cmd.err.txt')
    Save-Result 'sl-complete-cmd' $r2
    Write-Output 'OK sl-complete-cmd'
} catch {
    Save-Result 'sl-complete-cmd' ('ERROR ' + $_.Exception.ToString())
    Write-Output ('FAIL sl-complete-cmd ' + $_.Exception.Message)
}

try {
    $r3 = & $plugin -Command Invoke -Method 'client.SessionLog.CompleteTurnAsync' -ParamsObject @{
        agent = $agent
        sessionId = $sessionId
        requestId = $requestId
        response = $response
        queryTitle = 'Hostile C-green-P14 PLUGIN_ROOT_OVERRIDE isolation'
        status = 'completed'
    } -WorkspacePath 'F:\GitHub\McpServer' -CacheRoot $cacheRoot -TimeoutSeconds 120 2> (Join-Path $out 'sl-complete-flat.err.txt')
    Save-Result 'sl-complete-flat' $r3
    Write-Output 'OK sl-complete-flat'
} catch {
    Save-Result 'sl-complete-flat' ('ERROR ' + $_.Exception.ToString())
    Write-Output ('FAIL sl-complete-flat ' + $_.Exception.Message)
}

try {
    $q = & $plugin -Command Invoke -Method 'client.SessionLog.QueryAsync' -ParamsObject @{
        agent = $agent
        sessionId = $sessionId
        limit = 20
    } -WorkspacePath 'F:\GitHub\McpServer' -CacheRoot $cacheRoot -TimeoutSeconds 120 2> (Join-Path $out 'sl-query-sid-after.err.txt')
    Save-Result 'sl-query-sid-after' $q
    Write-Output 'OK sl-query-sid-after'
} catch {
    Save-Result 'sl-query-sid-after' ('ERROR ' + $_.Exception.ToString())
    Write-Output ('FAIL sl-query-sid-after ' + $_.Exception.Message)
}

try {
    $h = & $plugin -Command Invoke -Method 'workflow.sessionlog.queryHistory' -ParamsObject @{
        agent = $agent
        limit = 10
    } -WorkspacePath 'F:\GitHub\McpServer' -CacheRoot $cacheRoot -TimeoutSeconds 120 2> (Join-Path $out 'sl-query-history-after.err.txt')
    Save-Result 'sl-query-history-after' $h
    Write-Output 'OK sl-query-history-after'
} catch {
    Save-Result 'sl-query-history-after' ('ERROR ' + $_.Exception.ToString())
    Write-Output ('FAIL sl-query-history-after ' + $_.Exception.Message)
}

Write-Output 'COMPLETE_RETRY_DONE'
