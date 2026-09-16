#Requires -Version 7.0
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$out = 'F:\GitHub\McpServer\docs\receipts\_hv-c-red-p15\hv2'
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
    PluginJsonPath = $pluginJson
} | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $out 'plugin-version.json') -Encoding utf8

. 'F:\GitHub\McpServer\plugins\core\lib-ps\marker-resolver.ps1'
$coreSigOk = Test-MarkerSignature -MarkerFile 'F:\GitHub\McpServer\AGENTS-README-FIRST.yaml'
$coreBootOk = $false
try {
    $coreBootOk = Invoke-FullBootstrap -StartDir 'F:\GitHub\McpServer'
} catch {
    $coreBootOk = $false
    Save-Text (Join-Path $out 'core-bootstrap.err.txt') $_.Exception.ToString()
}
[ordered]@{
    TimestampUtc = [DateTime]::UtcNow.ToString('o')
    MarkerResolverPath = 'F:\GitHub\McpServer\plugins\core\lib-ps\marker-resolver.ps1'
    TestMarkerSignature = [bool]$coreSigOk
    InvokeFullBootstrap = [bool]$coreBootOk
} | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $out 'core-marker-sig.json') -Encoding utf8
Write-Output ("CORE_SIG=$coreSigOk CORE_BOOTSTRAP=$coreBootOk")

$nonce = 'nonce-hv2-' + [DateTime]::UtcNow.ToString('yyyyMMddHHmmss') + '-' + (Get-Random -Maximum 99999)
try {
    $health = Invoke-RestMethod -Uri ("http://PAYTON-LEGION2:7147/health?nonce=$nonce") -Method Get -TimeoutSec 15
    [ordered]@{
        nonceSent = $nonce
        nonceEcho = $health.nonce
        status = $health.status
        match = ($health.nonce -eq $nonce)
        storage = $health.storage
        TimestampUtc = [DateTime]::UtcNow.ToString('o')
    } | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath (Join-Path $out 'health-nonce.json') -Encoding utf8
    Write-Output ("HEALTH_NONCE_MATCH=" + ($health.nonce -eq $nonce) + " SENT=$nonce ECHO=" + $health.nonce)
} catch {
    Save-Text (Join-Path $out 'health-nonce.json') $_.Exception.ToString()
    Write-Output 'HEALTH_NONCE_FAIL'
}

$raw = Get-Content -LiteralPath 'F:\GitHub\McpServer\AGENTS-README-FIRST.yaml' -Raw
function Get-Scalar([string]$text, [string]$key) {
    if ($text -match "(?m)^${key}:\s*(.+)$") { return $Matches[1].Trim() }
    throw "missing $key"
}
$apiKey = REDACTED $raw 'apiKey'
$headers = @{ 'X-Api-Key' = $apiKey }
try {
    $search = Invoke-RestMethod -Uri 'http://PAYTON-LEGION2:7147/mcpserver/tools/search?keyword=mcpserver-grok-plugin' -Headers $headers -Method Get -TimeoutSec 30
    $search | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath (Join-Path $out 'tool-search-grok.json') -Encoding utf8
    $exact = $false
    if ($search.PSObject.Properties.Name -contains 'tools' -and $null -ne $search.tools) {
        $exact = @($search.tools | Where-Object { $_.name -eq 'mcpserver-grok-plugin' }).Count -gt 0
    }
    if (-not $exact -and $search.PSObject.Properties.Name -contains 'items' -and $null -ne $search.items) {
        $exact = @($search.items | Where-Object { $_.name -eq 'mcpserver-grok-plugin' }).Count -gt 0
    }
    Write-Output ("TOOL_SEARCH_EXACT=$exact")
} catch {
    Save-Text (Join-Path $out 'tool-search-grok.json') $_.Exception.ToString()
    Write-Output 'TOOL_SEARCH_FAIL'
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
$sessionId = "GrokSubagentHostile-$utc-pluginhandoff-p15red"
$requestId = "req-$utc-001-hostile-c-red-p15-failsafe"
Set-Content -LiteralPath (Join-Path $out 'session-id.txt') -Value $sessionId -Encoding utf8
Set-Content -LiteralPath (Join-Path $out 'request-id.txt') -Value $requestId -Encoding utf8
Write-Output ("SESSION=$sessionId")
Write-Output ("REQUEST=$requestId")

Invoke-Plugin -Method 'workflow.sessionlog.bootstrap' -Params @{} -Name 'sl-bootstrap'

Invoke-Plugin -Method 'client.SessionLog.OpenSessionAsync' -Params @{
    agent = $agent
    sessionId = $sessionId
    title = 'Hostile review PLAN-PLUGINHANDOFF-001 C-red-P15 failsafe pending isolation'
    model = 'grok-build-subagent'
} -Name 'sl-open'

Invoke-Plugin -Method 'client.SessionLog.BeginTurnAsync' -Params @{
    agent = $agent
    sessionId = $sessionId
    requestId = $requestId
    queryTitle = 'Hostile C-red-P15 failsafe pending isolation red gate'
    queryText = 'CLASS 1 hostile review of PLAN-PLUGINHANDOFF-001 Phase C-red-P15 ONLY. Surfaces A+B+C+D. Independently re-run the three named P15 theories. AGREE only if Failed 24 Passed 0 Skipped 0, P16 absent from filter, PLAN and MCP-PLUGININT remain done false. Review only. Do not implement P15 green. Do not write P16. Do not mark TODOs done.'
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
        @{ timestamp = $now; role = 'model'; category = 'observation'; content = 'add-profile executed first. 18 non-skill profile markdown files read under C:\Users\kingd\.claude\profile (excluded add-profile.grok.md). Work class 1. Surfaces A+B+C+D apply. Scope is C-red-P15 after claimed C-green-P14 AGREE receipts. Starting independent P15 theory rerun, live todo_get, V4 failsafe path check.' }
        @{ timestamp = $now; role = 'model'; category = 'decision'; content = 'Decision: treat this as CLASS 1 project implementation red-phase gate for P15 only. Consequence: AGREE only if the three named theories exist with eight PluginHostKind InlineData rows each, no Skip, independent filter Failed 24 Passed 0 Skipped 0, P16 AiTheory names absent from the filter, PLAN-PLUGINHANDOFF-001 and MCP-PLUGININT-001 remain done false. Alternatives: requiring P15 green or plan closeout. Rejected: parent forbids implementing P15 green, writing P16, and treating this as plan closeout.' }
    )
} -Name 'sl-dialog-start'

Invoke-Plugin -Method 'client.SessionLog.AppendActionsAsync' -Params @{
    agent = $agent
    sessionId = $sessionId
    requestId = $requestId
    actions = @(
        @{ order = 1; description = 'add-profile skill executed; 18 non-skill profile markdown files read'; type = 'observation'; status = 'completed'; filePath = 'C:\Users\kingd\.claude\profile' }
        @{ order = 2; description = 'Test-MarkerSignature via plugins/core/lib-ps/marker-resolver.ps1'; type = 'observation'; status = 'completed'; filePath = 'F:\GitHub\McpServer\plugins\core\lib-ps\marker-resolver.ps1' }
        @{ order = 3; description = 'GET /health with random nonce; exact echo required'; type = 'observation'; status = 'completed'; filePath = '' }
        @{ order = 4; description = 'CLASS 1: treat as C-red-P15 inter-phase red gate. AGREE does not close PLAN or authorize P16.'; type = 'design_decision'; status = 'completed'; filePath = 'docs/plans/PLAN-PLUGINHANDOFF-001.md' }
    )
} -Name 'sl-actions-start'

Invoke-Plugin -Method 'workflow.sessionlog.queryHistory' -Params @{
    agent = $agent
    sessionId = $sessionId
    limit = 5
    offset = 0
} -Name 'sl-query-history'

Invoke-Plugin -Method 'client.SessionLog.GetSessionAsync' -Params @{
    agent = $agent
    sessionId = $sessionId
} -Name 'sl-get-session'

Write-Output 'BOOTSTRAP_DONE'
