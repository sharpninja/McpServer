#Requires -Version 7.0
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

. 'F:\GitHub\mcpserver-grok-plugin\lib\marker-resolver.ps1'

$workspace = 'F:\GitHub\McpServer'
$marker = Join-Path $workspace 'AGENTS-README-FIRST.yaml'
$baseUrl = 'http://PAYTON-LEGION2:7147'
$outDir = Join-Path $workspace 'docs\receipts'
$utcStamp = [datetime]::UtcNow.ToString('yyyyMMddTHHmmssZ')
$sessionId = "GrokCode-$utcStamp-h5-done-products"
$requestId = "req-$utcStamp-001-hostile-h5-done-products"

@(
    'STAMP=' + $utcStamp
    'SESSION_ID=' + $sessionId
    'REQUEST_ID=' + $requestId
) | Set-Content -LiteralPath (Join-Path $outDir '_hv-h5-done-ids.txt') -Encoding utf8

Write-Output ('UTC_STAMP=' + $utcStamp)
Write-Output ('SESSION_ID=' + $sessionId)
Write-Output ('REQUEST_ID=' + $requestId)
Write-Output ('MACHINE=' + [System.Environment]::MachineName)
Write-Output ('UTC_NOW=' + [datetime]::UtcNow.ToString('o'))

Write-Output '=== PLUGIN_VERSION ==='
$pluginJson = Get-Content -LiteralPath 'F:\GitHub\mcpserver-grok-plugin\.grok-plugin\plugin.json' -Raw | ConvertFrom-Json
$pluginVer = (Get-Content -LiteralPath 'F:\GitHub\mcpserver-grok-plugin\.version' -Raw).Trim()
Write-Output ('PLUGIN_JSON_VERSION=' + $pluginJson.version)
Write-Output ('PLUGIN_VERSION_FILE=' + $pluginVer)

Write-Output '=== MARKER_SIGNATURE ==='
$sigOk = Test-MarkerSignature -MarkerFile $marker
Write-Output ('Test-MarkerSignature=' + $sigOk)

Write-Output '=== HEALTH_NONCE ==='
$nonce = 'h5dn' + [guid]::NewGuid().ToString('N').Substring(0, 28)
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
$nonce | Set-Content -LiteralPath (Join-Path $outDir '_hv-h5-done-nonce.txt') -Encoding utf8

Write-Output '=== TOOL_REGISTRY ==='
$apiKey = (Get-MarkerField -MarkerFile $marker -FieldName 'apiKey')
$headers = @{ 'X-Api-Key' = $apiKey }
$search = Invoke-WebRequest -Uri ($baseUrl + '/mcpserver/tools/search?keyword=mcpserver-grok-plugin') -Headers $headers -UseBasicParsing -TimeoutSec 30
$searchPath = Join-Path $outDir '_hv-h5-done-tool-search.json'
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

Write-Output '=== PLAN_HASH ==='
$sha = [System.Security.Cryptography.SHA256]::Create()
function Get-FileSha256([string]$Rel) {
    $path = Join-Path $workspace $Rel
    if (-not (Test-Path -LiteralPath $path)) {
        Write-Output ('MISSING ' + $Rel)
        return
    }
    $bytes = [System.IO.File]::ReadAllBytes($path)
    $hash = [BitConverter]::ToString($sha.ComputeHash($bytes)).Replace('-', '')
    $info = Get-Item -LiteralPath $path
    Write-Output ('FILE ' + $Rel + ' SHA256=' + $hash + ' BYTES=' + $info.Length + ' LWUTC=' + $info.LastWriteTimeUtc.ToString('o'))
}
Get-FileSha256 'docs/plans/mcp-products-001.md'
Get-FileSha256 'docs/USER-GUIDE.md'
Get-FileSha256 'docs/MCP-SERVER.md'
Get-FileSha256 'src/McpServer.Client/ENDPOINTS.md'
Get-FileSha256 'docs/Project/Functional-Requirements.md'
Get-FileSha256 'docs/Project/wiki/github/Functional-Requirements.md'
Get-FileSha256 'docs/Project/wiki/azure/Functional-Requirements.md'

Write-Output '=== GREP_IPRODUCTSERVICE_SRC_TESTS ==='
$csHits = @(Get-ChildItem -Path (Join-Path $workspace 'src'), (Join-Path $workspace 'tests') -Recurse -Filter '*.cs' -File |
    Where-Object { $_.FullName -notmatch '\\(bin|obj)\\' } |
    Select-String -Pattern 'IProductService' -SimpleMatch)
Write-Output ('IPRODUCTSERVICE_CS_COUNT=' + $csHits.Count)
foreach ($hit in $csHits) {
    Write-Output ('HIT ' + $hit.Path + ':' + $hit.LineNumber + ' ' + $hit.Line.Trim())
}

Write-Output '=== GREP_PUBLIC_PRODUCT_SERVICE ==='
$svcHits = @(Get-ChildItem -Path (Join-Path $workspace 'src') -Recurse -Filter '*.cs' -File |
    Where-Object { $_.FullName -notmatch '\\(bin|obj)\\' } |
    Select-String -Pattern 'public interface IProduct|public (sealed )?class ProductService\b')
Write-Output ('PUBLIC_PRODUCT_SERVICE_COUNT=' + $svcHits.Count)
foreach ($hit in $svcHits) {
    Write-Output ('HIT ' + $hit.Path + ':' + $hit.LineNumber + ' ' + $hit.Line.Trim())
}

Write-Output '=== GREP_PRODUCTS_DOCUMENTS_CHUNKS ==='
$prodDir = Join-Path $workspace 'src\McpServer.Support.Mcp\Products'
$chunkHits = @(Get-ChildItem -Path $prodDir -Recurse -Filter '*.cs' -File |
    Select-String -Pattern 'ContextDocument|ContextChunk|db\.Documents|db\.Chunks')
Write-Output ('PRODUCTS_CONTEXT_ROW_HIT_COUNT=' + $chunkHits.Count)
foreach ($hit in $chunkHits) {
    Write-Output ('HIT ' + $hit.Path + ':' + $hit.LineNumber + ' ' + $hit.Line.Trim())
}

Write-Output '=== GREP_PRODUCT_KEY_REGEX ==='
$keyHits = @(Get-ChildItem -Path (Join-Path $workspace 'src') -Recurse -Filter '*.cs' -File |
    Where-Object { $_.FullName -notmatch '\\(bin|obj)\\' } |
    Select-String -Pattern '\^PROD-\[A-Z\]')
Write-Output ('PRODUCT_KEY_REGEX_HIT_COUNT=' + $keyHits.Count)
foreach ($hit in $keyHits) {
    Write-Output ('HIT ' + $hit.Path + ':' + $hit.LineNumber + ' ' + $hit.Line.Trim())
}

Write-Output '=== GREP_STDIO_PRODUCT_TOOLS ==='
$stdio = Join-Path $workspace 'src\McpServer.Support.Mcp\McpStdio\FwhMcpTools.Products.cs'
$toolHits = @(Select-String -Path $stdio -Pattern 'McpServerTool\(Name = "product_')
Write-Output ('STDIO_PRODUCT_TOOL_COUNT=' + $toolHits.Count)
foreach ($hit in $toolHits) {
    Write-Output ('TOOL ' + $hit.Line.Trim())
}

Write-Output '=== GREP_DISPATCHER_STDIO ==='
$dispHits = @(Select-String -Path $stdio -Pattern '_dispatcher\.(SendAsync|QueryAsync)')
Write-Output ('STDIO_DISPATCH_COUNT=' + $dispHits.Count)

Write-Output '=== GREP_PYTHON ==='
# inventory only; this collect is pwsh
Write-Output 'COLLECT_SHELL=pwsh.exe'

Write-Output '=== WATCH_FILES ==='
$watch = @(
    'src/McpServer.Support.Mcp/Products/ProductCqrsHelpers.cs',
    'src/McpServer.Support.Mcp/Products/ProductShareHelper.cs',
    'src/McpServer.Support.Mcp/Products/Queries/GetProductEffectiveRequirementsQuery.cs',
    'src/McpServer.Support.Mcp/Products/Queries/GetProductRequirementContextQuery.cs',
    'src/McpServer.Support.Mcp/Controllers/ProductsController.cs',
    'src/McpServer.Support.Mcp/Controllers/ContextController.cs',
    'src/McpServer.Support.Mcp/Controllers/RequirementsController.cs',
    'src/McpServer.Support.Mcp/McpStdio/FwhMcpTools.Products.cs',
    'src/McpServer.Client/ProductClient.cs',
    'tests/McpServer.Support.Mcp.IntegrationTests/Controllers/ProductsLaunchTests.cs',
    'docs/USER-GUIDE.md',
    'docs/MCP-SERVER.md',
    'src/McpServer.Client/ENDPOINTS.md'
)
foreach ($rel in $watch) {
    $path = Join-Path $workspace $rel
    if (Test-Path -LiteralPath $path) {
        $info = Get-Item -LiteralPath $path
        Write-Output ('WATCH ' + $rel + ' LWUTC=' + $info.LastWriteTimeUtc.ToString('o') + ' BYTES=' + $info.Length)
    }
    else {
        Write-Output ('WATCH_MISSING ' + $rel)
    }
}

Write-Output '=== IMPLEMENTER_LOGS ==='
$implDir = 'C:\Users\kingd\AppData\Local\Temp\grok-goal-18747a5af710\implementer'
if (Test-Path -LiteralPath $implDir) {
    Get-ChildItem -LiteralPath $implDir -File | ForEach-Object {
        Write-Output ('IMPL ' + $_.Name + ' LWUTC=' + $_.LastWriteTimeUtc.ToString('o') + ' BYTES=' + $_.Length)
    }
    $bt = Join-Path $implDir 'build-test.txt'
    if (Test-Path -LiteralPath $bt) {
        $btText = Get-Content -LiteralPath $bt -Raw
        Write-Output ('BUILD_TEST_HAS_1997=' + ($btText -match 'Passed:\s+1997'))
        Write-Output ('BUILD_TEST_HAS_FAILED_0=' + ($btText -match 'Failed:\s+0'))
        Write-Output ('BUILD_TEST_HAS_HANDOFF=' + ($btText -match 'HandoffDurability'))
        Write-Output ('BUILD_TEST_HAS_SUCCEEDED=' + ($btText -match 'Target\s+Test\s+Succeeded|Test\s+Succeeded'))
        Write-Output ('BUILD_TEST_BUILD_LINE=' + (($btText -split "`n" | Where-Object { $_ -match 'Build succeeded on' } | Select-Object -Last 1)))
    }
}

Write-Output 'COLLECT_DONE'
