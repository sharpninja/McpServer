#Requires -Version 7.0
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$out = 'F:\GitHub\McpServer\docs\receipts\_hv-c-red-p7'
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
$utc = [DateTime]::UtcNow.ToString('yyyyMMddTHHMMssZ')
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

$raw = Get-Content -LiteralPath 'F:\GitHub\McpServer\AGENTS-README-FIRST.yaml' -Raw
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

$nonce = [guid]::NewGuid().ToString('N')
try {
    $health = Invoke-RestMethod -Uri ("http://PAYTON-LEGION2:7147/health?nonce=$nonce") -Method Get -TimeoutSec 15
    [ordered]@{ nonceSent = $nonce; nonceEcho = $health.nonce; status = $health.status; match = ($health.nonce -eq $nonce); storage = $health.storage } |
        ConvertTo-Json -Depth 8 | Set-Content -LiteralPath (Join-Path $out 'health-nonce.json') -Encoding utf8
    Write-Output ("HEALTH_NONCE_MATCH=" + ($health.nonce -eq $nonce))
} catch {
    Save-Text (Join-Path $out 'health-nonce.json') $_.Exception.ToString()
    Write-Output 'HEALTH_NONCE_FAIL'
}

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
$sessionId = "GrokSubagentHostile-$utc-c-red-p7"
$requestId = "req-$utc-001-hostile-c-red-p7-adapter"
Set-Content -LiteralPath (Join-Path $out 'session-id.txt') -Value $sessionId -Encoding utf8
Set-Content -LiteralPath (Join-Path $out 'request-id.txt') -Value $requestId -Encoding utf8

Invoke-Plugin -Method 'workflow.sessionlog.bootstrap' -Params @{} -Name 'sl-bootstrap'

Invoke-Plugin -Method 'client.SessionLog.OpenSessionAsync' -Params @{
    agent = $agent
    sessionId = $sessionId
    title = 'Hostile review PLAN-PLUGINHANDOFF-001 C-red-P7 process adapter'
    model = 'grok-build-subagent'
} -Name 'sl-open'

Invoke-Plugin -Method 'client.SessionLog.BeginTurnAsync' -Params @{
    agent = $agent
    sessionId = $sessionId
    requestId = $requestId
    queryTitle = 'Hostile C-red-P7 process-adapter reds'
    queryText = 'CLASS 1 hostile review of PLAN-PLUGINHANDOFF-001 Phase C-red-P7 ONLY. Surfaces A+B+C+D. Plugin Session Log process-adapter contract reds. Do not require P8-P10 green. PLAN-PLUGINHANDOFF-001 and MCP-PLUGININT-001 remain done false. Prior C-green-P5-P6 AGREE docs/receipts/hostile-validator-20260822T012355Z.md.'
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
        @{ timestamp = $now; role = 'model'; category = 'observation'; content = 'add-profile executed first. 18 non-skill profile markdown files read under C:\Users\kingd\.claude\profile (excluded add-profile.grok.md). Work class 1. Surfaces A+B+C+D apply. Scope is C-red-P7 only after C-green-P5-P6 AGREE. Starting independent PluginHostProcessAdapterTests rerun, LaunchAsync source read, live todo_get, P8-P10 mix attacks, AC3 process-capture pin attack.' }
        @{ timestamp = $now; role = 'model'; category = 'decision'; content = 'Decision: treat this as CLASS 1 project implementation red-phase gate for plugin Session Log process-adapter contract tests. Consequence: AGREE only if the named P7 Theory exists with eight InlineData host kinds, no Skip, LaunchAsync throws not implemented, independent filter is Failed 8 Passed 0 Skipped 0, full project keeps P1-P6 greens with only those eight P7 rows failing, pins live in the test body, P8-P10 named tests are absent, and PLAN/MCP-PLUGININT remain done false. Alternatives: scoring P8-P10 green or full PLAN DoD. Rejected: this brief forbids requiring P8-P10 green and forbids marking PLAN done.' }
    )
} -Name 'sl-dialog-start'

Write-Output ("SESSION_ID=$sessionId")
Write-Output ("REQUEST_ID=$requestId")
Write-Output 'BOOTSTRAP_DONE'
