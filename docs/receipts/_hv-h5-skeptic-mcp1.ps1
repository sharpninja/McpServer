#Requires -Version 7.0
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$workspace = 'F:\GitHub\McpServer'
$baseUrl = 'http://PAYTON-LEGION2:7147'
$sessionId = 'GrokCode-20260818T173615Z-h5-skeptic-rerun'
$requestId = 'req-20260818T173615Z-001-hostile-h5-skeptic'
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
    clientInfo = @{ name = 'hostile-validator-h5-skeptic-1'; version = '1.0.0' }
}
Save-Body -Name '_hv-h5-skeptic-init.json' -Result $init
Write-Output ('INIT_HTTP=' + $init.Status)
[void](Invoke-McpRpc -Method 'notifications/initialized' -Params @{})

$listed = Invoke-McpRpc -Method 'tools/list' -Params @{}
$listedObj = $listed.Body | ConvertFrom-Json
$names = @($listedObj.result.tools | ForEach-Object { $_.name } | Sort-Object -Unique)
Write-Output ('TOOLS_UNIQUE=' + $names.Count)
Write-Output ('HAS_SESSIONLOG_OPEN=' + ($names -contains 'sessionlog_open'))
Write-Output ('HAS_SESSIONLOG_BEGIN=' + ($names -contains 'sessionlog_begin_turn'))
Write-Output ('HAS_SESSIONLOG_DIALOG=' + ($names -contains 'sessionlog_dialog'))
Write-Output ('HAS_SESSIONLOG_COMPLETE=' + ($names -contains 'sessionlog_complete_turn'))
Write-Output ('HAS_SESSIONLOG_QUERY=' + ($names -contains 'sessionlog_query'))
Write-Output ('HAS_TODO_GET=' + ($names -contains 'todo_get'))
Write-Output ('HAS_REQUIREMENTS_LIST=' + ($names -contains 'requirements_list'))
Write-Output ('HAS_REQUIREMENTS_EFFECTIVE=' + ($names -contains 'requirements_effective'))
$productTools = @($names | Where-Object { $_ -like 'product_*' })
Write-Output ('PRODUCT_TOOLS_HTTP_COUNT=' + $productTools.Count)
$names | Set-Content -LiteralPath (Join-Path $outDir '_hv-h5-skeptic-tool-names.txt') -Encoding utf8

$todo = Invoke-McpTool -Name 'todo_get' -Arguments @{
    id = 'MCP-PRODUCTS-001'
    workspacePath = $workspace
}
Save-Body -Name '_hv-h5-skeptic-todo.json' -Result $todo
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
    Save-Body -Name ('_hv-h5-skeptic-req-' + $kind + '.json') -Result $res
    $parsed = Get-ToolObject -Result $res
    $items = Get-Items -Parsed $parsed
    Write-Output (($kind.ToUpper() + '_TOTAL=' + $items.Count))
    foreach ($item in $items) {
        $id = ''
        if ($item.Id) { $id = [string]$item.Id }
        elseif ($item.id) { $id = [string]$item.id }
        elseif ($item.FrId) { $id = [string]$item.FrId }
        $blob = ($item | ConvertTo-Json -Depth 12 -Compress)
        if ($id -match 'PRODUCT' -or $blob -match 'PRODUCT') {
            $clip = $blob
            if ($clip.Length -gt 2200) { $clip = $clip.Substring(0, 2200) }
            Write-Output ('PRODUCT_' + $kind.ToUpper() + ' ID=' + $id + ' ' + $clip)
        }
    }
}

$open = Invoke-McpTool -Name 'sessionlog_open' -Arguments @{
    agent = 'GrokCode'
    sessionId = $sessionId
    workspacePath = $workspace
    title = 'Hostile H5 skeptic rerun MCP-PRODUCTS-001'
    model = 'grok'
}
Save-Body -Name '_hv-h5-skeptic-open.json' -Result $open
Write-Output ('OPEN_TEXT=' + ((Get-ToolObject -Result $open) | ConvertTo-Json -Depth 6 -Compress))

$begin = Invoke-McpTool -Name 'sessionlog_begin_turn' -Arguments @{
    agent = 'GrokCode'
    sessionId = $sessionId
    requestId = $requestId
    workspacePath = $workspace
    planFile = 'docs/plans/mcp-products-001.md'
    todoId = 'MCP-PRODUCTS-001'
    queryTitle = 'Hostile H5 skeptic rerun MCP-PRODUCTS-001'
    queryText = 'Hostile validator H5-done skeptic rerun after skeptic rejected a prior done claim. Attack A1-A8 plus S1-S5. Class 1. Do not mark MCP-PRODUCTS-001 done.'
}
Save-Body -Name '_hv-h5-skeptic-begin.json' -Result $begin
Write-Output ('BEGIN_TEXT=' + ((Get-ToolObject -Result $begin) | ConvertTo-Json -Depth 8 -Compress))
Write-Output ('MCP_SESSION_HEADER=' + $script:McpSessionHeader)
Write-Output 'MCP1_DONE'
