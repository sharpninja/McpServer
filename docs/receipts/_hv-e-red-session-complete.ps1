#Requires -Version 7.0
[CmdletBinding()]
param()

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$baseUrl = 'http://PAYTON-LEGION2:7147/mcp-transport'
$workspacePath = 'F:\GitHub\McpServer'
$sessionId = 'GrokSubagentHostile-20260822T151657Z-e-red-hostile'
$requestId = 'req-20260822T151657Z-001-e-red-hostile-validate'
$outDir = 'F:\GitHub\McpServer\docs\receipts\_hv-e-red-20260822T151657Z'

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

function Get-McpPayloadText {
    param([string]$Payload)
    try {
        $obj = $Payload | ConvertFrom-Json
        if ($obj.result.content -and $obj.result.content.Count -gt 0) {
            return [string]$obj.result.content[0].text
        }
        if ($obj.error) { return ($obj.error | ConvertTo-Json -Compress -Depth 10) }
        return $Payload
    } catch {
        return $Payload
    }
}

$init = Invoke-McpJsonRpc -Body @{
    jsonrpc = '2.0'; id = 1; method = 'initialize'
    params = @{ protocolVersion = '2024-11-05'; capabilities = @{}; clientInfo = @{ name = 'GrokSubagentHostile'; version = '1.0.5' } }
}
$mcpSession = [string]$init.SessionId
try {
    Invoke-McpJsonRpc -Body @{ jsonrpc = '2.0'; method = 'notifications/initialized'; params = @{} } -McpSessionId $mcpSession | Out-Null
} catch {}

$todoPlan = Invoke-McpToolCall -Name 'todo_get' -McpSessionId $mcpSession -Arguments @{
    id = 'PLAN-PLUGINHANDOFF-001'
    workspacePath = $workspacePath
}
$todoHr = Invoke-McpToolCall -Name 'todo_get' -McpSessionId $mcpSession -Arguments @{
    id = 'MCP-HOSTILEREVIEW-001'
    workspacePath = $workspacePath
}

$now = [DateTimeOffset]::UtcNow.ToString('o')
$dialogItems = @(
    [ordered]@{
        timestamp = $now
        role = 'model'
        category = 'observation'
        content = 'Independent worktree HostileReview filter: list-tests 16 names matching plan section 9. Rerun Failed 16 Passed 0 Skipped 0 Total 16. TRX counters total=16 executed=16 passed=0 failed=16. Product surfaces absent. todo_get PLAN-PLUGINHANDOFF-001 and MCP-HOSTILEREVIEW-001 Done=false. FR/TR/TEST-MCP-HOSTILEREVIEW-001 through 006 present with AC.'
    }
    [ordered]@{
        timestamp = $now
        role = 'model'
        category = 'decision'
        content = 'Decision: OverallVerdict AGREE on E-red only. Rationale: all applicable A+B+C+D claims independently re-verified PASS. Alternatives rejected: DISAGREE for FR-005 each-dimension wording (TEST-005/TR-005/plan lock the three named query tests); treating throw-stub contracts as E-green mixed-in (parent allowed compile stub; REST/plugin/EF remain absent); flipping TODOs done (reds still fail; hostile-on-goal-state forbids it). Consequence: parent may start E-green; PLAN and MCP-HOSTILEREVIEW-001 stay done:false.'
    }
)
$dialog = Invoke-McpToolCall -Name 'sessionlog_dialog' -McpSessionId $mcpSession -Arguments @{
    agent = 'GrokSubagentHostile'
    sessionId = $sessionId
    requestId = $requestId
    workspacePath = $workspacePath
    itemsJson = (ConvertTo-Json -InputObject $dialogItems -Depth 8 -Compress)
}

$turn = [ordered]@{
    requestId = $requestId
    queryTitle = 'Hostile E-red gate for HostileReview tests'
    interpretation = 'Operator asked for an independent Class 1 hostile validation of E-red claims for PLAN-PLUGINHANDOFF-001 section 9. Surfaces A+B+C+D. No product implementation. No TODO done flip.'
    response = 'OverallVerdict AGREE. Receipt docs/receipts/hostile-validator-20260822T152722Z.md and .json. Independent HostileReview filter in the named worktree: Failed 16 Passed 0 Skipped 0. All 16 plan names exist as failing [Fact] methods. Product surfaces absent. PLAN-PLUGINHANDOFF-001 and MCP-HOSTILEREVIEW-001 remain Done=false. Did not implement HostileReview product. Did not mark TODOs done.'
    status = 'completed'
    model = 'grok-4'
    planFile = 'docs/plans/PLAN-PLUGINHANDOFF-001.md'
    todoId = 'PLAN-PLUGINHANDOFF-001'
    tags = @('PLAN-PLUGINHANDOFF-001', 'MCP-HOSTILEREVIEW-001', 'E-red', 'hostile-validator', 'FR-MCP-HOSTILEREVIEW-001', 'FR-MCP-HOSTILEREVIEW-002', 'FR-MCP-HOSTILEREVIEW-003', 'FR-MCP-HOSTILEREVIEW-004', 'FR-MCP-HOSTILEREVIEW-005', 'FR-MCP-HOSTILEREVIEW-006')
    contextList = @(
        'docs/plans/PLAN-PLUGINHANDOFF-001.md'
        'docs/receipts/e-red-20260822T143042Z.md'
        'docs/receipts/hostile-validator-20260822T152722Z.md'
        'C:\Users\kingd\.grok\worktrees\github-mcpserver\subagent-01a029d1-24e5-7603-879b-832cfd4fe7f7\tests\McpServer.Support.Mcp.Tests\Services\HostileReviewQueueTests.cs'
    )
    filesModified = @(
        'docs/receipts/hostile-validator-20260822T152722Z.md'
        'docs/receipts/hostile-validator-20260822T152722Z.json'
        'docs/receipts/_hv-e-red-20260822T151657Z/hostile-review-hv-e-red.trx'
    )
    designDecisions = @(
        'OverallVerdict AGREE for E-red only after independent 16/0/0 rerun and surface greps. Do not mark PLAN-PLUGINHANDOFF-001 or MCP-HOSTILEREVIEW-001 done. E-green is a later gate.'
        'Treat throw-stub plus contracts as allowed E-red compile scaffolding, not E-green, because REST, MCP tools, client, director, plugin skill, DbSet, and migrations remain absent.'
        'Do not FAIL C on FR-005 each-dimension wording: TEST-005, TR-005, and plan section 9 lock the three named query tests that exist and fail.'
    )
    requirementsDiscovered = @()
    blockers = @()
    actions = @(
        [ordered]@{ order = 1; type = 'tool_call'; status = 'completed'; description = 'add-profile: read 18 non-skill profile md files; excluded add-profile.grok.md'; filePath = 'C:\Users\kingd\.claude\profile' }
        [ordered]@{ order = 2; type = 'tool_call'; status = 'completed'; description = 'Test-MarkerSignature True; health nonce 56cec7485dc044e6be86d4c13170fdb9 echoed'; filePath = 'AGENTS-README-FIRST.yaml' }
        [ordered]@{ order = 3; type = 'tool_call'; status = 'completed'; description = 'sessionlog_open created GrokSubagentHostile-20260822T151657Z-e-red-hostile'; filePath = 'docs/receipts/_hv-e-red-20260822T151657Z/ids.json' }
        [ordered]@{ order = 4; type = 'tool_call'; status = 'completed'; description = 'sessionlog_begin_turn turnId=43058 status=in_progress'; filePath = 'docs/receipts/_hv-e-red-20260822T151657Z/ids.json' }
        [ordered]@{ order = 5; type = 'tool_call'; status = 'completed'; description = 'todo_get PLAN-PLUGINHANDOFF-001 and MCP-HOSTILEREVIEW-001 Done=false'; filePath = '' }
        [ordered]@{ order = 6; type = 'test'; status = 'completed'; description = 'Independent HostileReview filter Failed 16 Passed 0 Skipped 0 Total 16'; filePath = 'docs/receipts/_hv-e-red-20260822T151657Z/hostile-review-hv-e-red.trx' }
        [ordered]@{ order = 7; type = 'create'; status = 'completed'; description = 'Wrote hostile E-red validator receipt markdown'; filePath = 'docs/receipts/hostile-validator-20260822T152722Z.md' }
        [ordered]@{ order = 8; type = 'create'; status = 'completed'; description = 'Wrote hostile E-red validator receipt JSON twin'; filePath = 'docs/receipts/hostile-validator-20260822T152722Z.json' }
        [ordered]@{ order = 9; type = 'design_decision'; status = 'completed'; description = 'AGREE E-red only. Do not flip TODOs. Do not implement product.'; filePath = 'docs/plans/PLAN-PLUGINHANDOFF-001.md' }
    )
}

$complete = Invoke-McpToolCall -Name 'sessionlog_complete_turn' -McpSessionId $mcpSession -Arguments @{
    agent = 'GrokSubagentHostile'
    sessionId = $sessionId
    requestId = $requestId
    workspacePath = $workspacePath
    turnJson = (ConvertTo-Json -InputObject $turn -Depth 20 -Compress)
}

$query = Invoke-McpToolCall -Name 'sessionlog_query' -McpSessionId $mcpSession -Arguments @{
    workspacePath = $workspacePath
    agent = 'GrokSubagentHostile'
    text = $sessionId
    limit = 5
}

$proof = [ordered]@{
    mcpSession = $mcpSession
    dialogStatus = $dialog.StatusCode
    dialogText = Get-McpPayloadText $dialog.Payload
    completeStatus = $complete.StatusCode
    completeText = Get-McpPayloadText $complete.Payload
    queryStatus = $query.StatusCode
    queryText = Get-McpPayloadText $query.Payload
    todoPlan = Get-McpPayloadText $todoPlan.Payload
    todoHr = Get-McpPayloadText $todoHr.Payload
}
$proof | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath (Join-Path $outDir 'session-complete.json') -Encoding utf8
Write-Output ('DIALOG=' + (Get-McpPayloadText $dialog.Payload))
Write-Output ('COMPLETE=' + (Get-McpPayloadText $complete.Payload))
$queryText = Get-McpPayloadText $query.Payload
Write-Output ('QUERY_LEN=' + $queryText.Length)
if ($queryText -match '"status"\s*:\s*"([^"]+)"') { Write-Output ('QUERY_STATUS_HIT=' + $Matches[1]) }
if ($queryText -match $requestId) { Write-Output 'QUERY_HAS_REQUESTID=True' } else { Write-Output 'QUERY_HAS_REQUESTID=False' }
if ($queryText -match $sessionId) { Write-Output 'QUERY_HAS_SESSIONID=True' } else { Write-Output 'QUERY_HAS_SESSIONID=False' }
if ($queryText -match 'completed') { Write-Output 'QUERY_HAS_COMPLETED=True' } else { Write-Output 'QUERY_HAS_COMPLETED=False' }
Write-Output ('TODO_PLAN_SNIP=' + ((Get-McpPayloadText $todoPlan.Payload).Substring(0, [Math]::Min(200, (Get-McpPayloadText $todoPlan.Payload).Length))))
Write-Output ('TODO_HR_SNIP=' + ((Get-McpPayloadText $todoHr.Payload).Substring(0, [Math]::Min(200, (Get-McpPayloadText $todoHr.Payload).Length))))
