$ErrorActionPreference = 'Stop'
Set-Location 'F:\GitHub\McpServer\.worktrees\sessionlife-p2-contracts-5cb2'
git log -5 --oneline
git add -- 'docs/receipts/sessionlife-completion/20260928-p0-r3/acceptance-manifest.json' 'plugins/core/test-fixtures/pester/SessionLogLifecycle.Tests.ps1'
git diff --cached --stat
$msg = @"
docs(sessionlife): map executed P2 tests

Link the twelve HV07 gaps to tests that already passed. Keep acceptance not-accepted and proposed names proposed.
"@
$msgPath = Join-Path $env:TEMP 'p2-map-commit.txt'
[System.IO.File]::WriteAllText($msgPath, $msg.Trim() + "`n")
git commit -F $msgPath
git status --short -- 'docs/receipts/sessionlife-completion/20260928-p0-r3/acceptance-manifest.json' 'plugins/core/test-fixtures/pester/SessionLogLifecycle.Tests.ps1'
git rev-parse HEAD
