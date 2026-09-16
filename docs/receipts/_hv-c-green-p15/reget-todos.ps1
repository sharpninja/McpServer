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
function Invoke-Named {
    param([string]$Name, [string]$Method, [hashtable]$Params)
    $outFile = Join-Path $out ($Name + '.txt')
    try {
        $result = & $plugin -Command Invoke -Method $Method -ParamsObject $Params -WorkspacePath 'F:\GitHub\McpServer' -CacheRoot $cacheRoot -TimeoutSeconds 120 2>&1 | ForEach-Object { $_.ToString() }
        Set-Content -LiteralPath $outFile -Value (($result | Out-String)) -Encoding utf8
        Write-Output ('OK ' + $Name)
    } catch {
        Set-Content -LiteralPath $outFile -Value ('ERROR ' + $_.Exception.ToString()) -Encoding utf8
        Write-Output ('FAIL ' + $Name + ' ' + $_.Exception.Message)
    }
}
Invoke-Named -Name 'todo-plan-final' -Method 'client.Todo.GetAsync' -Params @{ id = 'PLAN-PLUGINHANDOFF-001' }
Invoke-Named -Name 'todo-pluginint-final' -Method 'client.Todo.GetAsync' -Params @{ id = 'MCP-PLUGININT-001' }
$todoYamlStatus = git -C 'F:\GitHub\McpServer' status --porcelain -- docs/Project/TODO.yaml docs/todo.yaml
[ordered]@{
    TimestampUtc = [DateTime]::UtcNow.ToString('o')
    TodoYamlPorcelain = [string]$todoYamlStatus
} | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $out 'todo-yaml-git-status-after.json') -Encoding utf8
Write-Output ('TODO_YAML=' + [string]$todoYamlStatus)
Write-Output ('UTC=' + [DateTime]::UtcNow.ToString('yyyyMMddTHHmmssZ'))
