#Requires -Version 7.0
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Continue'

$out = 'F:\GitHub\McpServer\docs\receipts\_hv-c-red-p16'
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

$todoYamlStatus = git -C 'F:\GitHub\McpServer' status --porcelain -- docs/Project/TODO.yaml docs/todo.yaml
$pluginintStatus = git -C 'F:\GitHub\McpServer' status --porcelain -- tests/McpServer.PluginIntegration.Tests
[ordered]@{
    TimestampUtc = [DateTime]::UtcNow.ToString('o')
    TodoYamlPorcelain = [string]$todoYamlStatus
    PluginintPorcelain = [string]$pluginintStatus
} | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $out 'todo-yaml-git-status-final.json') -Encoding utf8

Write-Output 'REGET_DONE'
