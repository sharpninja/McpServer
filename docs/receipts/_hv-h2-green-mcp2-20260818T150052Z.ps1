#Requires -Version 7.0
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$workspace = 'F:\GitHub\McpServer'
$baseUrl = 'http://PAYTON-LEGION2:7147'
$sessionId = 'GrokCode-20260818T150052Z-h2-green-products'
$requestId = 'req-20260818T150052Z-001-hostile-h2-green-products'
$outDir = 'F:\GitHub\McpServer\docs\receipts'
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
    clientInfo = @{ name = 'hostile-validator-h2-green-2'; version = '1.0.0' }
}
Save-Body -Name '_hv-h2-green-init.json' -Result $init
[void](Invoke-McpRpc -Method 'notifications/initialized' -Params @{})

$tools = Invoke-McpRpc -Method 'tools/list' -Params @{}
$toolsObj = $tools.Body | ConvertFrom-Json
$names = @($toolsObj.result.tools | ForEach-Object { $_.name } | Sort-Object -Unique)
Write-Output ("TOOLS_UNIQUE=" + $names.Count)
Write-Output ("HAS_SESSIONLOG_OPEN=" + ($names -contains 'sessionlog_open'))
Write-Output ("HAS_SESSIONLOG_QUERY=" + ($names -contains 'sessionlog_query'))
Write-Output ("HAS_TODO_GET=" + ($names -contains 'todo_get'))
Write-Output ("HAS_REQUIREMENTS_LIST=" + ($names -contains 'requirements_list'))

$open = Invoke-McpTool -Name 'sessionlog_open' -Arguments @{
    agent = 'GrokCode'
    sessionId = $sessionId
    workspacePath = $workspace
    title = 'Hostile H2-green products share review'
    model = 'grok'
}
Save-Body -Name '_hv-h2-green-open.json' -Result $open

$begin = Invoke-McpTool -Name 'sessionlog_begin_turn' -Arguments @{
    agent = 'GrokCode'
    sessionId = $sessionId
    requestId = $requestId
    workspacePath = $workspace
    queryTitle = 'Hostile H2-green products share review'
    queryText = 'Hostile validator H2-green: attack Phase 2 share implementation claims for MCP-PRODUCTS-001. Not claiming TODO done, Phase 3-5, or full unit suite.'
}
Save-Body -Name '_hv-h2-green-begin.json' -Result $begin

$todo = Invoke-McpTool -Name 'todo_get' -Arguments @{
    id = 'MCP-PRODUCTS-001'
    workspacePath = $workspace
}
Save-Body -Name '_hv-h2-green-todo.json' -Result $todo

foreach ($kind in @('fr','tr','test','mapping')) {
    $res = Invoke-McpTool -Name 'requirements_list' -Arguments @{
        workspacePath = $workspace
        type = $kind
    }
    Save-Body -Name ("_hv-h2-green-req-$kind.json") -Result $res
}

$query = Invoke-McpTool -Name 'sessionlog_query' -Arguments @{
    workspacePath = $workspace
    agent = 'GrokCode'
    text = 'h2-green-products'
    from = '2026-08-18T15:00:00Z'
    limit = 10
}
Save-Body -Name '_hv-h2-green-query-pre.json' -Result $query

Write-Output ("MCP_SESSION_HEADER=" + $script:McpSessionHeader)
Write-Output 'MCP2_DONE'
