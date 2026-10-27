$ErrorActionPreference = 'Continue'
$log = 'F:\GitHub\McpServer\.worktrees\sessionlife-p2-contracts-5cb2\docs\receipts\hv\p2-unit-legion-20261002T193045Z-gate.log'
Write-Output '=== FAIL LINES ==='
Select-String -LiteralPath $log -Pattern '\[-\]|Failed:|Tests Failed|[-] ' |
    ForEach-Object { $_.Line.Trim() }
Write-Output '=== RESULTS DIR ==='
$root = 'F:\GitHub\McpServer\.worktrees\sessionlife-p2-contracts-5cb2\TestResults\p2-unit-legion-20261002T193045Z'
if (Test-Path $root) {
    Get-ChildItem -LiteralPath $root -Recurse -File | Select-Object -First 30 | ForEach-Object { $_.FullName.Replace($root, '') + ' ' + $_.Length }
} else {
    Write-Output 'RESULTS_MISSING'
}
