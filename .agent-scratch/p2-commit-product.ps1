$ErrorActionPreference = 'Stop'
$wt = 'F:\GitHub\McpServer\.worktrees\sessionlife-p2-contracts-5cb2'
Set-Location -LiteralPath $wt
Write-Output ('HEAD=' + (git rev-parse HEAD))
Write-Output ('BRANCH=' + (git branch --show-current))
git status --short
$paths = @(
    'plugins/core/test-fixtures/pester/SessionLogP2Contracts.Tests.ps1',
    'tests/McpServer.Support.Mcp.Tests/Services/CliBrainSlotStrategyTests.cs'
)
git add -- $paths
$staged = @(git diff --cached --name-only)
Write-Output 'STAGED:'
$staged | ForEach-Object { Write-Output $_ }
if ($staged.Count -ne 2) { throw "Expected 2 staged paths, got $($staged.Count)" }
$msg = Join-Path $env:TEMP 'p2-product-commit.txt'
@(
    'fix(sessionlife): match typed P2 receipts'
    ''
    'Primary stdout is one persisted envelope, not empty.'
    'setSessionTitle binds sessionId and leaves requestId'
    'empty, matching the wrapper contract. The Grok recorder'
    'drops inherited env keys the product already removed.'
) | Set-Content -LiteralPath $msg -Encoding utf8NoBOM
git commit -F $msg
if ($LASTEXITCODE -ne 0) { throw "commit failed $LASTEXITCODE" }
Write-Output ('NEW_HEAD=' + (git rev-parse HEAD))
git status --short
git push -u origin HEAD
if ($LASTEXITCODE -ne 0) { throw "push failed $LASTEXITCODE" }
Write-Output 'PUSH_OK'
