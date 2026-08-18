#Requires -Version 7.0
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Continue'

$workspace = 'F:\GitHub\McpServer'
$outDir = Join-Path $workspace 'docs\receipts'
Set-Location -LiteralPath $workspace

Write-Output ('UTC_START=' + [datetime]::UtcNow.ToString('o'))

Write-Output '=== PRODUCT_FILTER ==='
$productLog = Join-Path $outDir '_hv-h5-done-test-product.txt'
$productArgs = @(
    'test',
    'tests/McpServer.Support.Mcp.Tests',
    '-c', 'Debug',
    '--filter', 'FullyQualifiedName~Product',
    '--nologo'
)
$product = & dotnet @productArgs 2>&1 | Tee-Object -FilePath $productLog
$productExit = $LASTEXITCODE
Write-Output ('PRODUCT_EXIT=' + $productExit)
$product | Select-Object -Last 20 | ForEach-Object { Write-Output ('PRODUCT_TAIL ' + $_) }

Write-Output '=== PRODUCT_LAUNCH ==='
$launchLog = Join-Path $outDir '_hv-h5-done-test-launch.txt'
$launchArgs = @(
    'test',
    'tests/McpServer.Support.Mcp.IntegrationTests',
    '-c', 'Debug',
    '--filter', 'FullyQualifiedName~ProductsLaunchTests',
    '--nologo'
)
$launch = & dotnet @launchArgs 2>&1 | Tee-Object -FilePath $launchLog
$launchExit = $LASTEXITCODE
Write-Output ('LAUNCH_EXIT=' + $launchExit)
$launch | Select-Object -Last 20 | ForEach-Object { Write-Output ('LAUNCH_TAIL ' + $_) }

Write-Output '=== VALIDATE_TRACEABILITY ==='
$traceLog = Join-Path $outDir '_hv-h5-done-traceability.txt'
$trace = & pwsh.exe -NoProfile -NonInteractive -File .\build.ps1 ValidateTraceability 2>&1 | Tee-Object -FilePath $traceLog
$traceExit = $LASTEXITCODE
Write-Output ('TRACE_EXIT=' + $traceExit)
$trace | Select-Object -Last 25 | ForEach-Object { Write-Output ('TRACE_TAIL ' + $_) }

Write-Output ('UTC_END=' + [datetime]::UtcNow.ToString('o'))
Write-Output 'TESTS_DONE'
