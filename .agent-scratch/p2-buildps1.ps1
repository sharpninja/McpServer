$ErrorActionPreference = 'Stop'
$p = 'F:\GitHub\McpServer\.worktrees\sessionlife-p2-contracts-5cb2\build.ps1'
$i = 0
foreach ($line in [System.IO.File]::ReadLines($p)) {
  $i++
  if ($i -le 120) { Write-Output ("{0}: {1}" -f $i, $line) }
}
