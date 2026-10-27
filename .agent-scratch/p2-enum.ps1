$ErrorActionPreference = 'Stop'
$p = 'F:\GitHub\McpServer\.worktrees\sessionlife-p2-contracts-5cb2\build\SessionLifeUnitGateManifest.cs'
$i = 0
$on = $false
foreach ($line in [System.IO.File]::ReadLines($p)) {
  $i++
  if ($line -match 'EnumeratePolicySources') { $on = $true }
  if ($on) {
    Write-Output ("{0}: {1}" -f $i, $line)
    if ($on -and $i -gt 0 -and $line -match '^\s+private static' -and $line -notmatch 'EnumeratePolicySources') { break }
  }
}
