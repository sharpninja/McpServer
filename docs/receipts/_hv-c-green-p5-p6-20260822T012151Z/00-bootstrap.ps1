#Requires -Version 7.0
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$out = 'F:\GitHub\McpServer\docs\receipts\_hv-c-green-p5-p6-20260822T012151Z'
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
        $result = & $plugin -Command Invoke -Method $Method -ParamsObject $Params -WorkspacePath 'F:\GitHub\McpServer' -TimeoutSeconds 120 2> $errFile
        if ($null -eq $result) { $result = '' }
        if ($result -isnot [string]) { $result = ($result | Out-String) }
        Set-Content -LiteralPath $outFile -Value $result -Encoding utf8
        Write-Output ('OK ' + $Name)
    } catch {
        Set-Content -LiteralPath $outFile -Value ('ERROR ' + $_.Exception.ToString()) -Encoding utf8
        Write-Output ('FAIL ' + $Name + ' ' + $_.Exception.Message)
    }
}

. 'F:\GitHub\McpServer\plugins\core\lib-ps\marker-resolver.ps1'
$markerPath = 'F:\GitHub\McpServer\AGENTS-README-FIRST.yaml'
$sigPlugin = Test-MarkerSignature -MarkerFile $markerPath
$pluginSig = [ordered]@{
    TimestampUtc = [DateTime]::UtcNow.ToString('o')
    TestMarkerSignature = [bool]$sigPlugin
}
$pluginSig | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $out 'plugin-marker-sig.json') -Encoding utf8
Write-Output ("PLUGIN_SIG=$sigPlugin")

$raw = Get-Content -LiteralPath $markerPath -Raw
function Get-Scalar([string]$text, [string]$key) {
    if ($text -match "(?m)^${key}:\s*(.+)$") { return $Matches[1].Trim() }
    throw "missing $key"
}
function Get-Endpoint([string]$text, [string]$key) {
    if ($text -match "(?m)^\s+${key}:\s*(.+)$") { return $Matches[1].Trim() }
    throw "missing endpoint $key"
}
$apiKey = REDACTED $raw 'apiKey'
$port = Get-Scalar $raw 'port'
$baseUrl = Get-Scalar $raw 'baseUrl'
$workspace = Get-Scalar $raw 'workspace'
$workspacePath = Get-Scalar $raw 'workspacePath'
$markerPid = Get-Scalar $raw 'pid'
$startedAt = Get-Scalar $raw 'startedAt'
$markerWrittenAtUtc = Get-Scalar $raw 'markerWrittenAtUtc'
$serverStartedAtUtc = Get-Scalar $raw 'serverStartedAtUtc'
$sigValue = if ($raw -match '(?m)^\s+value:\s*([0-9A-Fa-f]+)') { $Matches[1] } else { throw 'missing signature value' }
$policy = if ($raw -match '(?m)^\s+policy:\s*(.+)$') { $Matches[1].Trim() } else { throw 'missing policy' }
$digest = if ($raw -match '(?m)^\s+contract_digest:\s*(.+)$') { $Matches[1].Trim() } else { throw 'missing digest' }
$payload = @"
canonicalization=marker-v1
port=$port
baseUrl=$baseUrl
apiKey=REDACTED
workspace=$workspace
workspacePath=$workspacePath
pid=$markerPid
startedAt=$startedAt
markerWrittenAtUtc=$markerWrittenAtUtc
serverStartedAtUtc=$serverStartedAtUtc
endpoints.health=$(Get-Endpoint $raw 'health')
endpoints.swagger=$(Get-Endpoint $raw 'swagger')
endpoints.swaggerUi=$(Get-Endpoint $raw 'swaggerUi')
endpoints.mcpTransport=$(Get-Endpoint $raw 'mcpTransport')
endpoints.sessionLog=$(Get-Endpoint $raw 'sessionLog')
endpoints.sessionLogDialog=$(Get-Endpoint $raw 'sessionLogDialog')
endpoints.contextSearch=$(Get-Endpoint $raw 'contextSearch')
endpoints.contextPack=$(Get-Endpoint $raw 'contextPack')
endpoints.contextSources=$(Get-Endpoint $raw 'contextSources')
endpoints.todo=$(Get-Endpoint $raw 'todo')
endpoints.repo=$(Get-Endpoint $raw 'repo')
endpoints.desktop=$(Get-Endpoint $raw 'desktop')
endpoints.gitHub=$(Get-Endpoint $raw 'gitHub')
endpoints.tools=$(Get-Endpoint $raw 'tools')
endpoints.workspace=$(Get-Endpoint $raw 'workspace')
endpoints.serverStartupUtc=$(Get-Endpoint $raw 'serverStartupUtc')
endpoints.markerFileTimestamp=$(Get-Endpoint $raw 'markerFileTimestamp')
agentPlugins.policy=$policy
agentPlugins.contractDigest=$digest
"@
$hmac = [System.Security.Cryptography.HMACSHA256]::new([System.Text.Encoding]::UTF8.GetBytes($apiKey))
$hash = $hmac.ComputeHash([System.Text.Encoding]::UTF8.GetBytes($payload))
$hex = ([BitConverter]::ToString($hash) -replace '-', '').ToUpperInvariant()
$sigMatch = [string]::Equals($hex, $sigValue.ToUpperInvariant())
[ordered]@{
    TimestampUtc = [DateTime]::UtcNow.ToString('o')
    Computed = $hex
    MarkerValue = $sigValue.ToUpperInvariant()
    Match = $sigMatch
    PayloadLength = $payload.Length
} | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $out 'marker-sig.json') -Encoding utf8
Write-Output ("HOMEMADE_SIG_MATCH=$sigMatch")

$nonce = [guid]::NewGuid().ToString('N')
try {
    $health = Invoke-RestMethod -Uri ("http://PAYTON-LEGION2:7147/health?nonce=$nonce") -Method Get -TimeoutSec 15
    $healthObj = [ordered]@{ nonceSent = $nonce; nonceEcho = $health.nonce; status = $health.status; match = ($health.nonce -eq $nonce); storage = $health.storage; pid = $health.pid; version = $health.version }
    $healthObj | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath (Join-Path $out 'health-nonce.json') -Encoding utf8
    Write-Output ("HEALTH_NONCE_MATCH=" + $healthObj.match)
    Write-Output ("HEALTH_STATUS=" + $health.status)
} catch {
    Save-Text (Join-Path $out 'health-nonce.json') $_.Exception.ToString()
    Write-Output 'HEALTH_NONCE_FAIL'
}

$headers = @{ 'X-Api-Key' = $apiKey }
try {
    $search = Invoke-RestMethod -Uri 'http://PAYTON-LEGION2:7147/mcpserver/tools/search?keyword=mcpserver-grok-plugin' -Headers $headers -Method Get -TimeoutSec 30
    $search | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath (Join-Path $out 'tool-search-grok.json') -Encoding utf8
    $names = @()
    if ($search.items) { $names = @($search.items | ForEach-Object { $_.name }) }
    $exact = $names -contains 'mcpserver-grok-plugin'
    Write-Output ('TOOL_SEARCH_EXACT=' + $exact)
} catch {
    Save-Text (Join-Path $out 'tool-search-grok.json') $_.Exception.ToString()
    Write-Output 'TOOL_SEARCH_FAIL'
}

try {
    $status = & $plugin -Command Status -WorkspacePath 'F:\GitHub\McpServer' -TimeoutSeconds 60 2> (Join-Path $out 'plugin-status.err.txt')
    Save-Text (Join-Path $out 'plugin-status.txt') $status
    Write-Output 'PLUGIN_STATUS_OK'
} catch {
    Save-Text (Join-Path $out 'plugin-status.txt') $_.Exception.ToString()
    Write-Output 'PLUGIN_STATUS_FAIL'
}

$agent = 'GrokSubagentHostile'
$sessionId = "GrokSubagentHostile-$utc-c-green-p5-p6"
$requestId = "req-$utc-001-hostile-c-green-p5-p6"
Set-Content -LiteralPath (Join-Path $out 'session-id.txt') -Value $sessionId -Encoding utf8
Set-Content -LiteralPath (Join-Path $out 'request-id.txt') -Value $requestId -Encoding utf8

Invoke-Plugin -Method 'workflow.sessionlog.bootstrap' -Params @{} -Name 'sl-bootstrap'
Invoke-Plugin -Method 'client.SessionLog.OpenSessionAsync' -Params @{
    agent = $agent
    sessionId = $sessionId
    title = 'Hostile validate PLAN-PLUGINHANDOFF-001 C-green-P5-P6'
    model = 'grok-build-subagent'
} -Name 'sl-open'
Invoke-Plugin -Method 'client.SessionLog.BeginTurnAsync' -Params @{
    agent = $agent
    sessionId = $sessionId
    requestId = $requestId
    queryTitle = 'Hostile validate C-green-P5-P6 isolated server fixture'
    queryText = 'Independently re-verify PLAN-PLUGINHANDOFF-001 Phase C-green-P5-P6: PluginIntegrationServerFixture launches compiled Support.Mcp on loopback, isolated temp workspace and database, never developer 7147 DB, seven fixture tests green Failed 0 Skipped 0, P7 not implemented, PLAN and MCP-PLUGININT remain done false.'
    model = 'grok-build-subagent'
    planFile = 'docs/plans/PLAN-PLUGINHANDOFF-001.md'
    todoId = 'PLAN-PLUGINHANDOFF-001'
} -Name 'sl-begin'

Invoke-Plugin -Method 'workflow.todo.get' -Params @{ id = 'PLAN-PLUGINHANDOFF-001' } -Name 'todo-plan'
Invoke-Plugin -Method 'workflow.todo.get' -Params @{ id = 'MCP-PLUGININT-001' } -Name 'todo-pluginint'
Invoke-Plugin -Method 'client.Todo.GetAsync' -Params @{ id = 'PLAN-PLUGINHANDOFF-001' } -Name 'todo-plan-client'
Invoke-Plugin -Method 'client.Todo.GetAsync' -Params @{ id = 'MCP-PLUGININT-001' } -Name 'todo-pluginint-client'
Invoke-Plugin -Method 'workflow.requirements.getFr' -Params @{ id = 'FR-MCP-PLUGININT-001' } -Name 'req-fr'
Invoke-Plugin -Method 'workflow.requirements.getTr' -Params @{ id = 'TR-MCP-PLUGININT-001' } -Name 'req-tr'
Invoke-Plugin -Method 'workflow.requirements.getTest' -Params @{ id = 'TEST-MCP-PLUGININT-001' } -Name 'req-test'
Invoke-Plugin -Method 'workflow.requirements.listMappings' -Params @{ frId = 'FR-MCP-PLUGININT-001' } -Name 'req-map-list'
Invoke-Plugin -Method 'workflow.requirements.getMapping' -Params @{ frId = 'FR-MCP-PLUGININT-001' } -Name 'req-map'

Write-Output ("SESSION_ID=$sessionId")
Write-Output ("REQUEST_ID=$requestId")
Write-Output 'BOOTSTRAP_DONE'
