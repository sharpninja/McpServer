#Requires -Version 7.0
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$pluginRoot = 'F:\GitHub\mcpserver-grok-plugin'
$workspace = 'F:\GitHub\McpServer'
$ev = Join-Path $workspace 'docs\receipts\_hv-20260819T160006Z'
$invoke = Join-Path $pluginRoot 'lib\Invoke-McpPlugin.ps1'
$hostileCache = Join-Path $ev 'hostile-cache'

$env:MCP_PLUGIN_ROOT = $pluginRoot
$env:GROK_PLUGIN_ROOT = $pluginRoot
$env:PLUGIN_AGENT_NAME = 'GrokCode'
$env:MCP_AGENT_NAME = 'GrokCode'
$env:MCP_WORKSPACE_PATH = $workspace
Set-Location -LiteralPath $workspace

function Save-Text {
    param([string]$Name, [string]$Value)
    $path = Join-Path $ev $Name
    Set-Content -LiteralPath $path -Value $Value -Encoding utf8
    return $path
}

function Get-RootTurnId {
    $p = Join-Path $workspace '.mcpServer\grok\current-turn.yaml'
    $raw = Get-Content -LiteralPath $p -Raw
    $m = [regex]::Match($raw, 'turnRequestId:\s*(.+)')
    $s = [regex]::Match($raw, 'status:\s*(.+)')
    return [ordered]@{
        requestId = $m.Groups[1].Value.Trim()
        status = $s.Groups[1].Value.Trim()
        lastWriteUtc = (Get-Item -LiteralPath $p).LastWriteTimeUtc.ToString('o')
        length = (Get-Item -LiteralPath $p).Length
    }
}

function Invoke-PluginMethod {
    param(
        [Parameter(Mandatory)][string]$Method,
        [hashtable]$Params = @{},
        [string]$CacheRoot = '',
        [int]$TimeoutSeconds = 90
    )
    $args = @{
        Command = 'Invoke'
        Method = $Method
        WorkspacePath = $workspace
        PluginRoot = $pluginRoot
        TimeoutSeconds = $TimeoutSeconds
    }
    if ($Params.Count -gt 0) { $args['ParamsObject'] = $Params }
    if ($CacheRoot) { $args['CacheRoot'] = $CacheRoot }
    return & $invoke @args
}

Write-Output '=== ROOT TURN BEFORE ==='
$before = Get-RootTurnId
$before | ConvertTo-Json | Set-Content (Join-Path $ev 'root-turn-before-plugin.json') -Encoding utf8
$before | ConvertTo-Json

Write-Output '=== STATUS ==='
try {
    $status = & $invoke -Command Status -WorkspacePath $workspace -PluginRoot $pluginRoot -TimeoutSeconds 90
    Save-Text '01-status.txt' ([string]$status) | Out-Null
    Write-Output ([string]$status)
} catch {
    Save-Text '01-status.err.txt' ($_.Exception.ToString()) | Out-Null
    Write-Output "STATUS FAIL $($_.Exception.Message)"
}

Write-Output '=== QUERY title Remediate hook cache isolation ==='
try {
    $q1 = Invoke-PluginMethod -Method 'client.SessionLog.QueryAsync' -Params @{
        agent = 'GrokCode'
        text = 'Remediate hook cache isolation'
        limit = 20
    }
    Save-Text '02-query-title.txt' ([string]$q1) | Out-Null
    Write-Output "Q1_LEN=$([string]$q1.Length)"
} catch {
    Save-Text '02-query-title.err.txt' ($_.Exception.ToString()) | Out-Null
    Write-Output "Q1 FAIL $($_.Exception.Message)"
}

Write-Output '=== QUERY queryText remediATE hostile FAILs ==='
try {
    $q2 = Invoke-PluginMethod -Method 'client.SessionLog.QueryAsync' -Params @{
        agent = 'GrokCode'
        text = 'remediATE the hostile FAILs'
        limit = 20
    }
    Save-Text '03-query-querytext.txt' ([string]$q2) | Out-Null
    Write-Output "Q2_LEN=$([string]$q2.Length)"
} catch {
    Save-Text '03-query-querytext.err.txt' ($_.Exception.ToString()) | Out-Null
    Write-Output "Q2 FAIL $($_.Exception.Message)"
}

Write-Output '=== QUERY plugin-session ==='
try {
    $q3 = Invoke-PluginMethod -Method 'client.SessionLog.QueryAsync' -Params @{
        agent = 'GrokCode'
        text = 'plugin-session'
        limit = 5
    }
    Save-Text '04-query-plugin-session.txt' ([string]$q3) | Out-Null
    Write-Output "Q3_LEN=$([string]$q3.Length)"
} catch {
    Save-Text '04-query-plugin-session.err.txt' ($_.Exception.ToString()) | Out-Null
    Write-Output "Q3 FAIL $($_.Exception.Message)"
}

Write-Output '=== QUERY window 15:30-16:10 ==='
try {
    $q4 = Invoke-PluginMethod -Method 'client.SessionLog.QueryAsync' -Params @{
        agent = 'GrokCode'
        from = '2026-08-19T15:30:00Z'
        to = '2026-08-19T16:10:00Z'
        limit = 20
    }
    Save-Text '05-query-window.txt' ([string]$q4) | Out-Null
    Write-Output "Q4_LEN=$([string]$q4.Length)"
} catch {
    Save-Text '05-query-window.err.txt' ($_.Exception.ToString()) | Out-Null
    Write-Output "Q4 FAIL $($_.Exception.Message)"
}

Write-Output '=== QUERYHISTORY ==='
try {
    $qh = Invoke-PluginMethod -Method 'workflow.sessionlog.queryHistory' -Params @{
        agent = 'GrokCode'
        limit = 15
        offset = 0
    }
    Save-Text '06-queryHistory.txt' ([string]$qh) | Out-Null
    Write-Output "QH_LEN=$([string]$qh.Length)"
} catch {
    Save-Text '06-queryHistory.err.txt' ($_.Exception.ToString()) | Out-Null
    Write-Output "QH FAIL $($_.Exception.Message)"
}

Write-Output '=== TODO GET PLAN-TRIAGECLUSTER-001 ==='
try {
    $todo = Invoke-PluginMethod -Method 'workflow.todo.get' -Params @{
        id = 'PLAN-TRIAGECLUSTER-001'
    }
    Save-Text '07-todo-plan.txt' ([string]$todo) | Out-Null
    Write-Output ([string]$todo).Substring(0, [Math]::Min(4000, ([string]$todo).Length))
} catch {
    Save-Text '07-todo-plan.err.txt' ($_.Exception.ToString()) | Out-Null
    Write-Output "TODO FAIL $($_.Exception.Message)"
}

Write-Output '=== GET FR ==='
try {
    $fr = Invoke-PluginMethod -Method 'workflow.requirements.getFr' -Params @{
        id = 'FR-MCP-TRIAGEPLUGIN-001'
    }
    Save-Text '08-getFr.txt' ([string]$fr) | Out-Null
    Write-Output ([string]$fr).Substring(0, [Math]::Min(3000, ([string]$fr).Length))
} catch {
    Save-Text '08-getFr.err.txt' ($_.Exception.ToString()) | Out-Null
    Write-Output "FR FAIL $($_.Exception.Message)"
}

Write-Output '=== GET TEST 001 ==='
try {
    $test = Invoke-PluginMethod -Method 'workflow.requirements.getTest' -Params @{
        id = 'TEST-MCP-TRIAGEPLUGIN-001'
    }
    Save-Text '09-getTest.txt' ([string]$test) | Out-Null
    Write-Output ([string]$test).Substring(0, [Math]::Min(2500, ([string]$test).Length))
} catch {
    Save-Text '09-getTest.err.txt' ($_.Exception.ToString()) | Out-Null
    Write-Output "TEST FAIL $($_.Exception.Message)"
}

Write-Output '=== ROOT TURN AFTER QUERIES ==='
$afterQ = Get-RootTurnId
$afterQ | ConvertTo-Json | Set-Content (Join-Path $ev 'root-turn-after-queries.json') -Encoding utf8
$afterQ | ConvertTo-Json
Copy-Item (Join-Path $workspace '.mcpServer\grok\current-turn.yaml') (Join-Path $ev 'current-turn-after-queries.yaml') -Force
Copy-Item (Join-Path $workspace '.mcpServer\grok\session-state.yaml') (Join-Path $ev 'session-state-after-queries.yaml') -Force
