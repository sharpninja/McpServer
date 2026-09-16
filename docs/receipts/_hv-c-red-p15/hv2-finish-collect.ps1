#Requires -Version 7.0
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Continue'

$out = 'F:\GitHub\McpServer\docs\receipts\_hv-c-red-p15\hv2'
Set-Location -LiteralPath 'F:\GitHub\McpServer'
$env:MCP_WORKSPACE_PATH = 'F:\GitHub\McpServer'
$env:MCPSERVER_WORKSPACE_PATH = 'F:\GitHub\McpServer'
$env:PLUGIN_AGENT_NAME = 'GrokSubagentHostile'
$env:MCP_AGENT_NAME = 'GrokSubagentHostile'
$env:GROK_PLUGIN_ROOT = 'F:\GitHub\mcpserver-grok-plugin'
$env:MCP_PLUGIN_ROOT = 'F:\GitHub\mcpserver-grok-plugin'
$plugin = 'F:\GitHub\mcpserver-grok-plugin\lib\Invoke-McpPlugin.ps1'
$cacheRoot = Join-Path $out 'plugin-cache'

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
Invoke-Plugin -Method 'workflow.todo.get' -Params @{ id = 'MCP-PLUGININT-001' } -Name 'todo-pluginint-final'
Invoke-Plugin -Method 'workflow.requirements.getTest' -Params @{ id = 'TEST-MCP-PLUGININT-001' } -Name 'req-test-final'

$list = Get-Content -LiteralPath (Join-Path $out 'dotnet-list-tests.log')
$fqn = @($list | Where-Object { $_ -match '^\s+McpServer\.PluginIntegration\.Tests\.' })
$counts = [ordered]@{
    TimestampUtc = [DateTime]::UtcNow.ToString('o')
    ListFqn = $fqn.Count
    P15Success = @($fqn | Where-Object { $_ -match 'Theory_Agent_Success_NoPendingFailsafe' }).Count
    P15FailedSubmit = @($fqn | Where-Object { $_ -match 'Theory_Agent_FailedSubmit_RetainsRootIdPending' }).Count
    P15Retry = @($fqn | Where-Object { $_ -match 'Theory_Agent_RetrySuccess_DeletesOnlyMatchingPending' }).Count
    P16 = @($fqn | Where-Object { $_ -match 'AiTheory_' }).Count
    SkipListed = @($fqn | Where-Object { $_ -match 'SKIP|skipped' }).Count
}
$counts | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $out 'list-counts.json') -Encoding utf8
Write-Output ($counts | ConvertTo-Json)

$todoYamlStatus = git -C 'F:\GitHub\McpServer' status --porcelain -- docs/Project/TODO.yaml docs/todo.yaml
[ordered]@{
    TimestampUtc = [DateTime]::UtcNow.ToString('o')
    TodoYamlPorcelain = [string]$todoYamlStatus
} | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $out 'todo-yaml-git-status-final.json') -Encoding utf8

Write-Output 'FINISH_COLLECT_DONE'
