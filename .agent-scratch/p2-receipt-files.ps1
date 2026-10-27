$ErrorActionPreference = 'Stop'
$wt = 'F:\GitHub\McpServer\.worktrees\sessionlife-p2-contracts-5cb2'
Set-Location -LiteralPath $wt
Write-Output '==== new 223800 and gate log ===='
Get-ChildItem -LiteralPath (Join-Path $wt 'docs\receipts\hv') -File |
  Where-Object { $_.Name -match '20261002T223800Z|p2-unit-legion-20261002T222414Z' } |
  ForEach-Object { Write-Output ($_.Name + ' ' + $_.Length) }
Write-Output '==== ignore check ===='
$candidates = @(
  'docs/receipts/hv/hostile-validator-sessionlife-p2-20261002T223800Z.md',
  'docs/receipts/hv/hostile-validator-sessionlife-p2-20261002T223800Z.json',
  'docs/receipts/hv/20261002T223800Z-sessionlife-p2-contracts-hv.request.jsonl',
  'docs/receipts/hv/20261002T223800Z-sessionlife-p2-contracts-hv.response.jsonl',
  'docs/receipts/hv/p2-unit-legion-20261002T222414Z-gate.log'
)
foreach ($c in $candidates) {
  $ignored = git check-ignore -q -- $c
  Write-Output ($c + ' ignored=' + ($LASTEXITCODE -eq 0) + ' exists=' + (Test-Path -LiteralPath (Join-Path $wt $c)))
}
Write-Output '==== head ===='
git rev-parse HEAD
git status -sb --untracked-files=no
