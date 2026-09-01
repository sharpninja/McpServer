#Requires -Version 7.0
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$workspace = 'F:\GitHub\McpServer'
$baseUrl = 'http://PAYTON-LEGION2:7147'
$sessionId = 'GrokCode-20260818T160502Z-h4-green-products'
$requestId = 'req-20260818T160502Z-001-hostile-h4-green-products'
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
    clientInfo = @{ name = 'hostile-validator-h4-green-2'; version = '1.0.0' }
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
        content = 'Classified class 1 project requirement work. Surface C applies. Scoring Byrd at the H4-green gate: ProductRequirementContextTests green; handler uses ProductShareHelper; ContextController dispatches CQRS when IDispatcher is present. Not MCP-PRODUCTS-001 done. Not Phase 5. Not full ./build.ps1 Test.'
        category = 'observation'
    },
    [ordered]@{
        timestamp = $now
        role = 'model'
        content = 'Independent list-tests 2026-08-18T16:02:49Z to 16:03:00Z EXIT=0 named 4 cases. Independent filter run 16:03:00Z to 16:03:14Z compiled; Passed 4. Default-logger rerun 16:05:02Z to 16:05:15Z: Passed! Failed: 0, Passed: 4, Skipped: 0, Total: 4. Independent FullyQualifiedName~Product 16:05:15Z to 16:05:31Z: Passed! Failed: 0, Passed: 43, Skipped: 0, Total: 43. todo_get MCP-PRODUCTS-001 Done=false. Handler uses ProductShareHelper; IProductService cs count 0. ContextController search and pack dispatch GetProductRequirementContextQuery.'
        category = 'observation'
    },
    [ordered]@{
        timestamp = $now
        role = 'model'
        content = 'Decision: OverallVerdict AGREE on H4-green. Consequence: parent may start Phase 5; must not mark MCP-PRODUCTS-001 done; must not claim H5-done or full suite. Alternatives rejected: DISAGREE because implementer said Product filter 42 while independent FullyQualifiedName~Product is 43 (extra is pre-existing UseCase ProductKey; named 4/0/0 holds; not a done-state lie); DISAGREE because tests call the query handler instead of HTTP pack (H4-red accepted ProductRequirementContextTests as the named file; controller hook independently read); DISAGREE because GetPackAsync Take(limit) can drop appended product chunks (named tests cover the CQRS query; H5 can tighten pack ordering); DISAGREE because FR-005 AC remain isSatisfied=false (correct until H5-done). Affected: FR-MCP-PRODUCT-005, TR-MCP-PRODUCT-CTX-001, TEST-MCP-PRODUCT-006.'
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
Save-Body -Name '_hv-h4-green-dialog.json' -Result $dialog
Write-Output ('DIALOG=' + ((Get-ToolObject -Result $dialog) | ConvertTo-Json -Depth 6 -Compress))

$actions = @(
    [ordered]@{ order = 1; description = 'add-profile: read 18 non-skill profile files'; type = 'edit'; status = 'completed'; filePath = 'C:\Users\kingd\.claude\profile' }
    [ordered]@{ order = 2; description = 'Test-MarkerSignature True; health nonce h4grn4309c0464ea04cddae2ee23f1e0 echoed'; type = 'edit'; status = 'completed'; filePath = 'F:\GitHub\McpServer\AGENTS-README-FIRST.yaml' }
    [ordered]@{ order = 3; description = 'Read GetProductRequirementContextQueryHandler ProductShareHelper synthesis; ContextController search/pack dispatch'; type = 'edit'; status = 'completed'; filePath = 'src/McpServer.Support.Mcp/Products/Queries/GetProductRequirementContextQuery.cs' }
    [ordered]@{ order = 4; description = 'Independent ProductRequirementContextTests Failed 0 Passed 4 Skipped 0; FullyQualifiedName~Product Failed 0 Passed 43 Skipped 0'; type = 'edit'; status = 'completed'; filePath = 'tests/McpServer.Support.Mcp.Tests/Products/ProductRequirementContextTests.cs' }
    [ordered]@{ order = 5; description = 'todo_get MCP-PRODUCTS-001 Done=false; requirements_list FR-005 TR-CTX-001 TEST-006 mapping; IProductService cs=0'; type = 'edit'; status = 'completed'; filePath = 'MCP-PRODUCTS-001' }
    [ordered]@{ order = 6; description = 'Wrote hostile H4-green receipt pair'; type = 'create'; status = 'completed'; filePath = 'docs/receipts/hostile-validator-20260818T160833Z.md' }
    [ordered]@{ order = 7; description = 'Decision: H4-green AGREE; Phase 5 may start; TODO stays Done=false'; type = 'design_decision'; status = 'completed'; filePath = 'docs/plans/mcp-products-001.md' }
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
Save-Body -Name '_hv-h4-green-actions.json' -Result $repl
Write-Output ('ACTIONS=' + ((Get-ToolObject -Result $repl) | ConvertTo-Json -Depth 6 -Compress))

$turn = [ordered]@{
    requestId = $requestId
    queryTitle = 'Hostile H4-green products context review'
    queryText = 'Hostile validator H4-green: Phase 4 product-requirements context implemented. Not claiming MCP-PRODUCTS-001 done. Not claiming Phase 5 or full ./build.ps1 Test.'
    response = "OverallVerdict AGREE. H4-green for MCP-PRODUCTS-001 Phase 4 product-requirements context is complete.`n`nProductRequirementContextTests independently re-run: Failed 0, Passed 4, Skipped 0, Total 4, EXIT=0. FullyQualifiedName~Product: Failed 0, Passed 43, Skipped 0. GetProductRequirementContextQueryHandler uses ProductShareHelper and does not read sibling ContextDocument rows. ContextController search/pack dispatch the CQRS query when IDispatcher is present. MCP-PRODUCTS-001 Done=false.`n`nReceipt: docs/receipts/hostile-validator-20260818T160833Z.md"
    interpretation = 'Operator asked for hostile validation of H4-green: Phase 4 product-requirements context implemented. TODO stays false. Phase 5 and full suite not claimed.'
    status = 'completed'
    tags = @('hostile-validator','H4-green','MCP-PRODUCTS-001','FR-MCP-PRODUCT-005','TR-MCP-PRODUCT-CTX-001','TEST-MCP-PRODUCT-006')
    contextList = @(
        'docs/plans/mcp-products-001.md'
        'tests/McpServer.Support.Mcp.Tests/Products/ProductRequirementContextTests.cs'
        'src/McpServer.Support.Mcp/Products/Queries/GetProductRequirementContextQuery.cs'
        'src/McpServer.Support.Mcp/Controllers/ContextController.cs'
        'docs/receipts/hostile-validator-20260818T160833Z.md'
    )
    filesModified = @(
        'docs/receipts/hostile-validator-20260818T160833Z.md'
        'docs/receipts/hostile-validator-20260818T160833Z.json'
    )
    planFile = 'docs/plans/mcp-products-001.md'
    todoId = 'MCP-PRODUCTS-001'
    designDecisions = @(
        'AGREE H4-green. Product-requirements context is implemented via CQRS share helper and ContextController dispatch. Phase 5 may start. TODO stays Done=false.'
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
Save-Body -Name '_hv-h4-green-complete.json' -Result $complete
Write-Output ('COMPLETE=' + ((Get-ToolObject -Result $complete) | ConvertTo-Json -Depth 6 -Compress))

$query = Invoke-McpTool -Name 'sessionlog_query' -Arguments @{
    workspacePath = $workspace
    agent = 'GrokCode'
    todoId = 'MCP-PRODUCTS-001'
    from = '2026-08-18T16:04:00Z'
    limit = 10
}
Save-Body -Name '_hv-h4-green-query-proof.json' -Result $query
$proof = Get-ToolObject -Result $query
Write-Output ('QUERY_KEYS=' + ($proof.PSObject.Properties.Name -join ','))
Write-Output ('QUERY=' + ($proof | ConvertTo-Json -Depth 10 -Compress))

Write-Output 'MCP2_DONE'
