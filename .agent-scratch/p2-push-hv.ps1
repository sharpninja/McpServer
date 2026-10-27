$ErrorActionPreference = 'Stop'
Set-Location 'F:\GitHub\McpServer\.worktrees\sessionlife-p2-contracts-5cb2'
git push -u origin cursor/sessionlife-p2-contracts-5cb2
Write-Output ("PUSH_EXIT=" + $LASTEXITCODE)
gh pr view 72 --repo sharpninja/McpServer --json state,isDraft,mergedAt,headRefOid,baseRefName,url,title
