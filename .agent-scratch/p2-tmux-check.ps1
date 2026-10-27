$ErrorActionPreference = 'Continue'
$tmux = Get-Command tmux -ErrorAction SilentlyContinue
if ($tmux) { Write-Output ('TMUX=' + $tmux.Source) } else { Write-Output 'TMUX=MISSING' }
Set-Location 'F:\GitHub\McpServer\.worktrees\sessionlife-p2-contracts-5cb2'
git check-ignore -v 'docs/receipts/hv/p2-unit-legion-sample-gate.log'
Write-Output ('CHECK_IGNORE_EXIT=' + $LASTEXITCODE)
