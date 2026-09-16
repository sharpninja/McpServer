#Requires -Version 7.0
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$workspace = 'F:\GitHub\McpServer'
$outDir = 'F:\GitHub\McpServer\docs\receipts\_hv-failsafe-post-review'
$baseUrl = 'http://PAYTON-LEGION2:7147'
$ids = Get-Content -LiteralPath (Join-Path $outDir 'ids.json') -Raw | ConvertFrom-Json
$sessionId = [string]$ids.sessionId
$requestId = [string]$ids.requestId
$agent = 'GrokCode'

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
    [System.IO.File]::WriteAllText((Join-Path $outDir $Name), $Result.Body)
    Write-Output ("SAVED {0} HTTP={1} LEN={2}" -f $Name, $Result.Status, $Result.Body.Length)
}
function Invoke-ReplaceSection {
    param([string]$Section, $Payload, [string]$File)
    $sectionJson = ($Payload | ConvertTo-Json -Depth 20 -Compress)
    $r = Invoke-McpTool -Name 'sessionlog_replace_section' -Arguments @{
        agent = $agent
        sessionId = $sessionId
        requestId = $requestId
        section = $Section
        sectionJson = $sectionJson
        workspacePath = $workspace
    }
    Save-Body -Name $File -Result $r
}

[void](Invoke-McpRpc -Method 'initialize' -Params @{
    protocolVersion = '2025-03-26'
    capabilities = @{}
    clientInfo = @{ name = 'hostile-validator-failsafe-post'; version = '1.0.0' }
})
[void](Invoke-McpRpc -Method 'notifications/initialized' -Params @{})

$now = [datetime]::UtcNow.ToString('o')

Invoke-ReplaceSection -Section 'tags' -File 'self-sec-tags.json' -Payload ([ordered]@{
    tags = @(
        'hostile-validator'
        'class-2-ops'
        'failsafe-post'
        'OverallVerdict-DISAGREE'
    )
})

Invoke-ReplaceSection -Section 'designDecisions' -File 'self-sec-decisions.json' -Payload ([ordered]@{
    designDecisions = @(
        [ordered]@{
            decision = 'OverallVerdict DISAGREE because A2 167/54/48 is not itemized (124 proven) and B1/B2 fail honesty/receipts.'
            rationale = 'Hostile AGREE requires every applicable A+B claim PASS. Remaining-queue zeros, four grouped triage reports, no TODO done, and no timeout patch pass, but the posted-total headline does not.'
            alternativesConsidered = @(
                'Grant the 503 triage unitemized 41 grok-live posts and AGREE'
                'UNKNOWN A2 only and still DISAGREE'
            )
            rejected = 'Granting 41 still yields 165/53 not 167/54, and 41 has no file list. UNKNOWN is not PASS.'
            consequence = 'Parent must not mark any TODO or plan step done on this ops turn.'
            affected = @('A2', 'B1', 'B2')
        }
        [ordered]@{
            decision = 'Surface C and D are N/A for this class-2 ops review.'
            rationale = 'Operator directed posting queued failsafe YAML and triaging leftovers. No product implementation and no plan-step done claim.'
            alternativesConsidered = @('FAIL C for missing FR/TR')
            rejected = 'hostile-ops-vs-requirements.md forbids inventing a requirements gap for operator-directed ops.'
            consequence = 'N/A surfaces do not block AGREE; A2/B1/B2 already block AGREE.'
            affected = @('C', 'D')
        }
    )
})

Invoke-ReplaceSection -Section 'actions' -File 'self-sec-actions.json' -Payload ([ordered]@{
    actions = @(
        [ordered]@{ order = 1; type = 'design_decision'; status = 'completed'; filePath = ''; description = 'Classified work as class-2 ops. Surface C N/A. Surface D N/A. DISAGREE on 167 posted-total overclaim.' }
        [ordered]@{ order = 2; type = 'edit'; status = 'completed'; filePath = 'docs/receipts/hostile-validator-20260821T153416Z.md'; description = 'Wrote hostile markdown receipt.' }
        [ordered]@{ order = 3; type = 'edit'; status = 'completed'; filePath = 'docs/receipts/hostile-validator-20260821T153416Z.json'; description = 'Wrote hostile JSON twin.' }
        [ordered]@{ order = 4; type = 'edit'; status = 'completed'; filePath = 'docs/receipts/_hv-failsafe-post-review/'; description = 'Collected independent yaml counts, replay arithmetic, triage_status, todo_list, git, health, marker signature.' }
    )
})

Invoke-ReplaceSection -Section 'dialog' -File 'self-sec-dialog.json' -Payload ([ordered]@{
    processingDialog = @(
        [ordered]@{ timestamp = $now; role = 'model'; category = 'observation'; content = 'Independent yaml counts: grok live 0, grok quarantine 0, grok pending 0, Codex live 0, Codex pending 2 importRecovery, Codex quarantine 3, Claude quarantine 0.' }
        [ordered]@{ timestamp = $now; role = 'model'; category = 'observation'; content = 'Replay receipts itemize 124 SubmitAsync successes (12+47+65) and 3 Codex quarantine failures. Claimed total 167. Gap 43.' }
        [ordered]@{ timestamp = $now; role = 'model'; category = 'observation'; content = 'Four triage reports exist and are status grouped. Open TODOs 40 vs prior 36. Added BUG-TRIAGE-168/169/170/171. None of the prior 36 marked done.' }
        [ordered]@{ timestamp = $now; role = 'model'; category = 'decision'; content = 'Decision: DISAGREE. A2 FAIL on 167/54/48. B1/B2 FAIL honesty/receipts. A1/A3/A4/A5/A6 PASS. C N/A. D N/A.' }
    )
})

Invoke-ReplaceSection -Section 'context' -File 'self-sec-context.json' -Payload ([ordered]@{
    contextList = @(
        'docs/receipts/failsafe-post-all-summary-20260821T152147Z.json'
        'docs/receipts/failsafe-replay-20260821T144010Z.json'
        'docs/receipts/failsafe-quarantine-replay-20260821T150140Z.json'
        'docs/receipts/failsafe-other-replay-20260821T150540Z.json'
        'docs/receipts/hostile-validator-20260821T153416Z.md'
        'docs/receipts/hostile-validator-20260821T153416Z.json'
        'docs/receipts/_hv-failsafe-post-review/'
    )
})

Invoke-ReplaceSection -Section 'filesModified' -File 'self-sec-files.json' -Payload ([ordered]@{
    filesModified = @(
        'docs/receipts/hostile-validator-20260821T153416Z.md'
        'docs/receipts/hostile-validator-20260821T153416Z.json'
        'docs/receipts/_hv-failsafe-post-review/collect.ps1'
        'docs/receipts/_hv-failsafe-post-review/extract-and-query.ps1'
        'docs/receipts/_hv-failsafe-post-review/persist-complete.ps1'
    )
})

$response = @'
Hostile validator DISAGREE on failsafe post-all claims.

A2 FAIL: itemized SubmitAsync successes are 124 not 167 (grok live 12 not 54, grok quarantine 47 not 48). Four triage reports are grouped. Remaining queues match the 3+2 leftover claim. No TODO marked done. No product timeout patch.

Receipt: docs/receipts/hostile-validator-20260821T153416Z.md
OverallVerdict: DISAGREE
'@

$complete = Invoke-McpTool -Name 'sessionlog_complete_turn' -Arguments @{
    agent = $agent
    sessionId = $sessionId
    requestId = $requestId
    workspacePath = $workspace
    turnJson = (@{
        response = $response
        interpretation = 'Hostile review of implementer claims that queued failsafe SessionLog.SubmitAsync records were posted and leftover failures triaged. Class 2 ops.'
        status = 'completed'
        queryTitle = 'Hostile validate failsafe post-all claims'
    } | ConvertTo-Json -Depth 8 -Compress)
}
Save-Body -Name 'self-complete.json' -Result $complete

$proof = Invoke-McpTool -Name 'sessionlog_query' -Arguments @{
    workspacePath = $workspace
    agent = $agent
    text = 'Hostile validate failsafe post-all claims'
    limit = 5
}
Save-Body -Name 'q-self-proof.json' -Result $proof

Write-Output 'PERSIST COMPLETE'
