#Requires -Version 7.0
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

function Get-ReqItems {
    param([string]$Path)
    $outer = Get-Content -LiteralPath $Path -Raw | ConvertFrom-Json
    $text = [string]$outer.result.content[0].text
    $parsed = $text | ConvertFrom-Json
    return @($parsed.items)
}

$outDir = 'F:\GitHub\McpServer\docs\receipts'
$fr = Get-ReqItems -Path (Join-Path $outDir '_hv-h4-green-req-fr.json')
$tr = Get-ReqItems -Path (Join-Path $outDir '_hv-h4-green-req-tr.json')
$test = Get-ReqItems -Path (Join-Path $outDir '_hv-h4-green-req-test.json')
$map = Get-ReqItems -Path (Join-Path $outDir '_hv-h4-green-req-mapping.json')

$fr005 = $fr | Where-Object { $_.Id -eq 'FR-MCP-PRODUCT-005' }
$trCtx = $tr | Where-Object { $_.Id -eq 'TR-MCP-PRODUCT-CTX-001' }
$test006 = $test | Where-Object { $_.Id -eq 'TEST-MCP-PRODUCT-006' }
$map005 = $map | Where-Object { $_.FrId -eq 'FR-MCP-PRODUCT-005' }

Write-Output ('FR005_STATUS=' + $fr005.Status)
Write-Output ('FR005_JSON=' + ($fr005 | ConvertTo-Json -Depth 8 -Compress))
Write-Output ('TRCTX_STATUS=' + $trCtx.Status)
Write-Output ('TRCTX_AC_COUNT=' + @($trCtx.AcceptanceCriteria).Count)
Write-Output ('TRCTX_JSON=' + ($trCtx | ConvertTo-Json -Depth 8 -Compress))
Write-Output ('TEST006_STATUS=' + $test006.Status)
Write-Output ('TEST006_JSON=' + ($test006 | ConvertTo-Json -Depth 8 -Compress))
Write-Output ('MAP005_JSON=' + ($map005 | ConvertTo-Json -Depth 8 -Compress))

Set-Location -LiteralPath 'F:\GitHub\McpServer'
Write-Output '=== GIT_STAT ==='
& git --no-pager diff --stat -- 'tests/McpServer.Support.Mcp.Tests/Products/ProductRequirementContextTests.cs' 'src/McpServer.Support.Mcp/Products/Queries/GetProductRequirementContextQuery.cs' 'src/McpServer.Support.Mcp/Controllers/ContextController.cs'
Write-Output '=== GIT_DIFF_TEST ==='
& git --no-pager diff -- 'tests/McpServer.Support.Mcp.Tests/Products/ProductRequirementContextTests.cs'
