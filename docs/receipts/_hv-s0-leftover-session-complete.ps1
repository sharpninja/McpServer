#Requires -Version 7.0
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$workspace = 'F:\GitHub\McpServer'
$baseUrl = 'http://PAYTON-LEGION2:7147'
$outDir = Join-Path $workspace 'docs\receipts\_hv-s0-leftover'
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
    clientInfo = @{ name = 'hostile-validator-s0-leftover-complete'; version = '1.0.0' }
}
Save-Body -Name 'session-complete-init.json' -Result $init
[void](Invoke-McpRpc -Method 'notifications/initialized' -Params @{})

$actions = @(
    [ordered]@{ order = 1; description = 'add-profile: read 18 non-skill profile files'; type = 'edit'; status = 'completed'; filePath = 'C:\Users\kingd\.claude\profile' }
    [ordered]@{ order = 2; description = 'Test-MarkerSignature True; health nonce 71de2e5b231d4cbf92e837c90a15a9c9 echoed'; type = 'edit'; status = 'completed'; filePath = 'F:\GitHub\McpServer\AGENTS-README-FIRST.yaml' }
    [ordered]@{ order = 3; description = 'todo_get PLAN-TRIAGELEFTOVER-001 Done=false; FunctionalRequirements null TechnicalRequirements null'; type = 'edit'; status = 'completed'; filePath = 'PLAN-TRIAGELEFTOVER-001' }
    [ordered]@{ order = 4; description = 'todo_get 27 BUG-TRIAGE leftover IDs all Done=false'; type = 'edit'; status = 'completed'; filePath = 'docs/receipts/_hv-s0-leftover/bug-triage-27.json' }
    [ordered]@{ order = 5; description = 'requirements_list leftover FR/TR/TEST/mappings; structured AcceptanceCriteria empty'; type = 'edit'; status = 'completed'; filePath = 'docs/receipts/_hv-s0-leftover/req-summary.json' }
    [ordered]@{ order = 6; description = 'Independent ValidateTraceability Succeeded findings=0'; type = 'edit'; status = 'completed'; filePath = 'docs/receipts/_hv-s0-leftover/validate-traceability.txt' }
    [ordered]@{ order = 7; description = 'Wrote hostile H0 leftover S0 receipt pair'; type = 'create'; status = 'completed'; filePath = 'docs/receipts/hostile-validator-20260819T174750Z.md' }
    [ordered]@{ order = 8; description = 'Decision: DISAGREE H0 leftover S0; structured AC empty; PLAN TODO unlinked to leftover FR/TR'; type = 'design_decision'; status = 'completed'; filePath = 'docs/plans/triage-cluster-002.md' }
)
$section = [ordered]@{ actions = $actions }
$sectionJson = $section | ConvertTo-Json -Depth 8 -Compress
$repl = Invoke-McpTool -Name 'sessionlog_replace_section' -Arguments @{
    agent = 'GrokCode'
    sessionId = $sessionId
    requestId = $requestId
    section = 'actions'
    sectionJson = $sectionJson
    workspacePath = $workspace
}
Save-Body -Name 'session-actions.json' -Result $repl
Write-Output ('ACTIONS=' + ((Get-ToolObject -Result $repl) | ConvertTo-Json -Depth 6 -Compress))

$completedUtc = [datetime]::UtcNow.ToString('o')
$response = @"
OverallVerdict DISAGREE. Leftover-triage S0 H0 is not earned.

PASS 16 FAIL 2 UNKNOWN 0 N/A 1.

FAIL:
1. C2: leftover FR/TR/TEST AcceptanceCriteria arrays are empty. AC text lives only as markdown checkboxes in Body/Condition.
2. D2: PLAN-TRIAGELEFTOVER-001 FunctionalRequirements and TechnicalRequirements are null.

A claims that held: PLAN TODO Done=false; plan names 27 BUG-TRIAGE IDs and worktree protocol; .gitignore .worktrees/; store FR/TR/TEST plus 1:1 mappings; ValidateTraceability Succeeded findings=0; no leftover product implementation; 27 BUG-TRIAGE remain Done=false.

Receipt: docs/receipts/hostile-validator-20260819T174750Z.md
This review did not mark any TODO done and did not implement product code.
"@

$turn = [ordered]@{
    requestId = $requestId
    queryTitle = 'Hostile H0 leftover-triage S0 requirements'
    queryText = 'Hostile validator for leftover-triage S0. Class 1. Attack PLAN-TRIAGELEFTOVER-001, 27 BUG-TRIAGE IDs, gitignore .worktrees/, FR/TR/TEST/AC/mappings, ValidateTraceability, no product implementation, no BUG-TRIAGE done. Do not mark TODOs done. Do not implement product code.'
    response = $response
    interpretation = 'Operator asked for hostile validation of leftover-triage S0 requirements claims. AGREE only if PLAN TODO, plan, gitignore, FR/TR/TEST/AC/mappings, ValidateTraceability, no leftover product code, and 27 BUG-TRIAGE remain open. Do not mark TODOs done.'
    status = 'completed'
    tags = @(
        'hostile-validator'
        'H0'
        'PLAN-TRIAGELEFTOVER-001'
        'DISAGREE'
        'FR-MCP-SESSIONATTR-001'
        'FR-MCP-FAILSAFE-001'
        'FR-MCP-STRICTCOUNT-001'
        'FR-MCP-XAGENT-001'
        'FR-MCP-SESSIONEND-001'
        'FR-MCP-VERIFYWRAP-001'
        'FR-MCP-TRANSCRIPT-SEARCH-001'
        'FR-MCP-TEMPVOL-001'
    )
    contextList = @(
        'docs/plans/triage-cluster-002.md'
        'docs/receipts/hostile-validator-20260819T174750Z.md'
        'docs/receipts/_hv-s0-leftover/todo-plan-parsed.json'
        'docs/receipts/_hv-s0-leftover/req-summary.json'
        'docs/receipts/_hv-s0-leftover/bug-triage-27.json'
    )
    filesModified = @(
        'docs/receipts/hostile-validator-20260819T174750Z.md'
        'docs/receipts/hostile-validator-20260819T174750Z.json'
    )
    planFile = $planFile
    todoId = $todoId
    designDecisions = @(
        'DISAGREE leftover-triage S0 H0. Structured AcceptanceCriteria empty on eight leftover FR/TR/TEST sets. PLAN-TRIAGELEFTOVER-001 does not link FR/TR IDs. Parent must not start leftover worktrees on this H0.'
    )
    requirementsDiscovered = @(
        'FR-MCP-SESSIONATTR-001'
        'FR-MCP-FAILSAFE-001'
        'FR-MCP-STRICTCOUNT-001'
        'FR-MCP-XAGENT-001'
        'FR-MCP-SESSIONEND-001'
        'FR-MCP-VERIFYWRAP-001'
        'FR-MCP-TRANSCRIPT-SEARCH-001'
        'FR-MCP-TEMPVOL-001'
    )
}
$turnJson = $turn | ConvertTo-Json -Depth 8 -Compress
$complete = Invoke-McpTool -Name 'sessionlog_complete_turn' -Arguments @{
    agent = 'GrokCode'
    sessionId = $sessionId
    requestId = $requestId
    workspacePath = $workspace
    turnJson = $turnJson
}
Save-Body -Name 'session-complete.json' -Result $complete
Write-Output ('COMPLETE=' + ((Get-ToolObject -Result $complete) | ConvertTo-Json -Depth 8 -Compress))
Write-Output ('COMPLETED_UTC=' + $completedUtc)

$query = Invoke-McpTool -Name 'sessionlog_query' -Arguments @{
    workspacePath = $workspace
    agent = 'GrokCode'
    sessionId = $sessionId
    todoId = $todoId
    from = '2026-08-19T17:40:00Z'
    limit = 10
}
Save-Body -Name 'session-query-proof.json' -Result $query
$proof = Get-ToolObject -Result $query
Write-Output ('QUERY_KEYS=' + ($proof.PSObject.Properties.Name -join ','))
$clip = ($proof | ConvertTo-Json -Depth 12 -Compress)
if ($clip.Length -gt 8000) { $clip = $clip.Substring(0, 8000) }
Write-Output ('QUERY=' + $clip)
Write-Output 'SESSION_COMPLETE_DONE'
