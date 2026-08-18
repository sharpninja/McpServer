#Requires -Version 7.0
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Continue'

$workspace = 'F:\GitHub\McpServer'
$outDir = 'F:\GitHub\McpServer\docs\receipts'
Set-Location $workspace

Write-Output 'START_PRODUCT_FILTER'
$productLog = Join-Path $outDir '_hv-h5-rerun-product.txt'
& dotnet test tests/McpServer.Support.Mcp.Tests -c Debug --no-build --filter 'FullyQualifiedName~Product' --results-directory F:\GitHub\McpServer\TestResults | Tee-Object -FilePath $productLog
Write-Output ('PRODUCT_EXIT=' + $LASTEXITCODE)

Write-Output 'START_LAUNCH_FILTER'
$launchLog = Join-Path $outDir '_hv-h5-rerun-launch.txt'
& dotnet test tests/McpServer.Support.Mcp.IntegrationTests -c Debug --filter 'FullyQualifiedName~ProductsLaunchTests' --results-directory F:\GitHub\McpServer\TestResults | Tee-Object -FilePath $launchLog
Write-Output ('LAUNCH_EXIT=' + $LASTEXITCODE)

Write-Output 'TESTS_DONE'
