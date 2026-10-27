$ErrorActionPreference = 'Stop'
$wt = 'F:\GitHub\McpServer\.worktrees\sessionlife-p2-contracts-5cb2'
$files = @(
  (Join-Path $wt 'build\Build.SessionLifeUnitGate.cs'),
  (Join-Path $wt 'tools\validation\Invoke-SessionLifeUnitGate.ps1')
)
foreach ($f in $files) {
  Write-Output ("==== " + $f)
  $i = 0
  foreach ($line in [System.IO.File]::ReadLines($f)) {
    $i++
    if ($line -match 'criterionSpecific|acceptance-manifest|ExistingTest|trace') {
      Write-Output ("{0}: {1}" -f $i, $line.Trim())
    }
  }
}
