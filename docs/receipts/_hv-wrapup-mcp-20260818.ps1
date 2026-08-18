#Requires -Version 7.0
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$workspace = 'F:\GitHub\McpServer'
$baseUrl = 'http://PAYTON-LEGION2:7147'
$sessionId = (Get-Content -LiteralPath (Join-Path $workspace 'docs\receipts\_hv-wrapup-ids-20260818.txt') | Where-Object { $_ -like 'SESSION_ID=*' }).Substring(11)
$requestId = (Get-Content -LiteralPath (Join-Path $workspace 'docs\receipts\_hv-wrapup-ids-20260818.txt') | Where-Object { $_ -like 'REQUEST_ID=*' }).Substring(11)
$outDir = Join-Path $workspace 'docs\receipts'
$script:McpSessionHeader = $null
$script:McpId = 0
Write-Output ('SESSION_ID=' + $sessionId)
Write-Output ('REQUEST_ID=' + $requestId)

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
    }
    finally {
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
    Write-Output ('SAVED ' + $Name + ' HTTP=' + $Result.Status + ' LEN=' + $Result.Body.Length)
}

function Get-ToolObject {
    param($Result)
    $outer = $Result.Body | ConvertFrom-Json
    $text = [string]$outer.result.content[0].text
    return ($text | ConvertFrom-Json)
}

$init = Invoke-McpRpc -Method 'initialize' -Params @{
    protocolVersion = '2025-03-26'
    capabilities = @{}
    clientInfo = @{ name = 'hostile-validator-wrapup'; version = '1.0.0' }
}
Write-Output ('INIT_HTTP=' + $init.Status)
[void](Invoke-McpRpc -Method 'notifications/initialized' -Params @{})

$open = Invoke-McpTool -Name 'sessionlog_open' -Arguments @{
    agent = 'GrokCode'
    sessionId = $sessionId
    workspacePath = $workspace
    title = 'Hostile validation of refresh-docs wrap-up push'
    model = 'grok'
}
Save-Body -Name '_hv-wrapup-mcp-open.json' -Result $open
try { Write-Output ('OPEN=' + ((Get-ToolObject -Result $open) | ConvertTo-Json -Depth 6 -Compress)) } catch { Write-Output ('OPEN_RAW=' + $open.Body) }

$begin = Invoke-McpTool -Name 'sessionlog_begin_turn' -Arguments @{
    agent = 'GrokCode'
    sessionId = $sessionId
    requestId = $requestId
    workspacePath = $workspace
    planFile = 'None'
    todoId = 'None'
    queryTitle = 'Hostile validate wrap-up refresh-docs push'
    queryText = 'Adversarial review of wrap-up-20260818T183800Z claims.'
}
Save-Body -Name '_hv-wrapup-mcp-begin.json' -Result $begin
try { Write-Output ('BEGIN=' + ((Get-ToolObject -Result $begin) | ConvertTo-Json -Depth 8 -Compress)) } catch { Write-Output ('BEGIN_RAW=' + $begin.Body) }

foreach ($id in @('MCP-PRODUCTS-001', 'PLAN-LLMSTRATEGY-001', 'PLAN-SHARPMIND-001')) {
    $todo = Invoke-McpTool -Name 'todo_get' -Arguments @{
        id = $id
        workspacePath = $workspace
    }
    $safe = ($id -replace '[^A-Z0-9-]', '_')
    Save-Body -Name ('_hv-wrapup-todo-' + $safe + '.json') -Result $todo
    try {
        $obj = Get-ToolObject -Result $todo
        Write-Output ('TODO id=' + $obj.Id + ' Done=' + $obj.Done + ' Remaining=' + $obj.Remaining + ' CompletedDate=' + $obj.CompletedDate + ' Summary=' + $obj.DoneSummary)
    } catch {
        Write-Output ('TODO_RAW ' + $id + '=' + $todo.Body)
    }
}

# Implementer session query
$queryImpl = Invoke-McpTool -Name 'sessionlog_query' -Arguments @{
    workspacePath = $workspace
    agent = 'GrokCode'
    from = '2026-08-18T18:20:00Z'
    limit = 20
}
Save-Body -Name '_hv-wrapup-query-impl.json' -Result $queryImpl
try { Write-Output ('QUERY_IMPL=' + ((Get-ToolObject -Result $queryImpl) | ConvertTo-Json -Depth 10 -Compress)) } catch { Write-Output ('QUERY_IMPL_RAW=' + $queryImpl.Body) }

Write-Output ('MCP_SESSION_HEADER=' + $script:McpSessionHeader)
Write-Output 'MCP_QUERY_DONE'
