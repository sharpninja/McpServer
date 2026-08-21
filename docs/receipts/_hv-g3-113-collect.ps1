$ErrorActionPreference = 'Stop'
$logDir = 'F:\GitHub\McpServer\docs\receipts\_hv-g3-113'
New-Item -ItemType Directory -Force -Path $logDir | Out-Null

$repo = 'F:\GitHub\McpServer'
Set-Location $repo

$static = [ordered]@{}
$static.gitBranch = (git rev-parse --abbrev-ref HEAD)
$static.gitHead = (git rev-parse HEAD)
$static.gitShort = (git rev-parse --short HEAD)

$turnEntity = Get-Content -LiteralPath (Join-Path $repo 'src\McpServer.Storage\McpDbContext.cs') -Raw
$static.hasCompositeUnique = [bool]($turnEntity -match 'HasIndex\(x => new \{ x\.SessionLogId, x\.RequestId \}\)\.IsUnique\(\)')
$static.hasRequestIdOnlyIndex = [bool]($turnEntity -match 'HasIndex\(x => x\.RequestId\)')

$snapshotHits = @()
foreach ($snap in @(
    'src\McpServer.Storage\Migrations\McpDbContextModelSnapshot.cs',
    'src\McpServer.Storage.SqliteMigrations\Migrations\McpDbContextModelSnapshot.cs',
    'src\McpServer.Storage.SqlServerMigrations\Migrations\McpDbContextModelSnapshot.cs',
    'src\McpServer.Storage.PostgreSqlMigrations\Migrations\McpDbContextModelSnapshot.cs'
)) {
    $text = Get-Content -LiteralPath (Join-Path $repo $snap) -Raw
    $composite = [regex]::Matches($text, 'HasIndex\("SessionLogId", "RequestId"\)[\s\S]{0,80}IsUnique')
    $requestOnly = [regex]::Matches($text, 'HasIndex\("RequestId"\)\s*\.IsUnique')
    $snapshotHits += [ordered]@{
        file = $snap
        compositeUniqueMatches = $composite.Count
        requestIdOnlyUniqueMatches = $requestOnly.Count
    }
}
$static.snapshots = $snapshotHits

$classifier = Get-Content -LiteralPath (Join-Path $repo 'src\McpServer.Support.Mcp\Services\McpErrorClassifier.cs') -Raw
$static.classifierHasPersistenceError = $classifier.Contains('PersistenceError = "persistence_error"')
$static.classifierDbUpdateMapsPersistence = $classifier.Contains('conflict ? Conflict : PersistenceError')
$static.classifierRejectsSeeInner = $classifier.Contains('The change could not be saved.')

$schema = Get-Content -LiteralPath (Join-Path $repo 'docs\context\session-log-schema.md') -Raw
$workflow = Get-Content -LiteralPath (Join-Path $repo 'docs\context\session-log-workflow-api.md') -Raw
$static.schemaDocumentsTags = $schema.Contains('session-scoped tags; persist and return on query')
$static.schemaDocumentsCanceled = $schema.Contains("'canceled', or 'cancelled'")
$static.schemaDocumentsCompositeUnique = $schema.Contains('SessionLogId') -and $schema.Contains('RequestId')
$static.schemaDocumentsCrossSessionDup = $schema.Contains('cross-session')
$static.workflowDocumentsMergeVsReplace = $workflow.Contains('Additive merge') -and $workflow.Contains('Replace.')

($static | ConvertTo-Json -Depth 6) | Set-Content -LiteralPath (Join-Path $logDir 'static.json') -Encoding utf8
Write-Output 'STATIC_WRITTEN=true'
Write-Output ("COMPOSITE_UNIQUE={0}" -f $static.hasCompositeUnique)
Write-Output ("REQUESTID_ONLY_INDEX={0}" -f $static.hasRequestIdOnlyIndex)
Write-Output ("WORKFLOW_MERGE_REPLACE={0}" -f $static.workflowDocumentsMergeVsReplace)
Write-Output ("SCHEMA_CROSS_SESSION={0}" -f $static.schemaDocumentsCrossSessionDup)

$filter = @(
    'FullyQualifiedName~SessionLogTriageStoreTests',
    'FullyQualifiedName~McpErrorClassifierTests',
    'FullyQualifiedName~SessionLogControllerErrorTests',
    'FullyQualifiedName~SessionLogSchemaGuardTests'
) -join '|'
$project = Join-Path $repo 'tests\McpServer.Support.Mcp.Tests\McpServer.Support.Mcp.Tests.csproj'
$log = Join-Path $logDir 'named-unit.log'
$trx = Join-Path $logDir 'named-unit.trx'
$dotnetArgs = @(
    'test', $project,
    '-c', 'Debug',
    '--filter', $filter,
    '--logger', "trx;LogFileName=$trx",
    '--logger', 'console;verbosity=normal',
    '--nologo'
)
$sw = [System.Diagnostics.Stopwatch]::StartNew()
& dotnet @dotnetArgs 2>&1 | Tee-Object -FilePath $log
$code = $LASTEXITCODE
$sw.Stop()
$summary = [ordered]@{
    exitCode = $code
    elapsedMs = $sw.ElapsedMilliseconds
    filter = $filter
    log = $log
    trx = $trx
}
($summary | ConvertTo-Json) | Set-Content -LiteralPath (Join-Path $logDir 'named-unit-summary.json') -Encoding utf8
Write-Output ("TEST_EXIT={0}" -f $code)
Write-Output ("TEST_MS={0}" -f $sw.ElapsedMilliseconds)
Select-String -LiteralPath $log -Pattern 'Passed!|Failed!|Total tests|Skipped|Passed:|Failed:|Skipped:' | ForEach-Object { $_.Line }
