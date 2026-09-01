#Requires -Version 7.0
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$workspace = 'F:\GitHub\McpServer'
$baseUrl = 'http://PAYTON-LEGION2:7147'
$script:McpSessionHeader = $null
$script:McpId = 0

function Invoke-McpRpc {
    param([Parameter(Mandatory)][string]$Method, $Params = $null)
    $script:McpId++
    $payload = [ordered]@{ jsonrpc = '2.0'; id = $script:McpId; method = $Method }
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
    $client = [System.Net.Http.HttpClient]::new()
    $client.Timeout = [TimeSpan]::FromSeconds(60)
    try {
        $resp = $client.Send($req)
        $sid = $null
        if ($resp.Headers.TryGetValues('Mcp-Session-Id', [ref]$sid)) { $script:McpSessionHeader = @($sid)[0] }
        $body = $resp.Content.ReadAsStringAsync().GetAwaiter().GetResult()
        if ($body.Contains("`ndata:") -or $body.StartsWith('event:')) {
            $dataLines = @()
            foreach ($line in ($body -split "`n")) {
                $trim = $line.TrimEnd("`r")
                if ($trim.StartsWith('data:')) { $dataLines += $trim.Substring(5).Trim() }
            }
            $body = ($dataLines -join "`n")
        }
        $first = ($body -split "`n")[0]
        return $first
    } finally {
        $client.Dispose()
        $req.Dispose()
    }
}

[void](Invoke-McpRpc -Method 'initialize' -Params @{
    protocolVersion = '2025-03-26'
    capabilities = @{}
    clientInfo = @{ name = 'hostile-query'; version = '1.0.0' }
})

function Show-Query {
    param([string]$Label, [hashtable]$Arguments)
    Write-Output ("==== {0} ====" -f $Label)
    $raw = Invoke-McpRpc -Method 'tools/call' -Params @{
        name = 'sessionlog_query'
        arguments = $Arguments
    }
    $rpc = $raw | ConvertFrom-Json
    $inner = $rpc.result.content[0].text | ConvertFrom-Json
    Write-Output ("totalCount=" + $inner.totalCount)
    if ($inner.items) {
        foreach ($item in @($inner.items)) {
            Write-Output ("sessionId=" + $item.sessionId)
            Write-Output ("sourceType=" + $item.sourceType)
            Write-Output ("title=" + $item.title)
            Write-Output ("sessionStatus=" + $item.status)
            Write-Output ("turnCount=" + $item.turnCount)
            Write-Output ("started=" + $item.started)
            Write-Output ("lastUpdated=" + $item.lastUpdated)
            if ($item.turns) {
                foreach ($t in @($item.turns)) {
                    Write-Output ("  turn.requestId=" + $t.requestId)
                    Write-Output ("  turn.status=" + $t.status)
                    Write-Output ("  turn.queryTitle=" + $t.queryTitle)
                    $actionCount = 0
                    if ($t.actions) { $actionCount = @($t.actions).Count }
                    $dialogCount = 0
                    if ($t.processingDialog) { $dialogCount = @($t.processingDialog).Count }
                    Write-Output ("  turn.actionCount=" + $actionCount)
                    Write-Output ("  turn.dialogCount=" + $dialogCount)
                    Write-Output ("  turn.planFile=" + $t.planFile)
                    Write-Output ("  turn.todoId=" + $t.todoId)
                }
            }
        }
    }
}

Show-Query -Label 'text=sessionId' -Arguments @{
    workspacePath = $workspace
    agent = 'GrokCode'
    text = 'GrokCode-20260817T233333Z-hostile-svc-cfg'
    limit = 5
}

Show-Query -Label 'text=hostile-svc-cfg' -Arguments @{
    workspacePath = $workspace
    agent = 'GrokCode'
    text = 'hostile-svc-cfg'
    limit = 5
}

Show-Query -Label 'agent+from' -Arguments @{
    workspacePath = $workspace
    agent = 'GrokCode'
    from = '2026-08-17T23:33:00Z'
    limit = 10
}

Write-Output '=== SERVICE_PID ==='
$svc = Get-CimInstance -ClassName Win32_Service -Filter "Name = 'McpServer'"
Write-Output ("State=" + $svc.State + " ProcessId=" + $svc.ProcessId + " PathName=" + $svc.PathName)

Write-Output '=== GIT_PORCELAIN_PRODUCT ==='
Push-Location $workspace
try {
    git status --porcelain -- src tests appsettings.yaml Directory.Packages.props Directory.Build.props
} finally {
    Pop-Location
}

Write-Output 'QUERY_DONE'
