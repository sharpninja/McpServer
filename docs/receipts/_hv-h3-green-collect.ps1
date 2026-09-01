#Requires -Version 7.0
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

. 'F:\GitHub\mcpserver-grok-plugin\lib\marker-resolver.ps1'

$workspace = 'F:\GitHub\McpServer'
$marker = Join-Path $workspace 'AGENTS-README-FIRST.yaml'
$baseUrl = 'http://PAYTON-LEGION2:7147'
$outDir = Join-Path $workspace 'docs\receipts'
$utcStamp = [datetime]::UtcNow.ToString('yyyyMMddTHHmmssZ')
$sessionId = "GrokCode-$utcStamp-h3-green-products"
$requestId = "req-$utcStamp-001-hostile-h3-green-products"

@(
    'STAMP=' + $utcStamp
    'SESSION_ID=' + $sessionId
    'REQUEST_ID=' + $requestId
) | Set-Content -LiteralPath (Join-Path $outDir '_hv-h3-green-ids.txt') -Encoding utf8

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
$nonce = 'h3grn' + [guid]::NewGuid().ToString('N').Substring(0, 27)
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
$nonce | Set-Content -LiteralPath (Join-Path $outDir '_hv-h3-green-nonce.txt') -Encoding utf8

Write-Output '=== TOOL_REGISTRY ==='
$apiKey = (Get-MarkerField -MarkerFile $marker -FieldName 'apiKey')
$headers = @{ 'X-Api-Key' = $apiKey }
$search = Invoke-WebRequest -Uri ($baseUrl + '/mcpserver/tools/search?keyword=mcpserver-grok-plugin') -Headers $headers -UseBasicParsing -TimeoutSec 30
$searchPath = Join-Path $outDir '_hv-h3-green-tool-search.json'
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
foreach ($item in $exact) {
    $name = ''
    if ($item.name) { $name = [string]$item.name }
    elseif ($item.Name) { $name = [string]$item.Name }
    elseif ($item.toolName) { $name = [string]$item.toolName }
    Write-Output ('TOOL_SEARCH_NAME=' + $name)
}

Write-Output '=== IPRODUCTSERVICE ==='
Set-Location -LiteralPath $workspace
$csFiles = Get-ChildItem -Path 'src','tests' -Recurse -Filter '*.cs' -File |
    Where-Object { $_.FullName -notmatch '\\bin\\|\\obj\\' }
$iProduct = @($csFiles | Select-String -Pattern 'IProductService')
Write-Output ('IPRODUCTSERVICE_CS_COUNT=' + $iProduct.Count)
foreach ($hit in $iProduct) {
    $rel = $hit.Path.Replace((Get-Location).Path + '\', '')
    Write-Output ('IPRODUCTSERVICE ' + $rel + ':' + $hit.LineNumber + ':' + $hit.Line.Trim())
}

Write-Output '=== PUBLIC_PRODUCT_SERVICE ==='
$pubSvc = @($csFiles | Select-String -Pattern 'public\s+interface\s+IProduct|public\s+class\s+ProductService\b')
Write-Output ('PUBLIC_PRODUCT_SERVICE_COUNT=' + $pubSvc.Count)
foreach ($hit in $pubSvc) {
    $rel = $hit.Path.Replace((Get-Location).Path + '\', '')
    Write-Output ('PUBLIC_PRODUCT_SERVICE ' + $rel + ':' + $hit.LineNumber + ':' + $hit.Line.Trim())
}

function Write-FileHits {
    param([string]$Label, [string]$Pattern, [string[]]$Files)
    $count = 0
    foreach ($file in $Files) {
        $full = if ([System.IO.Path]::IsPathRooted($file)) { $file } else { Join-Path $workspace $file }
        if (-not (Test-Path -LiteralPath $full)) {
            Write-Output ($Label + '_ABSENT=' + $file)
            continue
        }
        $hits = @(Select-String -LiteralPath $full -Pattern $Pattern)
        $count += $hits.Count
        foreach ($hit in $hits) {
            Write-Output ($Label + ' ' + $file + ':' + $hit.LineNumber + ':' + $hit.Line.Trim())
        }
    }
    Write-Output ($Label + '_COUNT=' + $count)
}

Write-FileHits -Label 'CTRL_SEND' -Pattern 'SendAsync|QueryAsync|IDispatcher' -Files @('src/McpServer.Support.Mcp/Controllers/ProductsController.cs')
Write-FileHits -Label 'CTRL_IPRODUCT' -Pattern 'IProductService' -Files @('src/McpServer.Support.Mcp/Controllers/ProductsController.cs')
Write-FileHits -Label 'CTRL_STATUS501' -Pattern 'Status501NotImplemented' -Files @('src/McpServer.Support.Mcp/Controllers/ProductsController.cs')
Write-FileHits -Label 'CLIENT_POST' -Pattern '/mcpserver/products|ownerWorkspaceId|NotImplementedException' -Files @('src/McpServer.Client/ProductClient.cs')
Write-FileHits -Label 'CLIENT_PROP' -Pattern 'public ProductClient Products' -Files @('src/McpServer.Client/McpServerClient.cs')
Write-FileHits -Label 'PASSTHROUGH' -Pattern '"PRODUCTS"' -Files @('src/McpServer.Repl.Core/GenericClientPassthrough.cs')
Write-FileHits -Label 'MCP_TOOLS' -Pattern 'product_create|product_list|product_get|product_update|product_delete|product_list_members|product_add_member|product_remove_member|SendAsync|QueryAsync|IDispatcher|not implemented' -Files @('src/McpServer.Support.Mcp/McpStdio/FwhMcpTools.Products.cs')
Write-FileHits -Label 'REQ_SCOPE' -Pattern 'productScope|GetProductEffectiveRequirementsQuery|IDispatcher' -Files @('src/McpServer.Support.Mcp/Controllers/RequirementsController.cs')
Write-FileHits -Label 'JSON_CTX' -Pattern 'ProductClient|ProductDto|JsonSerializable' -Files @('src/McpServer.Client/Serialization/McpJsonContext.cs')
Write-FileHits -Label 'SKIP_CTRL' -Pattern 'Fact\(Skip|Skip\s*=' -Files @('tests/McpServer.Support.Mcp.Tests/Controllers/ProductsControllerTests.cs')
Write-FileHits -Label 'SKIP_CLIENT' -Pattern 'Fact\(Skip|Skip\s*=' -Files @('tests/McpServer.Client.Tests/ProductClientTests.cs')

Write-Output '=== PHASE4_ABSENT ==='
$p4 = 'tests/McpServer.Support.Mcp.Tests/Products/ProductRequirementContextTests.cs'
Write-Output ('PRODUCTREQUIREMENTCONTEXTTESTS_EXISTS=' + (Test-Path -LiteralPath (Join-Path $workspace $p4)))

Write-Output '=== PLAN_HASH ==='
$plan = Join-Path $workspace 'docs\plans\mcp-products-001.md'
Write-Output ('PLAN_SHA256=' + (Get-FileHash -LiteralPath $plan -Algorithm SHA256).Hash)
$goal = 'C:\Users\kingd\.grok\sessions\F%3A%5CGitHub%5CMcpServer\01a01290-749a-7271-8c76-d04be7e683d7\goal\plan.md'
if (Test-Path -LiteralPath $goal) {
    Write-Output ('GOAL_PLAN_SHA256=' + (Get-FileHash -LiteralPath $goal -Algorithm SHA256).Hash)
} else {
    Write-Output 'GOAL_PLAN_ABSENT'
}

Write-Output '=== FILE_TIMES ==='
$watch = @(
    'src/McpServer.Support.Mcp/Controllers/ProductsController.cs',
    'src/McpServer.Client/ProductClient.cs',
    'src/McpServer.Client/McpServerClient.cs',
    'src/McpServer.Repl.Core/GenericClientPassthrough.cs',
    'src/McpServer.Support.Mcp/McpStdio/FwhMcpTools.Products.cs',
    'src/McpServer.Support.Mcp/Controllers/RequirementsController.cs',
    'tests/McpServer.Support.Mcp.Tests/Controllers/ProductsControllerTests.cs',
    'tests/McpServer.Client.Tests/ProductClientTests.cs',
    'tests/McpServer.Repl.Core.Tests/GenericClientPassthroughValidClientNamesTests.cs'
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

Write-Output 'COLLECT_DONE'
