#Requires -Version 7.0
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$workspace = 'F:\GitHub\McpServer'
$baseUrl = 'http://PAYTON-LEGION2:7147'
$sessionId = 'GrokCode-20260818T153649Z-h3-green-products'
$requestId = 'req-20260818T153649Z-001-hostile-h3-green-products'
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
    clientInfo = @{ name = 'hostile-validator-h3-green-2'; version = '1.0.0' }
}
Save-Body -Name '_hv-h3-green-init.json' -Result $init
Write-Output ('INIT_HTTP=' + $init.Status)
[void](Invoke-McpRpc -Method 'notifications/initialized' -Params @{})

foreach ($kind in @('fr', 'tr', 'test', 'mapping')) {
    $res = Invoke-McpTool -Name 'requirements_list' -Arguments @{
        workspacePath = $workspace
        type = $kind
    }
    Save-Body -Name ('_hv-h3-green-req-' + $kind + '.json') -Result $res
    $parsed = Get-ToolObject -Result $res
    $items = @()
    foreach ($name in @('items', 'Items', 'requirements', 'Requirements', 'mappings', 'Mappings')) {
        if ($parsed.PSObject.Properties.Name -contains $name -and $null -ne $parsed.$name) {
            $items = @($parsed.$name)
            break
        }
    }
    if ($items.Count -eq 0 -and $parsed -is [System.Array]) { $items = @($parsed) }
    Write-Output ('KIND=' + $kind + ' RAW_KEYS=' + ($parsed.PSObject.Properties.Name -join ','))
    Write-Output (($kind.ToUpper() + '_TOTAL=' + $items.Count))
    foreach ($item in $items) {
        $blob = ($item | ConvertTo-Json -Depth 10 -Compress)
        if ($blob -match 'PRODUCT') {
            $clip = $blob
            if ($clip.Length -gt 900) { $clip = $clip.Substring(0, 900) }
            Write-Output ('PRODUCT_' + $kind.ToUpper() + ' ' + $clip)
        }
    }
}

$open = Invoke-McpTool -Name 'sessionlog_open' -Arguments @{
    agent = 'GrokCode'
    sessionId = $sessionId
    workspacePath = $workspace
    title = 'Hostile H3-green products adapter review'
    model = 'grok'
}
Save-Body -Name '_hv-h3-green-open.json' -Result $open
Write-Output ('OPEN_TEXT=' + ((Get-ToolObject -Result $open) | ConvertTo-Json -Depth 6 -Compress))

$begin = Invoke-McpTool -Name 'sessionlog_begin_turn' -Arguments @{
    agent = 'GrokCode'
    sessionId = $sessionId
    requestId = $requestId
    workspacePath = $workspace
    planFile = 'docs/plans/mcp-products-001.md'
    todoId = 'MCP-PRODUCTS-001'
    queryTitle = 'Hostile H3-green products adapter review'
    queryText = 'Hostile validator H3-green: attack Phase 3 adapters implemented for MCP-PRODUCTS-001. Not claiming TODO done, Phase 4-5, or full unit suite.'
}
Save-Body -Name '_hv-h3-green-begin.json' -Result $begin
Write-Output ('BEGIN_TEXT=' + ((Get-ToolObject -Result $begin) | ConvertTo-Json -Depth 8 -Compress))

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
        content = 'Classified class 1 project requirement work. Surface C applies. Scoring Byrd at the H3-green gate: thin IDispatcher adapters, ProductClient, REPL PRODUCTS, MCP product_* tools, productScope on effective. Not MCP-PRODUCTS-001 done. Not Phase 4-5. Not full ./build.ps1 Test.'
        category = 'observation'
    },
    [ordered]@{
        timestamp = $now
        role = 'model'
        content = 'Independent Support filter ProductsControllerTests|Products|ProductEntityTests|ProductMigrationApplyTests 2026-08-18T15:37:00Z to 15:37:20Z: Total 38 Failed 0 Passed 38 Skipped 0 EXIT=0. ProductClientTests 15:37:24Z to 15:37:27Z: Failed 0 Passed 1 Skipped 0 EXIT=0. REPL Products allow-list 15:37:32Z to 15:37:37Z: Failed 0 Passed 1 Skipped 0 EXIT=0. Extra RequirementsClient filter 15:37:39Z to 15:37:43Z: Failed 0 Passed 23 Skipped 0 EXIT=0. todo_get MCP-PRODUCTS-001 Done=false. IProductService cs hits=0.'
        category = 'observation'
    },
    [ordered]@{
        timestamp = $now
        role = 'model'
        content = 'Decision: OverallVerdict AGREE on H3-green. Consequence: parent may start Phase 4 red; must not mark MCP-PRODUCTS-001 done; must not claim Phase 4-5 or full suite. Alternatives rejected: DISAGREE because no dedicated FwhMcpTools product_* dispatch test (TEST-004 names ProductClientTests plus MCP/REPL allow-list tests; REPL allow-list is green and all eight tools dispatch IDispatcher in source); DISAGREE because RequirementsEffective_AcceptsProductScopeQuery only reflects the parameter name (implementation does dispatch GetProductEffectiveRequirementsQuery when IDispatcher is present); DISAGREE because existing RequirementsClient effective test does not assert productScope (plan gate is existing tests stay green; they did). Affected: TR-MCP-PRODUCT-API-001, TEST-MCP-PRODUCT-003, TEST-MCP-PRODUCT-004, FR-MCP-PRODUCT-001, FR-MCP-PRODUCT-003.'
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
Save-Body -Name '_hv-h3-green-dialog.json' -Result $dialog
Write-Output ('DIALOG=' + ((Get-ToolObject -Result $dialog) | ConvertTo-Json -Depth 6 -Compress))

$actions = @(
    [ordered]@{ order = 1; description = 'add-profile: read 18 non-skill profile files'; type = 'edit'; status = 'completed'; filePath = 'C:\Users\kingd\.claude\profile' }
    [ordered]@{ order = 2; description = 'Test-MarkerSignature True; health nonce h3grnc51a1f7db068447aa0bdd0e4368 echoed'; type = 'edit'; status = 'completed'; filePath = 'F:\GitHub\McpServer\AGENTS-README-FIRST.yaml' }
    [ordered]@{ order = 3; description = 'Read ProductsController IDispatcher-only; ProductClient posts mcpserver/products; FwhMcpTools eight product_* tools dispatch'; type = 'edit'; status = 'completed'; filePath = 'src/McpServer.Support.Mcp/Controllers/ProductsController.cs' }
    [ordered]@{ order = 4; description = 'Independent Support Product filter Failed 0 Passed 38 Skipped 0 EXIT=0'; type = 'edit'; status = 'completed'; filePath = 'tests/McpServer.Support.Mcp.Tests/Controllers/ProductsControllerTests.cs' }
    [ordered]@{ order = 5; description = 'Independent ProductClientTests Failed 0 Passed 1 Skipped 0 EXIT=0'; type = 'edit'; status = 'completed'; filePath = 'tests/McpServer.Client.Tests/ProductClientTests.cs' }
    [ordered]@{ order = 6; description = 'Independent REPL Products allow-list Failed 0 Passed 1 Skipped 0 EXIT=0'; type = 'edit'; status = 'completed'; filePath = 'tests/McpServer.Repl.Core.Tests/GenericClientPassthroughValidClientNamesTests.cs' }
    [ordered]@{ order = 7; description = 'todo_get MCP-PRODUCTS-001 Done=false; requirements_list PRODUCT FR/TR/TEST/mapping'; type = 'edit'; status = 'completed'; filePath = 'MCP-PRODUCTS-001' }
    [ordered]@{ order = 8; description = 'Wrote hostile H3-green receipt pair'; type = 'create'; status = 'completed'; filePath = 'docs/receipts/hostile-validator-20260818T154000Z.md' }
    [ordered]@{ order = 9; description = 'Decision: H3-green AGREE; Phase 4 red may start; TODO stays Done=false'; type = 'design_decision'; status = 'completed'; filePath = 'docs/plans/mcp-products-001.md' }
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
Save-Body -Name '_hv-h3-green-actions.json' -Result $repl
Write-Output ('ACTIONS=' + ((Get-ToolObject -Result $repl) | ConvertTo-Json -Depth 6 -Compress))

$turn = [ordered]@{
    requestId = $requestId
    queryTitle = 'Hostile H3-green products adapter review'
    queryText = 'Hostile validator H3-green: attack Phase 3 adapters implemented for MCP-PRODUCTS-001. Not claiming TODO done, Phase 4-5, or full unit suite.'
    response = "OverallVerdict AGREE. H3-green for MCP-PRODUCTS-001 Phase 3 adapters is complete.`n`nSupport Product filter independently re-run: Failed 0, Passed 38, Skipped 0, Total 38, EXIT=0. ProductClientTests: Failed 0, Passed 1, Skipped 0, EXIT=0. REPL Products allow-list: Failed 0, Passed 1, Skipped 0, EXIT=0. Extra RequirementsClient filter: Failed 0, Passed 23, Skipped 0, EXIT=0. MCP-PRODUCTS-001 Done=false. ProductRequirementContextTests absent. IProductService cs hits=0.`n`nReceipt: docs/receipts/hostile-validator-20260818T154000Z.md"
    interpretation = 'Operator asked for hostile validation of H3-green: Phase 3 adapters implemented. TODO stays false. Phase 4-5 and full suite not claimed.'
    status = 'completed'
    tags = @('hostile-validator','H3-green','MCP-PRODUCTS-001','TR-MCP-PRODUCT-API-001','TEST-MCP-PRODUCT-003','TEST-MCP-PRODUCT-004')
    contextList = @(
        'docs/plans/mcp-products-001.md',
        'src/McpServer.Support.Mcp/Controllers/ProductsController.cs',
        'src/McpServer.Client/ProductClient.cs',
        'src/McpServer.Support.Mcp/McpStdio/FwhMcpTools.Products.cs',
        'src/McpServer.Support.Mcp/Controllers/RequirementsController.cs',
        'docs/receipts/hostile-validator-20260818T154000Z.md'
    )
    filesModified = @(
        'docs/receipts/hostile-validator-20260818T154000Z.md',
        'docs/receipts/hostile-validator-20260818T154000Z.json'
    )
    planFile = 'docs/plans/mcp-products-001.md'
    todoId = 'MCP-PRODUCTS-001'
    designDecisions = @(
        'AGREE H3-green. Adapters dispatch IDispatcher only. Named filters green with zero skips. Phase 4 red may start. TODO stays Done=false.'
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
Save-Body -Name '_hv-h3-green-complete.json' -Result $complete
Write-Output ('COMPLETE=' + ((Get-ToolObject -Result $complete) | ConvertTo-Json -Depth 6 -Compress))

$query = Invoke-McpTool -Name 'sessionlog_query' -Arguments @{
    workspacePath = $workspace
    agent = 'GrokCode'
    todoId = 'MCP-PRODUCTS-001'
    from = '2026-08-18T15:36:00Z'
    limit = 10
}
Save-Body -Name '_hv-h3-green-query-proof.json' -Result $query
$proof = Get-ToolObject -Result $query
Write-Output ('QUERY_KEYS=' + ($proof.PSObject.Properties.Name -join ','))
Write-Output ('QUERY=' + ($proof | ConvertTo-Json -Depth 8 -Compress))

Write-Output 'MCP2_DONE'
