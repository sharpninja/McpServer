$ErrorActionPreference = 'Stop'
Set-Location -LiteralPath 'F:\GitHub\McpServer\.worktrees\sessionlife-p2-contracts-5cb2'
Write-Output 'BRANCH'
git branch --show-current
Write-Output 'STATUS'
git status --short
git add -- 'plugins/core/test-fixtures/pester/SessionLogP2Contracts.Tests.ps1'
$msg = @"
test(sessionlife): assert cache and 404 gaps

Cache checks now cover identity, status, build, and audit
fields. Empty query omits queryText. Stop checks method,
exit, and status. Dialog 404 keeps one recovery submit
and one session_dialog envelope.
"@
git commit -m $msg
if ($LASTEXITCODE -ne 0) { throw "commit exit $LASTEXITCODE" }
Write-Output 'HEAD'
git rev-parse HEAD
Write-Output 'SHOW'
git show --stat --oneline -1
