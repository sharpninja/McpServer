#Requires -Version 7.0
[CmdletBinding()]
param()

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$baseUrl = 'http://PAYTON-LEGION2:7147/mcp-transport'
$workspacePath = 'F:\GitHub\McpServer'
$sessionId = 'GrokCode-20260822T141644Z-pluginhandoff-d4-gate'
$requestId = 'req-20260822T141644Z-001-pluginhandoff-d4-gate'
$outDir = 'F:\GitHub\McpServer\docs\receipts\_d4-20260822T141644Z'

function Invoke-McpJsonRpc {
    param(
        [Parameter(Mandatory)][hashtable]$Body,
        [string]$McpSessionId
    )
    $headers = @{ Accept = 'application/json, text/event-stream' }
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
        if ($line.StartsWith('data:')) { $payload = $line.Substring(5).Trim() }
    }
    [pscustomobject]@{ StatusCode = [int]$response.StatusCode; SessionId = $sessionHeader; Payload = $payload }
}

function Invoke-McpToolCall {
    param([Parameter(Mandatory)][string]$Name, [Parameter(Mandatory)][hashtable]$Arguments, [string]$McpSessionId)
    Invoke-McpJsonRpc -Body @{
        jsonrpc = '2.0'; id = 2; method = 'tools/call'
        params = @{ name = $Name; arguments = $Arguments }
    } -McpSessionId $McpSessionId
}

$init = Invoke-McpJsonRpc -Body @{
    jsonrpc = '2.0'; id = 1; method = 'initialize'
    params = @{ protocolVersion = '2024-11-05'; capabilities = @{}; clientInfo = @{ name = 'GrokCode'; version = '1.0.5' } }
}
$mcpSession = [string]$init.SessionId
try {
    Invoke-McpJsonRpc -Body @{ jsonrpc = '2.0'; method = 'notifications/initialized'; params = @{} } -McpSessionId $mcpSession | Out-Null
} catch {}

$now = [DateTimeOffset]::UtcNow.ToString('o')
$items = @(
    [ordered]@{
        timestamp = $now
        role      = 'model'
        category  = 'observation'
        content   = 'Trust bootstrap: Test-MarkerSignature True. Health nonce 93a9873cb9d44c57b57c873436a3f6ba echoed exactly. status Healthy. storage reachable. version 1.4.30+ee89cd63f6d16aa43d8e8dfac2388246c6ba39f8. Plugin mcpserver-grok-plugin 1.105.0 from .version and plugin.json.'
    }
    [ordered]@{
        timestamp = $now
        role      = 'model'
        category  = 'observation'
        content   = 'sessionlog_open created=true sessionId=GrokCode-20260822T141644Z-pluginhandoff-d4-gate. sessionlog_begin_turn success turnId=43015 status=in_progress requestId=req-20260822T141644Z-001-pluginhandoff-d4-gate.'
    }
    [ordered]@{
        timestamp = $now
        role      = 'model'
        category  = 'observation'
        content   = 'todo_get done flags before D4 gate: PLAN-PLUGINHANDOFF-001=False, MCP-HANDOFF-001=False, MCP-HANDOFFPLAN-001=False, MCP-HANDOFFREVIEW-001=False. Will not flip any TODO done.'
    }
    [ordered]@{
        timestamp = $now
        role      = 'model'
        category  = 'decision'
        content   = 'Decision: run exact D4 HANDOFFPLAN commands sequentially and stop on first Failed>0, Skipped>0, or nonzero exit. Capture TRX/logs under docs/receipts/_d4-20260822T141644Z. Do not start D5, do not implement E/F/G, do not commit. Alternatives rejected: parallelizing the nine commands (would hide first-fail stop), treating skipped as pass, marking TODOs done on green.'
    }
)

$dialog = Invoke-McpToolCall -Name 'sessionlog_dialog' -McpSessionId $mcpSession -Arguments @{
    agent         = 'GrokCode'
    sessionId     = $sessionId
    requestId     = $requestId
    workspacePath = $workspacePath
    itemsJson     = ($items | ConvertTo-Json -Depth 8 -Compress)
}

$section = [ordered]@{
    actions = @(
        [ordered]@{ order = 1; type = 'tool_call'; status = 'completed'; description = 'Test-MarkerSignature True and health nonce echo'; filePath = 'docs/receipts/_d4-bootstrap-trust.ps1' }
        [ordered]@{ order = 2; type = 'tool_call'; status = 'completed'; description = 'Native MCP sessionlog_open created GrokCode-20260822T141644Z-pluginhandoff-d4-gate'; filePath = 'docs/receipts/_d4-20260822T141644Z/ids.json' }
        [ordered]@{ order = 3; type = 'tool_call'; status = 'completed'; description = 'Native MCP sessionlog_begin_turn turnId=43015'; filePath = 'docs/receipts/_d4-20260822T141644Z/ids.json' }
        [ordered]@{ order = 4; type = 'design_decision'; status = 'completed'; description = 'Run D4 commands sequentially; stop on Failed>0 or Skipped>0; do not mark TODOs done'; filePath = 'docs/plans/PLAN-PLUGINHANDOFF-001.md' }
    )
    designDecisions = @(
        [ordered]@{
            decision   = 'D4 is a read-only gate: execute the nine HANDOFFPLAN commands, persist receipts, leave PLAN-PLUGINHANDOFF-001 and MCP-HANDOFF* done:false.'
            rationale  = 'Operator scoped this turn to Phase D4 only and forbade TODO done flips, D5 Codex, and Phase E/F/G.'
            alternatives = 'Marking TODOs done on green; starting D5 after a green suite; skipping integration tests.'
            rejected   = 'Those expand scope past D4 and violate hostile-on-goal-state plus the explicit task.'
        }
    )
}

$replaceActions = Invoke-McpToolCall -Name 'sessionlog_replace_section' -McpSessionId $mcpSession -Arguments @{
    agent         = 'GrokCode'
    sessionId     = $sessionId
    requestId     = $requestId
    workspacePath = $workspacePath
    section       = 'actions'
    sectionJson   = ($section | ConvertTo-Json -Depth 10 -Compress)
}

$replaceDecisions = Invoke-McpToolCall -Name 'sessionlog_replace_section' -McpSessionId $mcpSession -Arguments @{
    agent         = 'GrokCode'
    sessionId     = $sessionId
    requestId     = $requestId
    workspacePath = $workspacePath
    section       = 'designDecisions'
    sectionJson   = ($section | ConvertTo-Json -Depth 10 -Compress)
}

[ordered]@{
    dialogStatus    = $dialog.StatusCode
    dialogPayload   = $dialog.Payload
    actionsStatus   = $replaceActions.StatusCode
    actionsPayload  = $replaceActions.Payload
    decisionsStatus = $replaceDecisions.StatusCode
    decisionsPayload= $replaceDecisions.Payload
} | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath (Join-Path $outDir 'session-dialog-start.json') -Encoding utf8
Write-Output ('DIALOG=' + $dialog.Payload.Substring(0, [Math]::Min(300, $dialog.Payload.Length)))
Write-Output ('ACTIONS=' + $replaceActions.Payload.Substring(0, [Math]::Min(300, $replaceActions.Payload.Length)))
Write-Output ('DECISIONS=' + $replaceDecisions.Payload.Substring(0, [Math]::Min(300, $replaceDecisions.Payload.Length)))
