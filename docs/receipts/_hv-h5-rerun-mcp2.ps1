#Requires -Version 7.0
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$workspace = 'F:\GitHub\McpServer'
$baseUrl = 'http://PAYTON-LEGION2:7147'
$sessionId = 'GrokCode-20260818T165022Z-h5-done-rerun-products'
$requestId = 'req-20260818T165022Z-001-hostile-h5-done-rerun'
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
    clientInfo = @{ name = 'hostile-validator-h5-rerun-2'; version = '1.0.0' }
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
    }
    [ordered]@{
        timestamp = $now
        role = 'model'
        content = 'Classified class 1 project requirement work. Surface C applies. Scoring Byrd at the H5-done gate. Independent Product filter 43/0/0. Launch 1/0/0. ValidateTraceability Succeeded. Independent ./build.ps1 Test Failed 0 Passed 1997 Skipped 0 after TrackingTodoService.CreateAsync lock. todo_get MCP-PRODUCTS-001 Done=false. IProductService cs count 0.'
        category = 'observation'
    }
    [ordered]@{
        timestamp = $now
        role = 'model'
        content = 'Observation: prior H5-done 20260818T163120Z DISAGREE was correct (independent Test Failed 1 on HandoffDurabilityTests). HandoffDurabilityTests.cs LastWriteUtc 2026-08-18T16:35:48Z now contains lock plus ContainsKey conflict before CreatedCount++. This review independently re-ran the official gate and it is green.'
        category = 'observation'
    }
    [ordered]@{
        timestamp = $now
        role = 'model'
        content = 'Decision: OverallVerdict AGREE on H5-done. Consequence: parent may mark MCP-PRODUCTS-001 done citing this receipt; this review does not flip the TODO. Alternatives rejected: DISAGREE because a prior independent run failed (that run predates the lock; this independent run is the gate); DISAGREE because live host has 0 product_* tools (deploy is out of scope unless the operator asks); DISAGREE because FR isSatisfied remains false (correct until after AGREE). Affected: MCP-PRODUCTS-001, all five FR-MCP-PRODUCT-*, TR-MCP-PRODUCT-*, TEST-MCP-PRODUCT-*.'
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
Save-Body -Name '_hv-h5-rerun-dialog.json' -Result $dialog
Write-Output ('DIALOG=' + ((Get-ToolObject -Result $dialog) | ConvertTo-Json -Depth 6 -Compress))

$actions = @(
    [ordered]@{ order = 1; description = 'add-profile: read 18 non-skill profile files'; type = 'edit'; status = 'completed'; filePath = 'C:\Users\kingd\.claude\profile' }
    [ordered]@{ order = 2; description = 'Test-MarkerSignature True; health nonce h5rrdedc9d09476c4219994655a628cdd074 echoed'; type = 'edit'; status = 'completed'; filePath = 'F:\GitHub\McpServer\AGENTS-README-FIRST.yaml' }
    [ordered]@{ order = 3; description = 'Grep IProductService src+tests count 0; product key regex present; 8 STDIO product_* tools dispatch'; type = 'edit'; status = 'completed'; filePath = 'src/McpServer.Support.Mcp/Products/ProductCqrsHelpers.cs' }
    [ordered]@{ order = 4; description = 'Independent Product filter Failed 0 Passed 43 Skipped 0; ProductsLaunchTests 1/0/0; ValidateTraceability Succeeded'; type = 'edit'; status = 'completed'; filePath = 'tests/McpServer.Support.Mcp.Tests' }
    [ordered]@{ order = 5; description = 'Independent ./build.ps1 Test Failed 0 Passed 1997/282/33/20/63/826/50 Skipped 0. TrackingTodoService.CreateAsync lock confirmed.'; type = 'edit'; status = 'completed'; filePath = 'docs/receipts/_hv-h5-rerun-full-test.txt' }
    [ordered]@{ order = 6; description = 'todo_get MCP-PRODUCTS-001 Done=false; requirements_list PRODUCT FR/TR/TEST/mappings; did not flip TODO'; type = 'edit'; status = 'completed'; filePath = 'MCP-PRODUCTS-001' }
    [ordered]@{ order = 7; description = 'Wrote hostile H5-done rerun receipt pair'; type = 'create'; status = 'completed'; filePath = 'docs/receipts/hostile-validator-20260818T165609Z.md' }
    [ordered]@{ order = 8; description = 'Decision: H5-done AGREE; TODO stays Done=false until parent flips it after this AGREE'; type = 'design_decision'; status = 'completed'; filePath = 'docs/plans/mcp-products-001.md' }
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
Save-Body -Name '_hv-h5-rerun-actions.json' -Result $repl
Write-Output ('ACTIONS=' + ((Get-ToolObject -Result $repl) | ConvertTo-Json -Depth 6 -Compress))

$turn = [ordered]@{
    requestId = $requestId
    queryTitle = 'Hostile H5-done rerun after handoff lock'
    queryText = 'Hostile validator H5-done rerun: attack MCP-PRODUCTS-001 done claim after prior DISAGREE 20260818T163120Z. Class 1. Do not mark TODO. Implementer claims TrackingTodoService.CreateAsync lock plus independent full Test 0 fail 0 skip and ValidateTraceability Succeeded. TODO stays Done=false until AGREE.'
    response = "OverallVerdict AGREE. H5-done for MCP-PRODUCTS-001 is earned on this independent re-run.`n`nIndependent ./build.ps1 Test Failed 0 Passed 1997 Skipped 0 (Client 282, Cqrs 33, Launcher 20, McpAgent 63, Repl.Core 826, QBAgent 50). TrackingTodoService.CreateAsync lock exists at HandoffDurabilityTests.cs:765-788. Product claims hold (CQRS-only, IProductService=0, Product filter 43/0/0, launch 1/0/0, docs, ValidateTraceability Succeeded). MCP-PRODUCTS-001 remains Done=false. This review did not flip the TODO.`n`nReceipt: docs/receipts/hostile-validator-20260818T165609Z.md"
    interpretation = 'Operator asked for hostile validation of the H5-done rerun claim on MCP-PRODUCTS-001 after prior DISAGREE. AGREE only if all five FRs, CQRS-only, isolation, DoD, full Test 0 fail 0 skip, and traceability Succeeded. Do not mark TODO.'
    status = 'completed'
    tags = @('hostile-validator','H5-done','MCP-PRODUCTS-001','AGREE','FR-MCP-PRODUCT-001','FR-MCP-PRODUCT-002','FR-MCP-PRODUCT-003','FR-MCP-PRODUCT-004','FR-MCP-PRODUCT-005')
    contextList = @(
        'docs/plans/mcp-products-001.md'
        'docs/receipts/hostile-validator-20260818T165609Z.md'
        'docs/receipts/hostile-validator-20260818T163120Z.md'
        'docs/receipts/_hv-h5-rerun-full-test.txt'
        'docs/receipts/_hv-h5-rerun-product.txt'
    )
    filesModified = @(
        'docs/receipts/hostile-validator-20260818T165609Z.md'
        'docs/receipts/hostile-validator-20260818T165609Z.json'
    )
    planFile = 'docs/plans/mcp-products-001.md'
    todoId = 'MCP-PRODUCTS-001'
    designDecisions = @(
        'AGREE H5-done. Independent full Test is Failed 0 Skipped 0 after the TrackingTodoService.CreateAsync lock. Product architecture holds. TODO stays Done=false; parent may flip it citing this receipt.'
    )
    requirementsDiscovered = @(
        'FR-MCP-PRODUCT-001'
        'FR-MCP-PRODUCT-002'
        'FR-MCP-PRODUCT-003'
        'FR-MCP-PRODUCT-004'
        'FR-MCP-PRODUCT-005'
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
Save-Body -Name '_hv-h5-rerun-complete.json' -Result $complete
Write-Output ('COMPLETE=' + ((Get-ToolObject -Result $complete) | ConvertTo-Json -Depth 6 -Compress))

$query = Invoke-McpTool -Name 'sessionlog_query' -Arguments @{
    workspacePath = $workspace
    agent = 'GrokCode'
    todoId = 'MCP-PRODUCTS-001'
    from = '2026-08-18T16:50:00Z'
    limit = 10
}
Save-Body -Name '_hv-h5-rerun-query-proof.json' -Result $query
$proof = Get-ToolObject -Result $query
Write-Output ('QUERY_KEYS=' + ($proof.PSObject.Properties.Name -join ','))
Write-Output ('QUERY=' + ($proof | ConvertTo-Json -Depth 10 -Compress))
Write-Output 'MCP2_DONE'
