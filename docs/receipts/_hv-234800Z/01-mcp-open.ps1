#Requires -Version 7.0
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$workspace = 'F:\GitHub\McpServer'
$baseUrl = 'http://PAYTON-LEGION2:7147'
$outDir = Join-Path $workspace 'docs\receipts\_hv-234800Z'
New-Item -ItemType Directory -Force -Path $outDir | Out-Null

$sessionId = 'GrokCode-20260818T234800Z-hostile-hgreen'
$requestId = 'req-20260818T234800Z-001-late-hgreen-s1s8'
$sessionHeaderPath = Join-Path $outDir 'mcp-session-header.txt'

$script:McpSessionHeader = $null
$script:McpId = 0
if (Test-Path -LiteralPath $sessionHeaderPath) {
    $script:McpSessionHeader = (Get-Content -LiteralPath $sessionHeaderPath -Raw).Trim()
}

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
    if ($outer.PSObject.Properties.Name -contains 'error' -and $null -ne $outer.error) {
        throw ('MCP RPC error: ' + ($outer.error | ConvertTo-Json -Compress -Depth 8))
    }
    $text = [string]$outer.result.content[0].text
    return ($text | ConvertFrom-Json)
}

$init = Invoke-McpRpc -Method 'initialize' -Params @{
    protocolVersion = '2025-03-26'
    capabilities = @{}
    clientInfo = @{ name = 'hostile-validator-hgreen-234800'; version = '1.0.0' }
}
Save-Body -Name 'mcp-init.json' -Result $init
[void](Invoke-McpRpc -Method 'notifications/initialized' -Params @{})

$listed = Invoke-McpRpc -Method 'tools/list' -Params @{}
Save-Body -Name 'mcp-tools-list.json' -Result $listed
$listedObj = $listed.Body | ConvertFrom-Json
$names = @($listedObj.result.tools | ForEach-Object { $_.name } | Sort-Object -Unique)
$names | Set-Content -LiteralPath (Join-Path $outDir 'tool-names.txt') -Encoding utf8
Write-Output ('TOOLS_UNIQUE=' + $names.Count)
Write-Output ('HAS_SESSIONLOG_OPEN=' + ($names -contains 'sessionlog_open'))
Write-Output ('HAS_SESSIONLOG_BEGIN=' + ($names -contains 'sessionlog_begin_turn'))
Write-Output ('HAS_TODO_GET=' + ($names -contains 'todo_get'))
Write-Output ('HAS_REQUIREMENTS_LIST=' + ($names -contains 'requirements_list'))

$open = Invoke-McpTool -Name 'sessionlog_open' -Arguments @{
    agent = 'GrokCode'
    sessionId = $sessionId
    workspacePath = $workspace
    title = 'Hostile H-green S1-S8 implementation review'
    model = 'grok'
    sourceType = 'GrokCode'
}
Save-Body -Name 'mcp-open.json' -Result $open
Write-Output ('OPEN=' + ((Get-ToolObject -Result $open) | ConvertTo-Json -Depth 6 -Compress))

$begin = Invoke-McpTool -Name 'sessionlog_begin_turn' -Arguments @{
    agent = 'GrokCode'
    sessionId = $sessionId
    requestId = $requestId
    workspacePath = $workspace
    planFile = 'docs/plans/triage-cluster-001.md'
    todoId = 'PLAN-TRIAGECLUSTER-001'
    queryTitle = 'Hostile late H-green S1-S8 implementation'
    queryText = 'Class 1 late H-green implementation-phase review for S1-S8. Attack shared four-field envelope, schema fail-closed, 5s budget, session store, plugin Pester/cache retain, prior EXEC/TR/HELP tests, H-red AGREE file, and scratch s2-tests.log. Do not mark TODOs done. Do not claim live deploy.'
}
Save-Body -Name 'mcp-begin.json' -Result $begin
Write-Output ('BEGIN=' + ((Get-ToolObject -Result $begin) | ConvertTo-Json -Depth 6 -Compress))

if ($script:McpSessionHeader) {
    Set-Content -LiteralPath $sessionHeaderPath -Value $script:McpSessionHeader -Encoding utf8
}
Write-Output ('MCP_SESSION_HEADER=' + $script:McpSessionHeader)
Write-Output 'MCP_OPEN_DONE'
