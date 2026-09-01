#Requires -Version 7.0
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$workspace = 'F:\GitHub\McpServer'
$baseUrl = 'http://PAYTON-LEGION2:7147'
$outDir = Join-Path $workspace 'docs\receipts\_hv-234800Z'
$sessionId = 'GrokCode-20260818T234800Z-hostile-hgreen'
$requestId = 'req-20260818T234800Z-001-late-hgreen-s1s8'
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

[void](Invoke-McpRpc -Method 'initialize' -Params @{
    protocolVersion = '2025-03-26'
    capabilities = @{}
    clientInfo = @{ name = 'hostile-validator-hgreen-234800-patch'; version = '1.0.0' }
})
[void](Invoke-McpRpc -Method 'notifications/initialized' -Params @{})

$turnJson = @{
    requestId = $requestId
    response = 'OverallVerdict AGREE. Receipt docs/receipts/hostile-validator-20260818T234800Z.md. PASS 21 FAIL 0 UNKNOWN 0 N/A 4. Late H-green for S1-S8 implementation. H-red 233800Z AGREE (PASS 27 FAIL 0) exists. Independent named filters all Failed 0 Skipped 0. Pester 9/0/0. Build ReplacePluginCache 2/0/0. Scratch s2-tests.log exists. All 16 BUG-TRIAGE ids remain Done=false. Live host remains 1.4.26. Do not mark TODOs done.'
    interpretation = 'Class 1 late H-green implementation-phase review for S1-S8. Do not mark TODOs done. Do not claim live deploy.'
    designDecisions = @(
        'AGREE this late H-green for S1-S8 implementation. H-red AGREE exists. Named filters green. Do not mark the 16 BUG-TRIAGE ids done.'
    )
} | ConvertTo-Json -Compress -Depth 6

$retry = Invoke-McpTool -Name 'sessionlog_complete_turn' -Arguments @{
    agent = 'GrokCode'
    sessionId = $sessionId
    requestId = $requestId
    workspacePath = $workspace
    turnJson = $turnJson
}
Save-Body -Name 'mcp-complete-retry.json' -Result $retry
Write-Output ('RETRY=' + ((Get-ToolObject -Result $retry) | ConvertTo-Json -Compress -Depth 8))

$query = Invoke-McpTool -Name 'sessionlog_query' -Arguments @{
    agent = 'GrokCode'
    workspacePath = $workspace
    todoId = 'PLAN-TRIAGECLUSTER-001'
    from = '2026-08-18T23:47:00Z'
    limit = 5
}
Save-Body -Name 'mcp-query2.json' -Result $query
$obj = Get-ToolObject -Result $query
$turn = $obj.items[0].turns[0]
Write-Output ('Q2_STATUS=' + $turn.status)
Write-Output ('Q2_RESPONSE_NULL=' + ($null -eq $turn.response))
if ($turn.response) {
    Write-Output ('Q2_RESPONSE_LEN=' + $turn.response.Length)
    Write-Output ('Q2_HAS_AGREE=' + $turn.response.Contains('OverallVerdict AGREE'))
}
Write-Output ('Q2_DD=' + @($turn.designDecisions).Count)
Write-Output ('Q2_ACTIONS=' + @($turn.actions).Count)
Write-Output ('Q2_DIALOG=' + @($turn.processingDialog).Count)
Write-Output 'PATCH_DONE'
