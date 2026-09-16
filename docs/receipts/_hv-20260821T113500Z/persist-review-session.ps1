#Requires -Version 7.0
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$workspace = 'F:\GitHub\McpServer'
$outDir = 'F:\GitHub\McpServer\docs\receipts\_hv-20260821T113500Z'
$baseUrl = 'http://PAYTON-LEGION2:7147'
$sessionId = 'GrokCode-20260821T114530Z-hostile-add-profile'
$requestId = 'req-20260821T114530Z-001-hostile-validate-add-profile'
$title = 'Hostile validate add-profile new session'
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
    $Result.Body | Set-Content -LiteralPath (Join-Path $outDir $Name) -Encoding utf8
    Write-Output ("SAVED $Name HTTP=$($Result.Status) LEN=$($Result.Body.Length)")
}

[void](Invoke-McpRpc -Method 'initialize' -Params @{ protocolVersion = '2025-03-26'; capabilities = @{}; clientInfo = @{ name = 'hostile-validator-add-profile'; version = '1.0.0' } })
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
    queryText = 'Hostile validator: attack implementer claims for /add-profile then new MCP session. Class 2 ops. No plan-step done claim.'
}
Save-Body -Name 'self-begin.json' -Result $begin

function Invoke-ReplaceSection {
    param([string]$Section, $Payload, [string]$File)
    $sectionJson = ($Payload | ConvertTo-Json -Depth 20 -Compress)
    $r = Invoke-McpTool -Name 'sessionlog_replace_section' -Arguments @{
        agent = 'GrokCode'
        sessionId = $sessionId
        requestId = $requestId
        section = $Section
        sectionJson = $sectionJson
        workspacePath = $workspace
    }
    Save-Body -Name $File -Result $r
}

Invoke-ReplaceSection -Section 'tags' -File 'self-sec-tags.json' -Payload ([ordered]@{
    tags = @('hostile-review', 'class-2', 'add-profile', 'GrokCode', 'session-bootstrap')
})

Invoke-ReplaceSection -Section 'designDecisions' -File 'self-sec-decisions.json' -Payload ([ordered]@{
    designDecisions = @(
        'Judgment: classify this review as class 2 (user-directed general action). Consequence: surface C and D are N/A; Byrd v4 is not scored. Alternative rejected: treat add-profile as project implementation.',
        'Judgment: OverallVerdict DISAGREE because A6 (session-state.yaml title/sessionId) and B1 (accuracy-first cache attribution) FAIL. Consequence: parent must not treat the bootstrap as fully verified cache-state. Alternative rejected: AGREE on current-turn.yaml alone.',
        'Judgment: do not call plugin workflow.sessionlog.beginTurn. Consequence: implementer in_progress turn on current-turn.yaml is not superseded. Alternative rejected: plugin beginTurn which would cancel req-20260821T113258Z-001-add-profile-new-session.'
    )
})

$actions = @(
    [ordered]@{ order = 1; description = 'add-profile: read 18 non-skill profile markdown files'; type = 'read'; status = 'completed'; filePath = 'C:\Users\kingd\.claude\profile' }
    [ordered]@{ order = 2; description = 'Test-MarkerSignature returned true'; type = 'read'; status = 'completed'; filePath = 'F:\GitHub\McpServer\AGENTS-README-FIRST.yaml' }
    [ordered]@{ order = 3; description = 'Health nonce nonce-1bcd467b9def481c859e97e863fae9d1 echoed; status Healthy'; type = 'read'; status = 'completed'; filePath = 'http://PAYTON-LEGION2:7147/health' }
    [ordered]@{ order = 4; description = 'client.Tools.SearchAsync exact name mcpserver-grok-plugin; .version 1.97.0; git pull --ff-only exit 0'; type = 'read'; status = 'completed'; filePath = 'F:\GitHub\mcpserver-grok-plugin\.version' }
    [ordered]@{ order = 5; description = 'sessionlog_query proven sessions 113141 and 112802; todo_list 36 open; memories LAB-001 and LAB-002'; type = 'read'; status = 'completed'; filePath = 'docs/receipts/_hv-20260821T113500Z' }
    [ordered]@{ order = 6; description = 'session-state.yaml lacks claimed title and currently has sessionId GrokCode-20260821T113501Z-plugin-session'; type = 'read'; status = 'completed'; filePath = 'F:\GitHub\McpServer\.mcpServer\grok\session-state.yaml' }
    [ordered]@{ order = 7; description = 'Wrote hostile receipt pair OverallVerdict DISAGREE'; type = 'create'; status = 'completed'; filePath = 'docs/receipts/hostile-validator-20260821T114530Z.md' }
    [ordered]@{ order = 8; description = 'Decision: DISAGREE on A6+B1; do not AGREE from current-turn.yaml alone'; type = 'design_decision'; status = 'completed'; filePath = 'docs/receipts/hostile-validator-20260821T114530Z.md' }
)
Invoke-ReplaceSection -Section 'actions' -File 'self-sec-actions.json' -Payload ([ordered]@{ actions = $actions })

Invoke-ReplaceSection -Section 'context' -File 'self-sec-context.json' -Payload ([ordered]@{
    contextList = @(
        'AGENTS-README-FIRST.yaml',
        'C:\Users\kingd\.claude\profile\PROFILE.md',
        'C:\Users\kingd\.claude\skills\add-profile\SKILL.md',
        'F:\GitHub\mcpserver-grok-plugin\.version',
        'F:\GitHub\McpServer\.mcpServer\grok\session-state.yaml',
        'F:\GitHub\McpServer\.mcpServer\grok\current-turn.yaml',
        'docs/receipts/hostile-validator-20260821T114530Z.md'
    )
})

$now = [datetime]::UtcNow.ToString('o')
$dialog = @(
    [ordered]@{ timestamp = $now; role = 'model'; content = 'add-profile executed first. 18 non-skill profile markdown files read. Excluded add-profile.grok.md.'; category = 'observation' }
    [ordered]@{ timestamp = $now; role = 'model'; content = 'Classified class 2 user-directed general action. Surface C N/A. Surface D N/A. Native MCP queries used so plugin beginTurn would not supersede the implementer turn.'; category = 'observation' }
    [ordered]@{ timestamp = $now; role = 'model'; content = 'Live re-check: Test-MarkerSignature true; health nonce echoed; plugin .version 1.97.0; todo_list 36 open; Status pendingCount 53 failsafe 53 quarantine 48; memories LAB-001 and LAB-002.'; category = 'observation' }
    [ordered]@{ timestamp = $now; role = 'model'; content = 'session-state.yaml at review: status verified, sessionId GrokCode-20260821T113501Z-plugin-session, no title key. current-turn.yaml still has GrokCode-20260821T113141Z-plugin-session and queryTitle Load operator profile and start new session.'; category = 'observation' }
    [ordered]@{ timestamp = $now; role = 'model'; content = 'Decision: OverallVerdict DISAGREE. A6 FAIL (session-state title/sessionId). B1 FAIL (cache attribution). C and D N/A PASS. Alternative rejected: AGREE because most MCP facts matched.'; category = 'decision' }
)
Invoke-ReplaceSection -Section 'dialog' -File 'self-sec-dialog.json' -Payload ([ordered]@{ processingDialog = $dialog })

$completePayload = [ordered]@{
    response = 'Hostile review DISAGREE. A6 FAIL: session-state.yaml lacks claimed title and currently has a later hook sessionId. B1 FAIL: title was on current-turn.yaml and the server session, not session-state.yaml. Receipt: docs/receipts/hostile-validator-20260821T114530Z.md'
    interpretation = 'Operator directed an independent hostile validation of the add-profile plus new-session bootstrap claims.'
    status = 'completed'
}
$completeJson = $completePayload | ConvertTo-Json -Depth 10 -Compress
$complete = Invoke-McpTool -Name 'sessionlog_complete_turn' -Arguments @{
    agent = 'GrokCode'
    sessionId = $sessionId
    requestId = $requestId
    workspacePath = $workspace
    turnJson = $completeJson
}
Save-Body -Name 'self-complete.json' -Result $complete

$proof = Invoke-McpTool -Name 'sessionlog_query' -Arguments @{
    workspacePath = $workspace
    agent = 'GrokCode'
    text = $sessionId
    limit = 5
}
Save-Body -Name 'q-hostile-self.json' -Result $proof

$ct = Get-Content -LiteralPath 'F:\GitHub\McpServer\.mcpServer\grok\current-turn.yaml' -Raw
Set-Content -LiteralPath (Join-Path $outDir 'current-turn-after-persist.yaml') -Value $ct -Encoding utf8
Write-Output 'PERSIST_OK'
