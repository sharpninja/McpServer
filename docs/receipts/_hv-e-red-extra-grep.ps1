#Requires -Version 7.0
[CmdletBinding()]
param()

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$worktree = 'C:\Users\kingd\.grok\worktrees\github-mcpserver\subagent-01a029d1-24e5-7603-879b-832cfd4fe7f7'
$outDir = 'F:\GitHub\McpServer\docs\receipts\_hv-e-red-20260822T151657Z'

function Count-Hits([string]$root, [string]$pattern, [string]$filter) {
    if (-not (Test-Path -LiteralPath $root)) { return 0 }
    return @(Get-ChildItem -LiteralPath $root -Recurse -Filter $filter -File -ErrorAction SilentlyContinue |
        Select-String -Pattern $pattern).Count
}

$migSqlite = Count-Hits (Join-Path $worktree 'src\McpServer.Storage.SqliteMigrations') 'HostileReview' '*.cs'
$migPg = Count-Hits (Join-Path $worktree 'src\McpServer.Storage.PostgreSqlMigrations') 'HostileReview' '*.cs'
$migSql = Count-Hits (Join-Path $worktree 'src\McpServer.Storage.SqlServerMigrations') 'HostileReview' '*.cs'
$clientHits = Count-Hits (Join-Path $worktree 'src\McpServer.Client') 'HostileReview|hostile_review|hostile-review' '*.cs'
$replHits = Count-Hits (Join-Path $worktree 'src\McpServer.Repl.Core') 'HostileReview|hostileReview|hostile_review' '*.cs'
$directorHits = Count-Hits (Join-Path $worktree 'src\McpServer.Cqrs.Mvvm') 'HostileReview|hostile-review' '*.cs'
$stdioHits = Count-Hits (Join-Path $worktree 'src\McpServer.Support.Mcp\McpStdio') 'hostile_review|HostileReview' '*.cs'
$ctrlHits = Count-Hits (Join-Path $worktree 'src\McpServer.Support.Mcp\Controllers') 'HostileReview|hostile-review' '*.cs'
$dbHits = Count-Hits (Join-Path $worktree 'src\McpServer.Storage') 'DbSet\s*<.*HostileReview|HostileReview' '*.cs'
$pluginHits = Count-Hits (Join-Path $worktree 'plugins') 'hostile-review|HostileReview|hostileReview' '*'
$testOther = @(Get-ChildItem -LiteralPath (Join-Path $worktree 'tests') -Recurse -Filter '*HostileReview*' -File -ErrorAction SilentlyContinue | ForEach-Object { $_.FullName })

$fwh = Join-Path $worktree 'src\McpServer.Support.Mcp\McpStdio\FwhMcpTools.cs'
$fwhToolHits = @()
if (Test-Path -LiteralPath $fwh) {
    $fwhToolHits = @(Select-String -LiteralPath $fwh -Pattern 'hostile_review|HostileReview' | ForEach-Object { $_.Line.Trim() })
}

Write-Output ('MIG_SQLITE=' + $migSqlite)
Write-Output ('MIG_PG=' + $migPg)
Write-Output ('MIG_SQL=' + $migSql)
Write-Output ('CLIENT_HITS=' + $clientHits)
Write-Output ('REPL_HITS=' + $replHits)
Write-Output ('DIRECTOR_HITS=' + $directorHits)
Write-Output ('STDIO_HITS=' + $stdioHits)
Write-Output ('CTRL_HITS=' + $ctrlHits)
Write-Output ('DB_HITS=' + $dbHits)
Write-Output ('PLUGIN_HITS=' + $pluginHits)
Write-Output ('TEST_FILES=' + ($testOther -join '|'))
Write-Output ('FWH_TOOL_HITS=' + $fwhToolHits.Count)
foreach ($h in $fwhToolHits) { Write-Output ('FWH_LINE=' + $h) }

$report = [ordered]@{
    migSqlite = $migSqlite
    migPg = $migPg
    migSql = $migSql
    clientHits = $clientHits
    replHits = $replHits
    directorHits = $directorHits
    stdioHits = $stdioHits
    ctrlHits = $ctrlHits
    dbHits = $dbHits
    pluginHits = $pluginHits
    testFiles = $testOther
    fwhToolHits = $fwhToolHits
}
$report | ConvertTo-Json -Depth 6 | Set-Content -LiteralPath (Join-Path $outDir 'a3-extra.json') -Encoding utf8
