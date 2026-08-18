#Requires -Version 7.0
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$workspace = 'F:\GitHub\McpServer'
$baseUrl = 'http://PAYTON-LEGION2:7147'
$sessionId = 'GrokCode-20260818T154849Z-h4-red-products'
$requestId = 'req-20260818T154849Z-001-hostile-h4-red-products'
$outDir = 'F:\GitHub\McpServer\docs\receipts'
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
    $text = [string]$outer.result.content[0].text
    return ($text | ConvertFrom-Json)
}

$init = Invoke-McpRpc -Method 'initialize' -Params @{
    protocolVersion = '2025-03-26'
    capabilities = @{}
    clientInfo = @{ name = 'hostile-validator-h4-red-3'; version = '1.0.0' }
}
Write-Output ('INIT_HTTP=' + $init.Status)
[void](Invoke-McpRpc -Method 'notifications/initialized' -Params @{})

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
        content = 'Classified class 1 project requirement work. Surface C applies. Scoring Byrd at the H4-red gate: ProductRequirementContextTests exist and fail for Failure(not implemented). Not MCP-PRODUCTS-001 done. Not Phase 4 green. Not Phase 5. Not full ./build.ps1 Test.'
        category = 'observation'
    },
    [ordered]@{
        timestamp = $now
        role = 'model'
        content = 'Independent list-tests 2026-08-18T15:49:30Z to 15:49:40Z EXIT=0 named 4 cases. Independent filter run 15:49:41Z to 15:49:55Z compiled; Failed 4; error not implemented. Default-logger rerun 15:51:41Z to 15:51:53Z: Failed! Failed: 4, Passed: 0, Skipped: 0, Total: 4. todo_get MCP-PRODUCTS-001 Done=false. Handler stub line 43 Failure("not implemented"). SRC_CTX hits confined to GetProductRequirementContextQuery.cs.'
        category = 'observation'
    },
    [ordered]@{
        timestamp = $now
        role = 'model'
        content = 'Decision: OverallVerdict AGREE on H4-red. Consequence: parent may start Phase 4 green; must not mark MCP-PRODUCTS-001 done; must not claim Phase 4 green, Phase 5, or full suite. Alternatives rejected: DISAGREE because tests call the query handler instead of HybridSearch pack (plan Phase 4 red file is ProductRequirementContextTests; H4-green is pack/search contribution); DISAGREE because TODO Remaining is stale (not a done-state lie); DISAGREE because TEST-006 structured AC array is empty (Condition plus FR-005 structured ac-1..ac-3 cover the H4-red bar; same store shape as prior product phases). Affected: FR-MCP-PRODUCT-005, TR-MCP-PRODUCT-CTX-001, TEST-MCP-PRODUCT-006.'
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
Save-Body -Name '_hv-h4-red-dialog.json' -Result $dialog
Write-Output ('DIALOG=' + ((Get-ToolObject -Result $dialog) | ConvertTo-Json -Depth 6 -Compress))

$actions = @(
    [ordered]@{ order = 1; description = 'add-profile: read 18 non-skill profile files'; type = 'edit'; status = 'completed'; filePath = 'C:\Users\kingd\.claude\profile' }
    [ordered]@{ order = 2; description = 'Test-MarkerSignature True; health nonce h4red16b22029a9ac4e448821f779bc8 echoed'; type = 'edit'; status = 'completed'; filePath = 'F:\GitHub\McpServer\AGENTS-README-FIRST.yaml' }
    [ordered]@{ order = 3; description = 'Read ProductRequirementContextTests four named cases; GetProductRequirementContextQueryHandler Failure(not implemented)'; type = 'edit'; status = 'completed'; filePath = 'tests/McpServer.Support.Mcp.Tests/Products/ProductRequirementContextTests.cs' }
    [ordered]@{ order = 4; description = 'Independent ProductRequirementContextTests Failed 4 Passed 0 Skipped 0 EXIT=1 error not implemented compiled'; type = 'edit'; status = 'completed'; filePath = 'tests/McpServer.Support.Mcp.Tests/Products/ProductRequirementContextTests.cs' }
    [ordered]@{ order = 5; description = 'todo_get MCP-PRODUCTS-001 Done=false; requirements_list FR-005 TR-CTX-001 TEST-006 mapping'; type = 'edit'; status = 'completed'; filePath = 'MCP-PRODUCTS-001' }
    [ordered]@{ order = 6; description = 'Wrote hostile H4-red receipt pair'; type = 'create'; status = 'completed'; filePath = 'docs/receipts/hostile-validator-20260818T155200Z.md' }
    [ordered]@{ order = 7; description = 'Decision: H4-red AGREE; Phase 4 green may start; TODO stays Done=false'; type = 'design_decision'; status = 'completed'; filePath = 'docs/plans/mcp-products-001.md' }
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
Save-Body -Name '_hv-h4-red-actions.json' -Result $repl
Write-Output ('ACTIONS=' + ((Get-ToolObject -Result $repl) | ConvertTo-Json -Depth 6 -Compress))

$turn = [ordered]@{
    requestId = $requestId
    queryTitle = 'Hostile H4-red products context review'
    queryText = 'Hostile validator H4-red: Phase 4 context tests exist and fail for the right reason. Not claiming Phase 4 green. Not claiming MCP-PRODUCTS-001 done.'
    response = "OverallVerdict AGREE. H4-red for MCP-PRODUCTS-001 Phase 4 context tests is complete.`n`nProductRequirementContextTests independently re-run: Failed 4, Passed 0, Skipped 0, Total 4, EXIT=1, compiled, error not implemented. GetProductRequirementContextQueryHandler returns Failure(not implemented). MCP-PRODUCTS-001 Done=false.`n`nReceipt: docs/receipts/hostile-validator-20260818T155200Z.md"
    interpretation = 'Operator asked for hostile validation of H4-red: Phase 4 context tests exist and fail for the right reason. TODO stays false. Phase 4 green and full suite not claimed.'
    status = 'completed'
    tags = @('hostile-validator','H4-red','MCP-PRODUCTS-001','FR-MCP-PRODUCT-005','TR-MCP-PRODUCT-CTX-001','TEST-MCP-PRODUCT-006')
    contextList = @(
        'docs/plans/mcp-products-001.md'
        'tests/McpServer.Support.Mcp.Tests/Products/ProductRequirementContextTests.cs'
        'src/McpServer.Support.Mcp/Products/Queries/GetProductRequirementContextQuery.cs'
        'docs/receipts/hostile-validator-20260818T155200Z.md'
    )
    filesModified = @(
        'docs/receipts/hostile-validator-20260818T155200Z.md'
        'docs/receipts/hostile-validator-20260818T155200Z.json'
    )
    planFile = 'docs/plans/mcp-products-001.md'
    todoId = 'MCP-PRODUCTS-001'
    designDecisions = @(
        'AGREE H4-red. Context tests exist and fail for not implemented. Phase 4 green may start. TODO stays Done=false.'
    )
    requirementsDiscovered = @(
        'FR-MCP-PRODUCT-005'
        'TR-MCP-PRODUCT-CTX-001'
        'TEST-MCP-PRODUCT-006'
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
Save-Body -Name '_hv-h4-red-complete.json' -Result $complete
Write-Output ('COMPLETE=' + ((Get-ToolObject -Result $complete) | ConvertTo-Json -Depth 6 -Compress))

$query = Invoke-McpTool -Name 'sessionlog_query' -Arguments @{
    workspacePath = $workspace
    agent = 'GrokCode'
    todoId = 'MCP-PRODUCTS-001'
    from = '2026-08-18T15:48:00Z'
    limit = 10
}
Save-Body -Name '_hv-h4-red-query-proof.json' -Result $query
$proof = Get-ToolObject -Result $query
Write-Output ('QUERY_KEYS=' + ($proof.PSObject.Properties.Name -join ','))
Write-Output ('QUERY=' + ($proof | ConvertTo-Json -Depth 10 -Compress))

Write-Output 'MCP3_DONE'
