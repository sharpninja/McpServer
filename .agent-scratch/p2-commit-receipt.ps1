$ErrorActionPreference = 'Stop'
Set-Location 'F:\GitHub\McpServer\.worktrees\sessionlife-p2-contracts-5cb2'
Write-Output '=== LOG ==='
git log -5 --oneline
Write-Output ('HEAD_BEFORE=' + (git rev-parse HEAD).Trim())
$files = @(
    'docs/receipts/hv/20261002T194331Z-sessionlife-p2-unit-gate-failed.md',
    'docs/receipts/hv/20261002T194331Z-sessionlife-p2-unit-gate-failed.json'
)
git add -- $files
Write-Output '=== CACHED ==='
git diff --cached --stat
git commit -m @"
docs(hv): record P2 unit gate failure

Gate p2-unit-legion-20261002T193045Z on 987cd8f exited 1. Astra was not started.
"@
Write-Output ('COMMIT_EXIT=' + $LASTEXITCODE)
Write-Output ('HEAD_AFTER=' + (git rev-parse HEAD).Trim())
git status --short --branch | Select-Object -First 8
git push -u origin HEAD
Write-Output ('PUSH_EXIT=' + $LASTEXITCODE)
Write-Output ('HEAD_PUSHED=' + (git rev-parse HEAD).Trim())
