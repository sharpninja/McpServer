$ErrorActionPreference = 'Stop'
$wt = 'F:\GitHub\McpServer\.worktrees\sessionlife-p2-contracts-5cb2'
$needles = @('failsafe not used', 'turn not found', 'retryable false', 'NeverDegraded', 'does not say failsafe')
$roots = @(
  (Join-Path $wt 'plugins\core\test-fixtures\pester'),
  (Join-Path $wt 'tests')
)
foreach ($root in $roots) {
  Get-ChildItem $root -Recurse -Include *.ps1,*.cs -File | ForEach-Object {
    $i = 0
    foreach ($line in [System.IO.File]::ReadLines($_.FullName)) {
      $i++
      foreach ($n in $needles) {
        if ($line.Contains($n)) {
          Write-Output ("{0}:{1}: {2}" -f $_.FullName.Substring($wt.Length), $i, $line.Trim())
        }
      }
    }
  }
}
Write-Output '==== product appendDialog missing-turn block ===='
$src = Join-Path $wt 'plugins\core\lib-ps\repl-invoke.ps1'
$lines = [System.IO.File]::ReadAllLines($src)
for ($i = 2480; $i -le 2600; $i++) {
  Write-Output ("{0}: {1}" -f ($i+1), $lines[$i])
}
