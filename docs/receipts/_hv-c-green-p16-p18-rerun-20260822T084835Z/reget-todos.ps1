#Requires -Version 7.0
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Continue'

$out = 'F:\GitHub\McpServer\docs\receipts\_hv-c-green-p16-p18-rerun-20260822T084835Z'
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

Invoke-Plugin -Method 'client.Todo.GetAsync' -Params @{ id = 'PLAN-PLUGINHANDOFF-001' } -Name 'todo-plan-client-final'
Invoke-Plugin -Method 'client.Todo.GetAsync' -Params @{ id = 'MCP-PLUGININT-001' } -Name 'todo-pluginint-client-final'
$gitTodo = git -C 'F:\GitHub\McpServer' status --porcelain -- docs/Project/TODO.yaml docs/todo.yaml
[ordered]@{ TimestampUtc = [DateTime]::UtcNow.ToString('o'); Porcelain = [string]$gitTodo } | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $out 'todo-yaml-git-status-final.json') -Encoding utf8

function Extract-TodoDone {
    param([string]$Path, [string]$Dest)
    $text = Get-Content -LiteralPath $Path -Raw
    $topDone = $null
    if ($text -match '(?m)^\s+done:\s+(true|false)\s*$') { $topDone = ($Matches[1] -eq 'true') }
    $tasks = [regex]::Matches($text, '(?m)^\s+- task:\s*(.+?)\r?\n\s+done:\s+(true|false)')
    $taskObjs = foreach ($m in $tasks) {
        [pscustomobject]@{ task = $m.Groups[1].Value.Trim(); done = ($m.Groups[2].Value -eq 'true') }
    }
    [ordered]@{
        topDone = $topDone
        p16 = @($taskObjs | Where-Object { $_.task -match 'P16' })
        p17 = @($taskObjs | Where-Object { $_.task -match 'P17' })
        p18 = @($taskObjs | Where-Object { $_.task -match 'P18' })
        p19 = @($taskObjs | Where-Object { $_.task -match 'P19' })
        combinedC = @($taskObjs | Where-Object { $_.task -match 'C P16|P16 red \+ hostile then P17' })
    } | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath $Dest -Encoding utf8
}

Extract-TodoDone -Path (Join-Path $out 'todo-plan-client-final.txt') -Dest (Join-Path $out 'todo-plan-final-done.json')
Extract-TodoDone -Path (Join-Path $out 'todo-pluginint-client-final.txt') -Dest (Join-Path $out 'todo-pluginint-final-done.json')
Write-Output 'REGET_DONE'
