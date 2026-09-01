#Requires -Version 7.0
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$workspace = 'F:\GitHub\McpServer'
$baseUrl = 'http://PAYTON-LEGION2:7147'
$outDir = Join-Path $workspace 'docs\receipts\_hv-234800Z'
$sessionId = 'GrokCode-20260818T234800Z-hostile-hgreen'
$requestId = 'req-20260818T234800Z-001-late-hgreen-s1s8'
$script:McpSessionHeader = $null
$script:McpId = 0
$headerPath = Join-Path $outDir 'mcp-session-header.txt'
if (Test-Path -LiteralPath $headerPath) {
    $script:McpSessionHeader = (Get-Content -LiteralPath $headerPath -Raw).Trim()
}

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
    if ($outer.PSObject.Properties.Name -contains 'error' -and $null -ne $outer.error) {
        throw ('MCP RPC error: ' + ($outer.error | ConvertTo-Json -Compress -Depth 8))
    }
    $text = [string]$outer.result.content[0].text
    return ($text | ConvertFrom-Json)
}

$init = Invoke-McpRpc -Method 'initialize' -Params @{
    protocolVersion = '2025-03-26'
    capabilities = @{}
    clientInfo = @{ name = 'hostile-validator-hgreen-234800-complete'; version = '1.0.0' }
}
[void](Invoke-McpRpc -Method 'notifications/initialized' -Params @{})
Write-Output ('INIT_HTTP=' + $init.Status)

$now = [datetime]::UtcNow.ToString('o')
$dialogItems = @(
    [ordered]@{
        timestamp = $now
        role = 'model'
        content = 'add-profile executed first. 18 non-skill profile markdown files read under C:\Users\kingd\.claude\profile. Excluded add-profile.grok.md.'
        category = 'observation'
    },
    [ordered]@{
        timestamp = $now
        role = 'model'
        content = 'Classified class 1 late H-green implementation-phase for S1-S8 of docs/plans/triage-cluster-001.md. Surfaces A+B+C+D apply. H-red AGREE exists at docs/receipts/hostile-validator-20260818T233800Z.md. Do not FAIL B1 for missing H-red. Do not FAIL B2 from FR createdAt versus file times. Implementer does not claim 16 TODOs done or live deploy.'
        category = 'observation'
    },
    [ordered]@{
        timestamp = $now
        role = 'model'
        content = 'Independent reruns this review: REST 4/0/0, tool envelope 3/0/0, tool backend 2/0/0, Repl classifier 10/0/0, classifier 5/0/0, schema 4/0/0, hung SaveChanges 1/0/0, budget 2/0/0, triage unreachable 1/0/0, store 7/0/0, Pester 9/0/0, Build cache 2/0/0, EXEC/HELP 4/0/0, REQ TR 2/0/0. All EXIT=0. Marker signature true. Health nonce match. Live host remains 1.4.26. Scratch s2-tests.log exists length 6105. All 16 BUG-TRIAGE ids and PLAN-TRIAGECLUSTER-001 Done=false.'
        category = 'observation'
    },
    [ordered]@{
        timestamp = $now
        role = 'model'
        content = @'
Decision: OverallVerdict AGREE for this late H-green.
Rationale: H-red 233800Z already AGREE (PASS 27 FAIL 0). This review independently re-ran the named implementation filters and found product wires for McpErrorClassifier, SessionLogController.ClassifiedError, McpToolErrors.Serialize, ReplMcpErrorClassifier/AgentStdioProtocol, SessionLogSchemaGuard plus text filter, StorageCommandBudget 5s, hung SaveChanges, session-store AC, Pester 9/0/0, and ReplacePluginCache 2/0/0.
Alternatives rejected: DISAGREE because ReplMcpErrorClassifier duplicates mapping instead of referencing McpErrorClassifier (rejected: brief names ReplMcpErrorClassifier as the REPL surface; Repl.Core cannot take a Support.Mcp reference; AgentStdioProtocol emits the four-field envelope and Repl tests 10/0/0). DISAGREE because full ./build.ps1 Test was not rerun (rejected: brief allows named filters when disk is tight; F: free was 1.3 GB then 0.94 GB). FAIL B2 from FR createdAt versus file times (rejected: locked late-review rule). FAIL because TEST status is still pending (rejected: this is implementation H-green, not S10).
Consequence: parent may proceed to later slices or H-done only after those gates. Do not mark PLAN-TRIAGECLUSTER-001 or the 16 BUG-TRIAGE ids done. Do not treat live 1.4.26 as this gate.
Affected: FR-MCP-TRIAGEERR-001, FR-MCP-TRIAGESTORE-001/002, FR-MCP-TRIAGESCHEMA-001, FR-MCP-TRIAGEPLUGIN-001, FR-MCP-TRIAGETODO-001, FR-MCP-TRIAGEREQ-001, FR-MCP-TRIAGEHELP-001.
'@
        category = 'decision'
    }
)
$dialog = Invoke-McpTool -Name 'sessionlog_dialog' -Arguments @{
    agent = 'GrokCode'
    sessionId = $sessionId
    requestId = $requestId
    itemsJson = ($dialogItems | ConvertTo-Json -Depth 8 -Compress)
    workspacePath = $workspace
}
Save-Body -Name 'mcp-dialog.json' -Result $dialog
Write-Output ('DIALOG=' + ((Get-ToolObject -Result $dialog) | ConvertTo-Json -Depth 6 -Compress))

$actions = @(
    [ordered]@{ order = 1; description = 'add-profile: read 18 non-skill profile markdown files'; type = 'edit'; status = 'completed'; filePath = 'C:\Users\kingd\.claude\profile' }
    [ordered]@{ order = 2; description = 'Test-MarkerSignature True; health nonce echoed; host 1.4.26'; type = 'edit'; status = 'completed'; filePath = 'F:\GitHub\McpServer\AGENTS-README-FIRST.yaml' }
    [ordered]@{ order = 3; description = 'Independent named C# filters all Failed 0 Skipped 0; Pester 9/0/0; Build cache 2/0/0'; type = 'edit'; status = 'completed'; filePath = 'docs/receipts/_hv-234800Z/test-summary.json' }
    [ordered]@{ order = 4; description = 'todo_get all 16 BUG-TRIAGE plus PLAN-TRIAGECLUSTER-001 remain Done=false'; type = 'edit'; status = 'completed'; filePath = 'docs/receipts/_hv-234800Z/todos.json' }
    [ordered]@{ order = 5; description = 'requirements_list TEST AC and FR mappings store-verified'; type = 'edit'; status = 'completed'; filePath = 'docs/receipts/_hv-234800Z/test-ac.json' }
    [ordered]@{ order = 6; description = 'H-red AGREE file exists OverallVerdict AGREE PASS 27 FAIL 0'; type = 'edit'; status = 'completed'; filePath = 'docs/receipts/hostile-validator-20260818T233800Z.md' }
    [ordered]@{ order = 7; description = 'Scratch implementer s2-tests.log exists length 6105'; type = 'edit'; status = 'completed'; filePath = 'C:\Users\kingd\AppData\Local\Temp\grok-goal-01353e344a72\implementer\s2-tests.log' }
    [ordered]@{ order = 8; description = 'Wrote hostile H-green receipt pair'; type = 'create'; status = 'completed'; filePath = 'docs/receipts/hostile-validator-20260818T234800Z.md' }
    [ordered]@{ order = 9; description = 'Decision: late H-green AGREE for S1-S8 implementation; do not mark 16 TODOs done'; type = 'design_decision'; status = 'completed'; filePath = 'docs/plans/triage-cluster-001.md' }
)
$repl = Invoke-McpTool -Name 'sessionlog_replace_section' -Arguments @{
    agent = 'GrokCode'
    sessionId = $sessionId
    requestId = $requestId
    section = 'actions'
    sectionJson = (([ordered]@{ actions = $actions }) | ConvertTo-Json -Depth 8 -Compress)
    workspacePath = $workspace
}
Save-Body -Name 'mcp-actions.json' -Result $repl
Write-Output ('ACTIONS=' + ((Get-ToolObject -Result $repl) | ConvertTo-Json -Depth 6 -Compress))

$decisions = @(
    [ordered]@{
        decision = 'AGREE this late H-green for S1-S8 implementation'
        rationale = 'H-red AGREE exists. Independent named filters and product wires cover S1-S8 AC. Implementer did not claim TODOs done or live deploy.'
        alternatives = 'DISAGREE on Repl classifier duplication; DISAGREE on missing full suite; FAIL B2 from timestamps'
        consequence = 'Do not mark PLAN-TRIAGECLUSTER-001 or the 16 BUG-TRIAGE ids done. Live 1.4.26 is out of this gate.'
    }
)
$decRepl = Invoke-McpTool -Name 'sessionlog_replace_section' -Arguments @{
    agent = 'GrokCode'
    sessionId = $sessionId
    requestId = $requestId
    section = 'designDecisions'
    sectionJson = (([ordered]@{ designDecisions = $decisions }) | ConvertTo-Json -Depth 8 -Compress)
    workspacePath = $workspace
}
Save-Body -Name 'mcp-decisions.json' -Result $decRepl
Write-Output ('DECISIONS=' + ((Get-ToolObject -Result $decRepl) | ConvertTo-Json -Depth 6 -Compress))

$files = @(
    'docs/receipts/hostile-validator-20260818T234800Z.md',
    'docs/receipts/hostile-validator-20260818T234800Z.json',
    'docs/receipts/_hv-234800Z/trust.json',
    'docs/receipts/_hv-234800Z/test-summary.json',
    'docs/receipts/_hv-234800Z/test-ac.json',
    'docs/receipts/_hv-234800Z/todos.json',
    'docs/receipts/_hv-234800Z/mappings.json',
    'docs/receipts/_hv-234800Z/inspect.json'
)
$fileRepl = Invoke-McpTool -Name 'sessionlog_replace_section' -Arguments @{
    agent = 'GrokCode'
    sessionId = $sessionId
    requestId = $requestId
    section = 'filesModified'
    sectionJson = (([ordered]@{ filesModified = $files }) | ConvertTo-Json -Depth 6 -Compress)
    workspacePath = $workspace
}
Save-Body -Name 'mcp-files.json' -Result $fileRepl
Write-Output ('FILES=' + ((Get-ToolObject -Result $fileRepl) | ConvertTo-Json -Depth 6 -Compress))

$tagRepl = Invoke-McpTool -Name 'sessionlog_replace_section' -Arguments @{
    agent = 'GrokCode'
    sessionId = $sessionId
    requestId = $requestId
    section = 'tags'
    sectionJson = (([ordered]@{ tags = @('hostile-validator','H-green','PLAN-TRIAGECLUSTER-001','S1-S8','FR-MCP-TRIAGEERR-001','AGREE') }) | ConvertTo-Json -Depth 6 -Compress)
    workspacePath = $workspace
}
Save-Body -Name 'mcp-tags.json' -Result $tagRepl

$ctxRepl = Invoke-McpTool -Name 'sessionlog_replace_section' -Arguments @{
    agent = 'GrokCode'
    sessionId = $sessionId
    requestId = $requestId
    section = 'context'
    sectionJson = (([ordered]@{ contextList = @(
        'docs/plans/triage-cluster-001.md',
        'docs/receipts/hostile-validator-20260818T233800Z.md',
        'docs/receipts/hostile-validator-20260818T234800Z.md',
        'src/McpServer.Support.Mcp/Services/McpErrorClassifier.cs',
        'src/McpServer.Repl.Core/ReplMcpErrorClassifier.cs'
    ) }) | ConvertTo-Json -Depth 6 -Compress)
    workspacePath = $workspace
}
Save-Body -Name 'mcp-context.json' -Result $ctxRepl

$complete = Invoke-McpTool -Name 'sessionlog_complete_turn' -Arguments @{
    agent = 'GrokCode'
    sessionId = $sessionId
    requestId = $requestId
    workspacePath = $workspace
    response = @'
OverallVerdict AGREE. Receipt docs/receipts/hostile-validator-20260818T234800Z.md. PASS 21 FAIL 0 UNKNOWN 0 N/A 4.
Late H-green for S1-S8 implementation. H-red 233800Z AGREE (PASS 27 FAIL 0) exists. Independent named filters all Failed 0 Skipped 0. Pester 9/0/0. Build ReplacePluginCache 2/0/0. Scratch s2-tests.log exists. All 16 BUG-TRIAGE ids remain Done=false. Live host remains 1.4.26. Do not mark TODOs done.
'@
}
Save-Body -Name 'mcp-complete.json' -Result $complete
Write-Output ('COMPLETE=' + ((Get-ToolObject -Result $complete) | ConvertTo-Json -Depth 8 -Compress))

$query = Invoke-McpTool -Name 'sessionlog_query' -Arguments @{
    agent = 'GrokCode'
    workspacePath = $workspace
    todoId = 'PLAN-TRIAGECLUSTER-001'
    from = '2026-08-18T23:47:00Z'
    limit = 10
}
Save-Body -Name 'mcp-query.json' -Result $query
$qobj = Get-ToolObject -Result $query
Write-Output ('QUERY_KEYS=' + ($qobj.PSObject.Properties.Name -join ','))
$clip = ($qobj | ConvertTo-Json -Depth 8 -Compress)
if ($clip.Length -gt 2500) { $clip = $clip.Substring(0, 2500) }
Write-Output ('QUERY_CLIP=' + $clip)
if ($script:McpSessionHeader) {
    Set-Content -LiteralPath $headerPath -Value $script:McpSessionHeader -Encoding utf8
}
Write-Output 'MCP_COMPLETE_DONE'
