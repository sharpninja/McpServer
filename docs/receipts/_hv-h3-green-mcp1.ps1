#Requires -Version 7.0
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$workspace = 'F:\GitHub\McpServer'
$baseUrl = 'http://PAYTON-LEGION2:7147'
$ids = Get-Content -LiteralPath 'F:\GitHub\McpServer\docs\receipts\_hv-h3-green-ids.txt'
$sessionId = (($ids | Where-Object { $_ -like 'SESSION_ID=*' }) -split '=', 2)[1]
$requestId = (($ids | Where-Object { $_ -like 'REQUEST_ID=*' }) -split '=', 2)[1]
$outDir = 'F:\GitHub\McpServer\docs\receipts'
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
    clientInfo = @{ name = 'hostile-validator-h3-green-1'; version = '1.0.0' }
}
[void](Invoke-McpRpc -Method 'notifications/initialized' -Params @{})
Write-Output ('INIT_HTTP=' + $init.Status)

$listed = Invoke-McpRpc -Method 'tools/list' -Params @{}
$listedObj = $listed.Body | ConvertFrom-Json
$names = @($listedObj.result.tools | ForEach-Object { $_.name } | Sort-Object -Unique)
Write-Output ('TOOLS_UNIQUE=' + $names.Count)
Write-Output ('HAS_SESSIONLOG_OPEN=' + ($names -contains 'sessionlog_open'))
Write-Output ('HAS_TODO_GET=' + ($names -contains 'todo_get'))
Write-Output ('HAS_REQUIREMENTS_LIST=' + ($names -contains 'requirements_list'))
Write-Output ('HAS_SESSIONLOG_QUERY=' + ($names -contains 'sessionlog_query'))
$names | Set-Content -LiteralPath (Join-Path $outDir '_hv-h3-green-tool-names.txt') -Encoding utf8

$todo = Invoke-McpTool -Name 'todo_get' -Arguments @{
    id = 'MCP-PRODUCTS-001'
    workspacePath = $workspace
}
Save-Body -Name '_hv-h3-green-todo.json' -Result $todo
$todoObj = Get-ToolObject -Result $todo
Write-Output ('TODO_ID=' + $todoObj.id)
Write-Output ('TODO_DONE=' + $todoObj.done)
Write-Output ('TODO_COMPLETED=' + $todoObj.completedDate)
Write-Output ('TODO_SUMMARY=' + $todoObj.doneSummary)
if ($todoObj.implementationTasks) {
    foreach ($task in $todoObj.implementationTasks) {
        Write-Output ('TASK ' + $task.id + ' Done=' + $task.done + ' ' + $task.title)
    }
}

foreach ($kind in @('fr', 'tr', 'test', 'mapping')) {
    $res = Invoke-McpTool -Name 'requirements_list' -Arguments @{
        workspacePath = $workspace
        type = $kind
    }
    Save-Body -Name ('_hv-h3-green-req-' + $kind + '.json') -Result $res
    $parsed = Get-ToolObject -Result $res
    $items = @()
    foreach ($name in @('items', 'Items', 'requirements', 'Requirements', 'mappings', 'Mappings')) {
        if ($parsed.PSObject.Properties.Name -contains $name -and $null -ne $parsed.$name) {
            $items = @($parsed.$name)
            break
        }
    }
    if ($items.Count -eq 0 -and $parsed -is [System.Array]) { $items = @($parsed) }
    Write-Output ('KIND=' + $kind + ' RAW_KEYS=' + ($parsed.PSObject.Properties.Name -join ','))
    Write-Output (($kind.ToUpper() + '_TOTAL=' + $items.Count))
    foreach ($item in $items) {
        $blob = ($item | ConvertTo-Json -Depth 10 -Compress)
        if ($blob -match 'PRODUCT') {
            $clip = $blob
            if ($clip.Length -gt 800) { $clip = $clip.Substring(0, 800) }
            Write-Output ('PRODUCT_' + $kind.ToUpper() + ' ' + $clip)
        }
    }
}

$open = Invoke-McpTool -Name 'sessionlog_open' -Arguments @{
    agent = 'GrokCode'
    sessionId = $sessionId
    workspacePath = $workspace
    title = 'Hostile H3-green products adapter review'
    model = 'grok'
}
Save-Body -Name '_hv-h3-green-open.json' -Result $open
Write-Output ('OPEN_TEXT=' + ((Get-ToolObject -Result $open) | ConvertTo-Json -Depth 6 -Compress))

$begin = Invoke-McpTool -Name 'sessionlog_begin_turn' -Arguments @{
    agent = 'GrokCode'
    sessionId = $sessionId
    requestId = $requestId
    workspacePath = $workspace
    planFile = 'docs/plans/mcp-products-001.md'
    todoId = 'MCP-PRODUCTS-001'
    queryTitle = 'Hostile H3-green products adapter review'
    queryText = 'Hostile validator H3-green: attack Phase 3 adapters implemented for MCP-PRODUCTS-001. Not claiming TODO done, Phase 4-5, or full unit suite.'
}
Save-Body -Name '_hv-h3-green-begin.json' -Result $begin
Write-Output ('BEGIN_TEXT=' + ((Get-ToolObject -Result $begin) | ConvertTo-Json -Depth 6 -Compress))

Write-Output ('MCP_SESSION_HEADER=' + $script:McpSessionHeader)
Write-Output 'MCP1_DONE'
