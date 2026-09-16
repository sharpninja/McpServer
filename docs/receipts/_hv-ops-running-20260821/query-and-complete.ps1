#Requires -Version 7.0
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$workspace = 'F:\GitHub\McpServer'
$baseUrl = 'http://PAYTON-LEGION2:7147'
$outDir = 'F:\GitHub\McpServer\docs\receipts\_hv-ops-running-20260821'
$ids = Get-Content -LiteralPath (Join-Path $outDir 'ids.json') -Raw | ConvertFrom-Json
$sessionId = [string]$ids.sessionId
$requestId = [string]$ids.requestId

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

function Get-ToolText {
    param($Result)
    $outer = $Result.Body | ConvertFrom-Json
    return [string]$outer.result.content[0].text
}

$init = Invoke-McpRpc -Method 'initialize' -Params @{
    protocolVersion = '2025-03-26'
    capabilities = @{}
    clientInfo = @{ name = 'hostile-validator-ops-running-2'; version = '1.0.0' }
}
Save-Body -Name 'mcp-initialize-2.json' -Result $init
[void](Invoke-McpRpc -Method 'notifications/initialized' -Params @{})

$q1 = Invoke-McpTool -Name 'sessionlog_query' -Arguments @{
    workspacePath = $workspace
    agent = 'GrokCode'
    from = '2026-08-21T14:30:00Z'
    limit = 20
}
Save-Body -Name 'sessionlog-query-from.json' -Result $q1
Write-Output ("Q1_TEXT=" + (Get-ToolText $q1).Substring(0, [Math]::Min(400, (Get-ToolText $q1).Length)))

$q2 = Invoke-McpTool -Name 'sessionlog_query' -Arguments @{
    workspacePath = $workspace
    text = 'hostile-mcp-running'
    limit = 10
}
Save-Body -Name 'sessionlog-query-text-slug.json' -Result $q2
Write-Output ("Q2_TEXT=" + (Get-ToolText $q2).Substring(0, [Math]::Min(400, (Get-ToolText $q2).Length)))

$q3 = Invoke-McpTool -Name 'sessionlog_query' -Arguments @{
    workspacePath = $workspace
    agent = 'GrokCode'
    limit = 5
}
Save-Body -Name 'sessionlog-query-agent-limit5.json' -Result $q3
Write-Output ("Q3_TEXT=" + (Get-ToolText $q3).Substring(0, [Math]::Min(500, (Get-ToolText $q3).Length)))

$turnJsonObj = [ordered]@{
    requestId = $requestId
    status = 'completed'
    queryTitle = 'Hostile validate MCP Server running'
    interpretation = 'Class 2 ops review of live MCP Server running claims. Surfaces C and D N/A. Re-verify health with a fresh nonce, process 16936, TCP 7147, McpServer service, marker signature, git porcelain, and TODO done list.'
    response = 'OverallVerdict pending receipt write. Live checks: Test-MarkerSignature True; health nonce-1991ee642fc9411b890c5d8c1a7a30b3 echoed Healthy; pid 16936 ProcessName McpServer.Support.Mcp; TCP 7147 Listen OwningProcess 16936; McpServer Running Automatic; McpManagementService Stopped Manual; git porcelain has no src product changes; todo_list done=true count 262 with 0 CompletedDate after 2026-08-21T14:00:00Z.'
    planFile = 'None'
    todoId = 'None'
    tags = @('hostile-validator', 'class-2', 'ops', 'mcp-running')
    filesModified = @(
        'docs/receipts/_hv-ops-running-20260821/live-evidence.json'
        'docs/receipts/_hv-ops-running-20260821/persist-session.ps1'
        'docs/receipts/_hv-ops-running-20260821/query-and-complete.ps1'
    )
}
$turnJson = $turnJsonObj | ConvertTo-Json -Depth 8 -Compress
$complete = Invoke-McpTool -Name 'sessionlog_complete_turn' -Arguments @{
    agent = 'GrokCode'
    sessionId = $sessionId
    requestId = $requestId
    workspacePath = $workspace
    turnJson = $turnJson
}
Save-Body -Name 'sessionlog-complete.json' -Result $complete
Write-Output ("COMPLETE_TEXT=" + (Get-ToolText $complete))

$q4 = Invoke-McpTool -Name 'sessionlog_query' -Arguments @{
    workspacePath = $workspace
    agent = 'GrokCode'
    text = $sessionId
    limit = 5
}
Save-Body -Name 'sessionlog-query-after-complete.json' -Result $q4
Write-Output ("Q4_TEXT=" + (Get-ToolText $q4).Substring(0, [Math]::Min(800, (Get-ToolText $q4).Length)))

$q5 = Invoke-McpTool -Name 'sessionlog_query' -Arguments @{
    workspacePath = $workspace
    from = '2026-08-21T14:30:00Z'
    limit = 20
}
Save-Body -Name 'sessionlog-query-from-nocagent.json' -Result $q5
$q5text = Get-ToolText $q5
Write-Output ("Q5_LEN=" + $q5text.Length)
Write-Output ("Q5_HEAD=" + $q5text.Substring(0, [Math]::Min(500, $q5text.Length)))
Write-Output ("Q5_HAS_SESSION=" + $q5text.Contains($sessionId))
Write-Output ("Q5_HAS_REQUEST=" + $q5text.Contains($requestId))
