$ErrorActionPreference = 'Stop'
Set-Location -LiteralPath 'F:\GitHub\McpServer\.worktrees\sessionlife-p2-contracts-5cb2'
Write-Output 'DIFFSTAT'
git diff --stat -- 'docs/receipts/sessionlife-completion/20260928-p0-r3/acceptance-manifest.json'
Write-Output 'NAMES'
git diff -U0 -- 'docs/receipts/sessionlife-completion/20260928-p0-r3/acceptance-manifest.json' | Select-String -Pattern 'outcomes\.' | Select-Object -First 8
git add -- 'docs/receipts/sessionlife-completion/20260928-p0-r3/acceptance-manifest.json'
$msg = @"
docs(sessionlife): fix outcome test names

Mapped Pester labels used a doubled dot. Native names use one.
"@
git commit -m $msg
if ($LASTEXITCODE -ne 0) { throw "commit exit $LASTEXITCODE" }
Write-Output 'HEAD'
git rev-parse HEAD
git status --short --branch
