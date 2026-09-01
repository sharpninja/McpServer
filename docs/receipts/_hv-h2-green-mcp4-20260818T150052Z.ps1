#Requires -Version 7.0
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$workspace = 'F:\GitHub\McpServer'
$baseUrl = 'http://PAYTON-LEGION2:7147'
$sessionId = 'GrokCode-20260818T150052Z-h2-green-products'
$requestId = 'req-20260818T150052Z-001-hostile-h2-green-products'
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
    } finally {
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
    Write-Output ("SAVED " + $Name + " HTTP=" + $Result.Status + " LEN=" + $Result.Body.Length)
}

[void](Invoke-McpRpc -Method 'initialize' -Params @{
    protocolVersion = '2025-03-26'
    capabilities = @{}
    clientInfo = @{ name = 'hostile-validator-h2-green-4'; version = '1.0.0' }
})
[void](Invoke-McpRpc -Method 'notifications/initialized' -Params @{})

$open = Invoke-McpTool -Name 'sessionlog_open' -Arguments @{
    agent = 'GrokCode'
    sessionId = $sessionId
    workspacePath = $workspace
    title = 'Hostile H2-green products share review'
    model = 'grok'
}
Save-Body -Name '_hv-h2-green-open4.json' -Result $open

$begin = Invoke-McpTool -Name 'sessionlog_begin_turn' -Arguments @{
    agent = 'GrokCode'
    sessionId = $sessionId
    requestId = $requestId
    workspacePath = $workspace
    planFile = 'docs/plans/mcp-products-001.md'
    todoId = 'MCP-PRODUCTS-001'
    queryTitle = 'Hostile H2-green products share review'
    queryText = 'Hostile validator H2-green: attack Phase 2 share implementation claims for MCP-PRODUCTS-001. Not claiming TODO done, Phase 3-5, or full unit suite.'
}
Save-Body -Name '_hv-h2-green-begin4.json' -Result $begin

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
        content = 'Classified class 1 project requirement work. Surface C applies. Scoring Byrd at the H2-green gate: handler-owned share, provenance, named ACs green, focused filter 32/0/0 and official Product+RequirementScope 37/0/0. Not MCP-PRODUCTS-001 done. Not Phase 3-5. Not full ./build.ps1 Test.'
        category = 'observation'
    },
    [ordered]@{
        timestamp = $now
        role = 'model'
        content = 'Independent focused filter 2026-08-18T15:01:05Z to 15:01:24Z: Total 32 Passed 32 Failed 0 Skipped 0 EXIT=0. Official H2-green filter Product|RequirementScopeLayerServiceTests 15:01:24Z to 15:01:42Z: Total 37 Passed 37 Failed 0 Skipped 0 EXIT=0. ProductShareHelper is internal static and only called from GetProductEffectiveRequirementsQueryHandler(McpDbContext). IProductService cs hits=0. todo_get MCP-PRODUCTS-001 Done=false.'
        category = 'observation'
    },
    [ordered]@{
        timestamp = $now
        role = 'model'
        content = 'Decision: OverallVerdict AGREE on H2-green. Consequence: parent may start Phase 3 red; must not mark MCP-PRODUCTS-001 done; must not claim Phase 3-5 or full suite. Alternatives rejected: DISAGREE because REST GetEffectiveRequirements is still local-only (disclosed; plan allows dedicated CQRS query; productScope REST is Phase 3); DISAGREE because LocalDelete does not issue update-fr (not a Phase 2 named case; TEST-002 Condition omits it); DISAGREE because existing RequirementScopeLayerServiceTests were not extended (official gate still green; zero-product covered in handler tests). Affected: FR-MCP-PRODUCT-003, FR-MCP-PRODUCT-004, TR-MCP-PRODUCT-SHARE-001, TEST-MCP-PRODUCT-002.'
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
Save-Body -Name '_hv-h2-green-dialog4.json' -Result $dialog

$actions = @(
    [ordered]@{ order = 1; description = 'add-profile: read 18 non-skill profile files'; type = 'edit'; status = 'completed'; filePath = 'C:\Users\kingd\.claude\profile' }
    [ordered]@{ order = 2; description = 'Test-MarkerSignature True; health nonce f60fc58e16c0412683764728f653bdf5 echoed'; type = 'edit'; status = 'completed'; filePath = 'F:\GitHub\McpServer\AGENTS-README-FIRST.yaml' }
    [ordered]@{ order = 3; description = 'Read ProductShareHelper and GetProductEffectiveRequirementsQueryHandler'; type = 'edit'; status = 'completed'; filePath = 'src/McpServer.Support.Mcp/Products/ProductShareHelper.cs' }
    [ordered]@{ order = 4; description = 'Independent focused Product filter Failed 0 Passed 32 Skipped 0'; type = 'edit'; status = 'completed'; filePath = 'tests/McpServer.Support.Mcp.Tests' }
    [ordered]@{ order = 5; description = 'Independent official H2-green Product+RequirementScope filter Failed 0 Passed 37 Skipped 0'; type = 'edit'; status = 'completed'; filePath = 'tests/McpServer.Support.Mcp.Tests' }
    [ordered]@{ order = 6; description = 'todo_get MCP-PRODUCTS-001 Done=false; requirements_list PRODUCT FR/TR/TEST/mapping'; type = 'edit'; status = 'completed'; filePath = 'MCP-PRODUCTS-001' }
    [ordered]@{ order = 7; description = 'Wrote hostile H2-green receipt pair'; type = 'create'; status = 'completed'; filePath = 'docs/receipts/hostile-validator-20260818T150200Z.md' }
    [ordered]@{ order = 8; description = 'Decision: H2-green AGREE; Phase 3 red may start; TODO stays Done=false'; type = 'design_decision'; status = 'completed'; filePath = 'docs/plans/mcp-products-001.md' }
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
Save-Body -Name '_hv-h2-green-actions4.json' -Result $repl

$turn = [ordered]@{
    requestId = $requestId
    queryTitle = 'Hostile H2-green products share review'
    queryText = 'Hostile validator H2-green: attack Phase 2 share implementation claims for MCP-PRODUCTS-001. Not claiming TODO done, Phase 3-5, or full unit suite.'
    response = "OverallVerdict AGREE. H2-green for MCP-PRODUCTS-001 Phase 2 share is complete.`n`nFocused filter independently re-run: Failed 0, Passed 32, Skipped 0, Total 32, EXIT=0. Official Product+RequirementScope filter: Failed 0, Passed 37, Skipped 0, Total 37, EXIT=0. Handler injects McpDbContext and calls internal ProductShareHelper. No public IProductService. MCP-PRODUCTS-001 Done=false. Phase 3-5 files absent.`n`nReceipt: docs/receipts/hostile-validator-20260818T150200Z.md"
    interpretation = 'Operator asked for hostile validation of H2-green: Phase 2 share implementation exists, named ACs pass, TODO stays false, Phase 3-5 not claimed.'
    status = 'completed'
    tags = @('hostile-validator','H2-green','MCP-PRODUCTS-001','FR-MCP-PRODUCT-003','FR-MCP-PRODUCT-004','TEST-MCP-PRODUCT-002')
    contextList = @(
        'docs/plans/mcp-products-001.md',
        'src/McpServer.Support.Mcp/Products/ProductShareHelper.cs',
        'src/McpServer.Support.Mcp/Products/Queries/GetProductEffectiveRequirementsQuery.cs',
        'tests/McpServer.Support.Mcp.Tests/Products/GetProductEffectiveRequirementsQueryHandlerTests.cs',
        'docs/receipts/hostile-validator-20260818T150200Z.md'
    )
    filesModified = @(
        'docs/receipts/hostile-validator-20260818T150200Z.md',
        'docs/receipts/hostile-validator-20260818T150200Z.json'
    )
    planFile = 'docs/plans/mcp-products-001.md'
    todoId = 'MCP-PRODUCTS-001'
    designDecisions = @(
        'AGREE H2-green. Share is handler-owned via internal ProductShareHelper. REST remains local-only and is Phase 3. TODO stays Done=false.'
    )
    requirementsDiscovered = @(
        'FR-MCP-PRODUCT-003',
        'FR-MCP-PRODUCT-004',
        'TR-MCP-PRODUCT-SHARE-001',
        'TEST-MCP-PRODUCT-002'
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
Save-Body -Name '_hv-h2-green-complete4.json' -Result $complete

$query = Invoke-McpTool -Name 'sessionlog_query' -Arguments @{
    workspacePath = $workspace
    agent = 'GrokCode'
    todoId = 'MCP-PRODUCTS-001'
    from = '2026-08-18T15:00:00Z'
    limit = 10
}
Save-Body -Name '_hv-h2-green-query-proof.json' -Result $query

Write-Output 'MCP4_DONE'
