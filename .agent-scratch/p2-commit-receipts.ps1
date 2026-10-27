$ErrorActionPreference = 'Stop'
$wt = 'F:\GitHub\McpServer\.worktrees\sessionlife-p2-contracts-5cb2'
Set-Location -LiteralPath $wt
Write-Output 'TRACKED_113858:'
git ls-files 'docs/receipts/hv/*20261001T113858Z*' 'docs/receipts/hv/hostile-validator-sessionlife-p2-20261001T113858Z.*'
Write-Output 'HEAD_BEFORE:'
git rev-parse --short HEAD
$files = @(
    'docs/receipts/hv/hostile-validator-sessionlife-p2-20261002T203600Z.md',
    'docs/receipts/hv/hostile-validator-sessionlife-p2-20261002T203600Z.json',
    'docs/receipts/hv/20261002T203600Z-sessionlife-p2-contracts-hv.request.jsonl',
    'docs/receipts/hv/20261002T203600Z-sessionlife-p2-contracts-hv.response.jsonl',
    'docs/receipts/hv/20261002T203600Z-boundary-probes.json',
    'docs/receipts/hv/20261002T203600Z-delivery-verification.json',
    'docs/receipts/hv/20261002T203600Z-evidence.jsonl',
    'docs/receipts/hv/20261002T203600Z-layout-environment.json',
    'docs/receipts/hv/20261002T203600Z-live-requirements.json',
    'docs/receipts/hv/20261002T203600Z-live-todos.json',
    'docs/receipts/hv/20261002T203600Z-native-audit-recorded-tools.json',
    'docs/receipts/hv/20261002T203600Z-native-audit.json',
    'docs/receipts/hv/20261002T203600Z-pr72.json',
    'docs/receipts/hv/20261002T203600Z-remaining-identity-probes-v2.json',
    'docs/receipts/hv/20261002T203600Z-trace-audit.json',
    'docs/receipts/hv/20261002T203600Z-verification.json'
)
git add -- $files
$staged = @(git diff --cached --name-only)
Write-Output 'STAGED:'
$staged | ForEach-Object { Write-Output $_ }
if ($staged.Count -ne $files.Count) { throw "staged $($staged.Count) expected $($files.Count)" }
$msg = Join-Path $env:TEMP 'p2-receipt-commit.txt'
@(
    'docs(hv): record P2 Astra round'
    ''
    'Gate p2-unit-legion-20261002T202158Z on e755378c'
    'accepted. Astra DISAGREE, accuracy 99, completeness 70.'
    'No merge and no second round.'
) | Set-Content -LiteralPath $msg -Encoding utf8NoBOM
git commit -F $msg
if ($LASTEXITCODE -ne 0) { throw "commit failed $LASTEXITCODE" }
Write-Output ('NEW_HEAD=' + (git rev-parse HEAD))
git push origin HEAD
if ($LASTEXITCODE -ne 0) { throw "push failed $LASTEXITCODE" }
Write-Output 'PUSH_OK'
