#Requires -Version 7.0
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$workspace = 'F:\GitHub\McpServer'
$baseUrl = 'http://PAYTON-LEGION2:7147'
$outDir = Join-Path $workspace 'docs\receipts\_hv-s0-leftover'
New-Item -ItemType Directory -Force -Path $outDir | Out-Null

$sessionId = 'GrokCode-20260819T174750Z-hostile-s0-leftover'
$requestId = 'req-20260819T174750Z-001-hostile-s0-leftover-triage'
$planFile = 'docs/plans/triage-cluster-002.md'
$todoId = 'PLAN-TRIAGELEFTOVER-001'

function Get-Prop {
    param($Obj, [string]$Name)
    if ($null -eq $Obj) { return $null }
    $prop = $Obj.PSObject.Properties[$Name]
    if ($null -eq $prop) { return $null }
    return $prop.Value
}

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
    $resultProp = Get-Prop -Obj $outer -Name 'result'
    if ($null -eq $resultProp) { return $outer }
    $content = Get-Prop -Obj $resultProp -Name 'content'
    if ($null -eq $content) { return $resultProp }
    $first = @($content)[0]
    $text = [string](Get-Prop -Obj $first -Name 'text')
    try { return ($text | ConvertFrom-Json) } catch { return [pscustomobject]@{ rawText = $text } }
}

$init = Invoke-McpRpc -Method 'initialize' -Params @{
    protocolVersion = '2025-03-26'
    capabilities = @{}
    clientInfo = @{ name = 'hostile-validator-s0-leftover-session'; version = '1.0.0' }
}
Save-Body -Name 'session-init.json' -Result $init
[void](Invoke-McpRpc -Method 'notifications/initialized' -Params @{})

$open = Invoke-McpTool -Name 'sessionlog_open' -Arguments @{
    agent = 'GrokCode'
    sessionId = $sessionId
    workspacePath = $workspace
    title = 'Hostile H0 leftover-triage S0 requirements'
    model = 'grok'
}
Save-Body -Name 'session-open.json' -Result $open
$openObj = Get-ToolObject -Result $open
Write-Output ('OPEN=' + ($openObj | ConvertTo-Json -Depth 8 -Compress))

$begin = Invoke-McpTool -Name 'sessionlog_begin_turn' -Arguments @{
    agent = 'GrokCode'
    sessionId = $sessionId
    requestId = $requestId
    workspacePath = $workspace
    planFile = $planFile
    todoId = $todoId
    queryTitle = 'Hostile H0 leftover-triage S0 requirements'
    queryText = 'Hostile validator for leftover-triage S0. Class 1. Attack PLAN-TRIAGELEFTOVER-001, 27 BUG-TRIAGE IDs, gitignore .worktrees/, FR/TR/TEST/AC/mappings, ValidateTraceability, no product implementation, no BUG-TRIAGE done. Do not mark TODOs done. Do not implement product code.'
}
Save-Body -Name 'session-begin.json' -Result $begin
$beginObj = Get-ToolObject -Result $begin
Write-Output ('BEGIN=' + ($beginObj | ConvertTo-Json -Depth 8 -Compress))
$turnId = Get-Prop -Obj $beginObj -Name 'turnId'
if ($null -eq $turnId) { $turnId = Get-Prop -Obj $beginObj -Name 'TurnId' }
Write-Output ('TURN_ID=' + $turnId)
('SESSION_ID=' + $sessionId) | Set-Content -LiteralPath (Join-Path $outDir 'session-ids.txt') -Encoding utf8
Add-Content -LiteralPath (Join-Path $outDir 'session-ids.txt') -Value ('REQUEST_ID=' + $requestId) -Encoding utf8
Add-Content -LiteralPath (Join-Path $outDir 'session-ids.txt') -Value ('TURN_ID=' + $turnId) -Encoding utf8

$now = [datetime]::UtcNow.ToString('o')
$dialogItems = @(
    [ordered]@{
        timestamp = $now
        role = 'model'
        content = 'add-profile executed first. 18 non-skill profile markdown files read under C:\Users\kingd\.claude\profile. Excluded add-profile.grok.md.'
        category = 'observation'
    }
    [ordered]@{
        timestamp = $now
        role = 'model'
        content = 'Classified class 1 project requirement work (Byrd requirements phase for leftover-triage S0). Surfaces A-D apply. Do not FAIL B2 from FR createdAt versus later files. Do not mark TODOs done.'
        category = 'observation'
    }
    [ordered]@{
        timestamp = $now
        role = 'model'
        content = 'Independent evidence: todo_get PLAN-TRIAGELEFTOVER-001 Done=false. 27/27 plan IDs present. gitignore .worktrees/. Native requirements_list: 8 leftover FR/TR/TEST plus 8 mappings exist. Structured AcceptanceCriteria arrays are empty on all 8 leftover FR, TR, and TEST records. ValidateTraceability Succeeded findings=0. git status has no src/plugins leftover product files. 27 BUG-TRIAGE items remain Done=false. PLAN TODO FunctionalRequirements and TechnicalRequirements are null.'
        category = 'observation'
    }
    [ordered]@{
        timestamp = $now
        role = 'model'
        content = 'Decision: OverallVerdict DISAGREE. S0 store records and mappings exist, but claimed-complete S0 lacks structured AcceptanceCriteria on the eight leftover FR/TR/TEST sets, and PLAN-TRIAGELEFTOVER-001 does not carry FR/TR IDs. Consequence: parent must not treat H0 as AGREE and must not start leftover worktrees. Alternatives rejected: AGREE because ValidateTraceability findings=0 (structured AC still empty); AGREE because Body checkbox bullets exist (skill requires structured AC); ignore TODO linkage (planning standard requires FR/TR IDs on the TODO). Affected: PLAN-TRIAGELEFTOVER-001, FR/TR/TEST-MCP-SESSIONATTR/FAILSAFE/STRICTCOUNT/XAGENT/SESSIONEND/VERIFYWRAP/TRANSCRIPT-SEARCH/TEMPVOL-001.'
        category = 'decision'
    }
)
$dialogJson = $dialogItems | ConvertTo-Json -Depth 8 -Compress
$dialog = Invoke-McpTool -Name 'sessionlog_dialog' -Arguments @{
    agent = 'GrokCode'
    sessionId = $sessionId
    requestId = $requestId
    itemsJson = $dialogJson
    workspacePath = $workspace
}
Save-Body -Name 'session-dialog.json' -Result $dialog
Write-Output ('DIALOG=' + ((Get-ToolObject -Result $dialog) | ConvertTo-Json -Depth 6 -Compress))

Write-Output 'SESSION_INIT_DONE'
