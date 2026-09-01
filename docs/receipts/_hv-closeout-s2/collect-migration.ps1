#Requires -Version 7.0
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$outDir = 'F:\GitHub\McpServer\docs\receipts\_hv-closeout-s2'
New-Item -ItemType Directory -Force -Path $outDir | Out-Null
$root = 'F:\GitHub\McpServer\.worktrees\triage-closeout'

$sqlite = Join-Path $root 'src\McpServer.Storage.SqliteMigrations\Migrations\20260818205751_AddSessionLogTagsAndAgentSessionHeaders.cs'
$sqlserver = Join-Path $root 'src\McpServer.Storage.SqlServerMigrations\Migrations\20260818205807_AddSessionLogTagsAndAgentSessionHeaders.cs'
$postgres = Join-Path $root 'src\McpServer.Storage.PostgreSqlMigrations\Migrations\20260818205822_AddSessionLogTagsAndAgentSessionHeaders.cs'
$ddl = Join-Path $root 'src\McpServer.Storage\Database\SqliteSessionLogHeaderDdl.cs'
$ctx = Join-Path $root 'src\McpServer.Storage\McpDbContext.cs'
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
$ddlText = Get-Content -LiteralPath $ddl -Raw
$ctxText = Get-Content -LiteralPath $ctx -Raw
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

$sqliteHasEach = [ordered]@{}
foreach ($col in $headerCols) {
    $sqliteHasEach[$col] = $sqliteText.Contains($col)
}

$docs = @(
    'docs\Project\Functional-Requirements.md',
    'docs\Project\Technical-Requirements.md',
    'docs\Project\Testing-Requirements.md',
    'docs\Project\TR-per-FR-Mapping.md',
    'docs\Project\Requirements-Matrix.md'
)
$docHits = [ordered]@{}
foreach ($rel in $docs) {
    $p = Join-Path $root $rel
    $docHits[$rel] = [ordered]@{
        Exists = Test-Path -LiteralPath $p
        Hit20260722214500 = if (Test-Path -LiteralPath $p) { (Get-CitationCounts -Path $p -Pattern '20260722214500') } else { $null }
        Hit20260818205751 = if (Test-Path -LiteralPath $p) { (Get-CitationCounts -Path $p -Pattern '20260818205751') } else { $null }
    }
}

$helperAlwaysAlters = $ddlText.Contains('ALTER TABLE "SessionLogs" ADD COLUMN "{column}" TEXT NULL;') -or
    $ddlText.Contains('ALTER TABLE "SessionLogs" ADD COLUMN "{column}" TEXT NULL')
$helperHasPragma = $ddlText -match 'pragma_table_info'
$helperHasIfMissingBranch = $ddlText -match 'IF NOT EXISTS' -or $ddlText -match 'ColumnExists'
$callerHasPragmaWhere = $sqliteText -match "pragma_table_info\('SessionLogs'\)" -and $sqliteText -match 'WHERE NOT EXISTS'
$callerInvokesHelper = $sqliteText.Contains('mcp_add_sessionlog_text_column_if_missing')
$registerOnContext = $ctxText.Contains('RegisterSqliteSessionLogHeaderDdl') -and $ctxText.Contains('SqliteSessionLogHeaderDdl.Register')

$sqlServerCreateTableIfNotExists = $sqlserverText -match 'IF OBJECT_ID' -or $sqlserverText -match 'CREATE TABLE IF NOT EXISTS'
$postgresCreateTableIfNotExists = $postgresText -match 'CREATE TABLE IF NOT EXISTS'
$sqlServerUsesCreateTableApi = $sqlserverText.Contains('migrationBuilder.CreateTable')
$postgresUsesCreateTableApi = $postgresText.Contains('migrationBuilder.CreateTable')

$alreadyHasTest = $testText.Contains('SqliteMigrateAsync_SessionLogsAlreadyHasHeaderColumnsAndTags_Succeeds')
$sqlServerSourceTest = $testText.Contains('SqlServerUp_AddsFourHeaderColumnsIdempotentlyWithColLengthGuards')
$postgresSourceTest = $testText.Contains('PostgreSqlUp_AddsFourHeaderColumnsIdempotentlyWithIfNotExists')
$liveSqlServer = $testText.Contains('SqlServerMigrateAsync') -or $testText -match 'UseSqlServer'
$livePostgres = $testText.Contains('PostgresMigrateAsync') -or $testText.Contains('PostgreSqlMigrateAsync') -or $testText -match 'UseNpgsql'
$sourceCheckedComment = $testText.Contains('those Up() scripts are source-checked here instead')
$skipAttr = [bool]($testText -match '\[Fact\(Skip') -or [bool]($testText -match 'Skip\.')

$result = [ordered]@{
    TimestampUtc = [DateTime]::UtcNow.ToString('yyyy-MM-ddTHH:mm:ssZ')
    SqlitePath = $sqlite
    DdlPath = $ddl
    SqliteHeaderColumnsPresent = $sqliteHasEach
    CallerInvokesHelper = [bool]$callerInvokesHelper
    CallerHasPragmaWhereNotExists = [bool]$callerHasPragmaWhere
    HelperFunctionAlwaysIssuesAlter = [bool]$helperAlwaysAlters
    HelperFunctionContainsPragma = [bool]$helperHasPragma
    HelperFunctionHasMissingColumnBranch = [bool]$helperHasIfMissingBranch
    HelperUsesSqlite3Exec = $ddlText.Contains('sqlite3_exec')
    RegisterOnMcpDbContext = [bool]$registerOnContext
    SqliteCreateSessionLogTagsIfNotExists = [bool]($sqliteText -match 'CREATE TABLE IF NOT EXISTS "SessionLogTags"')
    GuardPendingMessageHas20260818205751 = $guardText.Contains('20260818205751')
    GuardPendingMessageHas20260818205807 = $guardText.Contains('20260818205807')
    GuardPendingMessageHas20260818205822 = $guardText.Contains('20260818205822')
    GuardPendingMessageHas20260722214500 = $guardText.Contains('20260722214500')
    HandwrittenExists = Test-Path -LiteralPath $handwritten
    HandwrittenHasMigrationAttribute = [bool]($handText -match '\[Migration\(')
    HandwrittenHasDbContextAttribute = [bool]($handText -match '\[DbContext\(')
    TestsContainAlreadyHasColumnsFact = [bool]$alreadyHasTest
    TestsCallRepairLegacyMethod = $testText.Contains('RepairLegacySessionLogHeaderColumnsAsync(')
    TestsContainRepairLegacyToken = $testText.Contains('RepairLegacySessionLogHeaderColumnsAsync')
    TestsCallMigrateAsync = $testText.Contains('MigrateAsync')
    TestsSqlServerLiveMigrate = [bool]$liveSqlServer
    TestsPostgresLiveMigrate = [bool]$livePostgres
    TestsSqlServerSourceAssert = [bool]$sqlServerSourceTest
    TestsPostgresSourceAssert = [bool]$postgresSourceTest
    TestsDocumentSourceCheckedNotLive = [bool]$sourceCheckedComment
    TestsHaveSkipAttribute = [bool]$skipAttr
    ScratchStillDefinesRepair = $scratchText.Contains('RepairLegacySessionLogHeaderColumnsAsync')
    ScratchApplyStillCallsRepair = $scratchText.Contains('await RepairLegacySessionLogHeaderColumnsAsync')
    ScratchStaleCommentNoUpAdds = $scratchText.Contains('no provider Up() adds it')
    SqlServerHasColLength = $sqlserverText.Contains('COL_LENGTH')
    SqlServerCreateTableApi = [bool]$sqlServerUsesCreateTableApi
    SqlServerSessionLogTagsIfNotExists = [bool]$sqlServerCreateTableIfNotExists
    PostgresHasIfNotExists = $postgresText.Contains('ADD COLUMN IF NOT EXISTS')
    PostgresCreateTableApi = [bool]$postgresUsesCreateTableApi
    PostgresSessionLogTagsIfNotExists = [bool]$postgresCreateTableIfNotExists
    DocHits = $docHits
    DdlSnippet = (($ddlText -split "`n") | Select-Object -First 40) -join "`n"
    SqliteHelperCallSnippet = (($sqliteText -split "`n") | Select-Object -Skip 39 -First 18) -join "`n"
}

$jsonPath = Join-Path $outDir 'migration.json'
$result | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath $jsonPath -Encoding utf8
Write-Output $jsonPath
Write-Output ($result | ConvertTo-Json -Depth 8)
exit 0
