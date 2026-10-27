$ErrorActionPreference = 'Stop'
Set-Location -LiteralPath 'F:\GitHub\McpServer\.worktrees\sessionlife-p2-contracts-5cb2'
git add -- `
    'plugins/core/test-fixtures/pester/SessionLogP2Contracts.Tests.ps1' `
    'tests/McpServer.Support.Mcp.Tests/Services/SessionLogServiceTurnContextTests.cs' `
    'tests/McpServer.Support.Mcp.Tests/Services/SessionLogTurnContextValidatorTests.cs'
git commit -m @'
test(sessionlife): cover hook and metadata gaps

Fresh prompt-hook status, hook consumers, and raw no-insert
metadata cases are executed. Rejected mappings are not re-credited.
'@
if ($LASTEXITCODE -ne 0) { throw "test commit failed $LASTEXITCODE" }
git add -- 'docs/receipts/sessionlife-completion/20260928-p0-r3/acceptance-manifest.json'
git commit -m @'
docs(sessionlife): map hook and metadata tests

Bind the nine gaps to the new executed tests. Acceptance stays
not-accepted.
'@
if ($LASTEXITCODE -ne 0) { throw "docs commit failed $LASTEXITCODE" }
git status --short
git log -2 --oneline
git rev-parse HEAD
