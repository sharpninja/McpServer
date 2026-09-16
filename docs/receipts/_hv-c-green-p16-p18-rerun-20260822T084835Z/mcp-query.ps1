#Requires -Version 7.0
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Continue'

$out = 'F:\GitHub\McpServer\docs\receipts\_hv-c-green-p16-p18-rerun-20260822T084835Z'
$plugin = 'F:\GitHub\mcpserver-grok-plugin\lib\Invoke-McpPlugin.ps1'
$cacheRoot = Join-Path $out 'plugin-cache'
Set-Location -LiteralPath 'F:\GitHub\McpServer'
$env:MCP_WORKSPACE_PATH = 'F:\GitHub\McpServer'
$env:MCPSERVER_WORKSPACE_PATH = 'F:\GitHub\McpServer'
$env:GROK_WORKSPACE_PATH = 'F:\GitHub\McpServer'
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

Invoke-Plugin -Method 'client.Todo.GetAsync' -Params @{ id = 'PLAN-PLUGINHANDOFF-001' } -Name 'todo-plan-client'
Invoke-Plugin -Method 'workflow.todo.get' -Params @{ id = 'PLAN-PLUGINHANDOFF-001' } -Name 'todo-plan'
Invoke-Plugin -Method 'client.Todo.GetAsync' -Params @{ id = 'MCP-PLUGININT-001' } -Name 'todo-pluginint-client'
Invoke-Plugin -Method 'workflow.todo.get' -Params @{ id = 'MCP-PLUGININT-001' } -Name 'todo-pluginint'
Invoke-Plugin -Method 'workflow.requirements.getFr' -Params @{ id = 'FR-MCP-PLUGININT-001' } -Name 'req-fr'
Invoke-Plugin -Method 'workflow.requirements.getTr' -Params @{ id = 'TR-MCP-PLUGININT-001' } -Name 'req-tr'
Invoke-Plugin -Method 'workflow.requirements.getTest' -Params @{ id = 'TEST-MCP-PLUGININT-001' } -Name 'req-test'
Invoke-Plugin -Method 'workflow.requirements.listMappings' -Params @{ frId = 'FR-MCP-PLUGININT-001' } -Name 'req-map'

function Extract-TodoDone {
    param([string]$Path, [string]$Dest)
    if (-not (Test-Path -LiteralPath $Path)) {
        [ordered]@{ exists = $false; path = $Path } | ConvertTo-Json | Set-Content -LiteralPath $Dest -Encoding utf8
        return
    }
    $text = Get-Content -LiteralPath $Path -Raw
    $topDone = $null
    if ($text -match '(?m)^\s+done:\s+(true|false)\s*$') { $topDone = ($Matches[1] -eq 'true') }
    $tasks = [regex]::Matches($text, '(?m)^\s+- task:\s*(.+?)\r?\n\s+done:\s+(true|false)')
    $taskObjs = foreach ($m in $tasks) {
        [pscustomobject]@{ task = $m.Groups[1].Value.Trim(); done = ($m.Groups[2].Value -eq 'true') }
    }
    [ordered]@{
        exists = $true
        path = $Path
        length = (Get-Item -LiteralPath $Path).Length
        lastWriteTimeUtc = (Get-Item -LiteralPath $Path).LastWriteTimeUtc.ToString('o')
        topDone = $topDone
        taskCount = @($taskObjs).Count
        doneTrueCount = @($taskObjs | Where-Object { $_.done }).Count
        p16 = @($taskObjs | Where-Object { $_.task -match 'P16' })
        p17 = @($taskObjs | Where-Object { $_.task -match 'P17' })
        p18 = @($taskObjs | Where-Object { $_.task -match 'P18' })
        p19 = @($taskObjs | Where-Object { $_.task -match 'P19' })
        combinedC = @($taskObjs | Where-Object { $_.task -match 'C P16|C-green-P16|P16 red \+ hostile then P17' })
        tasks = $taskObjs
        hasError = [bool]($text -match '(?i)ERROR |not found|401|403')
    } | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath $Dest -Encoding utf8
}

function Extract-Req {
    param([string]$Path, [string]$Dest, [string]$Kind)
    if (-not (Test-Path -LiteralPath $Path)) {
        [ordered]@{ exists = $false; kind = $Kind } | ConvertTo-Json | Set-Content -LiteralPath $Dest -Encoding utf8
        return
    }
    $text = Get-Content -LiteralPath $Path -Raw
    $id = $null
    if ($text -match '(?m)^\s+id:\s+(\S+)') { $id = $Matches[1] }
    $title = $null
    if ($text -match '(?m)^\s+title:\s+(.+)$') { $title = $Matches[1].Trim() }
    $status = $null
    if ($text -match '(?m)^\s+status:\s+(\S+)') { $status = $Matches[1] }
    $isSatisfied = $null
    if ($text -match '(?m)^\s+isSatisfied:\s+(true|false)') { $isSatisfied = ($Matches[1] -eq 'true') }
    $acObjs = foreach ($m in [regex]::Matches($text, '(?ms)^\s+- id:\s*(ac-\d+|AC\d+)\s*\r?\n\s+text:\s*(.+?)(?=\r?\n\s+- id:|\r?\n\s+[a-zA-Z]|\z)')) {
        [pscustomobject]@{ id = $m.Groups[1].Value; text = $m.Groups[2].Value.Trim() }
    }
    [ordered]@{
        exists = $true
        kind = $Kind
        path = $Path
        length = (Get-Item -LiteralPath $Path).Length
        id = $id
        title = $title
        status = $status
        isSatisfied = $isSatisfied
        acCount = @($acObjs).Count
        acs = $acObjs
        snippet = if ($text.Length -gt 5000) { $text.Substring(0, 5000) } else { $text }
        hasError = [bool]($text -match '(?i)ERROR |not found|401|403')
    } | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath $Dest -Encoding utf8
}

Extract-TodoDone -Path (Join-Path $out 'todo-plan.txt') -Dest (Join-Path $out 'todo-plan-done-extract.json')
Extract-TodoDone -Path (Join-Path $out 'todo-plan-client.txt') -Dest (Join-Path $out 'todo-plan-client-done-extract.json')
Extract-TodoDone -Path (Join-Path $out 'todo-pluginint.txt') -Dest (Join-Path $out 'todo-pluginint-done-extract.json')
Extract-TodoDone -Path (Join-Path $out 'todo-pluginint-client.txt') -Dest (Join-Path $out 'todo-pluginint-client-done-extract.json')
Extract-Req -Path (Join-Path $out 'req-fr.txt') -Dest (Join-Path $out 'req-fr-extract.json') -Kind 'FR'
Extract-Req -Path (Join-Path $out 'req-tr.txt') -Dest (Join-Path $out 'req-tr-extract.json') -Kind 'TR'
Extract-Req -Path (Join-Path $out 'req-test.txt') -Dest (Join-Path $out 'req-test-extract.json') -Kind 'TEST'
if (Test-Path -LiteralPath (Join-Path $out 'req-map.txt')) {
    $mapText = Get-Content -LiteralPath (Join-Path $out 'req-map.txt') -Raw
    [ordered]@{
        exists = $true
        hasFr = [bool]($mapText -match 'FR-MCP-PLUGININT-001')
        hasTr = [bool]($mapText -match 'TR-MCP-PLUGININT-001')
        hasTest = [bool]($mapText -match 'TEST-MCP-PLUGININT-001')
        snippet = if ($mapText.Length -gt 4000) { $mapText.Substring(0, 4000) } else { $mapText }
    } | ConvertTo-Json -Depth 6 | Set-Content -LiteralPath (Join-Path $out 'req-map-extract.json') -Encoding utf8
}

$gitTodo = git -C 'F:\GitHub\McpServer' status --porcelain -- docs/Project/TODO.yaml docs/todo.yaml
[ordered]@{
    TimestampUtc = [DateTime]::UtcNow.ToString('o')
    Porcelain = [string]$gitTodo
} | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $out 'todo-yaml-git-status.json') -Encoding utf8

Write-Output 'MCP_QUERY_DONE'
