$ErrorActionPreference = 'Stop'
$targets = 'C:\Program Files\dotnet\sdk\10.0.401\Microsoft.Common.CurrentVersion.targets'
$lines = [System.IO.File]::ReadAllLines($targets)
for ($i = 0; $i -lt $lines.Length; $i++) {
  if ($lines[$i] -match 'IncrementalClean') {
    Write-Output ("{0}: {1}" -f ($i + 1), $lines[$i].Trim())
  }
}
Write-Output '==== nuke log tail ===='
$nlog = 'F:\GitHub\McpServer\.worktrees\sessionlife-p2-contracts-5cb2\TestResults\p2-unit-legion-20261002T220847Z\logs\nuke-test.log'
$nlines = [System.IO.File]::ReadAllLines($nlog)
Write-Output ("NUKE_LOG_LINES=" + $nlines.Length)
$start = [Math]::Max(0, $nlines.Length - 50)
for ($i = $start; $i -lt $nlines.Length; $i++) { Write-Output $nlines[$i] }
