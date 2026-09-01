#Requires -Version 7.0
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$workspace = 'F:\GitHub\McpServer'
$baseUrl = 'http://PAYTON-LEGION2:7147'
$idsPath = 'F:\GitHub\McpServer\docs\receipts\_hv-h5-rerun-ids.txt'
$ids = Get-Content -LiteralPath $idsPath
$sessionId = (($ids | Where-Object { $_ -like 'SESSION_ID=*' }) -split '=', 2)[1]
$requestId = (($ids | Where-Object { $_ -like 'REQUEST_ID=*' }) -split '=', 2)[1]
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

# Parse already-saved mapping file if present; else re-fetch
$mapPath = Join-Path $outDir '_hv-h5-rerun-req-mapping.json'
if (Test-Path $mapPath) {
    $outer = Get-Content -LiteralPath $mapPath -Raw | ConvertFrom-Json
    $parsed = $outer.result.content[0].text | ConvertFrom-Json
} else {
    $init = Invoke-McpRpc -Method 'initialize' -Params @{
        protocolVersion = '2025-03-26'
        capabilities = @{}
        clientInfo = @{ name = 'hostile-validator-h5-rerun-1b-map'; version = '1.0.0' }
    }
    Write-Output ('MAP_INIT=' + $init.Status)
    [void](Invoke-McpRpc -Method 'notifications/initialized' -Params @{})
    $res = Invoke-McpTool -Name 'requirements_list' -Arguments @{
        workspacePath = $workspace
        type = 'mapping'
    }
    Save-Body -Name '_hv-h5-rerun-req-mapping.json' -Result $res
    $parsed = Get-ToolObject -Result $res
}

$items = @()
if ($parsed.PSObject.Properties.Name -contains 'items') { $items = @($parsed.items) }
elseif ($parsed.PSObject.Properties.Name -contains 'mappings') { $items = @($parsed.mappings) }
Write-Output ('MAPPING_TOTAL=' + $items.Count)
foreach ($item in $items) {
    $blob = ($item | ConvertTo-Json -Depth 8 -Compress)
    if ($blob -match 'PRODUCT') {
        Write-Output ('PRODUCT_MAPPING ' + $blob)
    }
}

# New transport session for open/begin
$script:McpSessionHeader = $null
$script:McpId = 0
$init2 = Invoke-McpRpc -Method 'initialize' -Params @{
    protocolVersion = '2025-03-26'
    capabilities = @{}
    clientInfo = @{ name = 'hostile-validator-h5-rerun-1b'; version = '1.0.0' }
}
Save-Body -Name '_hv-h5-rerun-init.json' -Result $init2
Write-Output ('INIT2_HTTP=' + $init2.Status)
[void](Invoke-McpRpc -Method 'notifications/initialized' -Params @{})

$open = Invoke-McpTool -Name 'sessionlog_open' -Arguments @{
    agent = 'GrokCode'
    sessionId = $sessionId
    workspacePath = $workspace
    title = 'Hostile H5-done rerun products review'
    model = 'grok'
}
Save-Body -Name '_hv-h5-rerun-open.json' -Result $open
Write-Output ('OPEN_TEXT=' + ((Get-ToolObject -Result $open) | ConvertTo-Json -Depth 6 -Compress))

$begin = Invoke-McpTool -Name 'sessionlog_begin_turn' -Arguments @{
    agent = 'GrokCode'
    sessionId = $sessionId
    requestId = $requestId
    workspacePath = $workspace
    planFile = 'docs/plans/mcp-products-001.md'
    todoId = 'MCP-PRODUCTS-001'
    queryTitle = 'Hostile H5-done rerun after handoff lock'
    queryText = 'Hostile validator H5-done rerun: attack MCP-PRODUCTS-001 done claim after prior DISAGREE 20260818T163120Z. Class 1. Do not mark TODO. Implementer claims TrackingTodoService.CreateAsync lock plus independent full Test 0 fail 0 skip and ValidateTraceability Succeeded. TODO stays Done=false until AGREE.'
}
Save-Body -Name '_hv-h5-rerun-begin.json' -Result $begin
Write-Output ('BEGIN_TEXT=' + ((Get-ToolObject -Result $begin) | ConvertTo-Json -Depth 8 -Compress))
Write-Output ('MCP_SESSION_HEADER=' + $script:McpSessionHeader)
Write-Output 'MCP1B_DONE'
