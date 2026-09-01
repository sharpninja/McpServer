#Requires -Version 7.0
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$workspace = 'F:\GitHub\McpServer'
$baseUrl = 'http://PAYTON-LEGION2:7147'
$sessionId = 'GrokCode-20260818T001536Z-hostile-help-smoke'
$requestId = 'req-20260818T001536Z-001-hostile-validate-help-smoke'
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
        if ($body.Length -gt 5000) {
            Write-Output ($body.Substring(0, 5000))
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
    clientInfo = @{ name = 'hostile-validator-help-smoke-session'; version = '1.0.0' }
})

Invoke-McpTool -Name 'sessionlog_open' -Arguments @{
    agent = 'GrokCode'
    sessionId = $sessionId
    workspacePath = $workspace
    title = 'Hostile validate live Agent Help smoke'
    model = 'grok-4.5'
}

Invoke-McpTool -Name 'sessionlog_begin_turn' -Arguments @{
    agent = 'GrokCode'
    sessionId = $sessionId
    requestId = $requestId
    workspacePath = $workspace
    queryTitle = 'Hostile validate live Agent Help smoke'
    queryText = 'Hostile validator: attack implementer claims about a Class 2 live Agent Help smoke test on the running McpServer service. Session help-20260818001213-0aa9f6de59d2403296130363aa94bb75. No product code. No plan done.'
}

$dialogItems = @(
    @{
        timestamp = $utcNow
        role = 'model'
        content = 'add-profile executed first. 18 non-skill profile markdown files read under C:\Users\kingd\.claude\profile. Excluded add-profile.grok.md.'
        category = 'observation'
    },
    @{
        timestamp = $utcNow
        role = 'model'
        content = 'Classified as class 2 user-directed general action (live Agent Help smoke). Surface C N/A. Surface D N/A because implementer claimed no plan-step done.'
        category = 'observation'
    },
    @{
        timestamp = $utcNow
        role = 'model'
        content = 'Independent live checks: Get-Service Running; Win32_Service ProcessId=57744; marker pid=57744; Test-MarkerSignature=True; health nonce 1b881e6140984ed884726b15a66c4831 echoed; storage=reachable; /ready 200 storage Healthy.'
        category = 'observation'
    },
    @{
        timestamp = $utcNow
        role = 'model'
        content = 'Independent agent_help_get_status for help-20260818001213-0aa9f6de59d2403296130363aa94bb75: idle, lastTurnId=turn-0001, turnCounter=1, executionStrategy=grok-cli, topic=live-agent-help-smoke, terminated=false. Transcript: 3 items (system corpus 10 excerpts, user prompt, assistant text exact match).'
        category = 'observation'
    },
    @{
        timestamp = $utcNow
        role = 'model'
        content = 'Server log L354282 create_session output modelRequested=grok-4.5 modelResolved=grok-4.5 executionStrategy=grok-cli. L356856 submit_turn HTTP 200 in 55909.97ms. L356857 turn-0001 status=completed latencyMs=55827 guardResult.allowed=true assistantDisplayText exact match. Independent no-override create_session help-20260818001542-2266fbba1cbb4d669ae2a7d125ae54a0 returned the same model and strategy defaults.'
        category = 'observation'
    },
    @{
        timestamp = $utcNow
        role = 'model'
        content = 'Decision: all applicable A and B claims PASS. Surface C N/A. Surface D N/A. Consequence: OverallVerdict AGREE. Alternatives rejected: FAIL A1 for the pre-existing dirty handoff tree (RecentSrcTestsCount after 00:11Z is 0); treat model fields as UNKNOWN because get_status omits them (create_session log body and independent create_session prove them); FAIL C for missing FR/TR on class 2 ops.'
        category = 'decision'
    }
)

Invoke-McpTool -Name 'sessionlog_dialog' -Arguments @{
    agent = 'GrokCode'
    sessionId = $sessionId
    requestId = $requestId
    workspacePath = $workspace
    itemsJson = ($dialogItems | ConvertTo-Json -Depth 10 -Compress)
}

$actionsPayload = @{
    actions = @(
        @{
            order = 1
            description = 'add-profile executed; 18 non-skill profile markdown files read'
            type = 'design_decision'
            status = 'completed'
            filePath = 'C:\Users\kingd\.claude\profile\PROFILE.md'
        },
        @{
            order = 2
            description = 'Re-queried Win32_Service and Get-Service. State=Running ProcessId=57744 StartMode=Auto StartName=LocalSystem. Independent /health 200 nonce 1b881e6140984ed884726b15a66c4831 echoed; storage=reachable. /ready 200.'
            type = 'edit'
            status = 'completed'
            filePath = 'C:\ProgramData\McpServer\McpServer.Support.Mcp.exe'
        },
        @{
            order = 3
            description = 'Independent agent_help_get_status and agent_help_get_transcript for help-20260818001213-0aa9f6de59d2403296130363aa94bb75. Status idle turnCounter=1 executionStrategy=grok-cli. Transcript 3 items with matching assistant text.'
            type = 'edit'
            status = 'completed'
            filePath = 'http://PAYTON-LEGION2:7147/mcp-transport'
        },
        @{
            order = 4
            description = 'Independent no-override agent_help_create_session returned help-20260818001542-2266fbba1cbb4d669ae2a7d125ae54a0 modelRequested=grok-4.5 modelResolved=grok-4.5 executionStrategy=grok-cli.'
            type = 'edit'
            status = 'completed'
            filePath = 'C:\ProgramData\McpServer\appsettings.yaml'
        },
        @{
            order = 5
            description = 'Full-file log scan of C:\ProgramData\McpServer\logs\mcp-20260817.log proved create_session output and submit_turn latencyMs=55827 guard allowed HTTP 200 in 55909.97ms.'
            type = 'edit'
            status = 'completed'
            filePath = 'C:\ProgramData\McpServer\logs\mcp-20260817.log'
        },
        @{
            order = 6
            description = 'Decision: OverallVerdict AGREE. Class 2 smoke claims re-verified. No product edits. Service not restarted. --effort argv was not claimed and was not scored as a missing proof.'
            type = 'design_decision'
            status = 'completed'
            filePath = 'F:\GitHub\McpServer\docs\receipts\agenthelp-live-smoke-20260818T001316Z.md'
        }
    )
}

Invoke-McpTool -Name 'sessionlog_replace_section' -Arguments @{
    agent = 'GrokCode'
    sessionId = $sessionId
    requestId = $requestId
    workspacePath = $workspace
    section = 'actions'
    sectionJson = ($actionsPayload | ConvertTo-Json -Depth 10 -Compress)
}

$turnJson = @{
    requestId = $requestId
    queryTitle = 'Hostile validate live Agent Help smoke'
    queryText = 'Hostile validator: attack implementer claims about a Class 2 live Agent Help smoke test on the running McpServer service.'
    interpretation = 'Class 2 ops review of a claimed live Agent Help create-session plus one grok-cli turn. Independent service, health, MCP status/transcript, create-session, YAML, and server-log checks.'
    response = 'OverallVerdict AGREE. Independent get_status/get_transcript, no-override create_session, and mcp-20260817.log lines L354282/L356856/L356857 re-verify the implementer smoke claims. C N/A. D N/A.'
    status = 'completed'
    tags = @('hostile-validator', 'agent-help', 'class-2', 'smoke')
    contextList = @(
        'F:\GitHub\McpServer\docs\receipts\agenthelp-live-smoke-20260818T001316Z.md',
        'F:\GitHub\McpServer\AGENTS-README-FIRST.yaml',
        'C:\ProgramData\McpServer\appsettings.yaml',
        'C:\ProgramData\McpServer\logs\mcp-20260817.log'
    )
    planFile = 'None'
    todoId = 'None'
}

Invoke-McpTool -Name 'sessionlog_complete_turn' -Arguments @{
    agent = 'GrokCode'
    sessionId = $sessionId
    requestId = $requestId
    workspacePath = $workspace
    turnJson = ($turnJson | ConvertTo-Json -Depth 10 -Compress)
}

Write-Output '=== SESSIONLOG_QUERY text=sessionId ==='
Invoke-McpTool -Name 'sessionlog_query' -Arguments @{
    workspacePath = $workspace
    agent = 'GrokCode'
    text = $sessionId
    limit = 5
}

Write-Output '=== SESSIONLOG_QUERY text=hostile-help-smoke ==='
Invoke-McpTool -Name 'sessionlog_query' -Arguments @{
    workspacePath = $workspace
    agent = 'GrokCode'
    text = 'hostile-help-smoke'
    limit = 10
}

Write-Output '=== SESSIONLOG_QUERY agent+from ==='
Invoke-McpTool -Name 'sessionlog_query' -Arguments @{
    workspacePath = $workspace
    agent = 'GrokCode'
    from = '2026-08-18T00:15:00Z'
    limit = 10
}

Write-Output ('SESSION_IDS sessionId=' + $sessionId + ' requestId=' + $requestId)
Write-Output 'MCP_SESSION_DONE'
