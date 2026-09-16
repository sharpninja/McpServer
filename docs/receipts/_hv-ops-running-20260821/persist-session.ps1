#Requires -Version 7.0
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$workspace = 'F:\GitHub\McpServer'
$baseUrl = 'http://PAYTON-LEGION2:7147'
$outDir = 'F:\GitHub\McpServer\docs\receipts\_hv-ops-running-20260821'
New-Item -ItemType Directory -Force -Path $outDir | Out-Null

$stamp = [datetime]::UtcNow.ToString('yyyyMMddTHHmmssZ')
$sessionId = "GrokCode-$stamp-hostile-mcp-running"
$requestId = "req-$stamp-hostile-mcp-running"
$ids = [ordered]@{
    stamp = $stamp
    sessionId = $sessionId
    requestId = $requestId
    workspacePath = $workspace
    agent = 'GrokCode'
    model = 'grok-4.20-0309-reasoning'
}
($ids | ConvertTo-Json -Depth 4) | Set-Content -LiteralPath (Join-Path $outDir 'ids.json') -Encoding utf8
Write-Output ("IDS sessionId=" + $sessionId + " requestId=" + $requestId)

$script:McpSessionHeader = $null
$script:McpId = 0

function Invoke-McpRpc {
    param(
        [Parameter(Mandatory)][string]$Method,
        $Params = $null
    )
    $script:McpId++
    $payload = [ordered]@{
        jsonrpc = '2.0'
        id = $script:McpId
        method = $Method
    }
    if ($null -ne $Params) { $payload['params'] = $Params }
    $json = $payload | ConvertTo-Json -Depth 20 -Compress
    $req = [System.Net.Http.HttpRequestMessage]::new([System.Net.Http.HttpMethod]::Post, "$baseUrl/mcp-transport")
    $req.Headers.Accept.Clear()
    [void]$req.Headers.Accept.Add([System.Net.Http.Headers.MediaTypeWithQualityHeaderValue]::new('application/json'))
    [void]$req.Headers.Accept.Add([System.Net.Http.Headers.MediaTypeWithQualityHeaderValue]::new('text/event-stream'))
    [void]$req.Headers.TryAddWithoutValidation('X-Workspace-Path', $workspace)
    if ($script:McpSessionHeader) {
        [void]$req.Headers.TryAddWithoutValidation('Mcp-Session-Id', $script:McpSessionHeader)
    }
    $req.Content = [System.Net.Http.StringContent]::new($json, [System.Text.Encoding]::UTF8, 'application/json')
    $handler = [System.Net.Http.HttpClientHandler]::new()
    $client = [System.Net.Http.HttpClient]::new($handler)
    $client.Timeout = [TimeSpan]::FromSeconds(180)
    try {
        $resp = $client.Send($req)
        $sid = $null
        if ($resp.Headers.TryGetValues('Mcp-Session-Id', [ref]$sid)) {
            $script:McpSessionHeader = @($sid)[0]
        }
        $body = $resp.Content.ReadAsStringAsync().GetAwaiter().GetResult()
        if ($body.StartsWith('event:') -or $body.Contains("`ndata:")) {
            $dataLines = [System.Collections.Generic.List[string]]::new()
            foreach ($line in ($body -split "`n")) {
                $trim = $line.TrimEnd("`r")
                if ($trim.StartsWith('data:')) {
                    [void]$dataLines.Add($trim.Substring(5).Trim())
                }
            }
            $body = [string]::Join("`n", $dataLines)
        }
        return [pscustomobject]@{ Status = [int]$resp.StatusCode; Body = $body }
    } finally {
        $client.Dispose()
        $req.Dispose()
    }
}

function Invoke-McpTool {
    param([string]$Name, [hashtable]$Arguments)
    Invoke-McpRpc -Method 'tools/call' -Params @{ name = $Name; arguments = $Arguments }
}

function Save-Body {
    param([string]$Name, $Result)
    $path = Join-Path $outDir $Name
    $Result.Body | Set-Content -LiteralPath $path -Encoding utf8
    Write-Output ("SAVED " + $Name + " HTTP=" + $Result.Status + " LEN=" + $Result.Body.Length)
}

$init = Invoke-McpRpc -Method 'initialize' -Params @{
    protocolVersion = '2025-03-26'
    capabilities = @{}
    clientInfo = @{ name = 'hostile-validator-ops-running'; version = '1.0.0' }
}
Save-Body -Name 'mcp-initialize.json' -Result $init
[void](Invoke-McpRpc -Method 'notifications/initialized' -Params @{})

$open = Invoke-McpTool -Name 'sessionlog_open' -Arguments @{
    agent = 'GrokCode'
    sessionId = $sessionId
    workspacePath = $workspace
    title = 'Hostile validate MCP Server running'
    model = 'grok-4.20-0309-reasoning'
}
Save-Body -Name 'sessionlog-open.json' -Result $open

$begin = Invoke-McpTool -Name 'sessionlog_begin_turn' -Arguments @{
    agent = 'GrokCode'
    sessionId = $sessionId
    requestId = $requestId
    workspacePath = $workspace
    planFile = 'None'
    todoId = 'None'
    queryTitle = 'Hostile validate MCP Server running'
    queryText = 'Hostile validator class-2 ops review: independently re-check implementer claims that MCP Server is running, marker signature True, health nonce echo Healthy, pid 16936 is McpServer.Support.Mcp, TCP 7147 Listen owned by 16936, McpServer service Running Automatic, no product change and no TODO marked done.'
}
Save-Body -Name 'sessionlog-begin.json' -Result $begin

$now = [datetime]::UtcNow.ToString('o')
$dialogItems = @(
    [ordered]@{
        timestamp = $now
        role = 'model'
        content = 'add-profile executed first. 18 non-skill profile markdown files read under C:\Users\kingd\.claude\profile. Excluded add-profile.grok.md.'
        category = 'observation'
    },
    [ordered]@{
        timestamp = $now
        role = 'model'
        content = 'Classified class 2 user-directed ops: Is MCP Server running. Surface C N/A. Surface D N/A. No plan-step done claim. Byrd v4 not applied to this ops action.'
        category = 'decision'
    },
    [ordered]@{
        timestamp = $now
        role = 'model'
        content = 'Independent live checks at 2026-08-21T14:26:35Z on PAYTON-LEGION2. Test-MarkerSignature True against F:\GitHub\McpServer\AGENTS-README-FIRST.yaml via grok plugin marker-resolver.ps1. Fresh nonce nonce-1991ee642fc9411b890c5d8c1a7a30b3 echoed exactly; health status Healthy; version 1.4.30+ee89cd63f6d16aa43d8e8dfac2388246c6ba39f8; storage reachable. Get-Process 16936 ProcessName=McpServer.Support.Mcp Responding=True. Get-NetTCPConnection LocalPort 7147 State Listen OwningProcess 16936. Get-Service McpServer Running Automatic. McpManagementService Stopped Manual. CIM PathName C:\ProgramData\McpServer\McpServer.Support.Mcp.exe --urls http://+:7147 ProcessId 16936.'
        category = 'observation'
    },
    [ordered]@{
        timestamp = $now
        role = 'model'
        content = 'Decision: score live running claims against this review''s fresh nonce and live process/port/service, not the implementer historical nonce nonce-13afca6083364986bdd4aa3b3d435014. Historical nonce is not replayable; operator required a fresh nonce. Consequence: A3 passes if current health echoes the new nonce with status Healthy. Alternatives rejected: FAIL A3 because the old nonce string is absent from the repo (operator ordered a fresh nonce, not a replay).'
        category = 'decision'
    }
)
$dialogJson = $dialogItems | ConvertTo-Json -Depth 8 -Compress
$dialog = Invoke-McpTool -Name 'sessionlog_dialog' -Arguments @{
    agent = 'GrokCode'
    sessionId = $sessionId
    requestId = $requestId
    itemsJson = $dialogJson
    workspacePath = $workspace
}
Save-Body -Name 'sessionlog-dialog.json' -Result $dialog

$actions = @(
    [ordered]@{ order = 1; description = 'add-profile: read 18 non-skill profile files'; type = 'edit'; status = 'completed'; filePath = 'C:\Users\kingd\.claude\profile' }
    [ordered]@{ order = 2; description = 'Test-MarkerSignature True via F:\GitHub\mcpserver-grok-plugin\lib\marker-resolver.ps1 against AGENTS-README-FIRST.yaml'; type = 'edit'; status = 'completed'; filePath = 'F:\GitHub\McpServer\AGENTS-README-FIRST.yaml' }
    [ordered]@{ order = 3; description = 'GET /health?nonce=nonce-1991ee642fc9411b890c5d8c1a7a30b3 echoed nonce; status Healthy'; type = 'edit'; status = 'completed'; filePath = 'F:\GitHub\McpServer\docs\receipts\_hv-ops-running-20260821\live-evidence.json' }
    [ordered]@{ order = 4; description = 'Get-Process 16936 ProcessName=McpServer.Support.Mcp; Get-NetTCPConnection 7147 Listen OwningProcess 16936; Get-Service McpServer Running Automatic'; type = 'edit'; status = 'completed'; filePath = 'F:\GitHub\McpServer\docs\receipts\_hv-ops-running-20260821\live-evidence.json' }
    [ordered]@{ order = 5; description = 'Class 2 ops review; C N/A; D N/A; no plan-step done; score live state not historical nonce replay'; type = 'design_decision'; status = 'completed'; filePath = '' }
)
$section = [ordered]@{ actions = $actions }
$sectionJson = $section | ConvertTo-Json -Depth 8 -Compress
$repl = Invoke-McpTool -Name 'sessionlog_replace_section' -Arguments @{
    agent = 'GrokCode'
    sessionId = $sessionId
    requestId = $requestId
    section = 'actions'
    sectionJson = $sectionJson
    workspacePath = $workspace
}
Save-Body -Name 'sessionlog-actions.json' -Result $repl

$decisions = [ordered]@{
    designDecisions = @(
        'Classified as class 2 user-directed ops (Is MCP Server running). Surface C N/A. Surface D N/A. Byrd v4 not applied.'
        'Validate A3 with a fresh health nonce, not replay of nonce-13afca6083364986bdd4aa3b3d435014.'
        'Use native sessionlog_* tools over /mcp-transport JSON-RPC; plugin XML tools were not in the subagent function list.'
    )
}
$decJson = $decisions | ConvertTo-Json -Depth 8 -Compress
$dec = Invoke-McpTool -Name 'sessionlog_replace_section' -Arguments @{
    agent = 'GrokCode'
    sessionId = $sessionId
    requestId = $requestId
    section = 'designDecisions'
    sectionJson = $decJson
    workspacePath = $workspace
}
Save-Body -Name 'sessionlog-decisions.json' -Result $dec

$todosDone = Invoke-McpTool -Name 'todo_list' -Arguments @{
    workspacePath = $workspace
    done = $true
}
Save-Body -Name 'todo-list-done.json' -Result $todosDone

$query = Invoke-McpTool -Name 'sessionlog_query' -Arguments @{
    workspacePath = $workspace
    agent = 'GrokCode'
    text = $sessionId
    limit = 5
}
Save-Body -Name 'sessionlog-query-after-begin.json' -Result $query

Write-Output 'PERSIST_PARTIAL_DONE'
