#Requires -Version 7.0
[CmdletBinding()]
param(
    [Parameter(Mandatory)]
    [string]$ToolName,

    [Parameter(Mandatory)]
    [hashtable]$Arguments
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$baseUrl = 'http://PAYTON-LEGION2:7147/mcp-transport'

function Invoke-McpJsonRpc {
    param(
        [Parameter(Mandatory)][hashtable]$Body,
        [string]$SessionId
    )

    $headers = @{
        Accept = 'application/json, text/event-stream'
    }
    if (-not [string]::IsNullOrWhiteSpace($SessionId)) {
        $headers['Mcp-Session-Id'] = $SessionId
    }

    $json = $Body | ConvertTo-Json -Depth 20 -Compress
    $response = Invoke-WebRequest -Uri $baseUrl -Method POST -Headers $headers -ContentType 'application/json' -Body $json -UseBasicParsing
    $sessionHeader = $null
    if ($response.Headers['Mcp-Session-Id']) {
        $sessionHeader = [string]$response.Headers['Mcp-Session-Id']
    }

    $text = [string]$response.Content
    $payload = $text
    if ($text -match '(?m)^data:\s*(.+)$') {
        $payload = $Matches[1]
    }

    [pscustomobject]@{
        StatusCode   = [int]$response.StatusCode
        SessionId    = $sessionHeader
        Raw          = $text
        Payload      = $payload
    }
}

$init = Invoke-McpJsonRpc -Body @{
    jsonrpc = '2.0'
    id      = 1
    method  = 'initialize'
    params  = @{
        protocolVersion = '2024-11-05'
        capabilities    = @{}
        clientInfo      = @{
            name    = 'GrokCode'
            version = '1.0.5'
        }
    }
}

$sessionId = $init.SessionId
if ([string]::IsNullOrWhiteSpace($sessionId)) {
    $sessionId = ''
}

try {
    Invoke-McpJsonRpc -Body @{
        jsonrpc = '2.0'
        method  = 'notifications/initialized'
        params  = @{}
    } -SessionId $sessionId | Out-Null
} catch {
    # notification may 202/204; continue
}

$call = Invoke-McpJsonRpc -Body @{
    jsonrpc = '2.0'
    id      = 2
    method  = 'tools/call'
    params  = @{
        name      = $ToolName
        arguments = $Arguments
    }
} -SessionId $sessionId

[pscustomobject]@{
    InitStatus  = $init.StatusCode
    InitPayload = $init.Payload
    CallStatus  = $call.StatusCode
    CallPayload = $call.Payload
    CallRaw     = $call.Raw
    McpSession  = $sessionId
} | ConvertTo-Json -Depth 20
