#Requires -Version 7.0
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$workspace = 'F:\GitHub\McpServer'
$baseUrl = 'http://PAYTON-LEGION2:7147'
$sessionId = 'GrokCode-20260818T184548Z-hostile-wrapup'
$requestId = 'req-20260818T184548Z-001-hostile-wrap-up-review'
$outDir = Join-Path $workspace 'docs\receipts'
$script:McpSessionHeader = $null
$script:McpId = 0
$now = [datetime]::UtcNow.ToString('o')

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
    $text = [string]$outer.result.content[0].text
    return ($text | ConvertFrom-Json)
}

$init = Invoke-McpRpc -Method 'initialize' -Params @{
    protocolVersion = '2025-03-26'
    capabilities = @{}
    clientInfo = @{ name = 'hostile-validator-wrapup-complete'; version = '1.0.0' }
}
Write-Output ('INIT_HTTP=' + $init.Status)
[void](Invoke-McpRpc -Method 'notifications/initialized' -Params @{})

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
        content = 'Classified mixed / primary class 2. Surface C and Byrd phase-order N/A. Independent Test Failed 0 Passed 2004/283/33/20/63/826/50 Skipped 0. ValidateTraceability findings=0. origin/azure develop = bf000bb7. GitHub wiki 5764ee7 includes Handoff-Ingestion.md. ZIP sha256 8bbee5067d... 63 entries. PLAN TODOs Done=false. MCP-PRODUCTS-001 Done=true from prior H5 AGREE, not this wrap-up.'
        category = 'observation'
    }
    [ordered]@{
        timestamp = $now
        role = 'model'
        content = 'Decision: OverallVerdict AGREE. Consequence: wrap-up/push claims stand; this review does not flip any TODO. Alternatives rejected: DISAGREE because implementer left no wrap-up Test transcript (independent Test reproduced 3279/0/0); DISAGREE because MCP-PRODUCTS-001 is Done=true (that flip is the prior H5 skeptic AGREE, wrap-up todoId=None); FAIL C for missing FR on wrap-up (class 2 ops). Affected: wrap-up-20260818T183800Z, bf000bb7, wiki 5764ee7.'
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
Save-Body -Name '_hv-wrapup-dialog.json' -Result $dialog
try { Write-Output ('DIALOG=' + ((Get-ToolObject -Result $dialog) | ConvertTo-Json -Depth 6 -Compress)) } catch { Write-Output ('DIALOG_RAW=' + $dialog.Body) }

$actions = @(
    [ordered]@{ order = 1; description = 'add-profile: read 18 non-skill profile files'; type = 'edit'; status = 'completed'; filePath = 'C:\Users\kingd\.claude\profile' }
    [ordered]@{ order = 2; description = 'Test-MarkerSignature True; health nonce 80d314e1e20649298f3825f89cb66abb echoed; plugin 1.94.0'; type = 'edit'; status = 'completed'; filePath = 'F:\GitHub\McpServer\AGENTS-README-FIRST.yaml' }
    [ordered]@{ order = 3; description = 'Independent ZIP hash 8bbee5067d... 63 entries; wiki.yaml 26 docs 0 missing file sources'; type = 'edit'; status = 'completed'; filePath = 'docs/requirements/requirements-wiki-documents.zip' }
    [ordered]@{ order = 4; description = 'ls-remote origin/azure develop=bf000bb7; GitHub wiki 5764ee7 includes Handoff-Ingestion.md; Azure code wiki /docs main'; type = 'edit'; status = 'completed'; filePath = 'docs/receipts/_hv-wrapup-git-20260818.txt' }
    [ordered]@{ order = 5; description = 'Independent ./build.ps1 Test Failed 0 Passed 2004/283/33/20/63/826/50 Skipped 0 TEST_EXIT=0'; type = 'edit'; status = 'completed'; filePath = 'docs/receipts/_hv-wrapup-test-20260818.txt' }
    [ordered]@{ order = 6; description = 'Independent ValidateTraceability Succeeded findings=0 VT_EXIT=0'; type = 'edit'; status = 'completed'; filePath = 'docs/receipts/_hv-wrapup-vt-20260818.txt' }
    [ordered]@{ order = 7; description = 'todo_get PLAN-LLMSTRATEGY-001 Done=false; PLAN-SHARPMIND-001 Done=false; MCP-PRODUCTS-001 Done=true from H5 AGREE'; type = 'edit'; status = 'completed'; filePath = 'docs/receipts/_hv-wrapup-todo-MCP-PRODUCTS-001.json' }
    [ordered]@{ order = 8; description = 'sessionlog_query implementer wrap-up turn completed'; type = 'edit'; status = 'completed'; filePath = 'docs/receipts/_hv-wrapup-query-impl.json' }
    [ordered]@{ order = 9; description = 'Wrote hostile wrap-up receipt pair'; type = 'create'; status = 'completed'; filePath = 'docs/receipts/hostile-validator-20260818T185500Z.md' }
    [ordered]@{ order = 10; description = 'Decision: AGREE wrap-up/push; do not flip TODOs; C/Byrd N/A for class 2'; type = 'design_decision'; status = 'completed'; filePath = 'docs/receipts/hostile-validator-20260818T185500Z.md' }
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
Save-Body -Name '_hv-wrapup-actions.json' -Result $repl
try { Write-Output ('ACTIONS=' + ((Get-ToolObject -Result $repl) | ConvertTo-Json -Depth 6 -Compress)) } catch { Write-Output ('ACTIONS_RAW=' + $repl.Body) }

$turn = [ordered]@{
    requestId = $requestId
    queryTitle = 'Hostile validate wrap-up refresh-docs push'
    queryText = 'Adversarial review of wrap-up-20260818T183800Z claims.'
    interpretation = 'Mixed class-2 wrap-up/push. Attack remotes, wiki, ZIP, Test, VT, TODOs, session persist. Do not score C/Byrd as a new product gate.'
    response = "OverallVerdict AGREE.`n`nReceipt: docs/receipts/hostile-validator-20260818T185500Z.md`nPASS=18 FAIL=0 UNKNOWN=0 N/A=2`nFAIL list: none.`nIndependent Test 3279/0/0. ValidateTraceability findings=0. origin/azure develop=bf000bb7. GitHub wiki 5764ee7 includes Handoff-Ingestion.md. PLAN TODOs still false. MCP-PRODUCTS-001 already Done=true from H5."
    status = 'completed'
    planFile = 'None'
    todoId = 'None'
    tags = @('hostile-validator', 'wrap-up', 'refresh-docs', 'wiki')
    contextList = @(
        'docs/receipts/wrap-up-20260818T183800Z.md',
        'docs/receipts/hostile-validator-20260818T185500Z.md',
        'docs/receipts/_hv-wrapup-test-20260818.txt',
        'docs/receipts/_hv-wrapup-query-impl.json'
    )
    filesModified = @(
        'docs/receipts/hostile-validator-20260818T185500Z.md',
        'docs/receipts/hostile-validator-20260818T185500Z.json'
    )
    designDecisions = @(
        'Classify mixed / primary class 2 so C and Byrd are N/A.',
        'Re-run ./build.ps1 Test because implementer left no wrap-up Test transcript.',
        'Treat MCP-PRODUCTS-001 Done=true as prior H5 state, not a wrap-up flip.',
        'OverallVerdict AGREE; do not flip any TODO.'
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
Save-Body -Name '_hv-wrapup-complete.json' -Result $complete
try { Write-Output ('COMPLETE=' + ((Get-ToolObject -Result $complete) | ConvertTo-Json -Depth 6 -Compress)) } catch { Write-Output ('COMPLETE_RAW=' + $complete.Body) }

$query = Invoke-McpTool -Name 'sessionlog_query' -Arguments @{
    workspacePath = $workspace
    agent = 'GrokCode'
    from = '2026-08-18T18:45:00Z'
    limit = 10
}
Save-Body -Name '_hv-wrapup-query-proof.json' -Result $query
try { Write-Output ('QUERY=' + ((Get-ToolObject -Result $query) | ConvertTo-Json -Depth 10 -Compress)) } catch { Write-Output ('QUERY_RAW=' + $query.Body) }
Write-Output 'MCP_COMPLETE_DONE'
