#Requires -Version 7.0
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Continue'

$workspace = 'F:\GitHub\McpServer'
$pluginRoot = 'F:\GitHub\mcpserver-grok-plugin'
$outDir = Join-Path $workspace 'docs\receipts\_hv-hclassify-s3'
$invoke = Join-Path $pluginRoot 'lib\Invoke-McpPlugin.ps1'
$utc = [datetime]::UtcNow.ToString('yyyyMMddTHHmmssZ')
$sessionId = "GrokCode-$utc-hclassify-s3"
$requestId = "req-$utc-001-hostile-classify-s3-matrix"

$env:MCP_PLUGIN_ROOT = $pluginRoot
$env:GROK_PLUGIN_ROOT = $pluginRoot
$env:PLUGIN_AGENT_NAME = 'GrokCode'
$env:MCP_AGENT_NAME = 'GrokCode'
$env:MCP_WORKSPACE_PATH = $workspace
Set-Location -LiteralPath $workspace
New-Item -ItemType Directory -Force -Path $outDir | Out-Null

function Save-Raw {
    param([string]$Name, [string]$Text)
    $path = Join-Path $outDir $Name
    Set-Content -LiteralPath $path -Value $Text -Encoding utf8
    return $path
}

function Invoke-Plugin {
    param(
        [Parameter(Mandatory)][string]$Method,
        [hashtable]$Params = $null,
        [int]$TimeoutSeconds = 120
    )
    if ($null -ne $Params) {
        return (& pwsh.exe -NoProfile -NonInteractive -File $invoke -Command Invoke -Method $Method -ParamsObject $Params -WorkspacePath $workspace -PluginRoot $pluginRoot -TimeoutSeconds $TimeoutSeconds 2>&1 | Out-String)
    }
    return (& pwsh.exe -NoProfile -NonInteractive -File $invoke -Command Invoke -Method $Method -WorkspacePath $workspace -PluginRoot $pluginRoot -TimeoutSeconds $TimeoutSeconds 2>&1 | Out-String)
}

function Get-JsonObject {
    param([string]$Text)
    if ([string]::IsNullOrWhiteSpace($Text)) { return $null }
    $start = $Text.IndexOf('{')
    $startArr = $Text.IndexOf('[')
    if ($start -lt 0 -and $startArr -lt 0) { return $null }
    $idx = if ($start -ge 0 -and ($startArr -lt 0 -or $start -lt $startArr)) { $start } else { $startArr }
    $slice = $Text.Substring($idx)
    try { return ($slice | ConvertFrom-Json -ErrorAction Stop) } catch { return $null }
}

function Get-IdListFromText {
    param([string]$Text, [string]$Pattern)
    return @([regex]::Matches($Text, $Pattern) | ForEach-Object { $_.Value } | Select-Object -Unique)
}

@(
    "UTC=$utc"
    "SESSION_ID=$sessionId"
    "REQUEST_ID=$requestId"
) | Set-Content -LiteralPath (Join-Path $outDir '08-session-ids.txt') -Encoding utf8

$status = & pwsh.exe -NoProfile -NonInteractive -File $invoke -Command Status -WorkspacePath $workspace -PluginRoot $pluginRoot -TimeoutSeconds 60 2>&1 | Out-String
Save-Raw '08-status.txt' $status | Out-Null

$boot = Invoke-Plugin -Method 'workflow.sessionlog.bootstrap'
Save-Raw '08-bootstrap.txt' $boot | Out-Null

$open = Invoke-Plugin -Method 'workflow.sessionlog.openSession' -Params @{
    agent = 'GrokCode'
    sessionId = $sessionId
    title = 'Hostile classify S3 TODO-requirements matrix'
    model = 'grok-4'
    sourceType = 'GrokCode'
}
Save-Raw '08-open.txt' $open | Out-Null

$begin = Invoke-Plugin -Method 'workflow.sessionlog.beginTurn' -Params @{
    requestId = $requestId
    queryTitle = 'Hostile classify S3 TODO-requirements matrix'
    queryText = 'H-classify after S3 matrix. Attack implementer claims on s3-matrix.json vs live MCP store. Class 1 store hygiene. Do not apply S4 writes.'
}
Save-Raw '08-begin.txt' $begin | Out-Null

$query = Invoke-Plugin -Method 'workflow.todo.query' -Params @{ done = $false } -TimeoutSeconds 180
Save-Raw '08-todo-query.txt' $query | Out-Null

$spotIds = @(
    'PLAN-TRIAGELEFTOVER-001'
    'PLAN-QUADBRAIN-E1-001'
    'PLAN-FILETOOLS-001'
    'PLAN-DELETECOMPLIANCE-003'
    'MCP-HANDOFF-001'
    'BUG-TRIAGE-160'
    'PLAN-TODOALIGN-001'
    'PLAN-TODOAUDIT-001'
)
$spot = @()
foreach ($id in $spotIds) {
    $raw = Invoke-Plugin -Method 'workflow.todo.get' -Params @{ id = $id } -TimeoutSeconds 90
    Save-Raw ("08-todo-get-$id.txt") $raw | Out-Null
    $remaining = $null
    $m = [regex]::Match($raw, '(?i)"remaining"\s*:\s*"(.*?)"\s*,', [System.Text.RegularExpressions.RegexOptions]::Singleline)
    if ($m.Success) { $remaining = $m.Groups[1].Value -replace '\\"', '"' }
    if (-not $remaining) {
        $m2 = [regex]::Match($raw, '(?im)^remaining:\s*(.+)$')
        if ($m2.Success) { $remaining = $m2.Groups[1].Value.Trim() }
    }
    $done = $null
    $dm = [regex]::Match($raw, '(?i)"done"\s*:\s*(true|false)')
    if ($dm.Success) { $done = [bool]::Parse($dm.Groups[1].Value) }
    else {
        $dm2 = [regex]::Match($raw, '(?im)^done:\s*(true|false)')
        if ($dm2.Success) { $done = [bool]::Parse($dm2.Groups[1].Value) }
    }
    $spot += [ordered]@{
        id = $id
        done = $done
        remainingHas0711 = if ($remaining) { $remaining.Contains('2026-07-11') } else { $null }
        remainingHasAuditDate = if ($remaining) { $remaining.Contains('2026-08-20T101500Z') } else { $null }
        remainingPreview = if ($remaining -and $remaining.Length -gt 240) { $remaining.Substring(0,240) } else { $remaining }
        rawLength = $raw.Length
        hasId = $raw.Contains($id)
    }
}

$listFr = Invoke-Plugin -Method 'workflow.requirements.listFr' -TimeoutSeconds 180
Save-Raw '08-listFr.txt' $listFr | Out-Null
$listTr = Invoke-Plugin -Method 'workflow.requirements.listTr' -TimeoutSeconds 180
Save-Raw '08-listTr.txt' $listTr | Out-Null

$liveFr = Get-IdListFromText -Text $listFr -Pattern 'FR-[A-Z0-9]+(?:-[A-Z0-9]+)*-\d{3}|FR-MCP-\d+'
$liveTr = Get-IdListFromText -Text $listTr -Pattern 'TR-[A-Z0-9]+(?:-[A-Z0-9]+)*-\d{3}|TR-\d+|\[\]'

$summaries = Get-Content -LiteralPath (Join-Path $outDir '06-item-summaries.json') -Raw | ConvertFrom-Json
$neededFr = @($summaries.uniqueFr)
$neededTr = @($summaries.uniqueTr)
$placeholderTr = @('[]','TR-01','TR-02','TR-03','TR-04','TR-05','TR-06','TR-07','TR-08','TR-09','TR-10','TR-11','TR-12','TR-13','TR-14')

$missingFr = @($neededFr | Where-Object { $_ -notin $liveFr })
$missingTr = @($neededTr | Where-Object { $_ -notin $liveTr })
$missingPlaceholder = @($placeholderTr | Where-Object { $_ -notin $liveTr })

$openIds = Get-IdListFromText -Text $query -Pattern 'PLAN-[A-Z0-9]+-\d{3}|MCP-[A-Z0-9]+-\d{3}|BUG-TRIAGE-\d+|TR-AUDIT-001'

# MCP tools/list for read_file names
$script:McpSessionHeader = $null
$script:McpId = 0
function Invoke-McpRpc {
    param([Parameter(Mandatory)][string]$Method, $Params = $null)
    $script:McpId++
    $payload = [ordered]@{ jsonrpc = '2.0'; id = $script:McpId; method = $Method }
    if ($null -ne $Params) { $payload['params'] = $Params }
    $json = $payload | ConvertTo-Json -Depth 20 -Compress
    $req = [System.Net.Http.HttpRequestMessage]::new([System.Net.Http.HttpMethod]::Post, 'http://PAYTON-LEGION2:7147/mcp-transport')
    $req.Headers.Accept.Clear()
    [void]$req.Headers.Accept.Add([System.Net.Http.Headers.MediaTypeWithQualityHeaderValue]::new('application/json'))
    [void]$req.Headers.Accept.Add([System.Net.Http.Headers.MediaTypeWithQualityHeaderValue]::new('text/event-stream'))
    [void]$req.Headers.TryAddWithoutValidation('X-Workspace-Path', $workspace)
    if ($script:McpSessionHeader) { [void]$req.Headers.TryAddWithoutValidation('Mcp-Session-Id', $script:McpSessionHeader) }
    $req.Content = [System.Net.Http.StringContent]::new($json, [System.Text.Encoding]::UTF8, 'application/json')
    $handler = [System.Net.Http.HttpClientHandler]::new()
    $client = [System.Net.Http.HttpClient]::new($handler)
    $client.Timeout = [TimeSpan]::FromSeconds(60)
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
    } finally { $client.Dispose(); $handler.Dispose() }
}

$init = Invoke-McpRpc -Method 'initialize' -Params @{
    protocolVersion = '2024-11-05'
    capabilities = @{}
    clientInfo = @{ name = 'hostile-validator'; version = '1.0' }
}
Save-Raw '08-mcp-init.txt' $init.Body | Out-Null
try { [void](Invoke-McpRpc -Method 'notifications/initialized') } catch { }
$tools = Invoke-McpRpc -Method 'tools/list'
Save-Raw '08-tools-list.txt' $tools.Body | Out-Null
$toolNames = Get-IdListFromText -Text $tools.Body -Pattern '"name"\s*:\s*"([^"]+)"'
# Get-IdListFromText uses whole match; extract names properly
$toolNames = @([regex]::Matches($tools.Body, '"name"\s*:\s*"([^"]+)"') | ForEach-Object { $_.Groups[1].Value } | Select-Object -Unique)
$hasReadFile = $toolNames -contains 'read_file'
$hasListDir = $toolNames -contains 'list_dir'
$hasGrepFiles = $toolNames -contains 'grep_files'

[pscustomobject]@{
    timestampUtc = [datetime]::UtcNow.ToString('o')
    sessionId = $sessionId
    requestId = $requestId
    liveFrCount = $liveFr.Count
    liveTrCount = $liveTr.Count
    openIdCount = $openIds.Count
    openIds = $openIds
    leftoverInOpen = ($openIds -contains 'PLAN-TRIAGELEFTOVER-001')
    neededFr = $neededFr
    neededTr = $neededTr
    missingFr = $missingFr
    missingTr = $missingTr
    missingPlaceholder = $missingPlaceholder
    liveFrSample = @($liveFr | Select-Object -First 20)
    liveTrHasBracket = ($liveTr -contains '[]')
    liveTrHasTr01 = ($liveTr -contains 'TR-01')
    liveTrHasTr02 = ($liveTr -contains 'TR-02')
    spot = $spot
    fileToolNamesPresent = @{
        read_file = $hasReadFile
        list_dir = $hasListDir
        grep_files = $hasGrepFiles
    }
    toolNameCount = $toolNames.Count
} | ConvertTo-Json -Depth 20 | Set-Content -LiteralPath (Join-Path $outDir '08-mcp-summary.json') -Encoding utf8

Write-Output ('SESSION_ID=' + $sessionId)
Write-Output ('REQUEST_ID=' + $requestId)
Write-Output ('OPEN_COUNT=' + $openIds.Count)
Write-Output ('LEFTOVER_OPEN=' + ($openIds -contains 'PLAN-TRIAGELEFTOVER-001'))
Write-Output ('LIVE_FR=' + $liveFr.Count)
Write-Output ('LIVE_TR=' + $liveTr.Count)
Write-Output ('MISSING_FR=' + ($missingFr -join ','))
Write-Output ('MISSING_TR=' + ($missingTr -join ','))
Write-Output ('MISSING_PLACEHOLDER=' + ($missingPlaceholder -join ','))
Write-Output ('READ_FILE=' + $hasReadFile)
Write-Output ('LIST_DIR=' + $hasListDir)
Write-Output ('GREP_FILES=' + $hasGrepFiles)
foreach ($s in $spot) {
    Write-Output ('SPOT ' + $s.id + ' done=' + $s.done + ' 0711=' + $s.remainingHas0711 + ' auditDate=' + $s.remainingHasAuditDate)
}
