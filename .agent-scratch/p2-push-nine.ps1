$ErrorActionPreference = 'Stop'
Set-Location -LiteralPath 'F:\GitHub\McpServer\.worktrees\sessionlife-p2-contracts-5cb2'
git push -u origin cursor/sessionlife-p2-contracts-5cb2
if ($LASTEXITCODE -ne 0) { throw "push failed $LASTEXITCODE" }
Write-Output 'PUSH_OK'
