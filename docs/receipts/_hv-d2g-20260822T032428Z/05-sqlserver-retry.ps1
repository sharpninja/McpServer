#Requires -Version 7.0
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Continue'

$workspace = 'F:\GitHub\McpServer'
$outDir = 'F:\GitHub\McpServer\docs\receipts\_hv-d2g-20260822T032428Z'
Set-Location -LiteralPath $workspace

$snap = [ordered]@{
    utc = [DateTime]::UtcNow.ToString('o')
    testhost = @(Get-Process testhost, 'VSTest.Console' -ErrorAction SilentlyContinue | Select-Object Name, Id, StartTime)
    sqlservr = @(Get-Process sqlservr -ErrorAction SilentlyContinue | Select-Object Name, Id, StartTime)
}
($snap | ConvertTo-Json -Depth 6) | Set-Content -LiteralPath (Join-Path $outDir 'process-before-sql-retry.json') -Encoding utf8

$sqlTrx = Join-Path $outDir 'd2g-handoff-migration-sqlserver-retry.trx'
$started = [DateTime]::UtcNow
dotnet test tests/McpServer.Support.Mcp.IntegrationTests -c Debug --filter "FullyQualifiedName~HandoffIngestionStorageMigrationTests.SqlServer_HandoffMigration_DowngradeAndReupgrade" --logger "trx;LogFileName=$sqlTrx" --results-directory $outDir *>&1 | Tee-Object -FilePath (Join-Path $outDir 'dotnet-migration-sqlserver-retry-stdout.txt')
$code = $LASTEXITCODE
$ended = [DateTime]::UtcNow
$meta = [ordered]@{
    name = 'dotnet-migration-sqlserver-retry'
    exit = $code
    startedUtc = $started.ToString('o')
    endedUtc = $ended.ToString('o')
    durationSec = [int]($ended - $started).TotalSeconds
}
($meta | ConvertTo-Json) | Set-Content -LiteralPath (Join-Path $outDir 'dotnet-migration-sqlserver-retry-exit.json') -Encoding utf8
Write-Output ('SQL_RETRY_EXIT=' + $code)
