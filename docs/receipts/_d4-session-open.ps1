#Requires -Version 7.0
[CmdletBinding()]
param()

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$baseUrl = 'http://PAYTON-LEGION2:7147/mcp-transport'
$workspacePath = 'F:\GitHub\McpServer'
$utc = '20260822T141644Z'
$sessionId = "GrokCode-$utc-pluginhandoff-d4-gate"
$requestId = "req-$utc-001-pluginhandoff-d4-gate"
$outDir = 'F:\GitHub\McpServer\docs\receipts\_d4-20260822T141644Z'
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

function Get-McpPayloadText {
    param([string]$Payload)
    try {
        $obj = $Payload | ConvertFrom-Json
        if ($obj.result.content -and $obj.result.content.Count -gt 0) {
            return [string]$obj.result.content[0].text
        }
        if ($obj.error) {
            return ($obj.error | ConvertTo-Json -Compress -Depth 10)
        }
        return $Payload
    } catch {
        return $Payload
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

$tools = Invoke-McpJsonRpc -Body @{
    jsonrpc = '2.0'
    id      = 3
    method  = 'tools/list'
    params  = @{}
} -McpSessionId $mcpSession

$open = Invoke-McpToolCall -Name 'sessionlog_open' -McpSessionId $mcpSession -Arguments @{
    agent         = 'GrokCode'
    sessionId     = $sessionId
    workspacePath = $workspacePath
    title         = 'PLAN-PLUGINHANDOFF-001 Phase D4 HANDOFFPLAN gate'
    model         = 'grok-code'
}

$begin = Invoke-McpToolCall -Name 'sessionlog_begin_turn' -McpSessionId $mcpSession -Arguments @{
    agent         = 'GrokCode'
    sessionId     = $sessionId
    requestId     = $requestId
    workspacePath = $workspacePath
    planFile      = 'docs/plans/PLAN-PLUGINHANDOFF-001.md'
    todoId        = 'PLAN-PLUGINHANDOFF-001'
    queryTitle    = 'D4 HANDOFFPLAN gate Failed 0 Skipped 0'
    queryText     = 'Run PLAN-PLUGINHANDOFF-001 Phase D4 only. Execute the exact HANDOFFPLAN gate command list. Do not mark any TODO done. Do not start D5 Codex. Do not implement Phase E/F/G.'
}

$todoIds = @(
    'PLAN-PLUGINHANDOFF-001'
    'MCP-HANDOFF-001'
    'MCP-HANDOFFPLAN-001'
    'MCP-HANDOFFREVIEW-001'
)
$todoSnips = [ordered]@{}
foreach ($tid in $todoIds) {
    $got = Invoke-McpToolCall -Name 'todo_get' -McpSessionId $mcpSession -Arguments @{
        id            = $tid
        workspacePath = $workspacePath
    }
    $text = Get-McpPayloadText -Payload $got.Payload
    $done = $null
    try {
        $todoObj = $text | ConvertFrom-Json
        $done = [bool]$todoObj.done
    } catch {
        $done = 'parse_failed'
    }
    $todoSnips[$tid] = [ordered]@{
        statusCode = $got.StatusCode
        done       = $done
        snippet    = if ($text.Length -gt 400) { $text.Substring(0, 400) } else { $text }
    }
}

$ids = [ordered]@{
    utc         = $utc
    sessionId   = $sessionId
    requestId   = $requestId
    mcpSession  = $mcpSession
    initStatus  = $init.StatusCode
    openStatus  = $open.StatusCode
    openText    = Get-McpPayloadText -Payload $open.Payload
    beginStatus = $begin.StatusCode
    beginText   = Get-McpPayloadText -Payload $begin.Payload
    todos       = $todoSnips
}

$ids | ConvertTo-Json -Depth 12 | Set-Content -LiteralPath (Join-Path $outDir 'ids.json') -Encoding utf8
$tools.Payload | Set-Content -LiteralPath (Join-Path $outDir 'tools-list.json') -Encoding utf8
Write-Output ('SESSION=' + $sessionId)
Write-Output ('REQUEST=' + $requestId)
Write-Output ('OPEN=' + (Get-McpPayloadText -Payload $open.Payload))
Write-Output ('BEGIN=' + (Get-McpPayloadText -Payload $begin.Payload))
foreach ($tid in $todoIds) {
    Write-Output ('TODO_' + $tid + '_DONE=' + $todoSnips[$tid].done)
}
