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

function Get-ToolInnerObject {
    param([string]$Payload)
    $outer = $Payload | ConvertFrom-Json
    if ($outer.result.content -and $outer.result.content.Count -gt 0) {
        return ($outer.result.content[0].text | ConvertFrom-Json)
    }
    return $outer
}

$init = Invoke-McpJsonRpc -Body @{
    jsonrpc = '2.0'; id = 1; method = 'initialize'
    params = @{ protocolVersion = '2024-11-05'; capabilities = @{}; clientInfo = @{ name = 'GrokCode'; version = '1.0.5' } }
}
$mcpSession = [string]$init.SessionId
try {
    Invoke-McpJsonRpc -Body @{ jsonrpc = '2.0'; method = 'notifications/initialized'; params = @{} } -McpSessionId $mcpSession | Out-Null
} catch {}

$todoIds = @(
    'PLAN-PLUGINHANDOFF-001'
    'MCP-HANDOFF-001'
    'MCP-HANDOFFPLAN-001'
    'MCP-HANDOFFREVIEW-001'
)
$todoAfter = [ordered]@{}
foreach ($tid in $todoIds) {
    $got = Invoke-McpToolCall -Name 'todo_get' -McpSessionId $mcpSession -Arguments @{
        id = $tid
        workspacePath = $workspacePath
    }
    $obj = Get-ToolInnerObject $got.Payload
    $todoAfter[$tid] = [ordered]@{ done = [bool]$obj.done; statusCode = $got.StatusCode }
}

$now = [DateTimeOffset]::UtcNow.ToString('o')
$dialogItems = @(
    [ordered]@{
        timestamp = $now
        role = 'model'
        category = 'observation'
        content = 'Command 1 Client.Tests: exit 0 Passed 284 Failed 0 Skipped 0. Command 2 Support.Mcp.Tests: exit 1 Passed 2111 Failed 1 Skipped 0. Failed test HandleAsync_NullLayerKey_UsesPersistedCurrentLayerAfterExternalUpdate expected layer-2 actual layer-1. Gate stopped. Commands 3-9 not run.'
    }
    [ordered]@{
        timestamp = $now
        role = 'model'
        category = 'decision'
        content = 'Decision: stop D4 after command 2 Failed=1. Do not continue the remaining seven commands. Do not claim D4 complete. Do not flip TODO done. Do not start D5 or implement a product fix in this turn. Consequence: receipts record not_run for commands 3-9. Alternatives rejected: continuing the suite after a failed command; marking the plan done; planting skipped tests; silently fixing the layer-key test as if it were in D4 scope.'
    }
)
$dialog = Invoke-McpToolCall -Name 'sessionlog_dialog' -McpSessionId $mcpSession -Arguments @{
    agent = 'GrokCode'
    sessionId = $sessionId
    requestId = $requestId
    workspacePath = $workspacePath
    itemsJson = (ConvertTo-Json -InputObject $dialogItems -Depth 8 -Compress)
}

$turn = [ordered]@{
    requestId = $requestId
    queryTitle = 'D4 HANDOFFPLAN gate Failed 0 Skipped 0'
    interpretation = 'Operator asked for PLAN-PLUGINHANDOFF-001 Phase D4 only: run the nine exact HANDOFFPLAN gate commands, persist TRX/logs and a parent receipt, stop on Failed>0 or Skipped>0, leave TODOs done:false, do not start D5 or Phase E/F/G, do not commit.'
    response = 'D4 is NOT complete. Overall Failed 0 Skipped 0: no. Receipt: docs/receipts/d4-handoff-gate-20260822T141644Z.md. Command 1 Client.Tests exit 0 Passed 284 Failed 0 Skipped 0. Command 2 Support.Mcp.Tests exit 1 Passed 2111 Failed 1 Skipped 0. Failed test: GetProductEffectiveRequirementsQueryHandlerTests.HandleAsync_NullLayerKey_UsesPersistedCurrentLayerAfterExternalUpdate expected layer-2 actual layer-1. Commands 3-9 not run. PLAN-PLUGINHANDOFF-001, MCP-HANDOFF-001, MCP-HANDOFFPLAN-001, MCP-HANDOFFREVIEW-001 remain done:false.'
    status = 'completed'
    model = 'grok-code'
    planFile = 'docs/plans/PLAN-PLUGINHANDOFF-001.md'
    todoId = 'PLAN-PLUGINHANDOFF-001'
    tags = @('PLAN-PLUGINHANDOFF-001', 'MCP-HANDOFFPLAN-001', 'D4', 'handoff-gate')
    contextList = @(
        'docs/plans/PLAN-PLUGINHANDOFF-001.md'
        'docs/receipts/d4-handoff-gate-20260822T141644Z.md'
        'tests/McpServer.Support.Mcp.Tests/Products/GetProductEffectiveRequirementsQueryHandlerTests.cs'
    )
    filesModified = @(
        'docs/receipts/d4-handoff-gate-20260822T141644Z.md'
        'docs/receipts/d4-handoff-gate-20260822T141644Z.json'
        'docs/receipts/_d4-20260822T141644Z/summary.json'
        'docs/receipts/_d4-20260822T141644Z/01-client-tests.trx'
        'docs/receipts/_d4-20260822T141644Z/02-support-mcp-tests.trx'
    )
    designDecisions = @(
        'Run the nine D4 commands sequentially and stop on first Failed>0, Skipped>0, or nonzero exit. Do not parallelize.'
        'Treat skipped tests as not passing. Do not plant skipped tests to make the gate look green.'
        'D4 is a gate, not a product-fix slice. After Support.Mcp.Tests Failed=1, stop, write receipts, leave PLAN-PLUGINHANDOFF-001 and MCP-HANDOFF* done:false, and do not start D5 or Phase E/F/G.'
    )
    requirementsDiscovered = @()
    blockers = @(
        'D4 gate blocked by GetProductEffectiveRequirementsQueryHandlerTests.HandleAsync_NullLayerKey_UsesPersistedCurrentLayerAfterExternalUpdate: expected layer-2, actual layer-1.'
    )
    actions = @(
        [ordered]@{ order = 1; type = 'tool_call'; status = 'completed'; description = 'Test-MarkerSignature True and health nonce 93a9873cb9d44c57b57c873436a3f6ba echoed'; filePath = 'AGENTS-README-FIRST.yaml' }
        [ordered]@{ order = 2; type = 'tool_call'; status = 'completed'; description = 'sessionlog_open created GrokCode-20260822T141644Z-pluginhandoff-d4-gate'; filePath = 'docs/receipts/_d4-20260822T141644Z/ids.json' }
        [ordered]@{ order = 3; type = 'tool_call'; status = 'completed'; description = 'sessionlog_begin_turn turnId=43015'; filePath = 'docs/receipts/_d4-20260822T141644Z/ids.json' }
        [ordered]@{ order = 4; type = 'tool_call'; status = 'completed'; description = 'todo_get before gate: PLAN-PLUGINHANDOFF-001 MCP-HANDOFF-001 MCP-HANDOFFPLAN-001 MCP-HANDOFFREVIEW-001 done=false'; filePath = '' }
        [ordered]@{ order = 5; type = 'test'; status = 'completed'; description = 'dotnet test Client.Tests exit 0 Passed 284 Failed 0 Skipped 0'; filePath = 'docs/receipts/_d4-20260822T141644Z/01-client-tests.trx' }
        [ordered]@{ order = 6; type = 'test'; status = 'failed'; description = 'dotnet test Support.Mcp.Tests exit 1 Passed 2111 Failed 1 Skipped 0. HandleAsync_NullLayerKey_UsesPersistedCurrentLayerAfterExternalUpdate expected layer-2 actual layer-1'; filePath = 'docs/receipts/_d4-20260822T141644Z/02-support-mcp-tests.trx' }
        [ordered]@{ order = 7; type = 'create'; status = 'completed'; description = 'Wrote D4 parent receipt markdown'; filePath = 'docs/receipts/d4-handoff-gate-20260822T141644Z.md' }
        [ordered]@{ order = 8; type = 'create'; status = 'completed'; description = 'Wrote D4 parent receipt JSON twin'; filePath = 'docs/receipts/d4-handoff-gate-20260822T141644Z.json' }
        [ordered]@{ order = 9; type = 'design_decision'; status = 'completed'; description = 'Stop D4 after command 2 Failed=1. Do not mark TODOs done. Do not start D5.'; filePath = 'docs/plans/PLAN-PLUGINHANDOFF-001.md' }
        [ordered]@{ order = 10; type = 'tool_call'; status = 'completed'; description = 'todo_get after gate still done=false for the four HANDOFF TODOs'; filePath = '' }
    )
}

$complete = Invoke-McpToolCall -Name 'sessionlog_complete_turn' -McpSessionId $mcpSession -Arguments @{
    agent = 'GrokCode'
    sessionId = $sessionId
    requestId = $requestId
    workspacePath = $workspacePath
    turnJson = (ConvertTo-Json -InputObject $turn -Depth 20 -Compress)
}

$query = Invoke-McpToolCall -Name 'sessionlog_query' -McpSessionId $mcpSession -Arguments @{
    workspacePath = $workspacePath
    agent = 'GrokCode'
    text = $sessionId
    limit = 5
}

$finish = [ordered]@{
    dialog = $dialog.Payload
    complete = $complete.Payload
    query = $query.Payload
    todosAfter = $todoAfter
}
$finish | ConvertTo-Json -Depth 12 | Set-Content -LiteralPath (Join-Path $outDir 'session-complete.json') -Encoding utf8

Write-Output ('DIALOG=' + $dialog.Payload.Substring(0, [Math]::Min(250, $dialog.Payload.Length)))
Write-Output ('COMPLETE=' + $complete.Payload.Substring(0, [Math]::Min(400, $complete.Payload.Length)))
foreach ($tid in $todoIds) {
    Write-Output ('TODO_' + $tid + '_DONE=' + $todoAfter[$tid].done)
}
