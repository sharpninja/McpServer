#Requires -Version 7.0
[CmdletBinding()]
param()

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$baseUrl = 'http://PAYTON-LEGION2:7147/mcp-transport'
$workspacePath = 'F:\GitHub\McpServer'
$utc = '20260822T151657Z'
$sessionId = "GrokSubagentHostile-$utc-e-red-hostile"
$requestId = "req-$utc-001-e-red-hostile-validate"
$outDir = 'F:\GitHub\McpServer\docs\receipts\_hv-e-red-20260822T151657Z'
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
            name    = 'GrokSubagentHostile'
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
    agent         = 'GrokSubagentHostile'
    sessionId     = $sessionId
    workspacePath = $workspacePath
    title         = 'Hostile E-red validation of HostileReview tests'
    model         = 'grok-4'
}

$begin = Invoke-McpToolCall -Name 'sessionlog_begin_turn' -McpSessionId $mcpSession -Arguments @{
    agent         = 'GrokSubagentHostile'
    sessionId     = $sessionId
    requestId     = $requestId
    workspacePath = $workspacePath
    planFile      = 'docs/plans/PLAN-PLUGINHANDOFF-001.md'
    todoId        = 'PLAN-PLUGINHANDOFF-001'
    queryTitle    = 'Hostile E-red gate for HostileReview tests'
    queryText     = 'Class 1 hostile validation of E-red claims for PLAN-PLUGINHANDOFF-001 section 9. Surfaces A+B+C+D. Do not implement HostileReview product. Do not mark PLAN-PLUGINHANDOFF-001 or MCP-HOSTILEREVIEW-001 done.'
}

$todoIds = @(
    'PLAN-PLUGINHANDOFF-001'
    'MCP-HOSTILEREVIEW-001'
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
        snippet    = if ($text.Length -gt 800) { $text.Substring(0, 800) } else { $text }
        full       = $text
    }
}

$reqList = Invoke-McpToolCall -Name 'requirements_list' -McpSessionId $mcpSession -Arguments @{
    workspacePath = $workspacePath
    type          = 'all'
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
Get-McpPayloadText -Payload $reqList.Payload | Set-Content -LiteralPath (Join-Path $outDir 'requirements-all.json') -Encoding utf8
Write-Output ('SESSION=' + $sessionId)
Write-Output ('REQUEST=' + $requestId)
Write-Output ('MCP_SESSION=' + $mcpSession)
Write-Output ('OPEN=' + (Get-McpPayloadText -Payload $open.Payload))
Write-Output ('BEGIN=' + (Get-McpPayloadText -Payload $begin.Payload))
foreach ($tid in $todoIds) {
    Write-Output ('TODO_' + $tid + '_DONE=' + $todoSnips[$tid].done)
}
Write-Output ('REQ_LEN=' + (Get-McpPayloadText -Payload $reqList.Payload).Length)
