$ErrorActionPreference = 'Stop'
$p = 'F:\GitHub\McpServer\.worktrees\sessionlife-p2-contracts-5cb2\build\SessionLifeUnitGateManifest.cs'
$i = 0
foreach ($line in [System.IO.File]::ReadLines($p)) {
  $i++
  if ($line -match 'Included|Enumerate|git |GetFiles|build\\') {
    Write-Output ("{0}: {1}" -f $i, $line.Trim())
  }
}
