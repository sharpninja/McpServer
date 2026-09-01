#Requires -Version 7.0
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Continue'
Set-Location -LiteralPath 'F:\GitHub\McpServer'

Write-Output '--- pre-status ---'
Write-Output ("statusLines=" + @(git status --short --untracked-files=all).Count)
Write-Output ("branch=" + (git branch --show-current))
Write-Output ("origin=" + (git remote get-url origin))

git add -A -- .
Write-Output ("STAGED=" + @(git diff --cached --name-only).Count)

git commit -F 'docs/receipts/_hv-refresh-docs-20260821T104955Z/COMMIT_MSG.txt'
$commitExit = if ($null -eq $LASTEXITCODE) { 0 } else { $LASTEXITCODE }
Write-Output ("COMMIT_EXIT=$commitExit")
if ($commitExit -ne 0) { exit $commitExit }

Write-Output ("HEAD=" + (git rev-parse HEAD))
git log -1 --format='%h%n%B'

Write-Output '--- push ---'
git push origin HEAD:develop
$pushExit = if ($null -eq $LASTEXITCODE) { 0 } else { $LASTEXITCODE }
Write-Output ("PUSH_EXIT=$pushExit")
Write-Output '--- post-status ---'
git status --short --untracked-files=all
Write-Output ("POST_STATUS_LINES=" + @(git status --short --untracked-files=all).Count)
exit $pushExit
