#Requires -Version 7.0
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

. 'F:\GitHub\mcpserver-grok-plugin\lib\marker-resolver.ps1'

$workspace = 'F:\GitHub\McpServer'
$marker = Join-Path $workspace 'AGENTS-README-FIRST.yaml'
$baseUrl = 'http://PAYTON-LEGION2:7147'
$outDir = Join-Path $workspace 'docs\receipts'
$utcStamp = [datetime]::UtcNow.ToString('yyyyMMddTHHMMSSZ')
$sessionId = "GrokCode-$utcStamp-h4-green-products"
$requestId = "req-$utcStamp-001-hostile-h4-green-products"

@(
    'STAMP=' + $utcStamp
    'SESSION_ID=' + $sessionId
    'REQUEST_ID=' + $requestId
) | Set-Content -LiteralPath (Join-Path $outDir '_hv-h4-green-ids.txt') -Encoding utf8

Write-Output ('UTC_STAMP=' + $utcStamp)
Write-Output ('SESSION_ID=' + $sessionId)
Write-Output ('REQUEST_ID=' + $requestId)

Write-Output '=== PLUGIN_VERSION ==='
$pluginJson = Get-Content -LiteralPath 'F:\GitHub\mcpserver-grok-plugin\.grok-plugin\plugin.json' -Raw | ConvertFrom-Json
$pluginVer = (Get-Content -LiteralPath 'F:\GitHub\mcpserver-grok-plugin\.version' -Raw).Trim()
Write-Output ('PLUGIN_JSON_VERSION=' + $pluginJson.version)
Write-Output ('PLUGIN_VERSION_FILE=' + $pluginVer)

Write-Output '=== MARKER_SIGNATURE ==='
$sigOk = Test-MarkerSignature -MarkerFile $marker
Write-Output ('Test-MarkerSignature=' + $sigOk)

Write-Output '=== HEALTH_NONCE ==='
$nonce = 'h4grn' + [guid]::NewGuid().ToString('N').Substring(0, 27)
$health = Invoke-WebRequest -Uri ($baseUrl + '/health?nonce=' + $nonce) -UseBasicParsing -TimeoutSec 15
$healthJson = $health.Content | ConvertFrom-Json
Write-Output ('HealthStatusCode=' + [int]$health.StatusCode)
Write-Output ('HealthNonceSent=' + $nonce)
Write-Output ('HealthNonceEcho=' + $healthJson.nonce)
Write-Output ('HealthNonceMatch=' + ($healthJson.nonce -eq $nonce))
Write-Output ('HealthStatusText=' + $healthJson.status)
Write-Output ('HealthVersion=' + $healthJson.version)
Write-Output ('HealthStorage=' + $healthJson.storage)
Write-Output ('FULL_BOOTSTRAP=' + ($sigOk -and ($healthJson.nonce -eq $nonce) -and ([int]$health.StatusCode -eq 200)))
$nonce | Set-Content -LiteralPath (Join-Path $outDir '_hv-h4-green-nonce.txt') -Encoding utf8

Write-Output '=== TOOL_REGISTRY ==='
$apiKey = (Get-MarkerField -MarkerFile $marker -FieldName 'apiKey')
$headers = @{ 'X-Api-Key' = $apiKey }
$search = Invoke-WebRequest -Uri ($baseUrl + '/mcpserver/tools/search?keyword=mcpserver-grok-plugin') -Headers $headers -UseBasicParsing -TimeoutSec 30
$searchPath = Join-Path $outDir '_hv-h4-green-tool-search.json'
$search.Content | Set-Content -LiteralPath $searchPath -Encoding utf8
Write-Output ('TOOL_SEARCH_HTTP=' + [int]$search.StatusCode)
Write-Output ('TOOL_SEARCH_LEN=' + $search.Content.Length)
$searchObj = $search.Content | ConvertFrom-Json
$exact = @()
foreach ($prop in @('tools', 'Tools', 'items', 'Items', 'results', 'Results')) {
    if ($searchObj.PSObject.Properties.Name -contains $prop -and $null -ne $searchObj.$prop) {
        $exact = @($searchObj.$prop)
        break
    }
}
if ($exact.Count -eq 0 -and $searchObj -is [System.Array]) { $exact = @($searchObj) }
Write-Output ('TOOL_SEARCH_COUNT=' + $exact.Count)
$hasExact = $false
foreach ($item in $exact) {
    $name = ''
    if ($item.name) { $name = [string]$item.name }
    elseif ($item.Name) { $name = [string]$item.Name }
    elseif ($item.toolName) { $name = [string]$item.toolName }
    Write-Output ('TOOL_SEARCH_NAME=' + $name)
    if ($name -eq 'mcpserver-grok-plugin') { $hasExact = $true }
}
Write-Output ('TOOL_SEARCH_EXACT=' + $hasExact)

Write-Output '=== PHASE4_FILES ==='
$watch = @(
    'tests/McpServer.Support.Mcp.Tests/Products/ProductRequirementContextTests.cs',
    'src/McpServer.Support.Mcp/Products/Queries/GetProductRequirementContextQuery.cs',
    'src/McpServer.Support.Mcp/Controllers/ContextController.cs',
    'src/McpServer.Support.Mcp/Products/ProductShareHelper.cs',
    'src/McpServer.Support.Mcp/Products/ProductServiceCollectionExtensions.cs'
)
foreach ($rel in $watch) {
    $full = Join-Path $workspace $rel
    if (Test-Path -LiteralPath $full) {
        $item = Get-Item -LiteralPath $full
        Write-Output ('MTIME ' + $rel + ' LastWriteUtc=' + $item.LastWriteTimeUtc.ToString('o') + ' Bytes=' + $item.Length)
    } else {
        Write-Output ('MTIME_ABSENT ' + $rel)
    }
}

Write-Output '=== TEST_CASE_NAMES ==='
$testFile = Join-Path $workspace 'tests/McpServer.Support.Mcp.Tests/Products/ProductRequirementContextTests.cs'
$facts = @(Select-String -LiteralPath $testFile -Pattern 'public async Task (\w+)')
Write-Output ('FACT_COUNT=' + $facts.Count)
foreach ($hit in $facts) {
    Write-Output ('FACT ' + $hit.Matches[0].Groups[1].Value)
}
$skips = @(Select-String -LiteralPath $testFile -Pattern 'Fact\(Skip|Skip\s*=')
Write-Output ('SKIP_ATTR_COUNT=' + $skips.Count)
$redComment = @(Select-String -LiteralPath $testFile -Pattern 'Phase 4 red')
Write-Output ('STALE_RED_COMMENT_COUNT=' + $redComment.Count)

Write-Output '=== HANDLER_IMPL ==='
$handler = Join-Path $workspace 'src/McpServer.Support.Mcp/Products/Queries/GetProductRequirementContextQuery.cs'
$failHits = @(Select-String -LiteralPath $handler -Pattern 'Failure\("not implemented"\)')
$helperHits = @(Select-String -LiteralPath $handler -Pattern 'ProductShareHelper')
$ctorHits = @(Select-String -LiteralPath $handler -Pattern 'GetProductRequirementContextQueryHandler\(McpDbContext')
$docHits = @(Select-String -LiteralPath $handler -Pattern 'Documents|Chunks|ContextDocument|ContextChunk')
Write-Output ('FAILURE_NOT_IMPLEMENTED_COUNT=' + $failHits.Count)
Write-Output ('PRODUCT_SHARE_HELPER_COUNT=' + $helperHits.Count)
Write-Output ('CTOR_MCPDBCONTEXT_COUNT=' + $ctorHits.Count)
Write-Output ('HANDLER_DOC_CHUNK_COUNT=' + $docHits.Count)
foreach ($hit in $helperHits) {
    Write-Output ('HELPER ' + $hit.LineNumber + ':' + $hit.Line.Trim())
}
foreach ($hit in $ctorHits) {
    Write-Output ('CTOR ' + $hit.LineNumber + ':' + $hit.Line.Trim())
}
foreach ($hit in $docHits) {
    Write-Output ('HANDLER_DOC ' + $hit.LineNumber + ':' + $hit.Line.Trim())
}

Write-Output '=== SHARE_HELPER_DOC_CHUNK ==='
$helperFile = Join-Path $workspace 'src/McpServer.Support.Mcp/Products/ProductShareHelper.cs'
$helperDoc = @(Select-String -LiteralPath $helperFile -Pattern 'Documents|Chunks|ContextDocument|ContextChunk')
Write-Output ('HELPER_DOC_CHUNK_COUNT=' + $helperDoc.Count)
$internal = @(Select-String -LiteralPath $helperFile -Pattern 'internal static class ProductShareHelper')
Write-Output ('HELPER_INTERNAL_COUNT=' + $internal.Count)

Write-Output '=== CONTROLLER_HOOK ==='
$ctrl = Join-Path $workspace 'src/McpServer.Support.Mcp/Controllers/ContextController.cs'
$srcType = @(Select-String -LiteralPath $ctrl -Pattern 'product-requirements')
$load = @(Select-String -LiteralPath $ctrl -Pattern 'LoadProductRequirementChunksAsync|GetProductRequirementContextQuery|_dispatcher')
Write-Output ('CTRL_PRODUCT_REQ_COUNT=' + $srcType.Count)
foreach ($hit in $srcType) {
    Write-Output ('CTRL_SRC ' + $hit.LineNumber + ':' + $hit.Line.Trim())
}
Write-Output ('CTRL_LOAD_COUNT=' + $load.Count)
foreach ($hit in $load) {
    Write-Output ('CTRL_LOAD ' + $hit.LineNumber + ':' + $hit.Line.Trim())
}

Write-Output '=== IPRODUCTSERVICE_CS ==='
$csFiles = Get-ChildItem -Path $workspace -Recurse -Filter '*.cs' -File |
    Where-Object { $_.FullName -notmatch '\\bin\\|\\obj\\' }
$svcHits = @($csFiles | Select-String -Pattern 'IProductService')
Write-Output ('IPRODUCTSERVICE_CS_COUNT=' + $svcHits.Count)
foreach ($hit in $svcHits) {
    $rel = $hit.Path.Replace($workspace + '\', '')
    Write-Output ('IPRODUCTSERVICE ' + $rel + ':' + $hit.LineNumber + ':' + $hit.Line.Trim())
}

Write-Output '=== SRC_PRODUCT_REQUIREMENTS ==='
$srcCs = Get-ChildItem -Path (Join-Path $workspace 'src') -Recurse -Filter '*.cs' -File |
    Where-Object { $_.FullName -notmatch '\\bin\\|\\obj\\' }
$srcHits = @($srcCs | Select-String -Pattern 'product-requirements|GetProductRequirementContext')
Write-Output ('SRC_CTX_HIT_COUNT=' + $srcHits.Count)
foreach ($hit in $srcHits) {
    $rel = $hit.Path.Replace($workspace + '\', '')
    Write-Output ('SRC_CTX ' + $rel + ':' + $hit.LineNumber + ':' + $hit.Line.Trim())
}

Write-Output '=== PRODUCTS_DOC_CHUNK ==='
$prodCs = Get-ChildItem -Path (Join-Path $workspace 'src\McpServer.Support.Mcp\Products') -Recurse -Filter '*.cs' -File |
    Where-Object { $_.FullName -notmatch '\\bin\\|\\obj\\' }
$prodDoc = @($prodCs | Select-String -Pattern 'Documents|Chunks|ContextDocument|ContextChunk')
Write-Output ('PRODUCTS_DOC_CHUNK_COUNT=' + $prodDoc.Count)
foreach ($hit in $prodDoc) {
    $rel = $hit.Path.Replace($workspace + '\', '')
    Write-Output ('PRODUCTS_DOC ' + $rel + ':' + $hit.LineNumber + ':' + $hit.Line.Trim())
}

Write-Output '=== PLAN_HASH ==='
$plan = Join-Path $workspace 'docs\plans\mcp-products-001.md'
Write-Output ('PLAN_SHA256=' + (Get-FileHash -LiteralPath $plan -Algorithm SHA256).Hash)
$h4red = Join-Path $workspace 'docs\receipts\hostile-validator-20260818T155200Z.md'
Write-Output ('H4_RED_RECEIPT_EXISTS=' + (Test-Path -LiteralPath $h4red))
if (Test-Path -LiteralPath $h4red) {
    Write-Output ('H4_RED_SHA256=' + (Get-FileHash -LiteralPath $h4red -Algorithm SHA256).Hash)
}
$goal = 'C:\Users\kingd\.grok\sessions\F%3A%5CGitHub%5CMcpServer\01a01290-749a-7271-8c76-d04be7e683d7\goal\plan.md'
if (Test-Path -LiteralPath $goal) {
    Write-Output ('GOAL_PLAN_SHA256=' + (Get-FileHash -LiteralPath $goal -Algorithm SHA256).Hash)
} else {
    Write-Output 'GOAL_PLAN_ABSENT'
}

Write-Output 'COLLECT_DONE'
