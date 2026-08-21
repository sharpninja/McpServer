#Requires -Version 7.0
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$outDir = 'F:\GitHub\McpServer\docs\receipts\_hv-closeout-s2'
New-Item -ItemType Directory -Force -Path $outDir | Out-Null
$wt = 'F:\GitHub\McpServer\.worktrees\triage-closeout'

$paths = @(
    'src/McpServer.Storage/Database/SqliteSessionLogHeaderDdl.cs',
    'src/McpServer.Storage/McpDbContext.cs',
    'src/McpServer.Storage.SqliteMigrations/Migrations/20260818205751_AddSessionLogTagsAndAgentSessionHeaders.cs',
    'src/McpServer.Storage.SqlServerMigrations/Migrations/20260818205807_AddSessionLogTagsAndAgentSessionHeaders.cs',
    'src/McpServer.Storage.PostgreSqlMigrations/Migrations/20260818205822_AddSessionLogTagsAndAgentSessionHeaders.cs',
    'tests/McpServer.Support.Mcp.Tests/Storage/SessionLogAgentSessionHeaderMigrationTests.cs'
)

$status = @(git -C $wt status --porcelain)
$diffStat = git -C $wt diff --stat
$untracked = @(git -C $wt ls-files --others --exclude-standard)

$perFile = [ordered]@{}
foreach ($rel in $paths) {
    $abs = Join-Path $wt ($rel -replace '/', '\')
    $headText = ''
    $headExists = $true
    try {
        $headText = git -C $wt show "HEAD:$rel" 2>$null
        if ($LASTEXITCODE -ne 0) { $headExists = $false; $headText = '' }
    } catch {
        $headExists = $false
        $headText = ''
    }
    $workText = if (Test-Path -LiteralPath $abs) { Get-Content -LiteralPath $abs -Raw } else { '' }
    $perFile[$rel] = [ordered]@{
        WorkExists = Test-Path -LiteralPath $abs
        HeadExists = [bool]$headExists
        WorkHasPragma = [bool]($workText -match 'pragma_table_info')
        WorkHasHelper = [bool]($workText -match 'mcp_add_sessionlog_text_column_if_missing')
        WorkHasAlreadyHasTest = [bool]($workText -match 'SqliteMigrateAsync_SessionLogsAlreadyHasHeaderColumnsAndTags_Succeeds')
        WorkHasColLength = [bool]($workText -match 'COL_LENGTH')
        WorkHasIfNotExists = [bool]($workText -match 'ADD COLUMN IF NOT EXISTS')
        WorkHasBareAlter = [bool]($workText -match 'ALTER TABLE "SessionLogs" ADD COLUMN')
        HeadHasPragma = [bool]($headText -match 'pragma_table_info')
        HeadHasHelper = [bool]($headText -match 'mcp_add_sessionlog_text_column_if_missing')
        HeadHasAlreadyHasTest = [bool]($headText -match 'SqliteMigrateAsync_SessionLogsAlreadyHasHeaderColumnsAndTags_Succeeds')
    }
}

$result = [ordered]@{
    TimestampUtc = [DateTime]::UtcNow.ToString('yyyy-MM-ddTHH:mm:ssZ')
    Branch = (git -C $wt rev-parse --abbrev-ref HEAD)
    Head = (git -C $wt rev-parse HEAD)
    StatusPorcelain = $status
    Untracked = $untracked
    DiffStat = $diffStat
    PerFile = $perFile
}

$jsonPath = Join-Path $outDir 'git-diff.json'
$result | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath $jsonPath -Encoding utf8
Write-Output $jsonPath
Write-Output ($result | ConvertTo-Json -Depth 8)
exit 0
