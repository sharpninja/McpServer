$ErrorActionPreference = 'Stop'
$wt = 'F:\GitHub\McpServer\.worktrees\sessionlife-p2-contracts-5cb2'
Set-Location -LiteralPath $wt
Write-Output '==== status ===='
git status --short
$paths = @(
    'src/McpServer.Storage/SessionLogSchemaGuard.cs',
    'tests/McpServer.Support.Mcp.Tests/Services/SessionLogSchemaGuardTests.cs'
)
git add -- $paths
$msg = Join-Path $env:USERPROFILE '.grok\p2-commit-schema.txt'
[System.IO.File]::WriteAllText($msg, "fix(sessionlog): isolate memory schema cache`n`nSQLite :memory: connections share one string, so a ready database skipped the pending-migration check on a different database.`n")
git commit -F $msg
Write-Output '==== after ===='
git status --short
git log -1 --format='%H%n%s'
