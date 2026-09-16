#Requires -Version 7.0
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$workspace = 'F:\GitHub\McpServer'
$pluginRoot = 'F:\GitHub\mcpserver-grok-plugin'
$outDir = 'F:\GitHub\McpServer\docs\receipts\_hv-failsafe-post-review'
New-Item -ItemType Directory -Force -Path $outDir | Out-Null

$utc = [datetime]::UtcNow
$utcStamp = $utc.ToString('yyyyMMddTHHmmssZ')
$sessionId = "GrokCode-$utcStamp-hostile-failsafe-post"
$requestId = "req-$utcStamp-001-hostile-failsafe-post-review"
$title = 'Hostile validate failsafe post-all claims'
$baseUrl = 'http://PAYTON-LEGION2:7147'
$marker = Join-Path $workspace 'AGENTS-README-FIRST.yaml'
$env:MCP_PLUGIN_ROOT = $pluginRoot
$env:GROK_PLUGIN_ROOT = $pluginRoot
$env:PLUGIN_AGENT_NAME = 'GrokCode'
$env:MCP_AGENT_NAME = 'GrokCode'
$env:MCP_WORKSPACE_PATH = $workspace
$env:MCPSERVER_WORKSPACE_PATH = $workspace
$env:MCP_FAILSAFE_DRAIN_DISABLED = '1'
$env:REPL_TIMEOUT = '120'
$env:MCP_PLUGIN_TIMEOUT_SECONDS = '120'
Set-Location -LiteralPath $workspace

. (Join-Path $pluginRoot 'lib\marker-resolver.ps1')
. (Join-Path $pluginRoot 'lib\yaml-object-mutation.ps1')

function Save-Json {
    param([string]$Name, $Object)
    $path = Join-Path $outDir $Name
    $json = $Object | ConvertTo-Json -Depth 12
    [System.IO.File]::WriteAllText($path, $json)
    return $path
}
function Save-Text {
    param([string]$Name, [string]$Text)
    $path = Join-Path $outDir $Name
    [System.IO.File]::WriteAllText($path, $Text)
    return $path
}

$ids = [ordered]@{
    utc = $utc.ToString('o')
    utcStamp = $utcStamp
    sessionId = $sessionId
    requestId = $requestId
    title = $title
    workspace = $workspace
}
Save-Json -Name 'ids.json' -Object $ids
Write-Output ("IDS sessionId={0} requestId={1}" -f $sessionId, $requestId)

# --- Marker signature ---
$sig = $false
$sigError = $null
try { $sig = [bool](Test-MarkerSignature -MarkerFile $marker) } catch { $sigError = $_.Exception.Message }
Save-Json -Name 'marker-sig.json' -Object ([ordered]@{
    marker = $marker
    verified = $sig
    error = $sigError
    markerLastWriteUtc = (Get-Item -LiteralPath $marker).LastWriteTimeUtc.ToString('o')
})
Write-Output ("SIG verified={0}" -f $sig)

# --- Health nonce ---
$nonce = 'nonce-' + ([guid]::NewGuid().ToString('N'))
$healthUrl = "$baseUrl/health?nonce=$nonce"
$healthRaw = $null
$healthStatus = $null
$healthNonceMatch = $false
try {
    $healthRaw = Invoke-RestMethod -Uri $healthUrl -Method Get -TimeoutSec 30
    $healthStatus = [string]$healthRaw.status
    $echo = [string]$healthRaw.nonce
    $healthNonceMatch = ($echo -eq $nonce)
} catch {
    $healthRaw = $_.Exception.Message
}
Save-Json -Name 'health.json' -Object ([ordered]@{
    url = $healthUrl
    nonce = $nonce
    healthNonceMatch = $healthNonceMatch
    status = $healthStatus
    raw = $healthRaw
})
Write-Output ("HEALTH status={0} match={1}" -f $healthStatus, $healthNonceMatch)

# --- YAML counts ---
function Count-Yaml {
    param([string]$Path, [bool]$Recurse)
    $items = @(Get-ChildItem -LiteralPath $Path -File -Filter *.yaml -Recurse:$Recurse -ErrorAction Stop)
    if ($null -eq $items) { $items = @() }
    $files = @($items | Where-Object { $_ -ne $null })
    [ordered]@{
        path = $Path
        recurse = $Recurse
        count = $files.Count
        names = @($files | ForEach-Object { $_.Name })
    }
}
$yamlCounts = @(
    (Count-Yaml 'F:\GitHub\McpServer\.mcpServer\grok\failsafe' $false)
    (Count-Yaml 'F:\GitHub\McpServer\.mcpServer\grok\failsafe\quarantine' $false)
    (Count-Yaml 'F:\GitHub\McpServer\.mcpServer\grok\pending' $false)
    (Count-Yaml 'F:\GitHub\McpServer\.mcpServer\codex\failsafe' $false)
    (Count-Yaml 'F:\GitHub\McpServer\.mcpServer\codex\failsafe\pending' $false)
    (Count-Yaml 'F:\GitHub\McpServer\.mcpServer\codex\failsafe\quarantine' $false)
    (Count-Yaml 'F:\GitHub\McpServer\.mcpServer\claude\failsafe\quarantine' $false)
)
Save-Json -Name 'yaml-counts.json' -Object $yamlCounts
Write-Output 'YAML counts saved'

# --- Replay arithmetic ---
$replayFiles = @(
    'F:\GitHub\McpServer\docs\receipts\failsafe-replay-20260821T144010Z.json'
    'F:\GitHub\McpServer\docs\receipts\failsafe-quarantine-replay-20260821T150140Z.json'
    'F:\GitHub\McpServer\docs\receipts\failsafe-other-replay-20260821T150540Z.json'
)
$arith = [ordered]@{ receipts = @(); postedSum = 0; failedSum = 0; itemizedSuccess = 0 }
foreach ($p in $replayFiles) {
    $j = Get-Content -LiteralPath $p -Raw | ConvertFrom-Json
    $ok = @($j.items | Where-Object { $_.success -eq $true })
    $bad = @($j.items | Where-Object { $_.success -ne $true })
    $arith.receipts += [ordered]@{
        file = Split-Path $p -Leaf
        postedField = $j.posted
        failedField = $j.failed
        items = @($j.items).Count
        successTrue = $ok.Count
        successFalse = $bad.Count
    }
    $arith.postedSum += [int]$j.posted
    $arith.failedSum += [int]$j.failed
    $arith.itemizedSuccess += $ok.Count
}
$arith.claimedTotal = 167
$arith.gap = 167 - [int]$arith.itemizedSuccess
Save-Json -Name 'replay-arithmetic.json' -Object $arith
Write-Output ("ARITH itemizedSuccess={0} claimed=167 gap={1}" -f $arith.itemizedSuccess, $arith.gap)

# --- Remaining file shape ---
$remain = [ordered]@{
    codexQuarantine = @()
    codexPending = @()
}
foreach ($f in @(Get-ChildItem -LiteralPath 'F:\GitHub\McpServer\.mcpServer\codex\failsafe\quarantine' -File -Filter *.yaml)) {
    $raw = Get-Content -LiteralPath $f.FullName -Raw
    $remain.codexQuarantine += [ordered]@{
        name = $f.Name
        length = $f.Length
        lastWriteUtc = $f.LastWriteTimeUtc.ToString('o')
        hasPlanFile = [bool]($raw -match 'planFile')
        turnsIsMappingZero = [bool]($raw -match '(?m)^\s+"0"\s*:')
        method = if ($raw -match '(?m)^method:\s*(.+)$') { $Matches[1].Trim() } else { $null }
    }
}
foreach ($f in @(Get-ChildItem -LiteralPath 'F:\GitHub\McpServer\.mcpServer\codex\failsafe\pending' -File -Filter *.yaml)) {
    $raw = Get-Content -LiteralPath $f.FullName -Raw
    $remain.codexPending += [ordered]@{
        name = $f.Name
        length = $f.Length
        lastWriteUtc = $f.LastWriteTimeUtc.ToString('o')
        isImportRecovery = [bool]($raw -match '(?m)^importRecovery:')
        persistedFalse = [bool]($raw -match '(?m)^\s+persisted:\s*false\s*$')
    }
}
Save-Json -Name 'remaining-shape.json' -Object $remain

# --- Timeout source ---
$replPath = Join-Path $workspace 'plugins\core\lib-ps\repl-invoke.ps1'
$replText = Get-Content -LiteralPath $replPath -Raw
$timeoutHit = [regex]::Match($replText, '(?s)if \(\$script:ReplFailsafeDraining -and \$Method -eq ''client\.SessionLog\.SubmitAsync''\) \{\s*return 2\s*\}')
$timeoutMsg = [regex]::Match($replText, 'mcpserver-repl timed out after \$\{timeout\}s')
Save-Json -Name 'timeout-source.json' -Object ([ordered]@{
    path = $replPath
    lastWriteUtc = (Get-Item -LiteralPath $replPath).LastWriteTimeUtc.ToString('o')
    drainSubmitReturns2 = $timeoutHit.Success
    timeoutMessagePresent = $timeoutMsg.Success
    drainSubmitSnippet = if ($timeoutHit.Success) { $timeoutHit.Value } else { $null }
})

# --- Git ---
$gitStatus = & git -C $workspace status --porcelain
$gitDiffStat = & git -C $workspace diff --stat -- plugins/core/lib-ps/repl-invoke.ps1 plugins/core/lib-ps/Invoke-McpPlugin.ps1 src tests
$gitLogRepl = & git -C $workspace log -3 --format='%h %cI %s' -- plugins/core/lib-ps/repl-invoke.ps1
$gitPorcelainTodo = & git -C $workspace status --porcelain -- docs/todo.yaml docs/Project/TODO.yaml
Save-Text -Name 'git-status.txt' -Text ([string]::Join("`n", @($gitStatus)))
Save-Text -Name 'git-diff-timeout-paths.txt' -Text ([string]::Join("`n", @($gitDiffStat)))
Save-Text -Name 'git-log-repl.txt' -Text ([string]::Join("`n", @($gitLogRepl)))
Save-Text -Name 'git-todo-porcelain.txt' -Text ([string]::Join("`n", @($gitPorcelainTodo)))
Write-Output 'GIT saved'

# --- Plugin status ---
$pluginStatusPath = Join-Path $outDir 'plugin-status.txt'
$pluginStatus = & pwsh.exe -NoProfile -NonInteractive -File (Join-Path $pluginRoot 'lib\Invoke-McpPlugin.ps1') -Command Status -WorkspacePath $workspace -TimeoutSeconds 120 2>&1 | Out-String
Save-Text -Name 'plugin-status.txt' -Text $pluginStatus
Write-Output 'PLUGIN STATUS saved'

# --- Plugin failsafe status ---
$failsafeStatus = & pwsh.exe -NoProfile -NonInteractive -File (Join-Path $pluginRoot 'lib\repl-invoke.ps1') -Method 'workflow.failsafe.status' 2>&1 | Out-String
Save-Text -Name 'failsafe-status.txt' -Text $failsafeStatus
Write-Output 'FAILSAFE STATUS saved'

# --- Search for drain command output ---
$drainHits = [System.Collections.Generic.List[object]]::new()
$searchRoots = @(
    'F:\GitHub\McpServer\docs\receipts'
    'F:\GitHub\McpServer\.mcpServer\grok'
    'C:\Users\kingd\AppData\Local\Temp\PowerShell.MCP.Output'
)
foreach ($root in $searchRoots) {
    if (-not (Test-Path -LiteralPath $root)) { continue }
    Get-ChildItem -LiteralPath $root -File -Recurse -ErrorAction SilentlyContinue |
        Where-Object { $_.LastWriteTimeUtc -ge [datetime]'2026-08-21T14:00:00Z' -and $_.Length -lt 5MB } |
        ForEach-Object {
            try {
                $head = Get-Content -LiteralPath $_.FullName -TotalCount 80 -ErrorAction Stop | Out-String
                if ($head -match 'workflow\.failsafe\.drain|abortReason|timed out after 2s|scanned:\s*1') {
                    $drainHits.Add([ordered]@{
                        path = $_.FullName
                        lastWriteUtc = $_.LastWriteTimeUtc.ToString('o')
                        length = $_.Length
                        snippet = ($head -replace '\s+', ' ').Substring(0, [Math]::Min(300, ($head -replace '\s+', ' ').Length))
                    }) | Out-Null
                }
            } catch { }
        }
}
Save-Json -Name 'drain-search.json' -Object @{ hitCount = $drainHits.Count; hits = @($drainHits) }
Write-Output ("DRAIN SEARCH hits={0}" -f $drainHits.Count)

# --- Plugin version ---
$verPath = Join-Path $pluginRoot '.version'
$pluginJson = Join-Path $pluginRoot 'plugin.json'
$ver = $null
if (Test-Path -LiteralPath $verPath) { $ver = (Get-Content -LiteralPath $verPath -Raw).Trim() }
$pj = $null
if (Test-Path -LiteralPath $pluginJson) {
    try { $pj = (Get-Content -LiteralPath $pluginJson -Raw | ConvertFrom-Json).version } catch { $pj = $_.Exception.Message }
}
Save-Json -Name 'plugin-version.json' -Object ([ordered]@{ versionFile = $ver; pluginJsonVersion = $pj })

# --- MCP streamable HTTP ---
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
function Save-Body {
    param([string]$Name, $Result)
    Save-Text -Name $Name -Text $Result.Body
    Write-Output ("SAVED {0} HTTP={1} LEN={2}" -f $Name, $Result.Status, $Result.Body.Length)
}

$init = Invoke-McpRpc -Method 'initialize' -Params @{
    protocolVersion = '2025-03-26'
    capabilities = @{}
    clientInfo = @{ name = 'hostile-validator-failsafe-post'; version = '1.0.0' }
}
Save-Body -Name 'mcp-init.json' -Result $init
[void](Invoke-McpRpc -Method 'notifications/initialized' -Params @{})

$open = Invoke-McpTool -Name 'sessionlog_open' -Arguments @{
    agent = 'GrokCode'
    sessionId = $sessionId
    workspacePath = $workspace
    title = $title
    model = 'grok'
}
Save-Body -Name 'self-open.json' -Result $open

$begin = Invoke-McpTool -Name 'sessionlog_begin_turn' -Arguments @{
    agent = 'GrokCode'
    sessionId = $sessionId
    requestId = $requestId
    workspacePath = $workspace
    planFile = 'None'
    todoId = 'None'
    queryTitle = $title
    queryText = 'Hostile validator: attack implementer claims for posting queued failsafe data and triaging failures. Class 2 user-directed ops. Surface C N/A. Surface D N/A. No TODO marked done.'
}
Save-Body -Name 'self-begin.json' -Result $begin

# --- Triage reports ---
$triageIds = @(
    'triage-report-bb6214a43c40430da3285a530deec758'
    'triage-report-972623438ab84b489e8195f977698a86'
    'triage-report-f52c6edada3f437bbe7aadcc5611c684'
    'triage-report-ab5a9da38021414b96dcb651bc6697e0'
)
foreach ($tid in $triageIds) {
    $safe = $tid.Replace('triage-report-', '')
    $r = Invoke-McpTool -Name 'triage_status' -Arguments @{
        workspacePath = $workspace
        reportId = $tid
    }
    Save-Body -Name ("triage-{0}.json" -f $safe) -Result $r
}

# --- TODOs ---
$todoOpen = Invoke-McpTool -Name 'todo_list' -Arguments @{
    workspacePath = $workspace
    done = $false
}
Save-Body -Name 'todo-list-done-false.json' -Result $todoOpen
$todoDone = Invoke-McpTool -Name 'todo_list' -Arguments @{
    workspacePath = $workspace
    done = $true
}
Save-Body -Name 'todo-list-done-true.json' -Result $todoDone

# --- Sample persist queries ---
$queries = @(
    @{ Name = 'q-grok-live-sample.json'; Agent = 'GrokCode'; Text = 'GrokCode-20260821T125112Z-plugin-session' }
    @{ Name = 'q-grok-q-sample.json'; Agent = 'GrokCode'; Text = 'GrokCode-20260812T220151Z-plugin-session' }
    @{ Name = 'q-codex-live-sample.json'; Agent = 'Codex'; Text = 'Codex-20260817T104751Z-plugin-session' }
    @{ Name = 'q-claude-q-sample.json'; Agent = 'ClaudeCode'; Text = 'ClaudeCode-20260819T002343Z-plugin-session' }
    @{ Name = 'q-self.json'; Agent = 'GrokCode'; Text = $sessionId }
    @{ Name = 'q-implementer-post.json'; Agent = 'GrokCode'; Text = 'req-20260821T143526Z-prompt-de7d' }
)
foreach ($q in $queries) {
    $r = Invoke-McpTool -Name 'sessionlog_query' -Arguments @{
        workspacePath = $workspace
        agent = $q.Agent
        text = $q.Text
        limit = 5
    }
    Save-Body -Name $q.Name -Result $r
}

Write-Output 'COLLECT COMPLETE'
Write-Output ("OUTDIR={0}" -f $outDir)
