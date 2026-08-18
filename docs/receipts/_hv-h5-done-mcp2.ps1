#Requires -Version 7.0
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$workspace = 'F:\GitHub\McpServer'
$baseUrl = 'http://PAYTON-LEGION2:7147'
$sessionId = 'GrokCode-20260818T162441Z-h5-done-products'
$requestId = 'req-20260818T162441Z-001-hostile-h5-done-products'
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
    clientInfo = @{ name = 'hostile-validator-h5-done-2'; version = '1.0.0' }
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
        content = 'Classified class 1 project requirement work. Surface C applies. Scoring Byrd at the H5-done gate. Independent Product filter 43/0/0. Launch 1/0/0. ValidateTraceability Succeeded. Independent ./build.ps1 Test Failed 1 Passed 1996 Skipped 0 on HandoffDurabilityTests.ApproveAsync_LeaseExpiresDuringLiveCreate_SecondInstanceWins. Isolated rerun of that test Passed 1. todo_get MCP-PRODUCTS-001 Done=false. IProductService cs count 0.'
        category = 'observation'
    }
    [ordered]@{
        timestamp = $now
        role = 'model'
        content = 'Observation: implementer on-disk build-test.txt is green 1997/0/0 at 11:18:47 AM. This review independently reproduced the disclosed flake on the official H5 gate. Isolated green is not the plan gate.'
        category = 'observation'
    }
    [ordered]@{
        timestamp = $now
        role = 'model'
        content = 'Decision: OverallVerdict DISAGREE on H5-done. Consequence: parent must not mark MCP-PRODUCTS-001 done; must not treat the done claim as earned. Alternatives rejected: AGREE because Product implementation and docs hold (DoD still requires full suite 0 fail 0 skip); AGREE because implementer had a later green full Test (this review independently failed the same gate); AGREE after isolated Handoff pass (hostile-on-goal-state forbids treating a focused green as the full-suite exit). Affected: MCP-PRODUCTS-001, all five FR-MCP-PRODUCT-*, TR-MCP-PRODUCT-*, TEST-MCP-PRODUCT-*.'
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
Save-Body -Name '_hv-h5-done-dialog.json' -Result $dialog
Write-Output ('DIALOG=' + ((Get-ToolObject -Result $dialog) | ConvertTo-Json -Depth 6 -Compress))

$actions = @(
    [ordered]@{ order = 1; description = 'add-profile: read 18 non-skill profile files'; type = 'edit'; status = 'completed'; filePath = 'C:\Users\kingd\.claude\profile' }
    [ordered]@{ order = 2; description = 'Test-MarkerSignature True; health nonce h5dn879addf703704b24a7a638d03e39 echoed'; type = 'edit'; status = 'completed'; filePath = 'F:\GitHub\McpServer\AGENTS-README-FIRST.yaml' }
    [ordered]@{ order = 3; description = 'Grep IProductService src+tests count 0; product key regex present; 8 STDIO product_* tools dispatch'; type = 'edit'; status = 'completed'; filePath = 'src/McpServer.Support.Mcp/Products/ProductCqrsHelpers.cs' }
    [ordered]@{ order = 4; description = 'Independent Product filter Failed 0 Passed 43 Skipped 0; ProductsLaunchTests 1/0/0; ValidateTraceability Succeeded'; type = 'edit'; status = 'completed'; filePath = 'tests/McpServer.Support.Mcp.Tests' }
    [ordered]@{ order = 5; description = 'Independent ./build.ps1 Test Failed 1 Passed 1996 Skipped 0 on HandoffDurabilityTests. Isolated rerun Passed 1'; type = 'edit'; status = 'completed'; filePath = 'docs/receipts/_hv-h5-done-full-test.txt' }
    [ordered]@{ order = 6; description = 'todo_get MCP-PRODUCTS-001 Done=false; requirements_list PRODUCT FR/TR/TEST/mappings; did not flip TODO'; type = 'edit'; status = 'completed'; filePath = 'MCP-PRODUCTS-001' }
    [ordered]@{ order = 7; description = 'Wrote hostile H5-done receipt pair'; type = 'create'; status = 'completed'; filePath = 'docs/receipts/hostile-validator-20260818T163120Z.md' }
    [ordered]@{ order = 8; description = 'Decision: H5-done DISAGREE; TODO stays Done=false; full suite gate not independently green'; type = 'design_decision'; status = 'completed'; filePath = 'docs/plans/mcp-products-001.md' }
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
Save-Body -Name '_hv-h5-done-actions.json' -Result $repl
Write-Output ('ACTIONS=' + ((Get-ToolObject -Result $repl) | ConvertTo-Json -Depth 6 -Compress))

$turn = [ordered]@{
    requestId = $requestId
    queryTitle = 'Hostile H5-done products review'
    queryText = 'Hostile validator H5-done: attack MCP-PRODUCTS-001 done claim. Class 1. Do not mark TODO. Implementer claims all five FRs, CQRS-only, isolation, DoD, full Test 0 fail 0 skip, ValidateTraceability Succeeded. TODO stays Done=false until AGREE.'
    response = "OverallVerdict DISAGREE. H5-done for MCP-PRODUCTS-001 is not earned.`n`nIndependent ./build.ps1 Test Failed 1 Passed 1996 Skipped 0 on HandoffDurabilityTests.ApproveAsync_LeaseExpiresDuringLiveCreate_SecondInstanceWins (Expected 1 Actual 2). Isolated rerun of that test passed. Product implementation claims hold (CQRS-only, IProductService=0, Product filter 43/0/0, launch 1/0/0, docs, ValidateTraceability Succeeded). MCP-PRODUCTS-001 remains Done=false. This review did not flip the TODO.`n`nReceipt: docs/receipts/hostile-validator-20260818T163120Z.md"
    interpretation = 'Operator asked for hostile validation of the H5-done claim on MCP-PRODUCTS-001. AGREE only if all five FRs, CQRS-only, isolation, DoD, full Test 0 fail 0 skip, and traceability Succeeded. Do not mark TODO.'
    status = 'completed'
    tags = @('hostile-validator','H5-done','MCP-PRODUCTS-001','DISAGREE','FR-MCP-PRODUCT-001','FR-MCP-PRODUCT-002','FR-MCP-PRODUCT-003','FR-MCP-PRODUCT-004','FR-MCP-PRODUCT-005')
    contextList = @(
        'docs/plans/mcp-products-001.md'
        'docs/receipts/hostile-validator-20260818T163120Z.md'
        'docs/receipts/_hv-h5-done-full-test.txt'
        'docs/receipts/_hv-h5-done-test-product.txt'
    )
    filesModified = @(
        'docs/receipts/hostile-validator-20260818T163120Z.md'
        'docs/receipts/hostile-validator-20260818T163120Z.json'
    )
    planFile = 'docs/plans/mcp-products-001.md'
    todoId = 'MCP-PRODUCTS-001'
    designDecisions = @(
        'DISAGREE H5-done. Independent full Test failed the official gate on a flaky handoff lease race. Product architecture holds. TODO stays Done=false.'
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
Save-Body -Name '_hv-h5-done-complete.json' -Result $complete
Write-Output ('COMPLETE=' + ((Get-ToolObject -Result $complete) | ConvertTo-Json -Depth 6 -Compress))

$query = Invoke-McpTool -Name 'sessionlog_query' -Arguments @{
    workspacePath = $workspace
    agent = 'GrokCode'
    todoId = 'MCP-PRODUCTS-001'
    from = '2026-08-18T16:24:00Z'
    limit = 10
}
Save-Body -Name '_hv-h5-done-query-proof.json' -Result $query
$proof = Get-ToolObject -Result $query
Write-Output ('QUERY_KEYS=' + ($proof.PSObject.Properties.Name -join ','))
Write-Output ('QUERY=' + ($proof | ConvertTo-Json -Depth 10 -Compress))
Write-Output 'MCP2_DONE'
