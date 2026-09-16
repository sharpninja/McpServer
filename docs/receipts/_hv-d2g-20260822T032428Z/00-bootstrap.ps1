#Requires -Version 7.0
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$workspace = 'F:\GitHub\McpServer'
$outDir = 'F:\GitHub\McpServer\docs\receipts\_hv-d2g-20260822T032428Z'
$baseUrl = 'http://PAYTON-LEGION2:7147'
$plugin = 'F:\GitHub\mcpserver-grok-plugin'
$ids = Get-Content -LiteralPath (Join-Path $outDir 'ids.json') -Raw | ConvertFrom-Json
$agent = [string]$ids.agent
$sessionId = [string]$ids.sessionId
$requestId = [string]$ids.requestId

function Save-Text {
    param([string]$Name, [string]$Text)
    $path = Join-Path $outDir $Name
    $Text | Set-Content -LiteralPath $path -Encoding utf8
    Write-Output ('SAVED ' + $Name + ' LEN=' + $Text.Length)
}

. (Join-Path $workspace 'plugins\core\lib-ps\marker-resolver.ps1')
$marker = Join-Path $workspace 'AGENTS-README-FIRST.yaml'
$sig = Test-MarkerSignature -MarkerFile $marker
$sigObj = [ordered]@{ marker = $marker; valid = [bool]$sig; utc = $ids.utc }
$sigObj | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $outDir 'marker-sig.json') -Encoding utf8
Write-Output ('MARKER_SIG=' + $sig)

$nonce = [guid]::NewGuid().ToString('N')
$healthUrl = $baseUrl + '/health?nonce=' + $nonce
$health = Invoke-RestMethod -Uri $healthUrl -Method Get
$healthObj = [ordered]@{
    url = $healthUrl
    nonceSent = $nonce
    nonceEchoed = [string]$health.nonce
    nonceMatch = ([string]$health.nonce -eq $nonce)
    status = [string]$health.status
    storage = $health.storage
    version = [string]$health.version
}
$healthObj | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath (Join-Path $outDir 'health-nonce.json') -Encoding utf8
Write-Output ('HEALTH_STATUS=' + $health.status + ' NONCE_MATCH=' + $healthObj.nonceMatch)

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
    $json = $payload | ConvertTo-Json -Depth 30 -Compress
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
        return [pscustomobject]@{ Status = [int]$resp.StatusCode; Body = $body; Session = $script:McpSessionHeader }
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
    Write-Output ('SAVED ' + $Name + ' HTTP=' + $Result.Status + ' LEN=' + $Result.Body.Length)
}

Write-Output 'MCP_INIT'
$init = Invoke-McpRpc -Method 'initialize' -Params @{
    protocolVersion = '2025-03-26'
    capabilities = @{}
    clientInfo = @{ name = 'GrokSubagentHostile-d2g'; version = '1.0.0' }
}
Save-Body -Name 'mcp-init.json' -Result $init
[void](Invoke-McpRpc -Method 'notifications/initialized' -Params @{})
$script:McpSessionHeader | Set-Content -LiteralPath (Join-Path $outDir 'mcp-session-id.txt') -Encoding utf8

$open = Invoke-McpTool -Name 'sessionlog_open' -Arguments @{
    agent = $agent
    sessionId = $sessionId
    workspacePath = $workspace
    title = 'Hostile D2-green PLAN-PLUGINHANDOFF-001'
    model = 'grok-4-1-fast'
}
Save-Body -Name 'session-open.json' -Result $open

$begin = Invoke-McpTool -Name 'sessionlog_begin_turn' -Arguments @{
    agent = $agent
    sessionId = $sessionId
    requestId = $requestId
    workspacePath = $workspace
    planFile = 'docs/plans/PLAN-PLUGINHANDOFF-001.md'
    todoId = 'PLAN-PLUGINHANDOFF-001'
    queryTitle = 'Hostile D2-green remaining-gap tests PLAN-PLUGINHANDOFF-001'
    queryText = 'WorkClass 1 D2-green after D1.5 AGREE. Re-run remaining-gap tests. Do not mark PLAN or MCP-HANDOFF done.'
}
Save-Body -Name 'session-begin.json' -Result $begin

foreach ($todoId in @('PLAN-PLUGINHANDOFF-001', 'MCP-HANDOFF-001', 'MCP-HANDOFFPLAN-001', 'MCP-HANDOFFREVIEW-001')) {
    $tg = Invoke-McpTool -Name 'todo_get' -Arguments @{
        id = $todoId
        workspacePath = $workspace
    }
    Save-Body -Name ('todo-' + $todoId + '.json') -Result $tg
}

foreach ($reqType in @('fr', 'tr', 'test', 'mapping')) {
    $rl = Invoke-McpTool -Name 'requirements_list' -Arguments @{
        workspacePath = $workspace
        type = $reqType
    }
    Save-Body -Name ('req-' + $reqType + '.json') -Result $rl
}

Write-Output 'BOOTSTRAP_DONE'
Write-Output ('MCP_SESSION=' + $script:McpSessionHeader)
