$ErrorActionPreference = 'Stop'
Set-Location -LiteralPath 'F:\GitHub\McpServer\.worktrees\sessionlife-p2-contracts-5cb2'
$files = @(
    'docs/receipts/hv/hostile-validator-sessionlife-p2-20261003T011854Z.md',
    'docs/receipts/hv/hostile-validator-sessionlife-p2-20261003T011854Z.json',
    'docs/receipts/hv/20261003T011854Z-sessionlife-p2-contracts-hv.request.jsonl',
    'docs/receipts/hv/20261003T011854Z-sessionlife-p2-contracts-hv.response.jsonl',
    'docs/receipts/hv/20261003T011854Z-audit.json',
    'docs/receipts/hv/20261003T011854Z-boundary-probes.json',
    'docs/receipts/hv/20261003T011854Z-final-state.json',
    'docs/receipts/hv/20261003T011854Z-fresh-fixture-evidence.json',
    'docs/receipts/hv/20261003T011854Z-fresh-lineage.json',
    'docs/receipts/hv/20261003T011854Z-fresh-results.xml',
    'docs/receipts/hv/20261003T011854Z-fresh-summary.json',
    'docs/receipts/hv/20261003T011854Z-live-store-observations.json',
    'docs/receipts/hv/20261003T011854Z-pr.json',
    'docs/receipts/hv/20261003T011854Z-public-checkpoint.jsonl',
    'docs/receipts/hv/20261003T011854Z-receipt-verification.json',
    'docs/receipts/hv/20261003T011854Z-remaining-identity-probes-v2.json',
    'docs/receipts/hv/20261003T011854Z-trace-audit.json'
)
git add -- $files
$msg = @"
docs(hv): record P2 gap Astra

One review of the gated tip. Accuracy 99, completeness 100.
"@
git commit -m $msg
if ($LASTEXITCODE -ne 0) { throw "commit exit $LASTEXITCODE" }
Write-Output 'HEAD'
git rev-parse HEAD
git show --stat --oneline -1
