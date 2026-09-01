#Requires -Version 7.0
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Continue'

Set-Location -LiteralPath 'F:\GitHub\McpServer'
$outDir = 'F:\GitHub\McpServer\docs\receipts'

Write-Output '=== CTX_DEFAULT ==='
$s = [datetime]::UtcNow
$r = & dotnet test 'tests/McpServer.Support.Mcp.Tests' -c Debug --filter 'FullyQualifiedName~ProductRequirementContextTests' --nologo
$e = [datetime]::UtcNow
$r | Set-Content -LiteralPath (Join-Path $outDir '_hv-h4-green-test-ctx-default.txt') -Encoding utf8
Write-Output ('CTX_START=' + $s.ToString('o'))
Write-Output ('CTX_END=' + $e.ToString('o'))
Write-Output ('CTX_EXIT=' + $LASTEXITCODE)
$r | Where-Object { $_ -match 'Passed!|Failed!|Failed:|Passed:|Skipped:|Total:' } | ForEach-Object { Write-Output ('CTX_SUM ' + $_.Trim()) }

Write-Output '=== PRODUCT_DEFAULT ==='
$s2 = [datetime]::UtcNow
$r2 = & dotnet test 'tests/McpServer.Support.Mcp.Tests' -c Debug --filter 'FullyQualifiedName~Product' --nologo
$e2 = [datetime]::UtcNow
$r2 | Set-Content -LiteralPath (Join-Path $outDir '_hv-h4-green-test-product-default.txt') -Encoding utf8
Write-Output ('PRODUCT_START=' + $s2.ToString('o'))
Write-Output ('PRODUCT_END=' + $e2.ToString('o'))
Write-Output ('PRODUCT_EXIT=' + $LASTEXITCODE)
$r2 | Where-Object { $_ -match 'Passed!|Failed!|Failed:|Passed:|Skipped:|Total:' } | ForEach-Object { Write-Output ('PRODUCT_SUM ' + $_.Trim()) }
