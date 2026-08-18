#Requires -Version 7.0
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$workspace = 'F:\GitHub\McpServer'
$baseUrl = 'http://PAYTON-LEGION2:7147'
$sessionId = 'GrokCode-20260818T004041Z-hostile-slog-503'
$requestId = 'req-20260818T004041Z-001-hostile-validate-slog-503'
$implSession = 'GrokCode-20260817T120000Z-agent-help-grok-cli'
$utcNow = [datetime]::UtcNow.ToString('o')
Write-Output ('SESSION_ID=' + $sessionId)
Write-Output ('REQUEST_ID=' + $requestId)
Write-Output ('UTC_NOW=' + $utcNow)

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
    if ($null -ne $Params) { $payload['params'] = $Params }
    $json = $payload | ConvertTo-Json -Depth 30 -Compress
    Write-Output ('---- MCP {0} id={1} ----' -f $Label, $script:McpId)
    $req = [System.Net.Http.HttpRequestMessage]::new([System.Net.Http.HttpMethod]::Post, ($baseUrl + '/mcp-transport'))
    $req.Headers.Accept.Clear()
    [void]$req.Headers.Accept.Add([System.Net.Http.Headers.MediaTypeWithQualityHeaderValue]::new('application/json'))
    [void]$req.Headers.Accept.Add([System.Net.Http.Headers.MediaTypeWithQualityHeaderValue]::new('text/event-stream'))
    [void]$req.Headers.TryAddWithoutValidation('X-Workspace-Path', $workspace)
    if ($script:McpSessionHeader) {
        [void]$req.Headers.TryAddWithoutValidation('Mcp-Session-Id', $script:McpSessionHeader)
    }
    $req.Content = [System.Net.Http.StringContent]::new($json, [System.Text.Encoding]::UTF8, 'application/json')
    $client = [System.Net.Http.HttpClient]::new()
    $client.Timeout = [TimeSpan]::FromSeconds(120)
    try {
        $resp = $client.Send($req)
        $sid = $null
        if ($resp.Headers.TryGetValues('Mcp-Session-Id', [ref]$sid)) {
            $script:McpSessionHeader = @($sid)[0]
        }
        $body = $resp.Content.ReadAsStringAsync().GetAwaiter().GetResult()
        Write-Output ('HTTP=' + [int]$resp.StatusCode)
        Write-Output ('Mcp-Session-Id=' + $script:McpSessionHeader)
        if ($body.Contains("`ndata:") -or $body.StartsWith('event:')) {
            $dataLines = @()
            foreach ($line in ($body -split "`n")) {
                $trim = $line.TrimEnd("`r")
                if ($trim.StartsWith('data:')) { $dataLines += $trim.Substring(5).Trim() }
            }
            $body = ($dataLines -join "`n")
        }
        if ($body.Length -gt 6000) {
            Write-Output ($body.Substring(0, 6000))
            Write-Output ('... truncated body len=' + $body.Length)
        } else {
            Write-Output $body
        }
        return $body
    } finally {
        $client.Dispose()
        $req.Dispose()
    }
}

function Invoke-McpTool {
    param([Parameter(Mandatory)][string]$Name, [Parameter(Mandatory)][hashtable]$Arguments)
    Invoke-McpRpc -Method 'tools/call' -Label $Name -Params @{ name = $Name; arguments = $Arguments }
}

[void](Invoke-McpRpc -Method 'initialize' -Params @{
    protocolVersion = '2025-03-26'
    capabilities = @{}
    clientInfo = @{ name = 'hostile-validator-slog-503'; version = '1.0.0' }
})

Invoke-McpTool -Name 'sessionlog_open' -Arguments @{
    agent = 'GrokCode'
    sessionId = $sessionId
    workspacePath = $workspace
    title = 'Hostile validate session-log backend_unavailable diagnosis'
    model = 'grok-4.5'
}

Invoke-McpTool -Name 'sessionlog_begin_turn' -Arguments @{
    agent = 'GrokCode'
    sessionId = $sessionId
    requestId = $requestId
    workspacePath = $workspace
    planFile = 'None'
    todoId = 'None'
    queryTitle = 'Hostile validate session-log 503 diagnosis'
    queryText = 'Hostile validator: attack implementer claims about live session-log storage, Claude failsafe internal_server_error vs backend_unavailable, omitted planFile/todoId, and zero post-restart sessionlog 503 completions.'
}

Write-Output '---- QUERY IMPLEMENTER SESSION BY TEXT ----'
Invoke-McpTool -Name 'sessionlog_query' -Arguments @{
    agent = 'GrokCode'
    text = 'sessionlog-backend-unavailable'
    from = '2026-08-18T00:30:00Z'
    limit = 10
}

Write-Output '---- QUERY IMPLEMENTER SESSION BY AGENT FROM ----'
Invoke-McpTool -Name 'sessionlog_query' -Arguments @{
    agent = 'GrokCode'
    from = '2026-08-17T12:00:00Z'
    limit = 20
}

Write-Output '---- QUERY TEXT AGENT-HELP-GROK-CLI ----'
Invoke-McpTool -Name 'sessionlog_query' -Arguments @{
    agent = 'GrokCode'
    text = 'agent-help-grok-cli'
    limit = 10
}

Write-Output '---- QUERY REVIEW SESSION TEXT ----'
Invoke-McpTool -Name 'sessionlog_query' -Arguments @{
    agent = 'GrokCode'
    text = 'hostile-slog-503'
    from = '2026-08-18T00:40:00Z'
    limit = 10
}

Write-Output ('IMPL_SESSION_TOKEN=' + $implSession)
