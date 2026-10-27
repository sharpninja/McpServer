$ErrorActionPreference = 'Stop'
$wt = 'F:\GitHub\McpServer\.worktrees\sessionlife-p2-contracts-5cb2'
$hits = Get-ChildItem -LiteralPath $wt -Recurse -Filter 'SessionLifeUnitGate*.cs' -File |
  Where-Object { $_.FullName -notmatch '\\(bin|obj)\\' }
foreach ($f in $hits) {
  Write-Output $f.FullName.Substring($wt.Length)
}
