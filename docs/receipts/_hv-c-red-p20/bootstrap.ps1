#Requires -Version 7.0
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$out = 'F:\GitHub\McpServer\docs\receipts\_hv-c-red-p20'
New-Item -ItemType Directory -Force -Path $out | Out-Null
Set-Location -LiteralPath 'F:\GitHub\McpServer'

$env:MCP_WORKSPACE_PATH = 'F:\GitHub\McpServer'
$env:MCPSERVER_WORKSPACE_PATH = 'F:\GitHub\McpServer'
$env:GROK_WORKSPACE_PATH = 'F:\GitHub\McpServer'
$env:PLUGIN_AGENT_NAME = 'GrokSubagentHostile'
$env:MCP_AGENT_NAME = 'GrokSubagentHostile'
$env:GROK_PLUGIN_ROOT = 'F:\GitHub\mcpserver-grok-plugin'
$env:MCP_PLUGIN_ROOT = 'F:\GitHub\mcpserver-grok-plugin'
$env:MCP_PLUGIN_HOST = 'grok'

$plugin = 'F:\GitHub\mcpserver-grok-plugin\lib\Invoke-McpPlugin.ps1'
$cacheRoot = Join-Path $out 'plugin-cache'
New-Item -ItemType Directory -Force -Path $cacheRoot | Out-Null
$utc = [DateTime]::UtcNow.ToString('yyyyMMddTHHmmssZ')
Set-Content -LiteralPath (Join-Path $out 'stamp.txt') -Value $utc -Encoding utf8
Write-Output ("UTC=$utc")

function Save-Text {
    param([string]$Path, $Value)
    if ($null -eq $Value) { $Value = '' }
    if ($Value -isnot [string]) { $Value = ($Value | Out-String) }
    Set-Content -LiteralPath $Path -Value $Value -Encoding utf8
}

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

$pluginJson = 'F:\GitHub\mcpserver-grok-plugin\.grok-plugin\plugin.json'
$pluginVersionFile = 'F:\GitHub\mcpserver-grok-plugin\.version'
$pluginVersion = $null
if (Test-Path -LiteralPath $pluginJson) {
    $pj = Get-Content -LiteralPath $pluginJson -Raw | ConvertFrom-Json
    $pluginVersion = [string]$pj.version
}
$versionFileText = if (Test-Path -LiteralPath $pluginVersionFile) { (Get-Content -LiteralPath $pluginVersionFile -Raw).Trim() } else { $null }
[ordered]@{
    PluginJsonExists = (Test-Path -LiteralPath $pluginJson)
    PluginJsonVersion = $pluginVersion
    VersionFileExists = (Test-Path -LiteralPath $pluginVersionFile)
    VersionFileText = $versionFileText
} | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $out 'plugin-version.json') -Encoding utf8

. 'F:\GitHub\mcpserver-grok-plugin\lib\marker-resolver.ps1'
$sigOk = Test-MarkerSignature -MarkerFile 'F:\GitHub\McpServer\AGENTS-README-FIRST.yaml'
$bootOk = $false
try {
    $bootOk = Invoke-FullBootstrap -StartDir 'F:\GitHub\McpServer'
} catch {
    $bootOk = $false
    Save-Text (Join-Path $out 'plugin-bootstrap.err.txt') $_.Exception.ToString()
}
[ordered]@{
    TimestampUtc = [DateTime]::UtcNow.ToString('o')
    TestMarkerSignature = [bool]$sigOk
    InvokeFullBootstrap = [bool]$bootOk
} | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $out 'plugin-marker-sig.json') -Encoding utf8
Write-Output ("PLUGIN_SIG=$sigOk BOOTSTRAP=$bootOk")

$nonce = 'nonce-hv-' + [DateTime]::UtcNow.ToString('yyyyMMddHHmmss') + '-' + (Get-Random -Maximum 99999)
try {
    $health = Invoke-RestMethod -Uri ("http://PAYTON-LEGION2:7147/health?nonce=$nonce") -Method Get -TimeoutSec 15
    [ordered]@{ nonceSent = $nonce; nonceEcho = $health.nonce; status = $health.status; match = ($health.nonce -eq $nonce); storage = $health.storage; version = $health.version } |
        ConvertTo-Json -Depth 8 | Set-Content -LiteralPath (Join-Path $out 'health-nonce.json') -Encoding utf8
    Write-Output ("HEALTH_NONCE_MATCH=" + ($health.nonce -eq $nonce))
} catch {
    Save-Text (Join-Path $out 'health-nonce.json') $_.Exception.ToString()
    Write-Output 'HEALTH_NONCE_FAIL'
}

try {
    $status = & $plugin -Command Status -WorkspacePath 'F:\GitHub\McpServer' -CacheRoot $cacheRoot -TimeoutSeconds 60 2> (Join-Path $out 'plugin-status.err.txt')
    Save-Text (Join-Path $out 'plugin-status.txt') $status
    Write-Output 'PLUGIN_STATUS_OK'
} catch {
    Save-Text (Join-Path $out 'plugin-status.txt') $_.Exception.ToString()
    Write-Output 'PLUGIN_STATUS_FAIL'
}

$agent = 'GrokSubagentHostile'
$sessionId = "GrokSubagentHostile-$utc-c-red-p20"
$requestId = "req-$utc-001-hostile-c-red-p20"
Set-Content -LiteralPath (Join-Path $out 'session-id.txt') -Value $sessionId -Encoding utf8
Set-Content -LiteralPath (Join-Path $out 'request-id.txt') -Value $requestId -Encoding utf8

Invoke-Plugin -Method 'workflow.sessionlog.bootstrap' -Params @{} -Name 'sl-bootstrap'

Invoke-Plugin -Method 'client.SessionLog.OpenSessionAsync' -Params @{
    agent = $agent
    sessionId = $sessionId
    title = 'Hostile review PLAN-PLUGINHANDOFF-001 C-red-P20 named tests'
    model = 'grok-build-subagent'
} -Name 'sl-open'

Invoke-Plugin -Method 'client.SessionLog.BeginTurnAsync' -Params @{
    agent = $agent
    sessionId = $sessionId
    requestId = $requestId
    queryTitle = 'Hostile C-red-P20 named red tests'
    queryText = 'CLASS 1 hostile C-red-P20. Surfaces A+B+C+D. Review only. Do not mark PLAN-PLUGINHANDOFF-001 or MCP-PLUGININT-001 done. Do not write P20 green artifacts. Attack whether P20 named tests exist as new reds after C-green-P19 r2 AGREE 20260822T104054Z.'
    model = 'grok-build-subagent'
    planFile = 'docs/plans/PLAN-PLUGINHANDOFF-001.md'
    todoId = 'PLAN-PLUGINHANDOFF-001'
} -Name 'sl-begin'

$now = [DateTime]::UtcNow.ToString('o')
Invoke-Plugin -Method 'client.SessionLog.AppendDialogAsync' -Params @{
    agent = $agent
    sessionId = $sessionId
    requestId = $requestId
    items = @(
        @{ timestamp = $now; role = 'model'; category = 'observation'; content = 'add-profile executed first. 18 non-skill profile markdown files read under C:\Users\kingd\.claude\profile (excluded add-profile.grok.md). Work class 1. Surfaces A+B+C+D apply. Scope is C-red-P20 only. Independent re-run of PluginUpdateServiceHarnessTests, live todo_get, live TEST-MCP-PLUGININT-001, P20 product artifact absence, prior 104054Z AGREE confirmation.' }
        @{ timestamp = $now; role = 'model'; category = 'decision'; content = 'Decision: treat this as CLASS 1 C-red-P20 phase-gate. Consequence: AGREE only if 104054Z is independently AGREE FailCount 0, P20 named tests exist as new reds after that AGREE, independent named-filter is Failed 2 Passed 0 Skipped 0 for claimed FileNotFound reasons, product P20 green artifacts are absent, P19 tests are not mixed as passing substitutes, PLAN/PLUGININT remain done false. Alternatives rejected: requiring P20 green UpdateService harness; marking PLAN/PLUGININT done; trusting implementer chat without re-running tests.' }
    )
} -Name 'sl-dialog-start'

Invoke-Plugin -Method 'client.SessionLog.QueryAsync' -Params @{
    agent = $agent
    sessionId = $sessionId
    limit = 5
} -Name 'sl-query-sid'

Invoke-Plugin -Method 'workflow.sessionlog.queryHistory' -Params @{
    sessionId = $sessionId
    limit = 5
} -Name 'sl-query-history'

Invoke-Plugin -Method 'workflow.todo.get' -Params @{ id = 'PLAN-PLUGINHANDOFF-001' } -Name 'todo-plan'
Invoke-Plugin -Method 'client.Todo.GetAsync' -Params @{ id = 'PLAN-PLUGINHANDOFF-001' } -Name 'todo-plan-client'
Invoke-Plugin -Method 'workflow.todo.get' -Params @{ id = 'MCP-PLUGININT-001' } -Name 'todo-pluginint'
Invoke-Plugin -Method 'client.Todo.GetAsync' -Params @{ id = 'MCP-PLUGININT-001' } -Name 'todo-pluginint-client'
Invoke-Plugin -Method 'workflow.todo.get' -Params @{ id = 'MCP-WORKSPACEHYGIENE-002' } -Name 'todo-hygiene'
Invoke-Plugin -Method 'client.Todo.GetAsync' -Params @{ id = 'MCP-WORKSPACEHYGIENE-002' } -Name 'todo-hygiene-client'
Invoke-Plugin -Method 'workflow.requirements.getFr' -Params @{ id = 'FR-MCP-PLUGININT-001' } -Name 'req-fr'
Invoke-Plugin -Method 'workflow.requirements.getTr' -Params @{ id = 'TR-MCP-PLUGININT-001' } -Name 'req-tr'
Invoke-Plugin -Method 'workflow.requirements.getTest' -Params @{ id = 'TEST-MCP-PLUGININT-001' } -Name 'req-test'
Invoke-Plugin -Method 'workflow.requirements.listMappings' -Params @{ frId = 'FR-MCP-PLUGININT-001' } -Name 'req-map'

Write-Output 'BOOTSTRAP_DONE'
