#Requires -Version 7.0
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

. 'F:\GitHub\mcpserver-grok-plugin\lib\marker-resolver.ps1'

$workspace = 'F:\GitHub\McpServer'
$marker = Join-Path $workspace 'AGENTS-README-FIRST.yaml'
$baseUrl = 'http://PAYTON-LEGION2:7147'
$utcStamp = [datetime]::UtcNow.ToString('yyyyMMddTHHmmssZ')
$sessionId = "GrokCode-$utcStamp-hostile-svc-cfg"
$requestId = "req-$utcStamp-001-hostile-validate-svc-cfg"
Write-Output ("UTC_STAMP=" + $utcStamp)
Write-Output ("SESSION_ID=" + $sessionId)
Write-Output ("REQUEST_ID=" + $requestId)

Write-Output '=== MARKER_SIGNATURE ==='
$sigOk = Test-MarkerSignature -MarkerFile $marker
Write-Output ("Test-MarkerSignature=" + $sigOk)

Write-Output '=== HEALTH_NONCE ==='
$nonce = [guid]::NewGuid().ToString('N')
$healthUri = "$baseUrl/health?nonce=$nonce"
$health = Invoke-WebRequest -Uri $healthUri -UseBasicParsing -TimeoutSec 10
Write-Output ("HealthStatus=" + [int]$health.StatusCode)
$healthJson = $health.Content | ConvertFrom-Json
Write-Output ("HealthNonceSent=" + $nonce)
Write-Output ("HealthNonceEcho=" + $healthJson.nonce)
Write-Output ("HealthNonceMatch=" + ($healthJson.nonce -eq $nonce))
Write-Output ("FULL_BOOTSTRAP=" + ($sigOk -and ($healthJson.nonce -eq $nonce) -and ([int]$health.StatusCode -eq 200)))

$script:McpSessionHeader = $null
$script:McpId = 0

function Invoke-McpRpc {
    param(
        [Parameter(Mandatory)][string]$Method,
        $Params = $null,
        [string]$Label = $Method
    )
    $script:McpId++
    $payload = [ordered]@{
        jsonrpc = '2.0'
        id = $script:McpId
        method = $Method
    }
    if ($null -ne $Params) {
        $payload['params'] = $Params
    }
    $json = $payload | ConvertTo-Json -Depth 20 -Compress
    Write-Output ("---- MCP {0} id={1} ----" -f $Label, $script:McpId)
    $req = [System.Net.Http.HttpRequestMessage]::new([System.Net.Http.HttpMethod]::Post, "$baseUrl/mcp-transport")
    $req.Headers.Accept.Clear()
    [void]$req.Headers.Accept.Add([System.Net.Http.Headers.MediaTypeWithQualityHeaderValue]::new('application/json'))
    [void]$req.Headers.Accept.Add([System.Net.Http.Headers.MediaTypeWithQualityHeaderValue]::new('text/event-stream'))
    [void]$req.Headers.TryAddWithoutValidation('X-Workspace-Path', $workspace)
    if ($script:McpSessionHeader) {
        [void]$req.Headers.TryAddWithoutValidation('Mcp-Session-Id', $script:McpSessionHeader)
    }
    $req.Content = [System.Net.Http.StringContent]::new($json, [System.Text.Encoding]::UTF8, 'application/json')
    $handler = [System.Net.Http.HttpClientHandler]::new()
    $client = [System.Net.Http.HttpClient]::new($handler)
    $client.Timeout = [TimeSpan]::FromSeconds(120)
    try {
        $resp = $client.Send($req)
        $sid = $null
        if ($resp.Headers.TryGetValues('Mcp-Session-Id', [ref]$sid)) {
            $script:McpSessionHeader = @($sid)[0]
        }
        $body = $resp.Content.ReadAsStringAsync().GetAwaiter().GetResult()
        Write-Output ("HTTP=" + [int]$resp.StatusCode)
        Write-Output ("Mcp-Session-Id=" + $script:McpSessionHeader)
        if ($body.StartsWith('event:') -or $body.Contains("`ndata:")) {
            $dataLines = @()
            foreach ($line in ($body -split "`n")) {
                $trim = $line.TrimEnd("`r")
                if ($trim.StartsWith('data:')) {
                    $dataLines += $trim.Substring(5).Trim()
                }
            }
            $body = ($dataLines -join "`n")
        }
        Write-Output $body
        return $body
    } finally {
        $client.Dispose()
        $req.Dispose()
    }
}

function Invoke-McpTool {
    param(
        [Parameter(Mandatory)][string]$Name,
        [Parameter(Mandatory)][hashtable]$Arguments
    )
    Invoke-McpRpc -Method 'tools/call' -Label $Name -Params @{
        name = $Name
        arguments = $Arguments
    }
}

$initBody = Invoke-McpRpc -Method 'initialize' -Params @{
    protocolVersion = '2025-03-26'
    capabilities = @{}
    clientInfo = @{ name = 'hostile-validator-svc-cfg'; version = '1.0.0' }
}

Invoke-McpRpc -Method 'notifications/initialized' -Params @{} | Out-Null

Write-Output '=== AGENT_HELP_GET_STATUS claimed ==='
Invoke-McpTool -Name 'agent_help_get_status' -Arguments @{
    workspacePath = $workspace
    sessionId = 'help-20260817233017-0bf8ab01a3af4e92a0c6c38ab8dba245'
}

Write-Output '=== AGENT_HELP_CREATE_SESSION independent no overrides ==='
Invoke-McpTool -Name 'agent_help_create_session' -Arguments @{
    workspacePath = $workspace
    topic = 'hostile-validate-svc-cfg'
    callerAgent = 'GrokCode'
    callerSessionId = $sessionId
    callerRequestId = $requestId
    issueSummary = 'Hostile validator independent create-session with no executionStrategy or agentModel override. Observation: verifying live AgentHelp defaults after ProgramData yaml mutation. Inference: none.'
}

Write-Output '=== SESSIONLOG_OPEN ==='
Invoke-McpTool -Name 'sessionlog_open' -Arguments @{
    agent = 'GrokCode'
    sessionId = $sessionId
    workspacePath = $workspace
    title = 'Hostile validate Windows service AgentHelp config'
    model = 'grok'
}

Write-Output '=== SESSIONLOG_BEGIN_TURN ==='
Invoke-McpTool -Name 'sessionlog_begin_turn' -Arguments @{
    agent = 'GrokCode'
    sessionId = $sessionId
    requestId = $requestId
    workspacePath = $workspace
    queryTitle = 'Hostile validate Windows service AgentHelp config'
    queryText = 'Hostile validator: attack implementer claims about live Windows service AgentHelp config at C:\ProgramData\McpServer\appsettings.yaml.'
}

Write-Output ("MCP_SESSION_HEADER=" + $script:McpSessionHeader)
Write-Output 'MCP_PHASE1_DONE'
