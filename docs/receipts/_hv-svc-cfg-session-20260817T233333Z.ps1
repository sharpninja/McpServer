#Requires -Version 7.0
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$workspace = 'F:\GitHub\McpServer'
$baseUrl = 'http://PAYTON-LEGION2:7147'
$sessionId = 'GrokCode-20260817T233333Z-hostile-svc-cfg'
$requestId = 'req-20260817T233333Z-001-hostile-validate-svc-cfg'
$utcNow = [datetime]::UtcNow.ToString('o')

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
    Write-Output ("---- MCP {0} id={1} ----" -f $Label, $script:McpId)
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
        Write-Output ("HTTP=" + [int]$resp.StatusCode)
        Write-Output ("Mcp-Session-Id=" + $script:McpSessionHeader)
        if ($body.Contains("`ndata:") -or $body.StartsWith('event:')) {
            $dataLines = @()
            foreach ($line in ($body -split "`n")) {
                $trim = $line.TrimEnd("`r")
                if ($trim.StartsWith('data:')) { $dataLines += $trim.Substring(5).Trim() }
            }
            $body = ($dataLines -join "`n")
        }
        Write-Output $body
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
    clientInfo = @{ name = 'hostile-validator-svc-cfg-phase2'; version = '1.0.0' }
})

Invoke-McpTool -Name 'sessionlog_begin_turn' -Arguments @{
    agent = 'GrokCode'
    sessionId = $sessionId
    requestId = $requestId
    workspacePath = $workspace
    planFile = 'None'
    todoId = 'None'
    queryTitle = 'Hostile validate Windows service AgentHelp config'
    queryText = 'Hostile validator: attack implementer claims about live Windows service AgentHelp config at C:\ProgramData\McpServer\appsettings.yaml. Class 2 ops. No product code. No plan done.'
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
        content = 'Classified as class 2 user-directed general action (live Windows service appsettings). Surface C N/A. Surface D N/A because implementer claimed no plan-step done.'
        category = 'observation'
    },
    @{
        timestamp = $utcNow
        role = 'model'
        content = 'Decision: treat ProgramData appsettings.yaml plus live Agent Help create-session as the authoritative artifacts, not the implementer receipt. Consequence: AGREE only if live YAML, SCM, and independent create-session all match. Alternatives rejected: trusting the implementer receipt, restarting the service, scoring missing FR/TR as a fail.'
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
            description = 'Re-read live C:\ProgramData\McpServer\appsettings.yaml as object. AgentHelp keys DefaultExecutionStrategy=grok-cli HelperModel=grok-4.5 Enabled=true. No effort key.'
            type = 'edit'
            status = 'completed'
            filePath = 'C:\ProgramData\McpServer\appsettings.yaml'
        },
        @{
            order = 3
            description = 'Win32_Service McpServer State=Running StartMode=Auto StartName=LocalSystem PathName=C:\ProgramData\McpServer\McpServer.Support.Mcp.exe --urls http://+:7147 ProcessId=5572. Service not restarted.'
            type = 'edit'
            status = 'completed'
            filePath = 'C:\ProgramData\McpServer\McpServer.Support.Mcp.exe'
        },
        @{
            order = 4
            description = 'Decision: class 2 ops review. OverallVerdict AGREE if all applicable A+B claims PASS and C/D N/A. No product edits. No service restart.'
            type = 'design_decision'
            status = 'completed'
            filePath = ''
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

$decisionsPayload = @{
    designDecisions = @(
        @{
            decision = 'Classify this review as class 2 user-directed ops. Surface C and D are N/A. Do not FAIL for missing FR/TR.'
            rationale = 'Operator locked hostile-ops-vs-requirements on 2026-08-14. Implementer claimed no product code and no plan done.'
            alternativesConsidered = 'Treat live YAML mutation as product implementation requiring FR/TR.'
            affectedRequirements = @()
        }
    )
}

Invoke-McpTool -Name 'sessionlog_replace_section' -Arguments @{
    agent = 'GrokCode'
    sessionId = $sessionId
    requestId = $requestId
    workspacePath = $workspace
    section = 'designDecisions'
    sectionJson = ($decisionsPayload | ConvertTo-Json -Depth 10 -Compress)
}

$turnJson = @{
    requestId = $requestId
    queryTitle = 'Hostile validate Windows service AgentHelp config'
    queryText = 'Hostile validator: attack implementer claims about live Windows service AgentHelp config.'
    interpretation = 'Class 2 ops review of live ProgramData AgentHelp config. Independent re-read of YAML, SCM, and Agent Help create-session.'
    response = 'OverallVerdict pending receipt write. Session turn completed after independent verification.'
    status = 'completed'
    tags = @('hostile-validator', 'agent-help', 'windows-service', 'class-2')
    contextList = @(
        'C:\ProgramData\McpServer\appsettings.yaml',
        'F:\GitHub\McpServer\appsettings.yaml',
        'docs/receipts/windows-service-agenthelp-config-20260817T233017Z.md'
    )
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

Write-Output '=== SESSIONLOG_QUERY agent+from ==='
Invoke-McpTool -Name 'sessionlog_query' -Arguments @{
    workspacePath = $workspace
    agent = 'GrokCode'
    from = '2026-08-17T23:32:00Z'
    limit = 10
}

Write-Output '=== SESSIONLOG_QUERY text=hostile-svc-cfg ==='
Invoke-McpTool -Name 'sessionlog_query' -Arguments @{
    workspacePath = $workspace
    agent = 'GrokCode'
    text = 'hostile-svc-cfg'
    limit = 10
}

Write-Output 'MCP_PHASE2_DONE'
