#Requires -Version 7.0
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

Set-Location -LiteralPath 'F:\GitHub\McpServer'

function Write-SimpleHits {
    param([string]$Label, [string]$Pattern, [string[]]$Files)
    $count = 0
    foreach ($file in $Files) {
        if (-not (Test-Path -LiteralPath $file)) {
            Write-Output ($Label + '_ABSENT=' + $file)
            continue
        }
        $hits = Select-String -LiteralPath $file -Pattern $Pattern
        $count += @($hits).Count
        foreach ($hit in $hits) {
            Write-Output ($Label + ' ' + $file + ':' + $hit.LineNumber + ':' + $hit.Line.Trim())
        }
    }
    Write-Output ($Label + '_COUNT=' + $count)
}

$csFiles = Get-ChildItem -Path 'src','tests' -Recurse -Filter '*.cs' -File |
    Where-Object { $_.FullName -notmatch '\\bin\\|\\obj\\' }
$iProduct = $csFiles | Select-String -Pattern 'IProductService'
Write-Output ('IPRODUCTSERVICE_CS_COUNT=' + @($iProduct).Count)
foreach ($hit in $iProduct) {
    Write-Output ('IPRODUCTSERVICE ' + $hit.Path + ':' + $hit.LineNumber + ':' + $hit.Line.Trim())
}

$srcCs = Get-ChildItem -Path 'src' -Recurse -Filter '*.cs' -File |
    Where-Object { $_.FullName -notmatch '\\bin\\|\\obj\\' }
$scope = $srcCs | Select-String -Pattern 'productScope'
Write-Output ('PRODUCTSCOPE_SRC_COUNT=' + @($scope).Count)
foreach ($hit in $scope | Select-Object -First 30) {
    $rel = $hit.Path.Replace((Get-Location).Path + '\', '')
    Write-Output ('PRODUCTSCOPE ' + $rel + ':' + $hit.LineNumber + ':' + $hit.Line.Trim())
}

Write-SimpleHits -Label 'PRODUCTS_CASE' -Pattern '"PRODUCTS"' -Files @('src/McpServer.Repl.Core/GenericClientPassthrough.cs')
Write-SimpleHits -Label 'FACT_SKIP_CTRL' -Pattern 'Fact\(Skip|Skip\s*=' -Files @('tests/McpServer.Support.Mcp.Tests/Controllers/ProductsControllerTests.cs')
Write-SimpleHits -Label 'FACT_SKIP_CLIENT' -Pattern 'Fact\(Skip|Skip\s*=' -Files @('tests/McpServer.Client.Tests/ProductClientTests.cs')
Write-SimpleHits -Label 'NOTIMPL_CLIENT' -Pattern 'NotImplementedException' -Files @('src/McpServer.Client/ProductClient.cs')
Write-SimpleHits -Label 'STATUS501' -Pattern 'Status501NotImplemented' -Files @('src/McpServer.Support.Mcp/Controllers/ProductsController.cs')
Write-SimpleHits -Label 'NOTIMPL_JSON' -Pattern 'not implemented' -Files @('src/McpServer.Support.Mcp/McpStdio/FwhMcpTools.Products.cs')
Write-SimpleHits -Label 'DISPATCH_PRODUCTS_TOOL' -Pattern 'IDispatcher|SendAsync|QueryAsync' -Files @('src/McpServer.Support.Mcp/McpStdio/FwhMcpTools.Products.cs')
Write-SimpleHits -Label 'CLIENT_PRODUCTS_PROP' -Pattern 'ProductClient Products|public ProductClient' -Files @('src/McpServer.Client/McpServerClient.cs')

$ctrl = Get-Content -LiteralPath 'src/McpServer.Support.Mcp/Controllers/ProductsController.cs' -Raw
Write-Output ('CTRL_HAS_SENDASYNC=' + [bool]($ctrl -match 'SendAsync'))
Write-Output ('CTRL_HAS_QUERYASYNC=' + [bool]($ctrl -match 'QueryAsync'))
Write-Output ('CTRL_HAS_DISPATCHER_FIELD=' + [bool]($ctrl -match '_dispatcher'))

$req = Get-Content -LiteralPath 'src/McpServer.Support.Mcp/Controllers/RequirementsController.cs' -Raw
Write-Output ('REQ_HAS_PRODUCTSCOPE=' + [bool]($req -match 'productScope'))

$client = Get-Content -LiteralPath 'src/McpServer.Client/McpServerClient.cs' -Raw
Write-Output ('MCPSERVERCLIENT_HAS_PRODUCTS_PROP=' + [bool]($client -match 'ProductClient Products'))

$passthrough = Get-Content -LiteralPath 'src/McpServer.Repl.Core/GenericClientPassthrough.cs' -Raw
Write-Output ('PASSTHROUGH_HAS_PRODUCTS_CASE=' + [bool]($passthrough -match '"PRODUCTS"'))

$target = '0D73B5C6B754DEC494F4EAB445AD2A6EEB73D2F2923260366E07D4A9351FD92C'
$roots = @(
    'C:\Users\kingd\.grok\sessions\F%3A%5CGitHub%5CMcpServer',
    'C:\Users\kingd\AppData\Local\Temp'
)
foreach ($root in $roots) {
    if (-not (Test-Path -LiteralPath $root)) { continue }
    Get-ChildItem -Path $root -Recurse -Filter 'plan.md' -ErrorAction SilentlyContinue |
        ForEach-Object {
            $hash = (Get-FileHash -LiteralPath $_.FullName -Algorithm SHA256).Hash
            if ($hash -eq $target -or $_.FullName -match '18747a5af710' -or $_.DirectoryName -match 'goal') {
                Write-Output ('PLAN_CANDIDATE=' + $_.FullName + ' SHA256=' + $hash)
            }
        }
}
