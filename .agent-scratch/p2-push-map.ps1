$ErrorActionPreference = 'Stop'
Set-Location 'F:\GitHub\McpServer\.worktrees\sessionlife-p2-contracts-5cb2'
$branch = 'cursor/sessionlife-p2-contracts-5cb2'
$delays = @(0, 4, 8, 16, 32)
$ok = $false
foreach ($delay in $delays) {
  if ($delay -gt 0) { Start-Sleep -Seconds $delay }
  git push -u origin $branch
  if ($LASTEXITCODE -eq 0) { $ok = $true; break }
  Write-Output ("PUSH_FAIL delayNext after exit " + $LASTEXITCODE)
}
if (-not $ok) { exit 1 }
git rev-parse HEAD
exit 0
