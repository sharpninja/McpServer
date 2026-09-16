#Requires -Version 7.0
[CmdletBinding()]
param()

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$baseUrl = 'http://PAYTON-LEGION2:7147/mcp-transport'
$workspacePath = 'F:\GitHub\McpServer'
$utc = [DateTimeOffset]::UtcNow.ToString('yyyyMMddTHHmmssZ')
$sessionId = "GrokCode-$utc-pluginhandoff-g0"
$requestId = "req-$utc-001-g0-dump-binding"
$outDir = 'F:\GitHub\McpServer\docs\receipts\_g0-dump-binding'
[void][System.IO.Directory]::CreateDirectory($outDir)

function Invoke-McpJsonRpc {
    param(
        [Parameter(Mandatory)][hashtable]$Body,
        [string]$McpSessionId
    )

    $headers = @{
        Accept = 'application/json, text/event-stream'
    }
    if (-not [string]::IsNullOrWhiteSpace($McpSessionId)) {
        $headers['Mcp-Session-Id'] = $McpSessionId
    }

    $json = ConvertTo-Json -InputObject $Body -Depth 20 -Compress
    $response = Invoke-WebRequest -Uri $baseUrl -Method POST -Headers $headers -ContentType 'application/json' -Body $json -UseBasicParsing
    $sessionHeader = $null
    if ($response.Headers['Mcp-Session-Id']) {
        $sessionHeader = [string]$response.Headers['Mcp-Session-Id']
    }

    $text = [string]$response.Content
    $payload = $text
    foreach ($line in ($text -split "`n")) {
        if ($line.StartsWith('data:')) {
            $payload = $line.Substring(5).Trim()
        }
    }

    [pscustomobject]@{
        StatusCode = [int]$response.StatusCode
        SessionId  = $sessionHeader
        Raw        = $text
        Payload    = $payload
    }
}

function Invoke-McpToolCall {
    param(
        [Parameter(Mandatory)][string]$Name,
        [Parameter(Mandatory)][hashtable]$Arguments,
        [string]$McpSessionId
    )

    Invoke-McpJsonRpc -Body @{
        jsonrpc = '2.0'
        id      = 2
        method  = 'tools/call'
        params  = @{
            name      = $Name
            arguments = $Arguments
        }
    } -McpSessionId $McpSessionId
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

$mcpSession = [string]$init.SessionId
try {
    Invoke-McpJsonRpc -Body @{
        jsonrpc = '2.0'
        method  = 'notifications/initialized'
        params  = @{}
    } -McpSessionId $mcpSession | Out-Null
} catch {
    Write-Output ('INIT_NOTIFY_WARN=' + $_.Exception.Message)
}

$open = Invoke-McpToolCall -Name 'sessionlog_open' -McpSessionId $mcpSession -Arguments @{
    agent         = 'GrokCode'
    sessionId     = $sessionId
    workspacePath = $workspacePath
    title         = 'PLAN-PLUGINHANDOFF-001 Phase G0 dump-binding confirmation'
    model         = 'grok-code'
}

$begin = Invoke-McpToolCall -Name 'sessionlog_begin_turn' -McpSessionId $mcpSession -Arguments @{
    agent         = 'GrokCode'
    sessionId     = $sessionId
    requestId     = $requestId
    workspacePath = $workspacePath
    planFile      = 'docs/plans/PLAN-PLUGINHANDOFF-001.md'
    todoId        = 'PLAN-PLUGINHANDOFF-001'
    queryTitle    = 'G0 confirm add-workspace dump binding'
    queryText     = 'Confirm add-workspace --dump binding on WorkspaceClient.CreateAsync and WorkspaceController.CreateAsync. Do not implement dump/import. Do not write G-red tests. Do not mark TODOs done.'
}

$todoPlan = Invoke-McpToolCall -Name 'todo_get' -McpSessionId $mcpSession -Arguments @{
    id            = 'PLAN-PLUGINHANDOFF-001'
    workspacePath = $workspacePath
}
$todoWiki = Invoke-McpToolCall -Name 'todo_get' -McpSessionId $mcpSession -Arguments @{
    id            = 'MCP-WIKIEXPORT-001'
    workspacePath = $workspacePath
}
$todoPlugin = Invoke-McpToolCall -Name 'todo_get' -McpSessionId $mcpSession -Arguments @{
    id            = 'MCP-PLUGININT-001'
    workspacePath = $workspacePath
}

$ids = [ordered]@{
    utc            = $utc
    sessionId      = $sessionId
    requestId      = $requestId
    mcpSession     = $mcpSession
    initStatus     = $init.StatusCode
    initPayload    = $init.Payload
    openStatus     = $open.StatusCode
    openPayload    = $open.Payload
    beginStatus    = $begin.StatusCode
    beginPayload   = $begin.Payload
    planTodo       = $todoPlan.Payload
    wikiTodo       = $todoWiki.Payload
    pluginIntTodo  = $todoPlugin.Payload
}

$ids | ConvertTo-Json -Depth 20 | Set-Content -LiteralPath (Join-Path $outDir 'ids.json') -Encoding utf8
Write-Output ('SESSION=' + $sessionId)
Write-Output ('REQUEST=' + $requestId)
Write-Output ('OPEN=' + $open.Payload)
Write-Output ('BEGIN=' + $begin.Payload)
Write-Output ('PLAN_TODO_DONE_SNIP=' + $todoPlan.Payload.Substring(0, [Math]::Min(400, $todoPlan.Payload.Length)))
