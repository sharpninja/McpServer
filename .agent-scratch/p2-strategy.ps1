$ErrorActionPreference = 'Stop'
$wt = 'F:\GitHub\McpServer\.worktrees\sessionlife-p2-contracts-5cb2'
$src = Join-Path $wt 'plugins\core\lib-ps\repl-invoke.ps1'
$i = 0
foreach ($line in [System.IO.File]::ReadLines($src)) {
  $i++
  if ($line -match 'persistenceStrategy|filesystem-failsafe|independent') {
    Write-Output ("{0}: {1}" -f $i, $line.Trim())
  }
}
Write-Output '==== tests mentioning persistenceStrategy ===='
Get-ChildItem (Join-Path $wt 'plugins\core\test-fixtures\pester') -Filter *.ps1 | ForEach-Object {
  $n = 0
  foreach ($line in [System.IO.File]::ReadLines($_.FullName)) {
    $n++
    if ($line -match 'persistenceStrategy|filesystem-failsafe') {
      Write-Output ("{0}:{1}: {2}" -f $_.Name, $n, $line.Trim())
    }
  }
}
