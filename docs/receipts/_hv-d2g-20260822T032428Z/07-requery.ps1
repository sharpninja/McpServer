#Requires -Version 7.0
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$workspace = 'F:\GitHub\McpServer'
$outDir = 'F:\GitHub\McpServer\docs\receipts\_hv-d2g-20260822T032428Z'
$baseUrl = 'http://PAYTON-LEGION2:7147'

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

[void](Invoke-McpRpc -Method 'initialize' -Params @{
    protocolVersion = '2025-03-26'
    capabilities = @{}
    clientInfo = @{ name = 'GrokSubagentHostile-d2g'; version = '1.0.0' }
})
[void](Invoke-McpRpc -Method 'notifications/initialized' -Params @{})

$q1 = Invoke-McpTool -Name 'sessionlog_query' -Arguments @{
    workspacePath = $workspace
    agent = 'GrokSubagentHostile'
    from = '2026-08-22T03:20:00Z'
    limit = 10
}
$q1.Body | Set-Content -LiteralPath (Join-Path $outDir 'session-query-agent.json') -Encoding utf8
Write-Output ('Q1_LEN=' + $q1.Body.Length)

$q2 = Invoke-McpTool -Name 'sessionlog_query' -Arguments @{
    workspacePath = $workspace
    todoId = 'PLAN-PLUGINHANDOFF-001'
    from = '2026-08-22T03:20:00Z'
    limit = 10
}
$q2.Body | Set-Content -LiteralPath (Join-Path $outDir 'session-query-todo.json') -Encoding utf8
Write-Output ('Q2_LEN=' + $q2.Body.Length)

$q3 = Invoke-McpTool -Name 'sessionlog_query' -Arguments @{
    workspacePath = $workspace
    text = 'pluginhandoff-d2g'
    from = '2026-08-22T03:20:00Z'
    limit = 10
}
$q3.Body | Set-Content -LiteralPath (Join-Path $outDir 'session-query-text.json') -Encoding utf8
Write-Output ('Q3_LEN=' + $q3.Body.Length)
