$ErrorActionPreference = 'Stop'
$wt = 'F:\GitHub\McpServer\.worktrees\sessionlife-p2-contracts-5cb2'
Set-Location -LiteralPath $wt
$files = @(
  'docs/receipts/hv/20261002T223800Z-boundary-probes.json',
  'docs/receipts/hv/20261002T223800Z-evidence.jsonl',
  'docs/receipts/hv/20261002T223800Z-final-verification.json',
  'docs/receipts/hv/20261002T223800Z-mapping-audit.json',
  'docs/receipts/hv/20261002T223800Z-native-audit.json',
  'docs/receipts/hv/20261002T223800Z-public-checkpoint.jsonl',
  'docs/receipts/hv/20261002T223800Z-remaining-identity-probes-v2.json',
  'docs/receipts/hv/20261002T223800Z-sessionlife-p2-contracts-hv.request.jsonl',
  'docs/receipts/hv/20261002T223800Z-sessionlife-p2-contracts-hv.response.jsonl',
  'docs/receipts/hv/20261002T223800Z-trace-audit.json',
  'docs/receipts/hv/hostile-validator-sessionlife-p2-20261002T223800Z.json',
  'docs/receipts/hv/hostile-validator-sessionlife-p2-20261002T223800Z.md'
)
foreach ($f in $files) {
  if (-not (Test-Path -LiteralPath $f)) { throw "missing $f" }
}
git add -- $files
$msg = Join-Path $env:USERPROFILE '.grok\p2-commit-hv.txt'
[System.IO.File]::WriteAllText($msg, "docs(hv): record P2 mapping Astra`n`nOne round on gated tip 6e3215ff. DISAGREE, accuracy 99, completeness 77.5.`n")
git commit -F $msg
Write-Output '==== committed ===='
git log -1 --format='%H%n%s'
git status -sb --untracked-files=no
