#Requires -Version 7.0
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$workspace = 'F:\GitHub\McpServer'
$outDir = 'F:\GitHub\McpServer\docs\receipts'
Set-Location $workspace

function Count-Matches {
    param([string]$Pattern, [string[]]$Globs, [string]$Root = $workspace)
    $count = 0
    $files = @()
    foreach ($g in $Globs) {
        $files += Get-ChildItem -Path $Root -Recurse -File -Filter $g -ErrorAction SilentlyContinue |
            Where-Object { $_.FullName -notmatch '\\(bin|obj|\.git)\\' }
    }
    foreach ($f in $files) {
        $hits = Select-String -Path $f.FullName -Pattern $Pattern -SimpleMatch:$false -ErrorAction SilentlyContinue
        if ($hits) { $count += @($hits).Count }
    }
    return $count
}

$srcCs = Get-ChildItem -Path (Join-Path $workspace 'src') -Recurse -Filter *.cs |
    Where-Object { $_.FullName -notmatch '\\(bin|obj)\\' }
$testCs = Get-ChildItem -Path (Join-Path $workspace 'tests') -Recurse -Filter *.cs |
    Where-Object { $_.FullName -notmatch '\\(bin|obj)\\' }
$allCs = @($srcCs) + @($testCs)

$iprod = @($allCs | Select-String -Pattern 'IProductService' | Where-Object { $_.Path -notmatch '\\docs\\' })
$pubProd = @($allCs | Select-String -Pattern 'public (interface|class|sealed class).*(ProductService|ProductManager|ProductFacade)')
Write-Output ('IPRODUCTSERVICE_CS_COUNT=' + $iprod.Count)
$iprod | ForEach-Object { Write-Output ('IPROD_HIT=' + $_.Path + ':' + $_.LineNumber + ':' + $_.Line.Trim()) }
Write-Output ('PUBLIC_PRODUCT_SERVICE_COUNT=' + $pubProd.Count)

$productsDir = Join-Path $workspace 'src\McpServer.Support.Mcp\Products'
Write-Output 'PRODUCTS_DIR_FILES:'
Get-ChildItem -Path $productsDir -Recurse -File | ForEach-Object {
    Write-Output ('  ' + $_.FullName.Substring($workspace.Length + 1) + ' LW=' + $_.LastWriteTimeUtc.ToString('o'))
}

$ctxHits = Select-String -Path (Join-Path $productsDir '*') -Pattern 'ContextDocument|ContextChunk|db\.Documents|db\.Chunks' -Recurse -ErrorAction SilentlyContinue
Write-Output ('PRODUCTS_CTX_LEAK_GREP=' + @($ctxHits).Count)
$ctxHits | ForEach-Object { Write-Output ('CTX_HIT=' + $_.Path + ':' + $_.LineNumber + ':' + $_.Line.Trim()) }

$regexHits = Select-String -Path (Join-Path $productsDir '*') -Pattern 'PROD-\[A-Z\]\[A-Z0-9\]' -Recurse
Write-Output ('REGEX_HITS=' + @($regexHits).Count)
$regexHits | ForEach-Object { Write-Output ('REGEX=' + $_.Path + ':' + $_.LineNumber + ':' + $_.Line.Trim()) }

$ctrl = Join-Path $workspace 'src\McpServer.Support.Mcp\Controllers\ProductsController.cs'
$stdio = Join-Path $workspace 'src\McpServer.Support.Mcp\McpStdio\FwhMcpTools.Products.cs'
$reqCtrl = Join-Path $workspace 'src\McpServer.Support.Mcp\Controllers\RequirementsController.cs'
$ctxCtrl = Join-Path $workspace 'src\McpServer.Support.Mcp\Controllers\ContextController.cs'
Write-Output ('PRODUCTS_CTRL_EXISTS=' + (Test-Path $ctrl))
Write-Output ('STDIO_PRODUCTS_EXISTS=' + (Test-Path $stdio))
if (Test-Path $ctrl) {
    $send = @(Select-String -Path $ctrl -Pattern 'SendAsync|QueryAsync')
    Write-Output ('CTRL_DISPATCH_HITS=' + $send.Count)
}
if (Test-Path $stdio) {
    $tools = @(Select-String -Path $stdio -Pattern 'product_')
    Write-Output ('STDIO_PRODUCT_TOOL_HITS=' + $tools.Count)
}

$scopeHits = @(Select-String -Path $reqCtrl -Pattern 'productScope' -ErrorAction SilentlyContinue)
Write-Output ('REQ_PRODUCTSCOPE_HITS=' + $scopeHits.Count)
$scopeHits | ForEach-Object { Write-Output ('SCOPE=' + $_.LineNumber + ':' + $_.Line.Trim()) }

$packHits = @(Select-String -Path $ctxCtrl -Pattern 'product-requirements|GetProductRequirementContext|productChunks' -ErrorAction SilentlyContinue)
Write-Output ('CTX_PACK_HITS=' + $packHits.Count)
$packHits | ForEach-Object { Write-Output ('PACK=' + $_.LineNumber + ':' + $_.Line.Trim()) }

$handoff = Join-Path $workspace 'tests\McpServer.Support.Mcp.Tests\Services\HandoffDurabilityTests.cs'
Write-Output ('HANDOFF_TEST_EXISTS=' + (Test-Path $handoff))
if (Test-Path $handoff) {
    Write-Output ('HANDOFF_LW=' + (Get-Item $handoff).LastWriteTimeUtc.ToString('o'))
}

$tracking = Get-ChildItem -Path $workspace -Recurse -Filter TrackingTodoService.cs |
    Where-Object { $_.FullName -notmatch '\\(bin|obj)\\' }
Write-Output ('TRACKING_TODO_FILES=' + @($tracking).Count)
foreach ($t in $tracking) {
    Write-Output ('TRACKING_FILE=' + $t.FullName + ' LW=' + $t.LastWriteTimeUtc.ToString('o'))
}

# Docs
$ug = Join-Path $workspace 'docs\USER-GUIDE.md'
$ms = Join-Path $workspace 'docs\MCP-SERVER.md'
$ep = Join-Path $workspace 'src\McpServer.Client\ENDPOINTS.md'
Write-Output ('USERGUIDE_LW=' + (Get-Item $ug).LastWriteTimeUtc.ToString('o'))
Write-Output ('MCPSERVER_LW=' + (Get-Item $ms).LastWriteTimeUtc.ToString('o'))
Write-Output ('ENDPOINTS_LW=' + (Get-Item $ep).LastWriteTimeUtc.ToString('o'))
$ug7c = @(Select-String -Path $ug -Pattern '^## 7c\) Products')
$msProd = @(Select-String -Path $ms -Pattern '^## Products')
$epProd = @(Select-String -Path $ep -Pattern '^### Products')
Write-Output ('USERGUIDE_7C=' + $ug7c.Count + ' LINE=' + ($(if ($ug7c) { $ug7c[0].LineNumber } else { 0 })))
Write-Output ('MCPSERVER_PRODUCTS=' + $msProd.Count + ' LINE=' + ($(if ($msProd) { $msProd[0].LineNumber } else { 0 })))
Write-Output ('ENDPOINTS_PRODUCTS=' + $epProd.Count + ' LINE=' + ($(if ($epProd) { $epProd[0].LineNumber } else { 0 })))

$wikiGh = Join-Path $workspace 'docs\Project\wiki\github\Functional-Requirements.md'
$wikiAz = Join-Path $workspace 'docs\Project\wiki\azure\Functional-Requirements.md'
foreach ($w in @($wikiGh, $wikiAz)) {
    $hits = @(Select-String -Path $w -Pattern 'FR-MCP-PRODUCT-00[1-5]')
    Write-Output ('WIKI=' + $w + ' FR_HITS=' + $hits.Count + ' LW=' + (Get-Item $w).LastWriteTimeUtc.ToString('o'))
}

# Prior receipts OverallVerdict
$prior = @(
    'hostile-validator-20260818T132341Z.md',
    'hostile-validator-20260818T140630Z.md',
    'hostile-validator-20260818T143053Z.md',
    'hostile-validator-20260818T144836Z.md',
    'hostile-validator-20260818T150200Z.md',
    'hostile-validator-20260818T152430Z.md',
    'hostile-validator-20260818T154000Z.md',
    'hostile-validator-20260818T155200Z.md',
    'hostile-validator-20260818T160833Z.md',
    'hostile-validator-20260818T163120Z.md'
)
foreach ($p in $prior) {
    $path = Join-Path $outDir $p
    if (Test-Path $path) {
        $v = Select-String -Path $path -Pattern '^OverallVerdict:' | Select-Object -First 1
        Write-Output ('PRIOR=' + $p + ' ' + $v.Line)
    } else {
        Write-Output ('PRIOR_MISSING=' + $p)
    }
}

# Plan hash
$plan = Join-Path $workspace 'docs\plans\mcp-products-001.md'
$sha = (Get-FileHash -LiteralPath $plan -Algorithm SHA256).Hash
Write-Output ('PLAN_SHA256=' + $sha)
Write-Output ('PLAN_BYTES=' + (Get-Item $plan).Length)

# Implementer logs
$implTest = 'C:\Users\kingd\AppData\Local\Temp\grok-goal-18747a5af710\implementer\build-test-h5-rerun.txt'
$implTrace = 'C:\Users\kingd\AppData\Local\Temp\grok-goal-18747a5af710\implementer\traceability-h5-rerun.txt'
foreach ($f in @($implTest, $implTrace)) {
    if (Test-Path $f) {
        $item = Get-Item $f
        Write-Output ('IMPL_LOG=' + $f + ' LW=' + $item.LastWriteTimeUtc.ToString('o') + ' LEN=' + $item.Length)
        $tail = Get-Content -LiteralPath $f -Tail 20
        $tail | ForEach-Object { Write-Output ('IMPL_TAIL=' + $_) }
    } else {
        Write-Output ('IMPL_LOG_MISSING=' + $f)
    }
}

Write-Output 'GREPS_DONE'
