#Requires -Version 7.0
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$workspace = 'F:\GitHub\McpServer'
$outDir = 'F:\GitHub\McpServer\docs\receipts\_hv-failsafe-post-review'
$baseUrl = 'http://PAYTON-LEGION2:7147'
$ids = Get-Content -LiteralPath (Join-Path $outDir 'ids.json') -Raw | ConvertFrom-Json

function Get-McpTextObject {
    param([string]$Path)
    $outer = Get-Content -LiteralPath $Path -Raw | ConvertFrom-Json
    $text = $outer.result.content[0].text
    return $text | ConvertFrom-Json
}

function Save-Json {
    param([string]$Name, $Object)
    [System.IO.File]::WriteAllText((Join-Path $outDir $Name), ($Object | ConvertTo-Json -Depth 8))
}

$openTodos = Get-McpTextObject (Join-Path $outDir 'todo-list-done-false.json')
$doneTodos = Get-McpTextObject (Join-Path $outDir 'todo-list-done-true.json')
$openIds = @($openTodos.items | ForEach-Object { $_.Id })
$doneIds = @($doneTodos.items | ForEach-Object { $_.Id })
Save-Json -Name 'todo-counts.json' -Object ([ordered]@{
    openTotalCount = $openTodos.totalCount
    openItems = @($openTodos.items).Count
    doneTotalCount = $doneTodos.totalCount
    doneItems = @($doneTodos.items).Count
    openIds = $openIds
})
Write-Output ("TODO open={0} done={1}" -f $openTodos.totalCount, $doneTodos.totalCount)

# Prior open list from 11:45 hostile extract if present
$priorPath = 'F:\GitHub\McpServer\docs\receipts\_hv-20260821T113500Z\todo-list-done-false.json'
$priorCompare = $null
if (Test-Path -LiteralPath $priorPath) {
    $prior = Get-McpTextObject $priorPath
    $priorIds = @($prior.items | ForEach-Object { $_.Id })
    $newlyDone = @($priorIds | Where-Object { $_ -notin $openIds -and $_ -in $doneIds })
    $priorCompare = [ordered]@{
        priorOpen = $prior.totalCount
        nowOpen = $openTodos.totalCount
        newlyDoneFromPriorOpen = $newlyDone
    }
    Save-Json -Name 'todo-compare-114530.json' -Object $priorCompare
    Write-Output ("TODO compare priorOpen={0} newlyDone={1}" -f $prior.totalCount, $newlyDone.Count)
}

$script:McpSessionHeader = $null
$script:McpId = 0
function Invoke-McpRpc {
    param([Parameter(Mandatory)][string]$Method, $Params = $null)
    $script:McpId++
    $payload = [ordered]@{ jsonrpc = '2.0'; id = $script:McpId; method = $Method }
    if ($null -ne $Params) { $payload['params'] = $Params }
    $json = $payload | ConvertTo-Json -Depth 40 -Compress
    $req = [System.Net.Http.HttpRequestMessage]::new([System.Net.Http.HttpMethod]::Post, "$baseUrl/mcp-transport")
    $req.Headers.Accept.Clear()
    [void]$req.Headers.Accept.Add([System.Net.Http.Headers.MediaTypeWithQualityHeaderValue]::new('application/json'))
    [void]$req.Headers.Accept.Add([System.Net.Http.Headers.MediaTypeWithQualityHeaderValue]::new('text/event-stream'))
    [void]$req.Headers.TryAddWithoutValidation('X-Workspace-Path', $workspace)
    if ($script:McpSessionHeader) { [void]$req.Headers.TryAddWithoutValidation('Mcp-Session-Id', $script:McpSessionHeader) }
    $req.Content = [System.Net.Http.StringContent]::new($json, [System.Text.Encoding]::UTF8, 'application/json')
    $handler = [System.Net.Http.HttpClientHandler]::new()
    $client = [System.Net.Http.HttpClient]::new($handler)
    $client.Timeout = [TimeSpan]::FromSeconds(180)
    try {
        $resp = $client.Send($req)
        $sid = $null
        if ($resp.Headers.TryGetValues('Mcp-Session-Id', [ref]$sid)) { $script:McpSessionHeader = @($sid)[0] }
        $body = $resp.Content.ReadAsStringAsync().GetAwaiter().GetResult()
        if ($body.StartsWith('event:') -or $body.Contains("`ndata:")) {
            $dataLines = [System.Collections.Generic.List[string]]::new()
            foreach ($line in ($body -split "`n")) {
                $trim = $line.TrimEnd("`r")
                if ($trim.StartsWith('data:')) { [void]$dataLines.Add($trim.Substring(5).Trim()) }
            }
            $body = [string]::Join("`n", $dataLines)
        }
        return [pscustomobject]@{ Status = [int]$resp.StatusCode; Body = $body }
    } finally { $client.Dispose(); $req.Dispose() }
}
function Invoke-McpTool { param([string]$Name, [hashtable]$Arguments) Invoke-McpRpc -Method 'tools/call' -Params @{ name = $Name; arguments = $Arguments } }

[void](Invoke-McpRpc -Method 'initialize' -Params @{
    protocolVersion = '2025-03-26'
    capabilities = @{}
    clientInfo = @{ name = 'hostile-validator-failsafe-post'; version = '1.0.0' }
})
[void](Invoke-McpRpc -Method 'notifications/initialized' -Params @{})

$queries = @(
    @{ Name = 'q-self-from.json'; Args = @{ workspacePath = $workspace; agent = 'GrokCode'; from = '2026-08-21T15:30:00Z'; limit = 10 } }
    @{ Name = 'q-grok-q-req.json'; Args = @{ workspacePath = $workspace; agent = 'GrokCode'; text = 'req-20260812T214709Z-prompt-e362'; limit = 5 } }
    @{ Name = 'q-codex-req.json'; Args = @{ workspacePath = $workspace; agent = 'Codex'; text = 'req-20260817T123557Z-prompt-bda6'; limit = 5 } }
    @{ Name = 'q-claude-req.json'; Args = @{ workspacePath = $workspace; agent = 'ClaudeCode'; text = 'req-20260819T000529Z-prompt-e37f'; limit = 5 } }
    @{ Name = 'q-grok-live-first.json'; Args = @{ workspacePath = $workspace; agent = 'GrokCode'; text = 'req-20260819T153500Z-019-remediate-hook-cache-isolation'; limit = 5 } }
    @{ Name = 'q-self-text.json'; Args = @{ workspacePath = $workspace; agent = 'GrokCode'; text = 'Hostile validate failsafe post-all claims'; limit = 5 } }
)
foreach ($q in $queries) {
    $r = Invoke-McpTool -Name 'sessionlog_query' -Arguments $q.Args
    [System.IO.File]::WriteAllText((Join-Path $outDir $q.Name), $r.Body)
    Write-Output ("SAVED {0} HTTP={1} LEN={2}" -f $q.Name, $r.Status, $r.Body.Length)
}

# Summarize query hits
$summaries = @()
foreach ($name in @('q-self-from.json','q-grok-q-req.json','q-codex-req.json','q-claude-req.json','q-grok-live-first.json','q-self-text.json','q-implementer-post.json')) {
    $p = Join-Path $outDir $name
    if (-not (Test-Path -LiteralPath $p)) { continue }
    $obj = Get-McpTextObject $p
    $first = $null
    if (@($obj.items).Count -gt 0) {
        $it = @($obj.items)[0]
        $first = [ordered]@{ sessionId = $it.sessionId; turnCount = $it.turnCount; lastUpdated = [string]$it.lastUpdated; status = $it.status }
    }
    $summaries += [ordered]@{ file = $name; totalCount = $obj.totalCount; first = $first }
}
Save-Json -Name 'query-summaries.json' -Object $summaries
Write-Output 'EXTRACT COMPLETE'
