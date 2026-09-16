#Requires -Version 7.0
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Continue'

$workspace = 'F:\GitHub\McpServer'
$outDir = 'F:\GitHub\McpServer\docs\receipts\_hv-d2g-20260822T032428Z'
Set-Location -LiteralPath $workspace

function Save-Json {
    param([string]$Name, $Object)
    ($Object | ConvertTo-Json -Depth 8) | Set-Content -LiteralPath (Join-Path $outDir $Name) -Encoding utf8
}

function Invoke-Logged {
    param(
        [Parameter(Mandatory)][string]$Name,
        [Parameter(Mandatory)][scriptblock]$Script
    )
    $stdout = Join-Path $outDir ($Name + '-stdout.txt')
    $stderr = Join-Path $outDir ($Name + '-stderr.txt')
    $started = [DateTime]::UtcNow
    Write-Output ('BEGIN ' + $Name + ' ' + $started.ToString('o'))
    & $Script *>&1 | Tee-Object -FilePath $stdout
    $code = $LASTEXITCODE
    $ended = [DateTime]::UtcNow
    $meta = [ordered]@{
        name = $Name
        exit = $code
        startedUtc = $started.ToString('o')
        endedUtc = $ended.ToString('o')
        durationSec = [int]($ended - $started).TotalSeconds
        stdout = $stdout
    }
    Save-Json -Name ($Name + '-exit.json') -Object $meta
    Write-Output ('END ' + $Name + ' EXIT=' + $code + ' SEC=' + $meta.durationSec)
    return $code
}

# 1. Exact parent Pester command
$pesterCode = Invoke-Logged -Name 'pester-CI' -Script {
    pwsh.exe -NoProfile -Command "Invoke-Pester -Path 'plugins/core/test-fixtures/pester/PluginHandoffSkill.Tests.ps1' -CI"
}
if (Test-Path -LiteralPath (Join-Path $workspace 'testResults.xml')) {
    Copy-Item -LiteralPath (Join-Path $workspace 'testResults.xml') -Destination (Join-Path $outDir 'pester-CI-testResults.xml') -Force
}

# 2. Exact named remaining-gap C# tests
$namedTrx = Join-Path $outDir 'd2g-handoff-csharp.trx'
$namedCode = Invoke-Logged -Name 'dotnet-named' -Script {
    dotnet test tests/McpServer.Support.Mcp.Tests -c Debug --filter "FullyQualifiedName~ProcessingLease_RenewsAndFencesTerminalUpdates|FullyQualifiedName~Provenance_IncludesEffectiveCustomPromptIdentityAndVersion" --logger "trx;LogFileName=$namedTrx" --results-directory $outDir
}

# 3. HandoffDurabilityTests class
$durTrx = Join-Path $outDir 'd2g-handoff-durability.trx'
$durCode = Invoke-Logged -Name 'dotnet-durability' -Script {
    dotnet test tests/McpServer.Support.Mcp.Tests -c Debug --filter "FullyQualifiedName~HandoffDurabilityTests" --logger "trx;LogFileName=$durTrx" --results-directory $outDir
}

# 4. PluginSync + CustomPromptTemplate reject
$syncTrx = Join-Path $outDir 'd2g-plugin-sync-reject.trx'
$syncCode = Invoke-Logged -Name 'dotnet-sync-reject' -Script {
    dotnet test tests/McpServer.Support.Mcp.Tests -c Debug --filter "FullyQualifiedName~PluginSync_HandoffSkill_MatchesCoreArtifact|FullyQualifiedName~IngestAsync_CustomPromptTemplate_IsRejected" --logger "trx;LogFileName=$syncTrx" --results-directory $outDir
}

# 5. Sqlite HandoffIngestionStorageMigrationTests at minimum
$sqliteTrx = Join-Path $outDir 'd2g-handoff-migration-sqlite.trx'
$sqliteCode = Invoke-Logged -Name 'dotnet-migration-sqlite' -Script {
    dotnet test tests/McpServer.Support.Mcp.IntegrationTests -c Debug --filter "FullyQualifiedName~HandoffIngestionStorageMigrationTests.Sqlite_HandoffMigration_DowngradeAndReupgrade" --logger "trx;LogFileName=$sqliteTrx" --results-directory $outDir
}

# Probe competing LocalDB / testhost before optional SQL Server/PostgreSQL
$procSnap = [ordered]@{
    afterSqliteUtc = [DateTime]::UtcNow.ToString('o')
    testhost = @(Get-Process testhost, 'VSTest.Console' -ErrorAction SilentlyContinue | Select-Object Name, Id, StartTime)
    sqlservr = @(Get-Process sqlservr -ErrorAction SilentlyContinue | Select-Object Name, Id, StartTime)
}
Save-Json -Name 'process-after-sqlite.json' -Object $procSnap

$sqlBusy = @($procSnap.sqlservr).Count -gt 2
$pgTrx = Join-Path $outDir 'd2g-handoff-migration-postgres.trx'
$sqlTrx = Join-Path $outDir 'd2g-handoff-migration-sqlserver.trx'

# PostgreSQL first (no LocalDB)
$pgCode = Invoke-Logged -Name 'dotnet-migration-postgres' -Script {
    dotnet test tests/McpServer.Support.Mcp.IntegrationTests -c Debug --filter "FullyQualifiedName~HandoffIngestionStorageMigrationTests.PostgreSql_HandoffMigration_DowngradeAndReupgrade" --logger "trx;LogFileName=$pgTrx" --results-directory $outDir
}

# SQL Server serial if not obviously a competing suite flood
$sqlCode = Invoke-Logged -Name 'dotnet-migration-sqlserver' -Script {
    dotnet test tests/McpServer.Support.Mcp.IntegrationTests -c Debug --filter "FullyQualifiedName~HandoffIngestionStorageMigrationTests.SqlServer_HandoffMigration_DowngradeAndReupgrade" --logger "trx;LogFileName=$sqlTrx" --results-directory $outDir
}

$summary = [ordered]@{
    pesterExit = $pesterCode
    namedExit = $namedCode
    durabilityExit = $durCode
    syncRejectExit = $syncCode
    sqliteMigrationExit = $sqliteCode
    postgresMigrationExit = $pgCode
    sqlServerMigrationExit = $sqlCode
    sqlBusyHint = $sqlBusy
}
Save-Json -Name 'test-summary.json' -Object $summary
Write-Output 'TESTS_DONE'
$summary | ConvertTo-Json
