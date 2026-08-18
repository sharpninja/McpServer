#Requires -Version 7.0
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$workspace = 'F:\GitHub\McpServer'
$baseUrl = 'http://PAYTON-LEGION2:7147'
$want = 'GrokCode-20260818T005258Z-hostile-grok-503'
$script:McpSessionHeader = $null
$script:McpId = 0

function Invoke-McpRpc {
    param(
        [Parameter(Mandatory)][string]$Method,
        $Params = $null
    )
    $script:McpId++
    $payload = [ordered]@{ jsonrpc = '2.0'; id = $script:McpId; method = $Method }
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
    $client = [System.Net.Http.HttpClient]::new()
    $client.Timeout = [TimeSpan]::FromSeconds(120)
    try {
        $resp = $client.Send($req)
        $sid = $null
        if ($resp.Headers.TryGetValues('Mcp-Session-Id', [ref]$sid)) {
            $script:McpSessionHeader = @($sid)[0]
        }
        $body = $resp.Content.ReadAsStringAsync().GetAwaiter().GetResult()
        Write-Host ('HTTP=' + [int]$resp.StatusCode)
        if ($body.Contains("`ndata:") -or $body.StartsWith('event:')) {
            $dataLines = [System.Collections.Generic.List[string]]::new()
            foreach ($line in ($body -split "`n")) {
                $trim = $line.TrimEnd("`r")
                if ($trim.StartsWith('data:')) { [void]$dataLines.Add($trim.Substring(5).Trim()) }
            }
            $body = ($dataLines -join "`n")
        }
        return $body
    } finally {
        $client.Dispose()
        $req.Dispose()
    }
}

[void](Invoke-McpRpc -Method 'initialize' -Params @{
    protocolVersion = '2025-03-26'
    capabilities = @{}
    clientInfo = @{ name = 'hostile-validator-grok-503-proof'; version = '1.0.0' }
})

$body = Invoke-McpRpc -Method 'tools/call' -Params @{
    name = 'sessionlog_query'
    arguments = @{
        workspacePath = $workspace
        agent = 'GrokCode'
        from = '2026-08-18T00:50:00Z'
        limit = 20
    }
}

$parsed = $body | ConvertFrom-Json
$text = $parsed.result.content[0].text
$doc = $text | ConvertFrom-Json
Write-Output ('QUERY_TOTALCOUNT=' + $doc.totalCount)
$hit = $null
foreach ($item in @($doc.items)) {
    Write-Output ('ITEM_SESSION=' + $item.sessionId + ' TURNS=' + $item.turnCount + ' STATUS=' + $item.status)
    if ($item.sessionId -eq $want) { $hit = $item }
}
if ($null -eq $hit) {
    Write-Output 'PROOF_HIT=MISSING'
    exit 2
}
$turn = @($hit.turns)[0]
Write-Output 'PROOF_HIT=FOUND'
Write-Output ('PROOF_SESSION=' + $hit.sessionId)
Write-Output ('PROOF_TITLE=' + $hit.title)
Write-Output ('PROOF_STATUS=' + $hit.status)
Write-Output ('PROOF_TURNCOUNT=' + $hit.turnCount)
Write-Output ('PROOF_REQ=' + $turn.requestId)
Write-Output ('PROOF_TURN_STATUS=' + $turn.status)
Write-Output ('PROOF_QTITLE=' + $turn.queryTitle)
Write-Output ('PROOF_PLANFILE=' + $turn.planFile)
Write-Output ('PROOF_TODOID=' + $turn.todoId)
Write-Output ('PROOF_ACTIONS=' + @($turn.actions).Count)
Write-Output ('PROOF_DIALOG=' + @($turn.processingDialog).Count)
Write-Output ('PROOF_TAGS=' + (($turn.tags) -join ','))
