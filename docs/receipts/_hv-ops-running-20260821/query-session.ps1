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
    clientInfo = @{ name = 'hostile-validator-ops-running-query'; version = '1.0.0' }
}
Save-Body -Name 'mcp-initialize-query.json' -Result $init
[void](Invoke-McpRpc -Method 'notifications/initialized' -Params @{})

$q1 = Invoke-McpTool -Name 'sessionlog_query' -Arguments @{
    workspacePath = $workspace
    agent = 'GrokCode'
    from = '2026-08-21T14:30:00Z'
    limit = 20
}
Save-Body -Name 'sessionlog-query-from.json' -Result $q1
$t1 = Get-ToolText $q1
Write-Output ("Q1_HAS_SESSION=" + $t1.Contains($sessionId))
Write-Output ("Q1_HEAD=" + $t1.Substring(0, [Math]::Min(500, $t1.Length)))

$q2 = Invoke-McpTool -Name 'sessionlog_query' -Arguments @{
    workspacePath = $workspace
    text = 'hostile-mcp-running'
    limit = 10
}
Save-Body -Name 'sessionlog-query-text-slug.json' -Result $q2
$t2 = Get-ToolText $q2
Write-Output ("Q2_HAS_SESSION=" + $t2.Contains($sessionId))
Write-Output ("Q2_HEAD=" + $t2.Substring(0, [Math]::Min(500, $t2.Length)))

$q3 = Invoke-McpTool -Name 'sessionlog_query' -Arguments @{
    workspacePath = $workspace
    agent = 'GrokCode'
    limit = 3
}
Save-Body -Name 'sessionlog-query-agent-limit3.json' -Result $q3
$t3 = Get-ToolText $q3
Write-Output ("Q3_HAS_SESSION=" + $t3.Contains($sessionId))
Write-Output ("Q3_HEAD=" + $t3.Substring(0, [Math]::Min(500, $t3.Length)))

Write-Output ("TARGET_SESSION=" + $sessionId)
Write-Output ("TARGET_REQUEST=" + $requestId)
