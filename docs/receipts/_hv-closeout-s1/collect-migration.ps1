#Requires -Version 7.0
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$outDir = 'F:\GitHub\McpServer\docs\receipts\_hv-closeout-s1'
New-Item -ItemType Directory -Force -Path $outDir | Out-Null
$root = 'F:\GitHub\McpServer\.worktrees\triage-closeout'

$sqlite = Join-Path $root 'src\McpServer.Storage.SqliteMigrations\Migrations\20260818205751_AddSessionLogTagsAndAgentSessionHeaders.cs'
$sqlserver = Join-Path $root 'src\McpServer.Storage.SqlServerMigrations\Migrations\20260818205807_AddSessionLogTagsAndAgentSessionHeaders.cs'
$postgres = Join-Path $root 'src\McpServer.Storage.PostgreSqlMigrations\Migrations\20260818205822_AddSessionLogTagsAndAgentSessionHeaders.cs'
$guard = Join-Path $root 'src\McpServer.Storage\SessionLogSchemaGuard.cs'
$handwritten = Join-Path $root 'src\McpServer.Storage\Migrations\20260722214500_AddAgentSessionHeaderFields.cs'
$tests = Join-Path $root 'tests\McpServer.Support.Mcp.Tests\Storage\SessionLogAgentSessionHeaderMigrationTests.cs'
$scratch = Join-Path $root 'tests\McpServer.Support.Mcp.IntegrationTests\ScratchSqliteSchema.cs'

function Get-CitationCounts {
    param([string]$Path, [string]$Pattern)
    if (-not (Test-Path -LiteralPath $Path)) {
        return $null
    }
    return @(Select-String -LiteralPath $Path -Pattern $Pattern -SimpleMatch).Count
}

$sqliteText = Get-Content -LiteralPath $sqlite -Raw
$sqlserverText = Get-Content -LiteralPath $sqlserver -Raw
$postgresText = Get-Content -LiteralPath $postgres -Raw
$guardText = Get-Content -LiteralPath $guard -Raw
$handText = Get-Content -LiteralPath $handwritten -Raw
$testText = Get-Content -LiteralPath $tests -Raw
$scratchText = Get-Content -LiteralPath $scratch -Raw

$headerCols = @(
    'AgentSessionId',
    'AgentSessionTranscriptFile',
    'AgentExecutablePath',
    'AgentExecutableVersion'
)

$sqliteHasEach = @{}
foreach ($col in $headerCols) {
    $sqliteHasEach[$col] = $sqliteText.Contains($col)
}

$pragmaCheck = $sqliteText -match 'PRAGMA\s+table_info'
$colLengthCheck = $sqliteText -match 'COL_LENGTH'
$ifNotExistsOnAlter = $sqliteText -match 'ADD COLUMN IF NOT EXISTS'
$helperName = $sqliteText -match 'AddNullableTextColumnIfMissing'
$helperBodyIsBareAlter = $sqliteText -match 'ALTER TABLE "SessionLogs" ADD COLUMN "\{column\}" TEXT NULL;'
$sessionLogTagsIfNotExists = $sqliteText -match 'CREATE TABLE IF NOT EXISTS "SessionLogTags"'
$migrationAttrOnHandwritten = $handText -match '\[Migration\('
$dbContextAttrOnHandwritten = $handText -match '\[DbContext\('

$docs = @(
    'docs\Project\Functional-Requirements.md',
    'docs\Project\Technical-Requirements.md',
    'docs\Project\Testing-Requirements.md',
    'docs\Project\TR-per-FR-Mapping.md',
    'docs\Project\Requirements-Matrix.md'
)
$docHits = @{}
foreach ($rel in $docs) {
    $p = Join-Path $root $rel
    $docHits[$rel] = [ordered]@{
        Exists = Test-Path -LiteralPath $p
        Hit20260722214500 = if (Test-Path -LiteralPath $p) { (Get-CitationCounts -Path $p -Pattern '20260722214500') } else { $null }
        Hit20260818205751 = if (Test-Path -LiteralPath $p) { (Get-CitationCounts -Path $p -Pattern '20260818205751') } else { $null }
    }
}

$result = [ordered]@{
    TimestampUtc = [DateTime]::UtcNow.ToString('yyyy-MM-ddTHH:mm:ssZ')
    SqlitePath = $sqlite
    SqliteExists = Test-Path -LiteralPath $sqlite
    SqliteHeaderColumnsPresent = $sqliteHasEach
    SqliteHasPragmaTableInfo = [bool]$pragmaCheck
    SqliteHasColLength = [bool]$colLengthCheck
    SqliteHasAddColumnIfNotExists = [bool]$ifNotExistsOnAlter
    SqliteHelperNamedIfMissing = [bool]$helperName
    SqliteHelperBodyBareAlter = [bool]$helperBodyIsBareAlter
    SqliteCreateSessionLogTagsIfNotExists = [bool]$sessionLogTagsIfNotExists
    GuardPendingMessageHas20260818205751 = $guardText.Contains('20260818205751')
    GuardPendingMessageHas20260818205807 = $guardText.Contains('20260818205807')
    GuardPendingMessageHas20260818205822 = $guardText.Contains('20260818205822')
    GuardPendingMessageHas20260722214500 = $guardText.Contains('20260722214500')
    HandwrittenExists = Test-Path -LiteralPath $handwritten
    HandwrittenHasMigrationAttribute = [bool]$migrationAttrOnHandwritten
    HandwrittenHasDbContextAttribute = [bool]$dbContextAttrOnHandwritten
    TestsCallRepairLegacy = $testText.Contains('RepairLegacySessionLogHeaderColumnsAsync') -and -not ($testText -match 'without <c>ScratchSqliteSchema.RepairLegacySessionLogHeaderColumnsAsync')
    TestsContainRepairLegacyToken = $testText.Contains('RepairLegacySessionLogHeaderColumnsAsync')
    TestsCallMigrateAsync = $testText.Contains('MigrateAsync')
    TestsSqlServerLiveMigrate = $testText.Contains('SqlServerMigrateAsync')
    TestsPostgresLiveMigrate = $testText.Contains('PostgreSqlMigrateAsync') -or $testText.Contains('PostgresMigrateAsync')
    ScratchStillCallsRepair = $scratchText.Contains('RepairLegacySessionLogHeaderColumnsAsync')
    ScratchStaleCommentNoUpAdds = $scratchText.Contains('no provider Up() adds it')
    SqlServerHasColLength = $sqlserverText.Contains('COL_LENGTH')
    PostgresHasIfNotExists = $postgresText.Contains('ADD COLUMN IF NOT EXISTS')
    DocHits = $docHits
    SqliteHelperSnippet = (($sqliteText -split "`n") | Select-Object -Skip 39 -First 13) -join "`n"
}

$jsonPath = Join-Path $outDir 'migration.json'
$result | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath $jsonPath -Encoding utf8
Write-Output $jsonPath
Write-Output ($result | ConvertTo-Json -Depth 8)
exit 0
