#Requires -Version 7.0
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$workspace = 'F:\GitHub\McpServer'
$baseUrl = 'http://PAYTON-LEGION2:7147'
$sessionId = 'GrokCode-20260818T152309Z-h3-red-products'
$requestId = 'req-20260818T152309Z-001-hostile-h3-red-products'
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

[void](Invoke-McpRpc -Method 'initialize' -Params @{
    protocolVersion = '2025-03-26'
    capabilities = @{}
    clientInfo = @{ name = 'hostile-validator-h3-red-3'; version = '1.0.0' }
})
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
        content = 'Classified class 1 project requirement work. Surface C applies. Scoring Byrd at the H3-red gate: controller/client/REPL adapter tests exist and fail for 501 StatusCodeResult, NotImplementedException, and missing Products. Not MCP-PRODUCTS-001 done. Not Phase 3 green. Not Phase 4-5. Not full ./build.ps1 Test.'
        category = 'observation'
    },
    [ordered]@{
        timestamp = $now
        role = 'model'
        content = 'Independent ProductsControllerTests 2026-08-18T15:19:10Z to 15:19:23Z: Total 6 Failed 6 Passed 0 Skipped 0 EXIT=1, compiled, StatusCodeResult vs 201/400/409/404/403 and productScope missing. ProductClientTests 15:19:26Z to 15:19:30Z: Failed 1 Passed 0 Skipped 0 EXIT=1, NotImplementedException product client not implemented. REPL Products allow-list 15:19:34Z to 15:19:39Z: Failed 1 Passed 0 Skipped 0 EXIT=1, Products not advertised. todo_get MCP-PRODUCTS-001 Done=false. IProductService cs hits=0.'
        category = 'observation'
    },
    [ordered]@{
        timestamp = $now
        role = 'model'
        content = 'Decision: OverallVerdict AGREE on H3-red. Consequence: parent may start Phase 3 green; must not mark MCP-PRODUCTS-001 done; must not claim Phase 3 green, Phase 4-5, or full suite. Alternatives rejected: DISAGREE because no dedicated product_create MCP tool test (TEST-004 names ProductClientTests plus MCP/REPL allow-list tests; REPL allow-list exists and is red); DISAGREE because implementer omitted the missing McpServerClient.Products property (still the right red: Products is not advertised); DISAGREE because StatusCodeResult assertions do not print 501 (source is Status501NotImplemented). Affected: TR-MCP-PRODUCT-API-001, TEST-MCP-PRODUCT-003, TEST-MCP-PRODUCT-004, FR-MCP-PRODUCT-001, FR-MCP-PRODUCT-003.'
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
Save-Body -Name '_hv-h3-red-dialog.json' -Result $dialog
Write-Output ('DIALOG=' + ((Get-ToolObject -Result $dialog) | ConvertTo-Json -Depth 6 -Compress))

$actions = @(
    [ordered]@{ order = 1; description = 'add-profile: read 18 non-skill profile files'; type = 'edit'; status = 'completed'; filePath = 'C:\Users\kingd\.claude\profile' }
    [ordered]@{ order = 2; description = 'Test-MarkerSignature True; health nonce h3red23a90d8b31fb433b86c1edf54a99dd7b echoed'; type = 'edit'; status = 'completed'; filePath = 'F:\GitHub\McpServer\AGENTS-README-FIRST.yaml' }
    [ordered]@{ order = 3; description = 'Read ProductsController 501 stub, ProductClient NotImplementedException, FwhMcpTools.Products product_create stub'; type = 'edit'; status = 'completed'; filePath = 'src/McpServer.Support.Mcp/Controllers/ProductsController.cs' }
    [ordered]@{ order = 4; description = 'Independent ProductsControllerTests Failed 6 Passed 0 Skipped 0 compiled'; type = 'edit'; status = 'completed'; filePath = 'tests/McpServer.Support.Mcp.Tests/Controllers/ProductsControllerTests.cs' }
    [ordered]@{ order = 5; description = 'Independent ProductClientTests Failed 1 Passed 0 Skipped 0 NotImplementedException'; type = 'edit'; status = 'completed'; filePath = 'tests/McpServer.Client.Tests/ProductClientTests.cs' }
    [ordered]@{ order = 6; description = 'Independent REPL Products allow-list Failed 1 Passed 0 Skipped 0'; type = 'edit'; status = 'completed'; filePath = 'tests/McpServer.Repl.Core.Tests/GenericClientPassthroughValidClientNamesTests.cs' }
    [ordered]@{ order = 7; description = 'todo_get MCP-PRODUCTS-001 Done=false; requirements_list PRODUCT FR/TR/TEST/mapping'; type = 'edit'; status = 'completed'; filePath = 'MCP-PRODUCTS-001' }
    [ordered]@{ order = 8; description = 'Wrote hostile H3-red receipt pair'; type = 'create'; status = 'completed'; filePath = 'docs/receipts/hostile-validator-20260818T152430Z.md' }
    [ordered]@{ order = 9; description = 'Decision: H3-red AGREE; Phase 3 green may start; TODO stays Done=false'; type = 'design_decision'; status = 'completed'; filePath = 'docs/plans/mcp-products-001.md' }
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
Save-Body -Name '_hv-h3-red-actions.json' -Result $repl
Write-Output ('ACTIONS=' + ((Get-ToolObject -Result $repl) | ConvertTo-Json -Depth 6 -Compress))

$turn = [ordered]@{
    requestId = $requestId
    queryTitle = 'Hostile H3-red products adapter review'
    queryText = 'Hostile validator H3-red: attack Phase 3 adapter tests exist and fail for the right reason for MCP-PRODUCTS-001. Not claiming Phase 3 green, TODO done, Phase 4-5, or full unit suite.'
    response = "OverallVerdict AGREE. H3-red for MCP-PRODUCTS-001 Phase 3 adapters is complete.`n`nProductsControllerTests independently re-run: Failed 6, Passed 0, Skipped 0, Total 6, EXIT=1, compiled. Failures are StatusCodeResult vs 201/400/409/404/403 and missing productScope. ProductClientTests: Failed 1, Passed 0, Skipped 0, EXIT=1, NotImplementedException product client not implemented. REPL Products allow-list: Failed 1, Passed 0, Skipped 0, EXIT=1, Products not advertised. MCP-PRODUCTS-001 Done=false. ProductRequirementContextTests absent.`n`nReceipt: docs/receipts/hostile-validator-20260818T152430Z.md"
    interpretation = 'Operator asked for hostile validation of H3-red: Phase 3 adapter tests exist and fail for the right reason. TODO stays false. Phase 3 green and Phase 4-5 not claimed.'
    status = 'completed'
    tags = @('hostile-validator','H3-red','MCP-PRODUCTS-001','TR-MCP-PRODUCT-API-001','TEST-MCP-PRODUCT-003','TEST-MCP-PRODUCT-004')
    contextList = @(
        'docs/plans/mcp-products-001.md',
        'src/McpServer.Support.Mcp/Controllers/ProductsController.cs',
        'src/McpServer.Client/ProductClient.cs',
        'src/McpServer.Support.Mcp/McpStdio/FwhMcpTools.Products.cs',
        'tests/McpServer.Support.Mcp.Tests/Controllers/ProductsControllerTests.cs',
        'docs/receipts/hostile-validator-20260818T152430Z.md'
    )
    filesModified = @(
        'docs/receipts/hostile-validator-20260818T152430Z.md',
        'docs/receipts/hostile-validator-20260818T152430Z.json'
    )
    planFile = 'docs/plans/mcp-products-001.md'
    todoId = 'MCP-PRODUCTS-001'
    designDecisions = @(
        'AGREE H3-red. Adapter tests exist and are red for 501 / NotImplementedException / missing Products. Phase 3 green may start. TODO stays Done=false.'
    )
    requirementsDiscovered = @(
        'FR-MCP-PRODUCT-001',
        'FR-MCP-PRODUCT-003',
        'TR-MCP-PRODUCT-API-001',
        'TEST-MCP-PRODUCT-003',
        'TEST-MCP-PRODUCT-004'
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
Save-Body -Name '_hv-h3-red-complete.json' -Result $complete
Write-Output ('COMPLETE=' + ((Get-ToolObject -Result $complete) | ConvertTo-Json -Depth 6 -Compress))

$query = Invoke-McpTool -Name 'sessionlog_query' -Arguments @{
    workspacePath = $workspace
    agent = 'GrokCode'
    todoId = 'MCP-PRODUCTS-001'
    from = '2026-08-18T15:20:00Z'
    limit = 10
}
Save-Body -Name '_hv-h3-red-query-proof.json' -Result $query
$proof = Get-ToolObject -Result $query
Write-Output ('QUERY_KEYS=' + ($proof.PSObject.Properties.Name -join ','))
Write-Output ('QUERY=' + ($proof | ConvertTo-Json -Depth 8 -Compress))

Write-Output 'MCP3_DONE'
