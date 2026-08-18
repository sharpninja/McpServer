#Requires -Version 7.0
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$workspace = 'F:\GitHub\McpServer'
$baseUrl = 'http://PAYTON-LEGION2:7147'
$sessionId = 'GrokCode-20260818T173615Z-h5-skeptic-rerun'
$requestId = 'req-20260818T173615Z-001-hostile-h5-skeptic'
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
    clientInfo = @{ name = 'hostile-validator-h5-skeptic-2'; version = '1.0.0' }
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
        content = 'Classified class 1 project requirement work. Surface C applies. Scoring Byrd at the H5-done skeptic-rerun gate. Independent Product filter 50/0/0. ProductClientTests 2/0/0. Surface 6/0/0. Launch 2/0/0. ValidateTraceability Succeeded. Independent ./build.ps1 Test Failed 0 Passed 2004 Skipped 0. todo_get MCP-PRODUCTS-001 Done=false. IProductService cs count 0. Five skeptic bugs S1-S5 absent in current source.'
        category = 'observation'
    }
    [ordered]@{
        timestamp = $now
        role = 'model'
        content = 'Observation: prior H5-done 20260818T165609Z AGREE predates the five skeptic fixes (ProductShareHelper LastWriteUtc 17:21:41Z). Current MapFr attaches ToCriterionModels. context_search/pack dispatch GetProductRequirementContextQuery. RemoveMemberAsync is DeleteAsync of the DELETE body. requirements_effective exists in source. Launch scratch files are raw POST/GET JSON, not Passed 1 summaries.'
        category = 'observation'
    }
    [ordered]@{
        timestamp = $now
        role = 'model'
        content = 'Decision: OverallVerdict AGREE on H5-done skeptic rerun. Consequence: parent may mark MCP-PRODUCTS-001 done citing this receipt; this review does not flip the TODO. Alternatives rejected: DISAGREE because live host lacks requirements_effective (deploy is out of scope unless the operator asks); DISAGREE because launch functional arrays are empty (plan step 6 requires a sane envelope, and empty local functional is that envelope); DISAGREE because the plan markdown header still says Done: true (stale header after skeptic; native todo_get remains false). Affected: MCP-PRODUCTS-001, FR-MCP-PRODUCT-001..005, TR-MCP-PRODUCT-*, TEST-MCP-PRODUCT-*.'
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
Save-Body -Name '_hv-h5-skeptic-dialog.json' -Result $dialog
Write-Output ('DIALOG=' + ((Get-ToolObject -Result $dialog) | ConvertTo-Json -Depth 6 -Compress))

$actions = @(
    [ordered]@{ order = 1; description = 'add-profile: read 18 non-skill profile files'; type = 'edit'; status = 'completed'; filePath = 'C:\Users\kingd\.claude\profile' }
    [ordered]@{ order = 2; description = 'Marker HMAC MATCH=True; health nonce 31b4fcf2409a4a57b07165824bed67b4 echoed'; type = 'edit'; status = 'completed'; filePath = 'F:\GitHub\McpServer\AGENTS-README-FIRST.yaml' }
    [ordered]@{ order = 3; description = 'Read ProductShareHelper MapFr AC attach; FwhMcpTools context_search/pack dispatch; ProductClient DeleteAsync; requirements_effective'; type = 'edit'; status = 'completed'; filePath = 'src/McpServer.Support.Mcp/Products/ProductShareHelper.cs' }
    [ordered]@{ order = 4; description = 'Independent Product 50/0/0; ProductClientTests 2/0/0; Surface 6/0/0; Launch 2/0/0; ValidateTraceability Succeeded'; type = 'edit'; status = 'completed'; filePath = 'docs/receipts/_hv-h5-skeptic-focused-tests.txt' }
    [ordered]@{ order = 5; description = 'Independent ./build.ps1 Test Failed 0 Passed 2004/283/33/20/63/826/50 Skipped 0'; type = 'edit'; status = 'completed'; filePath = 'docs/receipts/_hv-h5-skeptic-full-test.txt' }
    [ordered]@{ order = 6; description = 'todo_get MCP-PRODUCTS-001 Done=false; did not flip TODO'; type = 'edit'; status = 'completed'; filePath = 'MCP-PRODUCTS-001' }
    [ordered]@{ order = 7; description = 'Wrote hostile H5 skeptic-rerun receipt pair'; type = 'create'; status = 'completed'; filePath = 'docs/receipts/hostile-validator-20260818T174337Z.md' }
    [ordered]@{ order = 8; description = 'Decision: H5-done skeptic rerun AGREE; TODO stays Done=false until parent flips it after this AGREE'; type = 'design_decision'; status = 'completed'; filePath = 'docs/plans/mcp-products-001.md' }
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
Save-Body -Name '_hv-h5-skeptic-actions.json' -Result $repl
Write-Output ('ACTIONS=' + ((Get-ToolObject -Result $repl) | ConvertTo-Json -Depth 6 -Compress))

$turn = [ordered]@{
    requestId = $requestId
    queryTitle = 'Hostile H5 skeptic rerun MCP-PRODUCTS-001'
    queryText = 'Hostile validator H5-done skeptic rerun after skeptic rejected a prior done claim. Attack A1-A8 plus S1-S5. Class 1. Do not mark MCP-PRODUCTS-001 done.'
    response = "OverallVerdict AGREE. H5-done skeptic rerun for MCP-PRODUCTS-001 is earned on this independent pass.`n`nIndependent ./build.ps1 Test Failed 0 Passed 2004 Skipped 0 (Client 283, Cqrs 33, Launcher 20, McpAgent 63, Repl.Core 826, QBAgent 50). Product 50/0/0. ProductClientTests 2/0/0. Surface 6/0/0. Launch 2/0/0. ValidateTraceability Succeeded. S1-S5 are absent in current source. MCP-PRODUCTS-001 remains Done=false. This review did not flip the TODO.`n`nReceipt: docs/receipts/hostile-validator-20260818T174337Z.md"
    interpretation = 'Operator asked for hostile validation of the H5-done skeptic rerun after a skeptic rejected a prior done claim. AGREE only if A+B+C+D pass and S1-S5 stay fixed. Do not mark TODO.'
    status = 'completed'
    tags = @('hostile-validator','H5-done','skeptic-rerun','MCP-PRODUCTS-001','AGREE','FR-MCP-PRODUCT-001','FR-MCP-PRODUCT-002','FR-MCP-PRODUCT-003','FR-MCP-PRODUCT-004','FR-MCP-PRODUCT-005')
    contextList = @(
        'docs/plans/mcp-products-001.md'
        'docs/receipts/hostile-validator-20260818T174337Z.md'
        'docs/receipts/hostile-validator-20260818T165609Z.md'
        'docs/receipts/_hv-h5-skeptic-full-test.txt'
        'docs/receipts/_hv-h5-skeptic-todo.json'
    )
    filesModified = @(
        'docs/receipts/hostile-validator-20260818T174337Z.md'
        'docs/receipts/hostile-validator-20260818T174337Z.json'
    )
    planFile = 'docs/plans/mcp-products-001.md'
    todoId = 'MCP-PRODUCTS-001'
    designDecisions = @(
        'AGREE H5-done skeptic rerun. Independent full Test is Failed 0 Skipped 0. Five skeptic bugs are fixed in source and tests. TODO stays Done=false; parent may flip it citing this receipt.'
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
Save-Body -Name '_hv-h5-skeptic-complete.json' -Result $complete
Write-Output ('COMPLETE=' + ((Get-ToolObject -Result $complete) | ConvertTo-Json -Depth 6 -Compress))

$query = Invoke-McpTool -Name 'sessionlog_query' -Arguments @{
    workspacePath = $workspace
    agent = 'GrokCode'
    todoId = 'MCP-PRODUCTS-001'
    from = '2026-08-18T17:36:00Z'
    limit = 10
}
Save-Body -Name '_hv-h5-skeptic-query-proof.json' -Result $query
$proof = Get-ToolObject -Result $query
Write-Output ('QUERY_KEYS=' + ($proof.PSObject.Properties.Name -join ','))
Write-Output ('QUERY=' + ($proof | ConvertTo-Json -Depth 10 -Compress))
Write-Output 'MCP2_DONE'
