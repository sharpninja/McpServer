#Requires -Version 7.0
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$workspace = 'F:\GitHub\McpServer'
$baseUrl = 'http://PAYTON-LEGION2:7147'
$stamp = [datetime]::UtcNow.ToString('yyyyMMddTHHmmssZ')
$sessionId = "GrokCode-$stamp-hostile-grok-503"
$requestId = "req-$stamp-001-hostile-validate-grok-503"
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
            $dataLines = [System.Collections.Generic.List[string]]::new()
            foreach ($line in ($body -split "`n")) {
                $trim = $line.TrimEnd("`r")
                if ($trim.StartsWith('data:')) { [void]$dataLines.Add($trim.Substring(5).Trim()) }
            }
            $body = ($dataLines -join "`n")
        }
        if ($body.Length -gt 4500) {
            Write-Output ($body.Substring(0, 4500))
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
    clientInfo = @{ name = 'hostile-validator-grok-503'; version = '1.0.0' }
})

Invoke-McpTool -Name 'sessionlog_open' -Arguments @{
    agent = 'GrokCode'
    sessionId = $sessionId
    workspacePath = $workspace
    title = 'Hostile validate Grok backend_unavailable attribution'
    model = 'grok-4.5'
}

Invoke-McpTool -Name 'sessionlog_begin_turn' -Arguments @{
    agent = 'GrokCode'
    sessionId = $sessionId
    requestId = $requestId
    workspacePath = $workspace
    planFile = 'None'
    todoId = 'None'
    queryTitle = 'Hostile validate Grok 503 backend_unavailable claims'
    queryText = 'Hostile validator class 2: attack implementer claims that Grok reported backend_unavailable, this Grok hit HTTP 503 at 2026-08-17T23:52:23Z on POST /mcp-transport, server log 18:52 SQL Named Pipes Access is denied, live Provider=sqlserver, Grok failsafe is internal_server_error not 503, earlier first-health DISAGREE still compatible.'
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
        content = 'Classified as class 2 user-directed incident correction. Surface C N/A. Surface D N/A because implementer claimed no plan-step done.'
        category = 'observation'
    },
    @{
        timestamp = $utcNow
        role = 'model'
        content = 'Independent re-read of live C:\ProgramData\McpServer\appsettings.yaml via Read-McpYamlObject: Mcp.Database.Provider=sqlserver. Marker signature True. Service PID 57744. Health nonce a5aabdd823f642b8b82084b9b7a86d76 echoed, storage currently reachable.'
        category = 'observation'
    },
    @{
        timestamp = $utcNow
        role = 'model'
        content = 'Server log mcp-20260817.log: 18:38-18:42 unreachable=0 backend=0 status503=0; first GET /health storage=reachable. 18:52:13 storage probe timed out 5s Unhealthy. 18:52:23.884 sessionlog_replace_section for GrokCode-20260817T120000Z-agent-help-grok-cli logged completed 200 Output none. 18:52:23.886 unhandled POST /mcp-transport TraceId 00-aab0888980690d5c55a8af5c029f0bd1-9c0f446ccbcb5618-01 SqlException Named Pipes error 40 Win32Exception (5) Access is denied WorkspaceService.EnsureBootstrappedAsync line 407. 18:52:26 storage Unhealthy and /health storage=unreachable. Failsafe a650 is method_invocation_error internal_server_error SubmitAsync canceled turn without planFile/todoId, raw 503=false.'
        category = 'observation'
    },
    @{
        timestamp = $utcNow
        role = 'model'
        content = 'Decision: PASS all applicable A/B claims. Interaction log 200 is the finally block running before GlobalExceptionHandlerMiddleware rewrites the response; SqlException with Win32Exception inner is classified backend_unavailable and that middleware sets HTTP 503. Consequence: OverallVerdict AGREE. Alternatives rejected: FAIL A2 because the interaction logger printed 200; treat the Grok plugin failsafe as the 503; reopen the 18:38-18:42 first-health DISAGREE; FAIL C for missing FR/TR on class 2 incident correction.'
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
            description = 'Re-read live ProgramData appsettings.yaml as object. Mcp.Database.Provider=sqlserver. SHA256=B42E2462D67EADE136EC3BF64A1224BF1253ADB73EA6596CFED1BC7C7A4E3D46'
            type = 'edit'
            status = 'completed'
            filePath = 'C:\ProgramData\McpServer\appsettings.yaml'
        },
        @{
            order = 3
            description = 'Independent scan of mcp-20260817.log. Trace 00-aab0888980690d5c55a8af5c029f0bd1-9c0f446ccbcb5618-01 is POST /mcp-transport unhandled SqlException Named Pipes / Access is denied / EnsureBootstrappedAsync:407. 18:52:13 probe timeout 5s. 18:52:26 Unhealthy.'
            type = 'edit'
            status = 'completed'
            filePath = 'C:\ProgramData\McpServer\logs\mcp-20260817.log'
        },
        @{
            order = 4
            description = 'Failsafe 20260818T001239Z-session_submit-a650.yaml deserialized. lastDrainError method_invocation_error internal_server_error SubmitAsync. Canceled turn req-20260818T001131Z-prompt-b813 has no planFile or todoId. Raw file has no 503 and no backend_unavailable.'
            type = 'edit'
            status = 'completed'
            filePath = 'F:\GitHub\McpServer\.mcpServer\grok\failsafe\20260818T001239Z-session_submit-a650.yaml'
        },
        @{
            order = 5
            description = 'Decision: earlier 18:38-18:42 DISAGREE remains compatible. 503-class SQL outage is at 18:52, not first health. OverallVerdict AGREE. No product edits. Service not restarted.'
            type = 'design_decision'
            status = 'completed'
            filePath = 'F:\GitHub\McpServer\docs\receipts\hostile-validator-20260818T000400Z.md'
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
    queryTitle = 'Hostile validate Grok 503 backend_unavailable claims'
    queryText = 'Hostile validator class 2: attack implementer claims that Grok reported backend_unavailable.'
    interpretation = 'Class 2 incident-correction review. Re-read live yaml, failsafe object, and server log independently.'
    response = 'OverallVerdict AGREE. All applicable A and B claims re-verified. C N/A. D N/A.'
    status = 'completed'
    tags = @('hostile-validator', 'backend-unavailable', 'grok-503', 'class-2')
    contextList = @(
        'F:\GitHub\McpServer\docs\receipts\grok-reported-backend-unavailable-20260818T003855Z.md',
        'F:\GitHub\McpServer\docs\receipts\hostile-validator-20260818T000400Z.md',
        'C:\ProgramData\McpServer\appsettings.yaml',
        'C:\ProgramData\McpServer\logs\mcp-20260817.log',
        'F:\GitHub\McpServer\.mcpServer\grok\failsafe\20260818T001239Z-session_submit-a650.yaml'
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

Write-Output '=== SESSIONLOG_QUERY text=hostile-grok-503 ==='
Invoke-McpTool -Name 'sessionlog_query' -Arguments @{
    workspacePath = $workspace
    agent = 'GrokCode'
    text = 'hostile-grok-503'
    limit = 10
}

Write-Output '=== SESSIONLOG_QUERY agent+from ==='
Invoke-McpTool -Name 'sessionlog_query' -Arguments @{
    workspacePath = $workspace
    agent = 'GrokCode'
    from = '2026-08-18T00:40:00Z'
    limit = 15
}

Write-Output ('SESSION_IDS sessionId=' + $sessionId + ' requestId=' + $requestId)
Write-Output 'MCP_SESSION_DONE'
