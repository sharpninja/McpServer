#Requires -Version 7.0
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$workspace = 'F:\GitHub\McpServer'
$pluginRoot = 'F:\GitHub\mcpserver-grok-plugin'
$baseUrl = 'http://PAYTON-LEGION2:7147'
$outDir = Join-Path $workspace 'docs\receipts\_hv-s0-leftover'
New-Item -ItemType Directory -Force -Path $outDir | Out-Null

$env:GROK_PLUGIN_ROOT = $pluginRoot
$env:MCP_PLUGIN_ROOT = $pluginRoot
$env:PLUGIN_AGENT_NAME = 'GrokCode'
$env:MCP_AGENT_NAME = 'GrokCode'
$env:MCP_PLUGIN_HOST = 'grok'
$env:MCP_WORKSPACE_PATH = $workspace
$env:MCPSERVER_WORKSPACE_PATH = $workspace

Set-Location -LiteralPath $workspace

$utc = [datetime]::UtcNow
$utcStamp = $utc.ToString('yyyyMMddTHHmmssZ')
$utc.ToString('o') | Set-Content -LiteralPath (Join-Path $outDir 'utc.txt') -Encoding utf8
Write-Output ('UTC=' + $utc.ToString('o'))
Write-Output ('UTCSTAMP=' + $utcStamp)

# Trust: marker signature + health nonce
. (Join-Path $workspace 'plugins\core\lib-ps\marker-resolver.ps1')
$marker = Join-Path $workspace 'AGENTS-README-FIRST.yaml'
$sig = Test-MarkerSignature -MarkerFile $marker
Write-Output ('SIGNATURE=' + $sig)
$sig | Set-Content -LiteralPath (Join-Path $outDir 'signature.txt') -Encoding utf8

$nonce = [guid]::NewGuid().ToString('N')
$nonce | Set-Content -LiteralPath (Join-Path $outDir 'nonce-sent.txt') -Encoding utf8
$healthUrl = $baseUrl + '/health?nonce=' + $nonce
$health = Invoke-WebRequest -Uri $healthUrl -UseBasicParsing -TimeoutSec 30
$health.Content | Set-Content -LiteralPath (Join-Path $outDir 'health.json') -Encoding utf8
Write-Output ('HEALTH_STATUS=' + [int]$health.StatusCode)
Write-Output ('HEALTH_BODY=' + $health.Content)
$healthObj = $health.Content | ConvertFrom-Json
$nonceOk = $false
if ($healthObj.PSObject.Properties.Name -contains 'nonce') {
    $nonceOk = ([string]$healthObj.nonce -eq $nonce)
}
Write-Output ('NONCE_OK=' + $nonceOk)
$nonceOk | Set-Content -LiteralPath (Join-Path $outDir 'nonce-ok.txt') -Encoding utf8

# Plugin Status
$statusOut = Join-Path $outDir 'plugin-status.txt'
try {
    $status = & pwsh.exe -NoProfile -NonInteractive -File (Join-Path $pluginRoot 'lib\Invoke-McpPlugin.ps1') -Command Status -WorkspacePath $workspace 2>&1
    $status | Set-Content -LiteralPath $statusOut -Encoding utf8
    Write-Output ('PLUGIN_STATUS_EXIT=' + $LASTEXITCODE)
    Write-Output ('PLUGIN_STATUS_LEN=' + ((Get-Content -LiteralPath $statusOut -Raw).Length))
} catch {
    $_ | Out-String | Set-Content -LiteralPath $statusOut -Encoding utf8
    Write-Output ('PLUGIN_STATUS_ERROR=' + $_.Exception.Message)
}

# gitignore / plan / worktrees / git
Select-String -LiteralPath (Join-Path $workspace '.gitignore') -Pattern 'worktrees' |
    ForEach-Object { $_.LineNumber.ToString() + ':' + $_.Line } |
    Set-Content -LiteralPath (Join-Path $outDir 'gitignore-worktrees.txt') -Encoding utf8

$planPath = Join-Path $workspace 'docs\plans\triage-cluster-002.md'
Test-Path -LiteralPath $planPath | Set-Content -LiteralPath (Join-Path $outDir 'plan-exists.txt') -Encoding utf8
$planText = Get-Content -LiteralPath $planPath -Raw
$ids = @(106,107,108,113,116,117,118,120,121,122,125,130,134,140,142,144,147,150,151,152,153,154,155,156,157,158,159)
$idHits = foreach ($n in $ids) {
    $pat = [regex]::Escape([string]$n)
    $hit = $planText -match $pat
    [pscustomobject]@{ Id = $n; Present = [bool]$hit }
}
$idHits | ConvertTo-Json -Depth 4 | Set-Content -LiteralPath (Join-Path $outDir 'plan-id-hits.json') -Encoding utf8
Write-Output ('PLAN_ID_PRESENT=' + (@($idHits | Where-Object Present).Count) + '/' + $ids.Count)
$protocolHits = @(
    'Worktree and subagent protocol',
    '.worktrees/',
    'git worktree add',
    'PLAN-TRIAGELEFTOVER-001'
) | ForEach-Object {
    [pscustomobject]@{ Needle = $_; Present = $planText.Contains($_) }
}
$protocolHits | ConvertTo-Json -Depth 4 | Set-Content -LiteralPath (Join-Path $outDir 'plan-protocol-hits.json') -Encoding utf8

Push-Location $workspace
try {
    git status --short | Set-Content -LiteralPath (Join-Path $outDir 'git-status.txt') -Encoding utf8
    git worktree list | Set-Content -LiteralPath (Join-Path $outDir 'git-worktree-list.txt') -Encoding utf8
    git log -15 --oneline --decorate | Set-Content -LiteralPath (Join-Path $outDir 'git-log.txt') -Encoding utf8
    git diff --stat HEAD | Set-Content -LiteralPath (Join-Path $outDir 'git-diff-stat.txt') -Encoding utf8
    git diff --name-only HEAD | Set-Content -LiteralPath (Join-Path $outDir 'git-diff-names.txt') -Encoding utf8
    git log -8 --format='%H %cI %s' -- .gitignore docs/plans/triage-cluster-002.md | Set-Content -LiteralPath (Join-Path $outDir 'git-log-s0-files.txt') -Encoding utf8
    git log -8 --format='%H %cI %s' -- plugins/core/lib-ps/repl-invoke.ps1 plugins/core/lib-ps/plugin-hook.ps1 | Set-Content -LiteralPath (Join-Path $outDir 'git-log-plugin-core.txt') -Encoding utf8
} finally {
    Pop-Location
}

$wtRoot = Join-Path $workspace '.worktrees'
$wtExists = Test-Path -LiteralPath $wtRoot
$wtExists | Set-Content -LiteralPath (Join-Path $outDir 'worktrees-dir-exists.txt') -Encoding utf8
if ($wtExists) {
    Get-ChildItem -LiteralPath $wtRoot -Force | Select-Object Name, Mode, LastWriteTimeUtc |
        ConvertTo-Json -Depth 4 |
        Set-Content -LiteralPath (Join-Path $outDir 'worktrees-children.json') -Encoding utf8
}

# Native MCP tools
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
    if ($null -eq $outer.result) { return $outer }
    $content = $outer.result.content
    if ($null -eq $content -or $content.Count -lt 1) { return $outer.result }
    $text = [string]$content[0].text
    try { return ($text | ConvertFrom-Json) } catch { return [pscustomobject]@{ rawText = $text } }
}

function Get-Items {
    param($Parsed)
    if ($null -eq $Parsed) { return @() }
    foreach ($name in @('items','Items','records','Records','requirements','Requirements','mappings','Mappings')) {
        $prop = $Parsed.PSObject.Properties[$name]
        if ($null -ne $prop -and $null -ne $prop.Value) { return @($prop.Value) }
    }
    if ($Parsed -is [System.Array]) { return @($Parsed) }
    return @($Parsed)
}

$init = Invoke-McpRpc -Method 'initialize' -Params @{
    protocolVersion = '2025-03-26'
    capabilities = @{}
    clientInfo = @{ name = 'hostile-validator-s0-leftover'; version = '1.0.0' }
}
Save-Body -Name 'mcp-init.json' -Result $init
Write-Output ('INIT_HTTP=' + $init.Status)
[void](Invoke-McpRpc -Method 'notifications/initialized' -Params @{})

$listed = Invoke-McpRpc -Method 'tools/list' -Params @{}
Save-Body -Name 'mcp-tools-list.json' -Result $listed
$listedObj = $listed.Body | ConvertFrom-Json
$names = @($listedObj.result.tools | ForEach-Object { $_.name } | Sort-Object -Unique)
$names | Set-Content -LiteralPath (Join-Path $outDir 'mcp-tool-names.txt') -Encoding utf8
Write-Output ('TOOLS_UNIQUE=' + $names.Count)
Write-Output ('HAS_TODO_GET=' + ($names -contains 'todo_get'))
Write-Output ('HAS_REQUIREMENTS_LIST=' + ($names -contains 'requirements_list'))
Write-Output ('HAS_SESSIONLOG_OPEN=' + ($names -contains 'sessionlog_open'))

$todo = Invoke-McpTool -Name 'todo_get' -Arguments @{
    id = 'PLAN-TRIAGELEFTOVER-001'
    workspacePath = $workspace
}
Save-Body -Name 'todo-plan.json' -Result $todo
$todoObj = Get-ToolObject -Result $todo
$todoObj | ConvertTo-Json -Depth 12 | Set-Content -LiteralPath (Join-Path $outDir 'todo-plan-parsed.json') -Encoding utf8
Write-Output ('TODO_ID=' + $todoObj.Id)
Write-Output ('TODO_DONE=' + $todoObj.Done)
Write-Output ('TODO_SUMMARY=' + $todoObj.DoneSummary)
Write-Output ('TODO_COMPLETED=' + $todoObj.CompletedDate)

$bugIds = $ids | ForEach-Object { 'BUG-TRIAGE-' + $_.ToString('000') }
$bugRows = @()
foreach ($bugId in $bugIds) {
    $res = Invoke-McpTool -Name 'todo_get' -Arguments @{
        id = $bugId
        workspacePath = $workspace
    }
    $obj = Get-ToolObject -Result $res
    $row = [ordered]@{
        id = $bugId
        exists = ($null -ne $obj.Id -and [string]$obj.Id -eq $bugId)
        Done = $obj.Done
        DoneSummary = [string]$obj.DoneSummary
        CompletedDate = [string]$obj.CompletedDate
        UpdatedAt = [string]$obj.UpdatedAt
        error = [string]$obj.error
        message = [string]$obj.message
        rawKeys = ($obj.PSObject.Properties.Name -join ',')
    }
    $bugRows += [pscustomobject]$row
    Write-Output ('BUG ' + $bugId + ' exists=' + $row.exists + ' Done=' + $row.Done + ' Completed=' + $row.CompletedDate)
}
$bugRows | ConvertTo-Json -Depth 6 | Set-Content -LiteralPath (Join-Path $outDir 'bug-triage-27.json') -Encoding utf8

$targetFr = @(
    'FR-MCP-SESSIONATTR-001'
    'FR-MCP-FAILSAFE-001'
    'FR-MCP-STRICTCOUNT-001'
    'FR-MCP-XAGENT-001'
    'FR-MCP-SESSIONEND-001'
    'FR-MCP-VERIFYWRAP-001'
    'FR-MCP-TRANSCRIPT-SEARCH-001'
    'FR-MCP-TEMPVOL-001'
)
$targetTr = @(
    'TR-MCP-SESSIONATTR-001'
    'TR-MCP-FAILSAFE-001'
    'TR-MCP-STRICTCOUNT-001'
    'TR-MCP-XAGENT-001'
    'TR-MCP-SESSIONEND-001'
    'TR-MCP-VERIFYWRAP-001'
    'TR-MCP-TRANSCRIPT-SEARCH-001'
    'TR-MCP-TEMPVOL-001'
)
$targetTest = @(
    'TEST-MCP-SESSIONATTR-001'
    'TEST-MCP-FAILSAFE-001'
    'TEST-MCP-STRICTCOUNT-001'
    'TEST-MCP-XAGENT-001'
    'TEST-MCP-SESSIONEND-001'
    'TEST-MCP-VERIFYWRAP-001'
    'TEST-MCP-TRANSCRIPT-SEARCH-001'
    'TEST-MCP-TEMPVOL-001'
)

$reqSummary = [ordered]@{}
foreach ($kind in @('fr', 'tr', 'test', 'mapping')) {
    $res = Invoke-McpTool -Name 'requirements_list' -Arguments @{
        workspacePath = $workspace
        type = $kind
    }
    Save-Body -Name ('req-' + $kind + '.json') -Result $res
    $parsed = Get-ToolObject -Result $res
    $items = Get-Items -Parsed $parsed
    Write-Output ('KIND=' + $kind + ' TOTAL=' + $items.Count + ' KEYS=' + ($parsed.PSObject.Properties.Name -join ','))
    $filtered = @()
    foreach ($item in $items) {
        $blob = ($item | ConvertTo-Json -Depth 16 -Compress)
        $id = ''
        foreach ($p in @('Id','id','FrId','frId','TrId','trId','TestId','testId')) {
            if ($item.PSObject.Properties.Name -contains $p -and $item.$p) { $id = [string]$item.$p; break }
        }
        $needles = $targetFr + $targetTr + $targetTest + @('SESSIONATTR','FAILSAFE','STRICTCOUNT','XAGENT','SESSIONEND','VERIFYWRAP','TRANSCRIPT-SEARCH','TEMPVOL','TRANSCRIPTSEARCH','TRANSCRIPT_SEARCH')
        $hit = $false
        foreach ($n in $needles) {
            if ($blob.IndexOf($n, [StringComparison]::OrdinalIgnoreCase) -ge 0) { $hit = $true; break }
        }
        if ($hit) {
            $filtered += [pscustomobject]@{ id = $id; blob = $blob }
        }
    }
    $filtered | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath (Join-Path $outDir ('req-' + $kind + '-leftover.json')) -Encoding utf8
    $reqSummary[$kind] = @{ total = $items.Count; leftoverHits = $filtered.Count; leftoverIds = @($filtered | ForEach-Object { $_.id }) }
}

$reqSummary | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath (Join-Path $outDir 'req-summary.json') -Encoding utf8
Write-Output ('REQ_SUMMARY=' + ($reqSummary | ConvertTo-Json -Depth 8 -Compress))

# Also call requirements_list with id filter if supported, plus targeted lookups via list + id
foreach ($fr in $targetFr) {
    $res = Invoke-McpTool -Name 'requirements_list' -Arguments @{
        workspacePath = $workspace
        type = 'fr'
        id = $fr
    }
    $obj = Get-ToolObject -Result $res
    $clip = ($obj | ConvertTo-Json -Depth 12 -Compress)
    if ($clip.Length -gt 4000) { $clip = $clip.Substring(0, 4000) }
    Write-Output ('GETFR ' + $fr + ' ' + $clip)
    $clip | Set-Content -LiteralPath (Join-Path $outDir ('fr-' + $fr + '.json')) -Encoding utf8
}

Write-Output 'COLLECT_DONE'
