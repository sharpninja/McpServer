#Requires -Version 7.0
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Continue'

$workspace = 'F:\GitHub\McpServer'
$outDir = Join-Path $workspace 'docs\receipts'
Set-Location -LiteralPath $workspace

Write-Output '=== LIST_TESTS ==='
$listStart = [datetime]::UtcNow
$list = & dotnet test 'tests/McpServer.Support.Mcp.Tests' -c Debug --filter 'FullyQualifiedName~ProductRequirementContextTests' --list-tests --nologo
$listEnd = [datetime]::UtcNow
$list | Set-Content -LiteralPath (Join-Path $outDir '_hv-h4-red-list.txt') -Encoding utf8
Write-Output ('LIST_START=' + $listStart.ToString('o'))
Write-Output ('LIST_END=' + $listEnd.ToString('o'))
Write-Output ('LIST_EXIT=' + $LASTEXITCODE)
$named = @($list | Where-Object { $_ -match 'ProductRequirementContextTests\.' })
Write-Output ('LIST_NAMED_COUNT=' + $named.Count)
foreach ($line in $named) {
    Write-Output ('LIST_CASE ' + $line.Trim())
}

Write-Output '=== RUN_FILTER ==='
$runStart = [datetime]::UtcNow
$run = & dotnet test 'tests/McpServer.Support.Mcp.Tests' -c Debug --filter 'FullyQualifiedName~ProductRequirementContextTests' --nologo --logger 'console;verbosity=detailed'
$runEnd = [datetime]::UtcNow
$run | Set-Content -LiteralPath (Join-Path $outDir '_hv-h4-red-test-output.txt') -Encoding utf8
Write-Output ('RUN_START=' + $runStart.ToString('o'))
Write-Output ('RUN_END=' + $runEnd.ToString('o'))
Write-Output ('RUN_EXIT=' + $LASTEXITCODE)
$summary = @($run | Where-Object { $_ -match 'Passed!|Failed!|Total tests|Failed\s+|Passed\s+|Skipped\s+|Total\s+' })
foreach ($line in $summary) {
    Write-Output ('SUMMARY ' + $line.Trim())
}
$notImpl = @($run | Where-Object { $_ -match 'not implemented' })
Write-Output ('NOT_IMPLEMENTED_LINE_COUNT=' + $notImpl.Count)
foreach ($line in $notImpl) {
    $clip = $line.Trim()
    if ($clip.Length -gt 300) { $clip = $clip.Substring(0, 300) }
    Write-Output ('NOT_IMPL ' + $clip)
}
$errorLines = @($run | Where-Object { $_ -match 'error CS|Build FAILED|error MSB' })
Write-Output ('COMPILE_ERROR_LINE_COUNT=' + $errorLines.Count)
foreach ($line in $errorLines) {
    Write-Output ('COMPILE ' + $line.Trim())
}

Write-Output 'TESTS_DONE'
