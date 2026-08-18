#Requires -Version 7.0
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Continue'

$workspace = 'F:\GitHub\McpServer'
$baseUrl = 'http://PAYTON-LEGION2:7147'
$outDir = Join-Path $workspace 'docs\receipts'
$marker = Join-Path $workspace 'AGENTS-README-FIRST.yaml'
. 'F:\GitHub\mcpserver-grok-plugin\lib\marker-resolver.ps1'
Set-Location -LiteralPath $workspace

Write-Output '=== TOOL_REGISTRY_RETRY ==='
try {
    $apiKey = (Get-MarkerField -MarkerFile $marker -FieldName 'apiKey')
    $headers = @{ 'X-Api-Key' = $apiKey }
    $search = Invoke-WebRequest -Uri ($baseUrl + '/mcpserver/tools/search?keyword=mcpserver-grok-plugin') -Headers $headers -UseBasicParsing -TimeoutSec 90
    $search.Content | Set-Content -LiteralPath (Join-Path $outDir '_hv-h3-green-tool-search.json') -Encoding utf8
    Write-Output ('TOOL_SEARCH_HTTP=' + [int]$search.StatusCode)
    Write-Output ('TOOL_SEARCH_LEN=' + $search.Content.Length)
    $searchObj = $search.Content | ConvertFrom-Json
    $names = @()
    foreach ($item in @($searchObj)) {
        if ($item.name) { $names += [string]$item.name }
        if ($item.Name) { $names += [string]$item.Name }
        if ($item.toolName) { $names += [string]$item.toolName }
    }
    foreach ($prop in @('tools', 'Tools', 'items', 'Items', 'results', 'Results')) {
        if ($searchObj.PSObject.Properties.Name -contains $prop -and $null -ne $searchObj.$prop) {
            foreach ($item in @($searchObj.$prop)) {
                if ($item.name) { $names += [string]$item.name }
                elseif ($item.Name) { $names += [string]$item.Name }
            }
        }
    }
    Write-Output ('TOOL_SEARCH_NAMES=' + (($names | Select-Object -Unique) -join ','))
} catch {
    Write-Output ('TOOL_SEARCH_ERROR=' + $_.Exception.Message)
}

Write-Output '=== IPRODUCTSERVICE ==='
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
        $full = Join-Path $workspace $file
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

Write-FileHits -Label 'CTRL_SEND' -Pattern 'SendAsync|QueryAsync|IDispatcher|_dispatcher' -Files @('src/McpServer.Support.Mcp/Controllers/ProductsController.cs')
Write-FileHits -Label 'CTRL_STATUS501' -Pattern 'Status501NotImplemented' -Files @('src/McpServer.Support.Mcp/Controllers/ProductsController.cs')
Write-FileHits -Label 'CLIENT_POST' -Pattern 'mcpserver/products|ownerWorkspaceId|NotImplementedException' -Files @('src/McpServer.Client/ProductClient.cs')
Write-FileHits -Label 'CLIENT_PROP' -Pattern 'public ProductClient Products' -Files @('src/McpServer.Client/McpServerClient.cs')
Write-FileHits -Label 'PASSTHROUGH' -Pattern '"PRODUCTS"' -Files @('src/McpServer.Repl.Core/GenericClientPassthrough.cs')
Write-FileHits -Label 'MCP_TOOLS' -Pattern 'product_create|product_list|product_get|product_update|product_delete|product_list_members|product_add_member|product_remove_member|SendAsync|QueryAsync|not implemented' -Files @('src/McpServer.Support.Mcp/McpStdio/FwhMcpTools.Products.cs')
Write-FileHits -Label 'REQ_SCOPE' -Pattern 'productScope|GetProductEffectiveRequirementsQuery' -Files @('src/McpServer.Support.Mcp/Controllers/RequirementsController.cs')
Write-FileHits -Label 'JSON_CTX' -Pattern 'ProductDto|CreateProductRequest|JsonSerializable' -Files @('src/McpServer.Client/Serialization/McpJsonContext.cs')
Write-FileHits -Label 'SKIP_CTRL' -Pattern 'Fact\(Skip|Skip\s*=' -Files @('tests/McpServer.Support.Mcp.Tests/Controllers/ProductsControllerTests.cs')
Write-FileHits -Label 'SKIP_CLIENT' -Pattern 'Fact\(Skip|Skip\s*=' -Files @('tests/McpServer.Client.Tests/ProductClientTests.cs')
Write-FileHits -Label 'ENDPOINTS' -Pattern 'products' -Files @('src/McpServer.Client/ENDPOINTS.md')

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

Write-Output '=== DESCRIPTORS ==='
$desc = Get-ChildItem -Path $workspace -Recurse -File -ErrorAction SilentlyContinue |
    Where-Object {
        $_.FullName -notmatch '\\bin\\|\\obj\\|\\node_modules\\|\\.git\\' -and
        ($_.Name -match 'product' -or $_.FullName -match 'descriptor') -and
        $_.Extension -match '\.(json|yaml|yml|md)$'
    } |
    Select-Object -First 40
foreach ($f in $desc) {
    Write-Output ('DESC ' + $f.FullName.Replace($workspace + '\', ''))
}

Write-Output 'COLLECT2_DONE'
