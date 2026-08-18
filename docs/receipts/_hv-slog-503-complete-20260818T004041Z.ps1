#Requires -Version 7.0
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$workspace = 'F:\GitHub\McpServer'
$baseUrl = 'http://PAYTON-LEGION2:7147'
$sessionId = 'GrokCode-20260818T004041Z-hostile-slog-503'
$requestId = 'req-20260818T004041Z-001-hostile-validate-slog-503'
$utcNow = [datetime]::UtcNow
Write-Output ('UTC_NOW=' + $utcNow.ToString('o'))
Write-Output ('UTC_COMPACT=' + $utcNow.ToString('yyyyMMddTHHMMssZ'))

$wmi = Get-CimInstance -ClassName Win32_Service -Filter "Name='McpServer'"
Write-Output ('WMI_PID=' + $wmi.ProcessId)
Write-Output ('WMI_STATE=' + $wmi.State)

$script:McpSessionHeader = $null
$script:McpId = 0

function Invoke-McpRpc {
    param(
        [Parameter(Mandatory)][string]$Method,
        $Params = $null,
        [string]$Label = $Method
    )
    $script:McpId++
    $payload = [ordered]@{ jsonrpc = '2.0'; id = $script:McpId; method = $Method }
    if ($null -ne $Params) { $payload['params'] = $Params }
    $json = $payload | ConvertTo-Json -Depth 30 -Compress
    Write-Output ('---- MCP {0} ----' -f $Label)
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
    $client.Timeout = [TimeSpan]::FromSeconds(180)
    try {
        $resp = $client.Send($req)
        $sid = $null
        if ($resp.Headers.TryGetValues('Mcp-Session-Id', [ref]$sid)) { $script:McpSessionHeader = @($sid)[0] }
        $body = $resp.Content.ReadAsStringAsync().GetAwaiter().GetResult()
        Write-Output ('HTTP=' + [int]$resp.StatusCode)
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
    clientInfo = @{ name = 'hostile-validator-slog-503-complete'; version = '1.0.0' }
})

$ts = $utcNow.ToString('o')
$dialogItems = @(
    @{
        timestamp = $ts
        role = 'model'
        content = 'add-profile executed first. 18 non-skill profile markdown files read under C:\Users\kingd\.claude\profile. Excluded add-profile.grok.md.'
        category = 'observation'
    },
    @{
        timestamp = $ts
        role = 'model'
        content = 'Classified as class 2 user-directed incident diagnosis. Surface C N/A. Surface D N/A. Byrd v4 not applied to the ops action.'
        category = 'observation'
    },
    @{
        timestamp = $ts
        role = 'model'
        content = 'Independent live: Get-Service Running; Win32_Service PID 57744 matches marker; Test-MarkerSignature True; health nonce 5a05395663364549986a2f4092cbaf36 echoed; storage=reachable; /ready 200 storage Healthy workspace-ready Healthy.'
        category = 'observation'
    },
    @{
        timestamp = $ts
        role = 'model'
        content = 'Failsafe 20260818T001252Z-session_submit-f830.yaml deserialized: lastDrainError message internal_server_error, no backend_unavailable. Canceled turn keys omit planFile and todoId. File still being retried (drainAttempts increased after implementer receipt).'
        category = 'observation'
    },
    @{
        timestamp = $ts
        role = 'model'
        content = 'mcp-20260817.log: 19:31-19:35 local has ArgumentException planFile is omitted (18 window hits). Prefix POST /mcpserver/sessionlog completed with 503 since 18:38 local: 0. Post-restart sessionlog statuses 200/201/400 only. Morning 05:42 vice-sharp requirements_update returned error backend_unavailable against SQL 192.168.1.77.'
        category = 'observation'
    },
    @{
        timestamp = $ts
        role = 'model'
        content = 'sessionlog_query proved implementer session GrokCode-20260817T120000Z-agent-help-grok-cli turn req-20260818T003125Z-008-sessionlog-backend-unavailable completed; log L373060 begin_turn turnId 41593. This review session begin_turn turnId 41616.'
        category = 'observation'
    },
    @{
        timestamp = $ts
        role = 'model'
        content = 'Decision: all applicable A and B claims PASS. Surface C N/A. Surface D N/A. Consequence: OverallVerdict AGREE. Alternatives rejected: FAIL A4 because naive substring completed-with-503 appears inside 201 bodies (prefix status is 201, not 503); FAIL A5 because 18:52 Unhealthy exists (that is mcp-transport/SQL, not a sessionlog 503 completion, and claim 5 is existential about the morning vice-sharp incident); FAIL B honesty for 221 vs 123 planFile lines (their script counted all 18:xx hour lines, not a fabricated 503).'
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
            description = 'Independent /health 200 nonce 5a05395663364549986a2f4092cbaf36 echoed; storage=reachable. /ready 200 storage Healthy. PID 57744 unchanged.'
            type = 'edit'
            status = 'completed'
            filePath = 'C:\ProgramData\McpServer\McpServer.Support.Mcp.exe'
        },
        @{
            order = 3
            description = 'Deserialized Claude failsafe: lastDrainError message=internal_server_error; canceled turn omits planFile and todoId'
            type = 'edit'
            status = 'completed'
            filePath = 'F:\GitHub\McpServer\.mcpServer\claude\failsafe\20260818T001252Z-session_submit-f830.yaml'
        },
        @{
            order = 4
            description = 'Share-read scan of mcp-20260817.log: prefix sessionlog 503 after 18:38=0; window ArgumentException planFile omitted=18; morning L53803 backend_unavailable on vice-sharp requirements_update'
            type = 'edit'
            status = 'completed'
            filePath = 'C:\ProgramData\McpServer\logs\mcp-20260817.log'
        },
        @{
            order = 5
            description = 'sessionlog_query returned implementer session GrokCode-20260817T120000Z-agent-help-grok-cli including completed turn 008 and this review session GrokCode-20260818T004041Z-hostile-slog-503'
            type = 'edit'
            status = 'completed'
            filePath = 'http://PAYTON-LEGION2:7147/mcp-transport'
        },
        @{
            order = 6
            description = 'Decision: OverallVerdict AGREE. Numbered claims re-verified. No product edits. Service not restarted. Supporting 221 planFile count is the 18:xx hour, not post-18:38.'
            type = 'design_decision'
            status = 'completed'
            filePath = 'F:\GitHub\McpServer\docs\receipts\sessionlog-backend-unavailable-20260818T003530Z.md'
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
        'Class 2 diagnosis. Consequence: Surface C and Byrd v4 are N/A; do not invent an FR/TR gap.',
        'AGREE the six numbered claims. Prefix HTTP status, not body substring, is the 503 test. Consequence: 201 lines that quote completed with 503 in payloads are not sessionlog 503 completions.',
        'Do not FAIL A5 for the 18:52 Unhealthy/Named Pipes blip. That is not a POST /mcpserver/sessionlog 503 and the numbered claim names the morning vice-sharp requirements_update as a real backend_unavailable.'
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
    queryTitle = 'Hostile validate session-log 503 diagnosis'
    queryText = 'Hostile validator: attack implementer claims about live session-log storage, Claude failsafe internal_server_error vs backend_unavailable, omitted planFile/todoId, and zero post-restart sessionlog 503 completions.'
    interpretation = 'Class 2 incident diagnosis review. Independently re-read failsafe, health/ready, mcp-20260817.log, and sessionlog_query.'
    response = 'OverallVerdict AGREE. Live storage reachable. Failsafe is internal_server_error with omitted planFile/todoId. Window ArgumentException confirmed. Zero prefix POST /mcpserver/sessionlog 503 since restart. Morning vice-sharp requirements_update is a real backend_unavailable. No product code change and no service restart.'
    status = 'completed'
    tags = @('hostile-validator', 'sessionlog', 'class-2', 'backend_unavailable')
    contextList = @(
        'F:\GitHub\McpServer\docs\receipts\sessionlog-backend-unavailable-20260818T003530Z.md',
        'F:\GitHub\McpServer\.mcpServer\claude\failsafe\20260818T001252Z-session_submit-f830.yaml',
        'C:\ProgramData\McpServer\logs\mcp-20260817.log',
        'F:\GitHub\McpServer\AGENTS-README-FIRST.yaml'
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

Write-Output '==== PROOF QUERY agent+from ===='
Invoke-McpTool -Name 'sessionlog_query' -Arguments @{
    workspacePath = $workspace
    agent = 'GrokCode'
    text = 'Hostile validate session-log 503 diagnosis'
    from = '2026-08-18T00:40:00Z'
    limit = 5
}
