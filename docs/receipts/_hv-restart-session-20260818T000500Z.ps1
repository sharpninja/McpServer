#Requires -Version 7.0
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$workspace = 'F:\GitHub\McpServer'
$baseUrl = 'http://PAYTON-LEGION2:7147'
$stamp = [datetime]::UtcNow.ToString('yyyyMMddTHHmmssZ')
$sessionId = "GrokCode-$stamp-hostile-restart"
$requestId = "req-$stamp-001-hostile-validate-restart"
$utcNow = [datetime]::UtcNow.ToString('o')
Write-Output ('UTC_STAMP=' + $stamp)
Write-Output ('SESSION_ID=' + $sessionId)
Write-Output ('REQUEST_ID=' + $requestId)

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
    $req = [System.Net.Http.HttpRequestMessage]::new([System.Net.Http.HttpMethod]::Post, "$baseUrl/mcp-transport")
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
        if ($body.Length -gt 4000) {
            Write-Output ($body.Substring(0, 4000))
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
    clientInfo = @{ name = 'hostile-validator-restart'; version = '1.0.0' }
})

Invoke-McpTool -Name 'sessionlog_open' -Arguments @{
    agent = 'GrokCode'
    sessionId = $sessionId
    workspacePath = $workspace
    title = 'Hostile validate McpServer Windows service restart'
    model = 'grok-4.5'
}

Invoke-McpTool -Name 'sessionlog_begin_turn' -Arguments @{
    agent = 'GrokCode'
    sessionId = $sessionId
    requestId = $requestId
    workspacePath = $workspace
    planFile = 'None'
    todoId = 'None'
    queryTitle = 'Hostile validate McpServer service restart'
    queryText = 'Hostile validator: attack implementer claims about restarting Windows service McpServer. Class 2 ops. Old PID 5572, new PID 57744, marker pid/apiKey rotation, first health storage unreachable then later reachable, AgentHelp survived, no binary deploy, no SCM change.'
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
        content = 'Classified as class 2 user-directed general action (Windows service restart). Surface C N/A. Surface D N/A because implementer claimed no plan-step done.'
        category = 'observation'
    },
    @{
        timestamp = $utcNow
        role = 'model'
        content = 'Independent live checks: Get-Service Running; Win32_Service ProcessId=57744 StartMode=Auto StartName=LocalSystem; marker pid=57744 matches; health nonce echoed; storage currently reachable; live AgentHelp grok-cli / grok-4.5 / Enabled=true.'
        category = 'observation'
    },
    @{
        timestamp = $utcNow
        role = 'model'
        content = 'Server log 2026-08-17 18:38:27 graceful shutdown PID=5572; 18:38:31 startup PID=57744; 18:38:56 first GET /health 200 Output storage=reachable nonce=ffbf87a5a57c46cdada44497d922e256. Zero unreachable hits in 18:38-18:42. Claim that first post-restart health was storage unreachable is false.'
        category = 'observation'
    },
    @{
        timestamp = $utcNow
        role = 'model'
        content = 'Decision: FAIL A3 and B5 because the first logged post-start GET /health body is storage=reachable, not unreachable. Consequence: OverallVerdict DISAGREE. Alternatives rejected: treat later reachable health as enough to pass the compound claim; treat implementer receipt as proof of the first body; FAIL C for missing FR/TR on a class 2 restart.'
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
            description = 'Re-queried Win32_Service and Get-Service. State=Running ProcessId=57744 StartMode=Auto StartName=LocalSystem PathName=C:\ProgramData\McpServer\McpServer.Support.Mcp.exe --urls http://+:7147'
            type = 'edit'
            status = 'completed'
            filePath = 'C:\ProgramData\McpServer\McpServer.Support.Mcp.exe'
        },
        @{
            order = 3
            description = 'Marker pid=57744 matches service. Test-MarkerSignature=True. Pre-restart workspace RequestHeaders key IHOW...idDI; current marker key N3fW...RMao. Marker rewritten 18:38:48.798.'
            type = 'edit'
            status = 'completed'
            filePath = 'F:\GitHub\McpServer\AGENTS-README-FIRST.yaml'
        },
        @{
            order = 4
            description = 'Independent /health 200 nonce 36d0cbc1c48647afa537ca0a4e50d71d echoed; storage=reachable. /ready 200 storage Healthy workspace-ready Healthy.'
            type = 'edit'
            status = 'completed'
            filePath = 'http://127.0.0.1:7147/health'
        },
        @{
            order = 5
            description = 'Live ProgramData appsettings.yaml SHA256 unchanged from prior 23:36 hostile receipt. AgentHelp DefaultExecutionStrategy=grok-cli HelperModel=grok-4.5 Enabled=true.'
            type = 'edit'
            status = 'completed'
            filePath = 'C:\ProgramData\McpServer\appsettings.yaml'
        },
        @{
            order = 6
            description = 'Decision: first logged post-start /health Output is storage=reachable. FAIL A3 and B5. OverallVerdict DISAGREE. No product edits. Service not restarted by this review.'
            type = 'design_decision'
            status = 'completed'
            filePath = 'C:\ProgramData\McpServer\logs\mcp-20260817.log'
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
    queryTitle = 'Hostile validate McpServer service restart'
    queryText = 'Hostile validator: attack implementer claims about restarting Windows service McpServer.'
    interpretation = 'Class 2 ops review of a claimed one-shot Restart-Service. Independent CIM, marker, health, ready, live YAML, and server-log checks.'
    response = 'OverallVerdict DISAGREE. A3 first-health storage=unreachable is false; first logged GET /health after Application started is storage=reachable. B5 honesty fail. Other A claims re-verified.'
    status = 'completed'
    tags = @('hostile-validator', 'windows-service', 'restart', 'class-2')
    contextList = @(
        'F:\GitHub\McpServer\docs\receipts\restart-mcpserver-20260817T233829Z.md',
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

Write-Output '=== SESSIONLOG_QUERY text=hostile-restart ==='
Invoke-McpTool -Name 'sessionlog_query' -Arguments @{
    workspacePath = $workspace
    agent = 'GrokCode'
    text = 'hostile-restart'
    limit = 10
}

Write-Output '=== SESSIONLOG_QUERY agent+from ==='
Invoke-McpTool -Name 'sessionlog_query' -Arguments @{
    workspacePath = $workspace
    agent = 'GrokCode'
    from = '2026-08-18T00:00:00Z'
    limit = 10
}

Write-Output ('SESSION_IDS sessionId=' + $sessionId + ' requestId=' + $requestId)
Write-Output 'MCP_SESSION_DONE'
