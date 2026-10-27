$ErrorActionPreference = 'Stop'
Set-Location -LiteralPath 'F:\GitHub\McpServer\.worktrees\sessionlife-p2-contracts-5cb2'
git push -u origin cursor/sessionlife-p2-contracts-5cb2
Write-Output ("PUSH_EXIT=" + $LASTEXITCODE)
git rev-parse HEAD
git status -sb
