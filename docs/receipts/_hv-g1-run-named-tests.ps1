$ErrorActionPreference = 'Stop'
$logDir = 'F:\GitHub\McpServer\docs\receipts\_hv-g1-closeout'
New-Item -ItemType Directory -Force -Path $logDir | Out-Null
$filter = @(
    'FullyQualifiedName~SessionLogTriageStoreTests',
    'FullyQualifiedName~SessionLogServiceTurnContextTests',
    'FullyQualifiedName~SessionLogTurnContextValidatorTests',
    'FullyQualifiedName~SessionLogControllerErrorTests'
) -join '|'
$project = 'F:\GitHub\McpServer\tests\McpServer.Support.Mcp.Tests\McpServer.Support.Mcp.Tests.csproj'
$log = Join-Path $logDir 'named-unit.log'
$trx = Join-Path $logDir 'named-unit.trx'
$args = @(
    'test', $project,
    '-c', 'Debug',
    '--filter', $filter,
    '--logger', "trx;LogFileName=$trx",
    '--logger', 'console;verbosity=detailed',
    '--nologo'
)
$sw = [System.Diagnostics.Stopwatch]::StartNew()
& dotnet @args 2>&1 | Tee-Object -FilePath $log
$code = $LASTEXITCODE
$sw.Stop()
$summary = @{
    exitCode = $code
    elapsedMs = $sw.ElapsedMilliseconds
    filter = $filter
    log = $log
    trx = $trx
}
($summary | ConvertTo-Json) | Set-Content -LiteralPath (Join-Path $logDir 'named-unit-summary.json') -Encoding UTF8
Write-Output "EXIT=$code elapsedMs=$($sw.ElapsedMilliseconds)"
Select-String -LiteralPath $log -Pattern 'Passed!|Failed!|Total tests|Skipped' | ForEach-Object { $_.Line }
