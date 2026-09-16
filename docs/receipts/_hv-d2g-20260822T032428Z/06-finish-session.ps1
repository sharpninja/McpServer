#Requires -Version 7.0
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$workspace = 'F:\GitHub\McpServer'
$outDir = 'F:\GitHub\McpServer\docs\receipts\_hv-d2g-20260822T032428Z'
$baseUrl = 'http://PAYTON-LEGION2:7147'
$ids = Get-Content -LiteralPath (Join-Path $outDir 'ids.json') -Raw | ConvertFrom-Json
$agent = [string]$ids.agent
$sessionId = [string]$ids.sessionId
$requestId = [string]$ids.requestId
$now = [DateTime]::UtcNow.ToString('o')

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
    $json = $payload | ConvertTo-Json -Depth 40 -Compress
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
    $Result.Body | Set-Content -LiteralPath (Join-Path $outDir $Name) -Encoding utf8
    Write-Output ('SAVED ' + $Name + ' HTTP=' + $Result.Status)
}

$init = Invoke-McpRpc -Method 'initialize' -Params @{
    protocolVersion = '2025-03-26'
    capabilities = @{}
    clientInfo = @{ name = 'GrokSubagentHostile-d2g'; version = '1.0.0' }
}
Save-Body -Name 'mcp-init-finish.json' -Result $init
[void](Invoke-McpRpc -Method 'notifications/initialized' -Params @{})

$sectionArgs = {
    param([string]$Section, $Dto)
    @{
        agent = $agent
        sessionId = $sessionId
        requestId = $requestId
        workspacePath = $workspace
        section = $Section
        sectionJson = ($Dto | ConvertTo-Json -Depth 20 -Compress)
    }
}

$actions = [ordered]@{
    requestId = $requestId
    actions = @(
        [ordered]@{ order = 1; type = 'create'; status = 'completed'; description = 'add-profile: read 18 non-skill profile markdown files'; filePath = 'C:\Users\kingd\.claude\profile' }
        [ordered]@{ order = 2; type = 'create'; status = 'completed'; description = 'Native MCP sessionlog_open/begin_turn for D2-green review'; filePath = 'docs/receipts/_hv-d2g-20260822T032428Z/session-begin.json' }
        [ordered]@{ order = 3; type = 'create'; status = 'completed'; description = 'Re-ran Pester PluginHandoffSkill.Tests.ps1 -CI Passed 1 Failed 0 Skipped 0'; filePath = 'docs/receipts/_hv-d2g-20260822T032428Z/pester-CI-testResults.xml' }
        [ordered]@{ order = 4; type = 'create'; status = 'completed'; description = 'Re-ran named lease/provenance tests Passed 2 Failed 0 Skipped 0'; filePath = 'docs/receipts/_hv-d2g-20260822T032428Z/d2g-handoff-csharp.trx' }
        [ordered]@{ order = 5; type = 'create'; status = 'completed'; description = 'Re-ran HandoffDurabilityTests class Passed 24 Failed 0 Skipped 0'; filePath = 'docs/receipts/_hv-d2g-20260822T032428Z/d2g-handoff-durability.trx' }
        [ordered]@{ order = 6; type = 'create'; status = 'completed'; description = 'Re-ran Sqlite and PostgreSQL HandoffIngestionStorageMigrationTests; SQL Server residual timeout'; filePath = 'docs/receipts/_hv-d2g-20260822T032428Z/d2g-handoff-migration-sqlite.trx' }
        [ordered]@{ order = 7; type = 'design_decision'; status = 'completed'; description = 'OverallVerdict AGREE on D2-green remaining-gap tests. Did not mark PLAN or MCP-HANDOFF done.'; filePath = 'docs/receipts/hostile-validator-20260822T033842Z.md' }
        [ordered]@{ order = 8; type = 'create'; status = 'completed'; description = 'Wrote hostile validator receipt md+json'; filePath = 'docs/receipts/hostile-validator-20260822T033842Z.md' }
    )
}
$act = Invoke-McpTool -Name 'sessionlog_replace_section' -Arguments (& $sectionArgs 'actions' $actions)
Save-Body -Name 'sec-actions.json' -Result $act

$dialog = [ordered]@{
    requestId = $requestId
    processingDialog = @(
        [ordered]@{ timestamp = $now; role = 'model'; category = 'observation'; content = 'add-profile executed. 18 non-skill profile files read. Marker signature True. Health nonce 53b064f5df1c4e12bd5b55a2a07a6dec echoed. D1.5 AGREE file exists at 2026-08-22T01:30:32Z. invoke.ps1 LastWriteTimeUtc 2026-08-22T02:28:44Z is after that AGREE.' }
        [ordered]@{ timestamp = $now; role = 'model'; category = 'observation'; content = 'Re-runs: Pester 1/0/0; named C# 2/0/0; durability 24/0/0; PluginSync+reject 2/0/0; Sqlite 1/0/0; PostgreSQL 1/0/0. SHA256 invoke.ps1 core=grok. PLAN Done=false. MCP-HANDOFF* Done=false. D2/D3/D4/D5 tasks Done=false.' }
        [ordered]@{ timestamp = $now; role = 'model'; category = 'decision'; content = 'Decision: OverallVerdict AGREE. Rationale: every applicable A/B/C/D claim re-verified PASS. SQL Server timeout is competing LocalDB residual per brief, not a D2 schema defect. Alternatives rejected: DISAGREE solely because SQL Server timed out; FAIL isSatisfied=false (that is D5). Consequence: parent may treat D2 remaining-gap tests as green; must not mark PLAN or MCP-HANDOFF done; D3/D4/D5 remain.' }
        [ordered]@{ timestamp = $now; role = 'model'; category = 'decision'; content = 'Decision: do not call todo_update. Rationale: hostile-on-goal-state forbids this reviewer from flipping PLAN/MCP-HANDOFF, and the brief forbids marking those done. Alternative rejected: marking D2 ImplementationTask done from this review.' }
        [ordered]@{ timestamp = $now; role = 'model'; category = 'decision'; content = 'Decision: remaining-gap Pester hermetic MCP_PLUGIN_REPL_LOG seam is enough for TEST-HANDOFF-006 remaining skill-file invoke. Rationale: plan forbids live 7147 in unit tests; invoke.ps1 live path still calls repl-invoke.ps1. Alternative rejected: FAIL because the test does not hit live workflow.handoff against :7147.' }
    )
}
$dlg = Invoke-McpTool -Name 'sessionlog_replace_section' -Arguments (& $sectionArgs 'dialog' $dialog)
Save-Body -Name 'sec-dialog.json' -Result $dlg

$decisions = [ordered]@{
    requestId = $requestId
    designDecisions = @(
        'AGREE D2-green remaining-gap tests after independent re-run. Do not treat D4 full suite or D5 AC isSatisfied as this gate.',
        'SQL Server Execution Timeout under competing testhosts is residual LocalDB, not FAIL, because Sqlite and PostgreSQL apply/round-trip/down-up passed and no new Handoff migration exists.',
        'Do not mark PLAN-PLUGINHANDOFF-001 or MCP-HANDOFF* done. D3 wiki export was not implemented in this review.'
    )
}
$dec = Invoke-McpTool -Name 'sessionlog_replace_section' -Arguments (& $sectionArgs 'designDecisions' $decisions)
Save-Body -Name 'sec-decisions.json' -Result $dec

$files = [ordered]@{
    requestId = $requestId
    filesModified = @(
        'docs/receipts/hostile-validator-20260822T033842Z.md',
        'docs/receipts/hostile-validator-20260822T033842Z.json'
    )
}
$fm = Invoke-McpTool -Name 'sessionlog_replace_section' -Arguments (& $sectionArgs 'filesModified' $files)
Save-Body -Name 'sec-files.json' -Result $fm

$tags = [ordered]@{
    requestId = $requestId
    tags = @(
        'hostile-validator',
        'D2-green',
        'PLAN-PLUGINHANDOFF-001',
        'TEST-HANDOFF-006',
        'TEST-HANDOFF-007',
        'TEST-HANDOFF-003',
        'AGREE'
    )
}
$tg = Invoke-McpTool -Name 'sessionlog_replace_section' -Arguments (& $sectionArgs 'tags' $tags)
Save-Body -Name 'sec-tags.json' -Result $tg

$ctx = [ordered]@{
    requestId = $requestId
    contextList = @(
        'docs/plans/PLAN-PLUGINHANDOFF-001.md',
        'docs/receipts/hostile-validator-20260822T013032Z.md',
        'docs/receipts/d2-implement-20260822T022012Z.md',
        'plugins/core/skills/handoff/invoke.ps1',
        'src/McpServer.Services/Services/HandoffIngestionService.cs'
    )
}
$cx = Invoke-McpTool -Name 'sessionlog_replace_section' -Arguments (& $sectionArgs 'context' $ctx)
Save-Body -Name 'sec-context.json' -Result $cx

$turn = [ordered]@{
    requestId = $requestId
    queryTitle = 'Hostile D2-green remaining-gap tests PLAN-PLUGINHANDOFF-001'
    interpretation = 'Independent D2-green hostile review after D1.5 AGREE. Re-run remaining-gap tests. Do not mark PLAN or MCP-HANDOFF done. Do not implement D3 wiki export.'
    response = 'OverallVerdict AGREE. Remaining-gap tests Failed 0 Skipped 0 (Pester 1, named C# 2, durability 24). invoke.ps1 SHA256 matches official grok copy. PLAN and MCP-HANDOFF* remain done false. Receipt docs/receipts/hostile-validator-20260822T033842Z.md'
    status = 'completed'
    planFile = 'docs/plans/PLAN-PLUGINHANDOFF-001.md'
    todoId = 'PLAN-PLUGINHANDOFF-001'
}
$complete = Invoke-McpTool -Name 'sessionlog_complete_turn' -Arguments @{
    agent = $agent
    sessionId = $sessionId
    requestId = $requestId
    workspacePath = $workspace
    turnJson = ($turn | ConvertTo-Json -Depth 10 -Compress)
}
Save-Body -Name 'session-complete.json' -Result $complete

$query = Invoke-McpTool -Name 'sessionlog_query' -Arguments @{
    workspacePath = $workspace
    agent = $agent
    text = $sessionId
    from = '2026-08-22T03:20:00Z'
    limit = 5
}
Save-Body -Name 'session-query.json' -Result $query

Write-Output 'FINISH_DONE'
