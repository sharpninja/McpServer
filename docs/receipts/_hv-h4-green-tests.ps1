#Requires -Version 7.0
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Continue'

$workspace = 'F:\GitHub\McpServer'
$outDir = Join-Path $workspace 'docs\receipts'
Set-Location -LiteralPath $workspace

Write-Output '=== LIST_CONTEXT_TESTS ==='
$listStart = [datetime]::UtcNow
$list = & dotnet test 'tests/McpServer.Support.Mcp.Tests' -c Debug --filter 'FullyQualifiedName~ProductRequirementContextTests' --list-tests --nologo
$listEnd = [datetime]::UtcNow
$list | Set-Content -LiteralPath (Join-Path $outDir '_hv-h4-green-list-ctx.txt') -Encoding utf8
Write-Output ('LIST_CTX_START=' + $listStart.ToString('o'))
Write-Output ('LIST_CTX_END=' + $listEnd.ToString('o'))
Write-Output ('LIST_CTX_EXIT=' + $LASTEXITCODE)
$named = @($list | Where-Object { $_ -match 'ProductRequirementContextTests\.' })
Write-Output ('LIST_CTX_NAMED_COUNT=' + $named.Count)
foreach ($line in $named) {
    Write-Output ('LIST_CTX_CASE ' + $line.Trim())
}

Write-Output '=== RUN_CONTEXT_FILTER ==='
$runStart = [datetime]::UtcNow
$run = & dotnet test 'tests/McpServer.Support.Mcp.Tests' -c Debug --filter 'FullyQualifiedName~ProductRequirementContextTests' --nologo --logger 'console;verbosity=detailed'
$runEnd = [datetime]::UtcNow
$run | Set-Content -LiteralPath (Join-Path $outDir '_hv-h4-green-test-ctx.txt') -Encoding utf8
Write-Output ('RUN_CTX_START=' + $runStart.ToString('o'))
Write-Output ('RUN_CTX_END=' + $runEnd.ToString('o'))
Write-Output ('RUN_CTX_EXIT=' + $LASTEXITCODE)
$summary = @($run | Where-Object { $_ -match 'Passed!|Failed!|Total tests|Failed\s+|Passed\s+|Skipped\s+|Total\s+' })
foreach ($line in $summary) {
    Write-Output ('SUMMARY_CTX ' + $line.Trim())
}
$notImpl = @($run | Where-Object { $_ -match 'not implemented' })
Write-Output ('NOT_IMPLEMENTED_LINE_COUNT=' + $notImpl.Count)
$errorLines = @($run | Where-Object { $_ -match 'error CS|Build FAILED|error MSB' })
Write-Output ('COMPILE_ERROR_LINE_COUNT=' + $errorLines.Count)
foreach ($line in $errorLines) {
    Write-Output ('COMPILE ' + $line.Trim())
}

Write-Output '=== LIST_PRODUCT_FILTER ==='
$listPStart = [datetime]::UtcNow
$listP = & dotnet test 'tests/McpServer.Support.Mcp.Tests' -c Debug --filter 'FullyQualifiedName~Product' --list-tests --nologo
$listPEnd = [datetime]::UtcNow
$listP | Set-Content -LiteralPath (Join-Path $outDir '_hv-h4-green-list-product.txt') -Encoding utf8
Write-Output ('LIST_PRODUCT_START=' + $listPStart.ToString('o'))
Write-Output ('LIST_PRODUCT_END=' + $listPEnd.ToString('o'))
Write-Output ('LIST_PRODUCT_EXIT=' + $LASTEXITCODE)
$namedP = @($listP | Where-Object { $_ -match '^\s+McpServer\.Support\.Mcp\.Tests\.' })
if ($namedP.Count -eq 0) {
    $namedP = @($listP | Where-Object { $_ -match 'Tests\.' -and $_ -notmatch 'The following Tests are available' })
}
Write-Output ('LIST_PRODUCT_NAMED_COUNT=' + $namedP.Count)

Write-Output '=== RUN_PRODUCT_FILTER ==='
$runPStart = [datetime]::UtcNow
$runP = & dotnet test 'tests/McpServer.Support.Mcp.Tests' -c Debug --filter 'FullyQualifiedName~Product' --nologo --logger 'console;verbosity=detailed'
$runPEnd = [datetime]::UtcNow
$runP | Set-Content -LiteralPath (Join-Path $outDir '_hv-h4-green-test-product.txt') -Encoding utf8
Write-Output ('RUN_PRODUCT_START=' + $runPStart.ToString('o'))
Write-Output ('RUN_PRODUCT_END=' + $runPEnd.ToString('o'))
Write-Output ('RUN_PRODUCT_EXIT=' + $LASTEXITCODE)
$summaryP = @($runP | Where-Object { $_ -match 'Passed!|Failed!|Total tests|Failed\s+|Passed\s+|Skipped\s+|Total\s+' })
foreach ($line in $summaryP) {
    Write-Output ('SUMMARY_PRODUCT ' + $line.Trim())
}
$skipP = @($runP | Where-Object { $_ -match 'Skipped' })
Write-Output ('PRODUCT_SKIP_LINE_COUNT=' + $skipP.Count)
$errorP = @($runP | Where-Object { $_ -match 'error CS|Build FAILED|error MSB' })
Write-Output ('PRODUCT_COMPILE_ERROR_LINE_COUNT=' + $errorP.Count)

Write-Output 'TESTS_DONE'
