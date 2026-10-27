$ErrorActionPreference = 'Stop'
$wt = 'F:\GitHub\McpServer\.worktrees\sessionlife-p2-contracts-5cb2'
$src = Join-Path $wt 'plugins\core\lib-ps\repl-invoke.ps1'
$i = 0
$print = $false
foreach ($line in [System.IO.File]::ReadLines($src)) {
  $i++
  if ($line -match 'function Write-ReplFailsafe') { $print = $true }
  if ($print) {
    Write-Output ("{0}: {1}" -f $i, $line)
    if ($i -gt 0 -and $line -match '^function ' -and $line -notmatch 'function Write-ReplFailsafe') { break }
  }
}
Write-Output '==== requirement docs mentioning FR-MCP-REPL-009 ===='
Get-ChildItem (Join-Path $wt 'docs') -Recurse -Include *.md,*.json -File -ErrorAction SilentlyContinue |
  Where-Object { $_.FullName -notmatch '\\receipts\\hv\\|node_modules' } |
  Select-Object -First 1 | Out-Null
$rg = Get-Command rg -ErrorAction SilentlyContinue
if ($rg) {
  & rg -n --glob '!docs/receipts/hv/**' -g '!**/node_modules/**' 'FR-MCP-REPL-009' $wt | Select-Object -First 40
} else {
  Write-Output 'no rg'
}
