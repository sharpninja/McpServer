#Requires -Version 7.0
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$workspace = 'F:\GitHub\McpServer'
$baseUrl = 'http://PAYTON-LEGION2:7147'
$ids = Get-Content -LiteralPath 'F:\GitHub\McpServer\docs\receipts\_hv-h4-red-ids.txt'
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

function Get-Items {
    param($Parsed)
    foreach ($name in @('items', 'Items', 'requirements', 'Requirements', 'mappings', 'Mappings')) {
        if ($Parsed.PSObject.Properties.Name -contains $name -and $null -ne $Parsed.$name) {
            return @($Parsed.$name)
        }
    }
    if ($Parsed -is [System.Array]) { return @($Parsed) }
    return @()
}

$init = Invoke-McpRpc -Method 'initialize' -Params @{
    protocolVersion = '2025-03-26'
    capabilities = @{}
    clientInfo = @{ name = 'hostile-validator-h4-red-2'; version = '1.0.0' }
}
Save-Body -Name '_hv-h4-red-init.json' -Result $init
Write-Output ('INIT_HTTP=' + $init.Status)
[void](Invoke-McpRpc -Method 'notifications/initialized' -Params @{})

$todo = Invoke-McpTool -Name 'todo_get' -Arguments @{
    id = 'MCP-PRODUCTS-001'
    workspacePath = $workspace
}
Save-Body -Name '_hv-h4-red-todo.json' -Result $todo
$todoObj = Get-ToolObject -Result $todo
Write-Output ('TODO_ID=' + $todoObj.Id)
Write-Output ('TODO_DONE=' + $todoObj.Done)
Write-Output ('TODO_COMPLETED=' + $todoObj.CompletedDate)
Write-Output ('TODO_SUMMARY=' + $todoObj.DoneSummary)
Write-Output ('TODO_REMAINING=' + $todoObj.Remaining)
$tasks = @()
if ($todoObj.PSObject.Properties.Name -contains 'ImplementationTasks' -and $null -ne $todoObj.ImplementationTasks) {
    $tasks = @($todoObj.ImplementationTasks)
}
Write-Output ('TASK_COUNT=' + $tasks.Count)
foreach ($task in $tasks) {
    $title = ''
    if ($task.PSObject.Properties.Name -contains 'Task') { $title = [string]$task.Task }
    elseif ($task.PSObject.Properties.Name -contains 'title') { $title = [string]$task.title }
    $done = $null
    if ($task.PSObject.Properties.Name -contains 'Done') { $done = $task.Done }
    elseif ($task.PSObject.Properties.Name -contains 'done') { $done = $task.done }
    Write-Output ('TASK Done=' + $done + ' ' + $title)
}

foreach ($kind in @('fr', 'tr', 'test', 'mapping')) {
    $res = Invoke-McpTool -Name 'requirements_list' -Arguments @{
        workspacePath = $workspace
        type = $kind
    }
    Save-Body -Name ('_hv-h4-red-req-' + $kind + '.json') -Result $res
    $parsed = Get-ToolObject -Result $res
    $items = Get-Items -Parsed $parsed
    Write-Output ('KIND=' + $kind + ' RAW_KEYS=' + ($parsed.PSObject.Properties.Name -join ','))
    Write-Output (($kind.ToUpper() + '_TOTAL=' + $items.Count))
    foreach ($item in $items) {
        $blob = ($item | ConvertTo-Json -Depth 12 -Compress)
        if ($blob -match 'PRODUCT') {
            $clip = $blob
            if ($clip.Length -gt 1200) { $clip = $clip.Substring(0, 1200) }
            Write-Output ('PRODUCT_' + $kind.ToUpper() + ' ' + $clip)
        }
    }
}

$open = Invoke-McpTool -Name 'sessionlog_open' -Arguments @{
    agent = 'GrokCode'
    sessionId = $sessionId
    workspacePath = $workspace
    title = 'Hostile H4-red products context review'
    model = 'grok'
}
Save-Body -Name '_hv-h4-red-open.json' -Result $open
Write-Output ('OPEN_TEXT=' + ((Get-ToolObject -Result $open) | ConvertTo-Json -Depth 6 -Compress))

$begin = Invoke-McpTool -Name 'sessionlog_begin_turn' -Arguments @{
    agent = 'GrokCode'
    sessionId = $sessionId
    requestId = $requestId
    workspacePath = $workspace
    planFile = 'docs/plans/mcp-products-001.md'
    todoId = 'MCP-PRODUCTS-001'
    queryTitle = 'Hostile H4-red products context review'
    queryText = 'Hostile validator H4-red: Phase 4 context tests exist and fail for the right reason. Not claiming Phase 4 green. Not claiming MCP-PRODUCTS-001 done.'
}
Save-Body -Name '_hv-h4-red-begin.json' -Result $begin
Write-Output ('BEGIN_TEXT=' + ((Get-ToolObject -Result $begin) | ConvertTo-Json -Depth 8 -Compress))
Write-Output ('MCP_SESSION_HEADER=' + $script:McpSessionHeader)
Write-Output 'MCP2_DONE'
