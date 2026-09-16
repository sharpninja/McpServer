$ErrorActionPreference = 'Continue'
$out = 'F:\GitHub\McpServer\docs\receipts\_hv-b5-20260821T224147Z'
Set-Location 'F:\GitHub\McpServer'
$plugin = 'F:\GitHub\mcpserver-grok-plugin\lib\Invoke-McpPlugin.ps1'
$env:MCP_WORKSPACE_PATH = 'F:\GitHub\McpServer'
$env:PLUGIN_AGENT_NAME = 'GrokSubagentHostile'
$env:MCP_AGENT_NAME = 'GrokSubagentHostile'
$env:GROK_PLUGIN_ROOT = 'F:\GitHub\mcpserver-grok-plugin'
$env:MCP_PLUGIN_ROOT = 'F:\GitHub\mcpserver-grok-plugin'

function Save-Text {
    param($Path, $Value)
    if ($null -eq $Value) { $Value = '' }
    if ($Value -isnot [string]) { $Value = ($Value | Out-String) }
    Set-Content -LiteralPath $Path -Value $Value -Encoding utf8
}

function Invoke-Plugin {
    param([string]$Method, [hashtable]$Params, [string]$Name)
    $outFile = Join-Path $out ($Name + '.txt')
    $errFile = Join-Path $out ($Name + '.err.txt')
    try {
        $result = & $plugin -Command Invoke -Method $Method -ParamsObject $Params -WorkspacePath 'F:\GitHub\McpServer' -TimeoutSeconds 90 2> $errFile
        Save-Text -Path $outFile -Value $result
        Write-Output ('PLUGIN_OK ' + $Name)
    } catch {
        Save-Text -Path $outFile -Value ('ERROR ' + $_.Exception.ToString())
        Write-Output ('PLUGIN_FAIL ' + $Name + ' ' + $_.Exception.Message)
    }
}

Invoke-Plugin -Method 'workflow.todo.get' -Params @{ id = 'PLAN-PLUGINHANDOFF-001' } -Name 'todo-plan'
Invoke-Plugin -Method 'workflow.todo.get' -Params @{ id = 'MCP-PLUGINCORE-004' } -Name 'todo-plugincore'
Invoke-Plugin -Method 'workflow.todo.get' -Params @{ id = 'MCP-WORKSPACEHYGIENE-002' } -Name 'todo-hygiene'

# Requirements via client if available
foreach ($pair in @(
    @{ n = 'req-FR-MCP-REPL-009'; m = 'client.Requirements.GetFrAsync'; p = @{ id = 'FR-MCP-REPL-009' } },
    @{ n = 'req-FR-MCP-PLUGINCORE-004'; m = 'client.Requirements.GetFrAsync'; p = @{ id = 'FR-MCP-PLUGINCORE-004' } },
    @{ n = 'req-TR-MCP-REPL-012'; m = 'client.Requirements.GetTrAsync'; p = @{ id = 'TR-MCP-REPL-012' } },
    @{ n = 'req-TR-MCP-REPL-010'; m = 'client.Requirements.GetTrAsync'; p = @{ id = 'TR-MCP-REPL-010' } },
    @{ n = 'req-TEST-MCP-195'; m = 'client.Requirements.GetTestAsync'; p = @{ id = 'TEST-MCP-195' } }
)) {
    Invoke-Plugin -Method $pair.m -Params $pair.p -Name $pair.n
}
