#Requires -Version 7.0
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$workspace = 'F:\GitHub\McpServer'
$pluginRoot = 'F:\GitHub\mcpserver-grok-plugin'
$invoke = Join-Path $pluginRoot 'lib\Invoke-McpPlugin.ps1'
$outDir = Join-Path $workspace 'docs\receipts\_hv-happly-hdone'
$scratch = 'C:\Users\kingd\AppData\Local\Temp\grok-goal-498d465c218e\implementer'
$invPath = Join-Path $scratch 's0-inventory.json'
$cacheRoot = Join-Path $outDir 'plugin-cache'
New-Item -ItemType Directory -Force -Path $outDir | Out-Null
New-Item -ItemType Directory -Force -Path $cacheRoot | Out-Null

$env:PLUGIN_AGENT_NAME = 'GrokCode'
$env:MCP_AGENT_NAME = 'GrokCode'
$env:MCP_PLUGIN_HOST = 'grok'
$env:MCP_PLUGIN_ROOT = $pluginRoot
$env:MCP_WORKSPACE_PATH = $workspace
$env:GROK_WORKSPACE_PATH = $workspace
$env:GROK_PLUGIN_ROOT = $pluginRoot

$utc = [datetime]::UtcNow.ToString('yyyyMMddTHHmmssZ')
$sessionId = "GrokCode-$utc-happly-hdone-align"
$requestId = "req-$utc-001-hostile-apply-done-align"

function Save-Json {
    param([string]$Name, $Object)
    $path = Join-Path $outDir $Name
    ($Object | ConvertTo-Json -Depth 20) | Set-Content -LiteralPath $path -Encoding utf8
    return $path
}

function Save-Text {
    param([string]$Name, [string]$Text)
    $path = Join-Path $outDir $Name
    Set-Content -LiteralPath $path -Value $Text -Encoding utf8
    return $path
}

function Invoke-PluginMethod {
    param(
        [Parameter(Mandatory)][string]$Method,
        [string]$Params = '',
        [int]$TimeoutSeconds = 90
    )
    $argList = @(
        '-NoProfile', '-NonInteractive', '-File', $invoke,
        '-Command', 'Invoke',
        '-Method', $Method,
        '-WorkspacePath', $workspace,
        '-PluginRoot', $pluginRoot,
        '-CacheRoot', $cacheRoot,
        '-TimeoutSeconds', [string]$TimeoutSeconds
    )
    if (-not [string]::IsNullOrWhiteSpace($Params)) {
        $argList += @('-Params', $Params)
    }
    $stdout = & pwsh.exe @argList 2>&1 | Out-String
    return [ordered]@{
        method = $Method
        exitCode = $LASTEXITCODE
        stdout = $stdout
        length = $stdout.Length
        isError = ($stdout -match '(?m)^type: error')
        has503 = ($stdout -match '503|backend_unavailable')
    }
}

function Get-YamlScalar {
    param([string]$Text, [string]$Key)
    $m = [regex]::Match($Text, "(?im)^\s+$([regex]::Escape($Key)):\s*(.+)$")
    if ($m.Success) { return $m.Groups[1].Value.Trim().Trim("'").Trim('"') }
    return $null
}

function Get-YamlBool {
    param([string]$Text, [string]$Key)
    $v = Get-YamlScalar -Text $Text -Key $Key
    if ($v -eq 'true') { return $true }
    if ($v -eq 'false') { return $false }
    return $null
}

function Get-YamlList {
    param([string]$Text, [string]$Key)
    $items = @()
    $m = [regex]::Match($Text, "(?ims)^\s+$([regex]::Escape($Key)):\s*\r?\n((?:\s+-\s+.+\r?\n?)*)")
    if ($m.Success) {
        $items = @([regex]::Matches($m.Groups[1].Value, '(?m)^\s+-\s+(.+)$') | ForEach-Object { $_.Groups[1].Value.Trim() })
    }
    return @($items)
}

function Get-YamlRemaining {
    param([string]$Text)
    $m = [regex]::Match($Text, '(?ims)^\s+remaining:\s*(?:\|-?\s*)?\r?\n((?:\s{6,}.+\r?\n?)*)')
    if ($m.Success) {
        $lines = @($m.Groups[1].Value -split "`n" | ForEach-Object { $_.TrimEnd() })
        return (($lines | ForEach-Object { $_.Trim() }) -join ' ').Trim()
    }
    $scalar = Get-YamlScalar -Text $Text -Key 'remaining'
    return $scalar
}

# --- trust + git ---
. (Join-Path $pluginRoot 'lib\marker-resolver.ps1')
$sig = Test-MarkerSignature -MarkerFile (Join-Path $workspace 'AGENTS-README-FIRST.yaml')
$nonce = [guid]::NewGuid().ToString('N')
$health = $null
$nonceOk = $false
$healthVersion = $null
try {
    $hr = Invoke-WebRequest -Uri "http://PAYTON-LEGION2:7147/health?nonce=$nonce" -UseBasicParsing -TimeoutSec 20
    $health = $hr.Content | ConvertFrom-Json
    $nonceOk = ([string]$hr.Content).Contains($nonce)
    $healthVersion = [string]$health.version
} catch {
    $health = [ordered]@{ error = $_.Exception.Message }
}

Push-Location $workspace
try {
    $head = (git rev-parse HEAD).Trim()
    $branch = (git rev-parse --abbrev-ref HEAD).Trim()
    $srcTests = @(git status --short -- src tests)
    $todoYamlStatus = @(git status --short -- docs/Project/TODO.yaml docs/todo.yaml)
} finally {
    Pop-Location
}

$statusRaw = & pwsh.exe -NoProfile -NonInteractive -File $invoke -Command Status -WorkspacePath $workspace -PluginRoot $pluginRoot -CacheRoot $cacheRoot -TimeoutSeconds 60 2>&1 | Out-String
Save-Text '01-status.txt' $statusRaw | Out-Null

Save-Json '01-trust-git.json' ([ordered]@{
    timestampUtc = [datetime]::UtcNow.ToString('o')
    sessionId = $sessionId
    requestId = $requestId
    signatureOk = [bool]$sig
    nonce = $nonce
    nonceOk = $nonceOk
    healthVersion = $healthVersion
    health = $health
    head = $head
    branch = $branch
    srcTestsStatus = $srcTests
    todoYamlStatus = $todoYamlStatus
    pluginStatusExit = $LASTEXITCODE
    pluginStatusSnippet = if ($statusRaw.Length -gt 1500) { $statusRaw.Substring(0,1500) } else { $statusRaw }
}) | Out-Null

Write-Output ('SIG=' + [bool]$sig)
Write-Output ('NONCE_OK=' + $nonceOk)
Write-Output ('HEAD=' + $head)
Write-Output ('VERSION=' + $healthVersion)

# --- session lifecycle ---
$session = [ordered]@{ utc = $utc; sessionId = $sessionId; requestId = $requestId; steps = [ordered]@{} }
$session.steps.bootstrap = Invoke-PluginMethod 'workflow.sessionlog.bootstrap'
Write-Output ('BOOTSTRAP=' + $session.steps.bootstrap.exitCode)
$session.steps.openSession = Invoke-PluginMethod 'workflow.sessionlog.openSession' @"
agent: GrokCode
sessionId: $sessionId
title: Hostile H-apply H-done PLAN-TODOALIGN-001 store hygiene
model: grok-4.6-build
sourceType: GrokCode
"@
Write-Output ('OPEN=' + $session.steps.openSession.exitCode)
$session.steps.beginTurn = Invoke-PluginMethod 'workflow.sessionlog.beginTurn' @"
requestId: $requestId
queryTitle: Hostile H-apply H-done PLAN-TODOALIGN-001 store hygiene
queryText: Independently re-verify S4-S7 store hygiene claims for PLAN-TODOALIGN-001. Class 1. Do not implement product slices.
planFile: docs/plans/todo-requirements-audit.md
todoId: PLAN-TODOALIGN-001
"@
Write-Output ('BEGIN=' + $session.steps.beginTurn.exitCode + ' HAS503=' + $session.steps.beginTurn.has503)
Save-Json '02-session-begin.json' $session | Out-Null

# --- native MCP Streamable HTTP (todo_list / requirements_list / tools/list) ---
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
    $json = $payload | ConvertTo-Json -Depth 20 -Compress
    $req = [System.Net.Http.HttpRequestMessage]::new([System.Net.Http.HttpMethod]::Post, 'http://PAYTON-LEGION2:7147/mcp-transport')
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
                if ($trim.StartsWith('data:')) { [void]$dataLines.Add($trim.Substring(5).Trim()) }
            }
            $body = [string]::Join("`n", $dataLines)
        }
        return [pscustomobject]@{ Status = [int]$resp.StatusCode; Body = $body }
    }
    finally {
        $client.Dispose()
        $handler.Dispose()
        $req.Dispose()
    }
}

function Get-McpToolPayload {
    param($Rpc)
    $outer = $Rpc.Body | ConvertFrom-Json
    if ($null -eq $outer.result) { return $null }
    $content = $outer.result.content
    if ($content -and $content.Count -ge 1) {
        $text = [string]$content[0].text
        if ([string]::IsNullOrWhiteSpace($text)) { return $null }
        try { return ($text | ConvertFrom-Json) } catch { return $text }
    }
    return $outer.result
}

$init = Invoke-McpRpc -Method 'initialize' -Params @{
    protocolVersion = '2024-11-05'
    capabilities = @{}
    clientInfo = @{ name = 'hostile-validator'; version = '1.0' }
}
Save-Text '03-mcp-init.txt' $init.Body | Out-Null
try { [void](Invoke-McpRpc -Method 'notifications/initialized') } catch { }

$todoListRpc = Invoke-McpRpc -Method 'tools/call' -Params @{
    name = 'todo_list'
    arguments = @{ workspacePath = $workspace; done = $false }
}
Save-Text '03-todo-list-open.txt' $todoListRpc.Body | Out-Null
$todoList = Get-McpToolPayload $todoListRpc
$liveItems = @()
if ($todoList -and $todoList.PSObject.Properties.Name -contains 'items') {
    $liveItems = @($todoList.items)
} elseif ($todoList -is [System.Array]) {
    $liveItems = @($todoList)
}
Write-Output ('TODO_LIST_HTTP=' + $todoListRpc.Status)
Write-Output ('LIVE_OPEN=' + $liveItems.Count)

$inv = Get-Content -LiteralPath $invPath -Raw | ConvertFrom-Json
$s0Ids = @($inv.openTodoIds)
$byId = @{}
foreach ($t in $liveItems) {
    $id = [string]($t.Id)
    if (-not $id) { $id = [string]($t.id) }
    if ($id) { $byId[$id] = $t }
}

function Get-ItemField {
    param($Item, [string]$Name)
    if ($null -eq $Item) { return $null }
    $p = $Item.PSObject.Properties[$Name]
    if ($p) { return $p.Value }
    $alt = $Item.PSObject.Properties | Where-Object { $_.Name -ieq $Name } | Select-Object -First 1
    if ($alt) { return $alt.Value }
    return $null
}

$missingLive = @()
$missingDate = @()
$staleOnly = @()
$orphanBad = @()
$stillOpenDone = @()
$rows = @()
foreach ($id in $s0Ids) {
    $t = $byId[$id]
    if (-not $t) {
        $missingLive += $id
        $rows += [ordered]@{ id = $id; present = $false }
        continue
    }
    $done = [bool](Get-ItemField $t 'Done')
    $rem = [string](Get-ItemField $t 'Remaining')
    $fr = @(Get-ItemField $t 'FunctionalRequirements')
    $tr = @(Get-ItemField $t 'TechnicalRequirements')
    if ($done) { $stillOpenDone += $id }
    if ($rem -notmatch '2026-08-20T101500Z') { $missingDate += $id }
    if ($rem.Contains('2026-07-11') -and $rem -notmatch '2026-08-20') { $staleOnly += $id }
    $frEmpty = ($fr.Count -eq 0 -or ($fr.Count -eq 1 -and [string]$fr[0] -eq ''))
    $trEmpty = ($tr.Count -eq 0 -or ($tr.Count -eq 1 -and [string]$tr[0] -eq ''))
    if ($frEmpty -and $trEmpty -and $rem -notmatch 'OrphanReason') { $orphanBad += $id }
    $rows += [ordered]@{
        id = $id
        present = $true
        done = $done
        remainingHasAuditDate = ($rem -match '2026-08-20T101500Z')
        remainingHas0711 = $rem.Contains('2026-07-11')
        remainingPreview = if ($rem.Length -gt 280) { $rem.Substring(0,280) } else { $rem }
        functionalRequirements = @($fr | ForEach-Object { [string]$_ })
        technicalRequirements = @($tr | ForEach-Object { [string]$_ })
        priority = [string](Get-ItemField $t 'Priority')
        section = [string](Get-ItemField $t 'Section')
    }
}
$newOpenIds = @($liveItems | ForEach-Object {
    $id = [string](Get-ItemField $_ 'Id')
    if (-not $id) { $id = [string](Get-ItemField $_ 'id') }
    $id
} | Where-Object { $_ -and ($_ -notin $s0Ids) })

Save-Json '04-s0-remaining.json' ([ordered]@{
    timestampUtc = [datetime]::UtcNow.ToString('o')
    s0Count = $s0Ids.Count
    liveOpenCount = $liveItems.Count
    missingLive = $missingLive
    missingDate = $missingDate
    staleOnly = $staleOnly
    orphanBad = $orphanBad
    stillOpenDone = $stillOpenDone
    newOpenIds = $newOpenIds
    rows = $rows
}) | Out-Null
Write-Output ('MISSING_DATE=' + ($missingDate -join ','))
Write-Output ('STALE_ONLY=' + ($staleOnly -join ','))
Write-Output ('ORPHAN_BAD=' + ($orphanBad -join ','))
Write-Output ('NEW_OPEN=' + ($newOpenIds -join ','))
Write-Output ('MISSING_LIVE=' + ($missingLive -join ','))

# --- plugin todo.get spot IDs ---
$spotIds = @(
    'PLAN-TRIAGELEFTOVER-001',
    'PLAN-TODOALIGN-001',
    'PLAN-TODOAUDIT-001',
    'PLAN-DELETECOMPLIANCE-003',
    'PLAN-QUADBRAIN-E1-001',
    'PLAN-FILETOOLS-001',
    'BUG-TRIAGE-160',
    'BUG-TRIAGE-161',
    'BUG-TRIAGE-162',
    'BUG-TRIAGE-163',
    'MCP-HANDOFF-001',
    'MCP-HANDOFFPLAN-001',
    'MCP-HANDOFFREVIEW-001'
)
$spot = @()
foreach ($id in $spotIds) {
    $rawObj = Invoke-PluginMethod 'workflow.todo.get' "id: $id"
    Save-Text ("05-todo-get-$id.txt") $rawObj.stdout | Out-Null
    $text = [string]$rawObj.stdout
    $remaining = Get-YamlRemaining -Text $text
    $done = Get-YamlBool -Text $text -Key 'done'
    $fr = Get-YamlList -Text $text -Key 'functionalRequirements'
    $tr = Get-YamlList -Text $text -Key 'technicalRequirements'
    $spot += [ordered]@{
        id = $id
        exitCode = $rawObj.exitCode
        isError = $rawObj.isError
        done = $done
        remaining = $remaining
        remainingHasAuditDate = if ($remaining) { $remaining.Contains('2026-08-20T101500Z') } else { $null }
        remainingHas0711 = if ($remaining) { $remaining.Contains('2026-07-11') } else { $null }
        remainingHasP0 = if ($remaining) { $remaining -match '\bP0\b' } else { $null }
        remainingHas503 = if ($remaining) { $remaining.Contains('503') } else { $null }
        remainingHasRename = if ($remaining) { $remaining -match 'RenameQuadBrainRolesToCreativityLogic|rename migration' } else { $null }
        remainingHasRepoFileService = if ($remaining) { $remaining.Contains('RepoFileService') } else { $null }
        remainingHasReadFile = if ($remaining) { $remaining.Contains('read_file') } else { $null }
        functionalRequirements = $fr
        technicalRequirements = $tr
        frHasBracket = ($fr -contains '[]' -or ($fr -join ',') -match '\[\]')
    }
    Write-Output ('GET ' + $id + ' exit=' + $rawObj.exitCode + ' done=' + $done + ' date=' + ($remaining -and $remaining.Contains('2026-08-20T101500Z')))
}
Save-Json '05-spot-gets.json' ([ordered]@{ timestampUtc = [datetime]::UtcNow.ToString('o'); items = $spot }) | Out-Null

# leftover native get via MCP too
$leftoverRpc = Invoke-McpRpc -Method 'tools/call' -Params @{
    name = 'todo_get'
    arguments = @{ workspacePath = $workspace; id = 'PLAN-TRIAGELEFTOVER-001' }
}
Save-Text '05-native-leftover.txt' $leftoverRpc.Body | Out-Null
$leftoverNative = Get-McpToolPayload $leftoverRpc

$alignRpc = Invoke-McpRpc -Method 'tools/call' -Params @{
    name = 'todo_get'
    arguments = @{ workspacePath = $workspace; id = 'PLAN-TODOALIGN-001' }
}
$alignNative = Get-McpToolPayload $alignRpc

Save-Json '05-native-leftover-align.json' ([ordered]@{
    leftoverHttp = $leftoverRpc.Status
    leftoverDone = [bool](Get-ItemField $leftoverNative 'Done')
    leftoverId = [string](Get-ItemField $leftoverNative 'Id')
    alignHttp = $alignRpc.Status
    alignDone = [bool](Get-ItemField $alignNative 'Done')
    alignId = [string](Get-ItemField $alignNative 'Id')
    leftoverPreview = if ($leftoverRpc.Body.Length -gt 800) { $leftoverRpc.Body.Substring(0,800) } else { $leftoverRpc.Body }
    alignPreview = if ($alignRpc.Body.Length -gt 800) { $alignRpc.Body.Substring(0,800) } else { $alignRpc.Body }
}) | Out-Null
Write-Output ('LEFTOVER_NATIVE_DONE=' + [bool](Get-ItemField $leftoverNative 'Done'))
Write-Output ('ALIGN_NATIVE_DONE=' + [bool](Get-ItemField $alignNative 'Done'))

# --- requirements ---
$frInProg = Invoke-PluginMethod 'workflow.requirements.listFr' "status: in_progress" 180
Save-Text '06-listFr-inprogress.txt' $frInProg.stdout | Out-Null
$handoffFrIds = @('FR-HANDOFF-001','FR-HANDOFF-002','FR-HANDOFF-003','FR-HANDOFF-004','FR-HANDOFF-005','FR-HANDOFF-006','FR-HANDOFF-007')
$frGets = @()
foreach ($id in $handoffFrIds) {
    $g = Invoke-PluginMethod 'workflow.requirements.getFr' "id: $id"
    Save-Text ("06-getFr-$id.txt") $g.stdout | Out-Null
    $frGets += [ordered]@{
        id = $id
        exitCode = $g.exitCode
        isError = $g.isError
        status = Get-YamlScalar -Text $g.stdout -Key 'status'
        isSatisfiedMention = ($g.stdout -match 'isSatisfied:\s*true')
        completed = ($g.stdout -match '(?im)^\s+status:\s*completed')
    }
    Write-Output ('FR ' + $id + ' status=' + (Get-YamlScalar -Text $g.stdout -Key 'status'))
}
Save-Json '06-handoff-fr.json' ([ordered]@{
    listFrInProgressExit = $frInProg.exitCode
    inProgressIds = @([regex]::Matches($frInProg.stdout, 'FR-HANDOFF-\d{3}') | ForEach-Object { $_.Value } | Select-Object -Unique)
    gets = $frGets
}) | Out-Null

$trIds = @('[]','TR-02','TR-03','TR-04','TR-05','TR-06','TR-07','TR-08','TR-09','TR-10','TR-11','TR-12','TR-13','TR-14')
$trGets = @()
foreach ($id in $trIds) {
    $g = Invoke-PluginMethod 'workflow.requirements.getTr' "id: $id"
    $safe = ($id -replace '[^A-Za-z0-9\-]', '_')
    Save-Text ("07-getTr-$safe.txt") $g.stdout | Out-Null
    $desc = Get-YamlRemaining -Text $g.stdout
    if (-not $desc) {
        $dm = [regex]::Match($g.stdout, '(?ims)^\s+(description|notes|body):\s*(.+?)(?=\r?\n\s+\w+:|\r?\n\s+deprecated:|\z)')
        if ($dm.Success) { $desc = $dm.Groups[2].Value.Trim() }
    }
    $blob = $g.stdout
    $trGets += [ordered]@{
        id = $id
        exitCode = $g.exitCode
        isError = $g.isError
        errorCode = Get-YamlScalar -Text $g.stdout -Key 'code'
        errorMessage = Get-YamlScalar -Text $g.stdout -Key 'message'
        status = Get-YamlScalar -Text $g.stdout -Key 'status'
        hasAlignNote = ($blob -match 'PLAN-TODOALIGN-001')
        snippet = if ($blob.Length -gt 500) { $blob.Substring(0,500) } else { $blob }
    }
    Write-Output ('TR ' + $id + ' exit=' + $g.exitCode + ' status=' + (Get-YamlScalar -Text $g.stdout -Key 'status') + ' note=' + ($blob -match 'PLAN-TODOALIGN-001'))
}
Save-Json '07-placeholder-tr.json' ([ordered]@{ timestampUtc = [datetime]::UtcNow.ToString('o'); items = $trGets }) | Out-Null

# native requirements_list for TR-02 body if plugin get fails
$trListRpc = Invoke-McpRpc -Method 'tools/call' -Params @{
    name = 'requirements_list'
    arguments = @{ workspacePath = $workspace; type = 'tr' }
}
$trList = Get-McpToolPayload $trListRpc
$trNativeHits = @()
if ($trList) {
    $trItems = @()
    if ($trList.PSObject.Properties.Name -contains 'items') { $trItems = @($trList.items) }
    elseif ($trList -is [System.Array]) { $trItems = @($trList) }
    foreach ($want in $trIds) {
        $hit = $trItems | Where-Object {
            $tid = [string](Get-ItemField $_ 'Id')
            if (-not $tid) { $tid = [string](Get-ItemField $_ 'id') }
            $tid -eq $want
        } | Select-Object -First 1
        if ($hit) {
            $body = [string](Get-ItemField $hit 'Body')
            if (-not $body) { $body = [string](Get-ItemField $hit 'Description') }
            if (-not $body) { $body = [string](Get-ItemField $hit 'Notes') }
            $trNativeHits += [ordered]@{
                id = $want
                status = [string](Get-ItemField $hit 'Status')
                hasAlignNote = ($body -match 'PLAN-TODOALIGN-001')
                bodyPreview = if ($body.Length -gt 240) { $body.Substring(0,240) } else { $body }
            }
        } else {
            $trNativeHits += [ordered]@{ id = $want; missing = $true }
        }
    }
    Write-Output ('TR_LIST_COUNT=' + $trItems.Count)
}
Save-Json '07-native-tr-placeholders.json' ([ordered]@{
    http = $trListRpc.Status
    hits = $trNativeHits
}) | Out-Null

$frListRpc = Invoke-McpRpc -Method 'tools/call' -Params @{
    name = 'requirements_list'
    arguments = @{ workspacePath = $workspace; type = 'fr' }
}
$frList = Get-McpToolPayload $frListRpc
$frNativeHandoff = @()
$completedHandoff = @()
if ($frList) {
    $frItems = @()
    if ($frList.PSObject.Properties.Name -contains 'items') { $frItems = @($frList.items) }
    elseif ($frList -is [System.Array]) { $frItems = @($frList) }
    foreach ($want in $handoffFrIds) {
        $hit = $frItems | Where-Object {
            $fid = [string](Get-ItemField $_ 'Id')
            if (-not $fid) { $fid = [string](Get-ItemField $_ 'id') }
            $fid -eq $want
        } | Select-Object -First 1
        $st = if ($hit) { [string](Get-ItemField $hit 'Status') } else { $null }
        $frNativeHandoff += [ordered]@{ id = $want; present = [bool]$hit; status = $st }
        if ($st -eq 'completed') { $completedHandoff += $want }
    }
}
Save-Json '06-native-handoff-fr.json' ([ordered]@{
    http = $frListRpc.Status
    items = $frNativeHandoff
    completedHandoff = $completedHandoff
}) | Out-Null
Write-Output ('HANDOFF_COMPLETED=' + ($completedHandoff -join ','))

# tools/list file names
$toolsRpc = Invoke-McpRpc -Method 'tools/list'
$toolNames = @([regex]::Matches($toolsRpc.Body, '"name"\s*:\s*"([^"]+)"') | ForEach-Object { $_.Groups[1].Value } | Select-Object -Unique)
Save-Json '08-tools-list.json' ([ordered]@{
    http = $toolsRpc.Status
    count = $toolNames.Count
    read_file = ($toolNames -contains 'read_file')
    list_dir = ($toolNames -contains 'list_dir')
    grep_files = ($toolNames -contains 'grep_files')
}) | Out-Null
Write-Output ('READ_FILE_TOOL=' + ($toolNames -contains 'read_file'))

# --- generated markdown timestamps ---
$mdNames = @(
    'Functional-Requirements.md',
    'Technical-Requirements.md',
    'Testing-Requirements.md',
    'TR-per-FR-Mapping.md',
    'Requirements-Matrix.md'
)
$mdInfo = @()
foreach ($n in $mdNames) {
    $p = Join-Path $workspace ('docs\Project\' + $n)
    $item = Get-Item -LiteralPath $p
    $text = Get-Content -LiteralPath $p -Raw
    $mdInfo += [ordered]@{
        name = $n
        lastWriteTimeUtc = $item.LastWriteTimeUtc.ToString('o')
        length = $item.Length
        hasAlignNote = $text.Contains('PLAN-TODOALIGN-001')
        alignNoteCount = ([regex]::Matches($text, 'PLAN-TODOALIGN-001')).Count
    }
}
Save-Json '09-markdown.json' ([ordered]@{ files = $mdInfo; claimedGenerateUtc = '2026-08-20T12:50:45Z' }) | Out-Null
foreach ($m in $mdInfo) { Write-Output ('MD ' + $m.name + ' ' + $m.lastWriteTimeUtc + ' align=' + $m.hasAlignNote) }

# --- python / honesty extras ---
$pyScripts = @(Get-ChildItem -LiteralPath $scratch -Filter '*.py' -ErrorAction SilentlyContinue)
Save-Json '10-scratch-python.json' ([ordered]@{
    pythonFiles = @($pyScripts | ForEach-Object { $_.Name })
    s4VerifyUsesSessionDump = $true
    s4VerifyDumpPath = 'C:\Users\kingd\.grok\sessions\F%3A%5CGitHub%5CMcpServer\01a01290-749a-7271-8c76-d04be7e683d7\mcp\call-4212431f-c9a3-48a0-a24e-b2d5b9fdf463-214.json'
}) | Out-Null

Write-Output 'LIVE_VERIFY_CORE_DONE'
