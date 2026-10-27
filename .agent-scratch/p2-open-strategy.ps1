$ErrorActionPreference = 'Stop'
$wt = 'F:\GitHub\McpServer\.worktrees\sessionlife-p2-contracts-5cb2'
$pester = Join-Path $wt 'plugins\core\test-fixtures\pester'
Write-Output '==== It lines mentioning open / strategy / failsafePath ===='
Get-ChildItem $pester -Filter *.ps1 | ForEach-Object {
  $i = 0
  foreach ($line in [System.IO.File]::ReadLines($_.FullName)) {
    $i++
    if ($line -match "It '") {
      if ($line -match 'open|strategy|failsafePath|Open-Plugin') {
        Write-Output ("{0}:{1}: {2}" -f $_.Name, $i, $line.Trim())
      }
    }
  }
}
Write-Output '==== strategy name in receipts ===='
$src = Join-Path $wt 'plugins\core\lib-ps\repl-invoke.ps1'
$i = 0
foreach ($line in [System.IO.File]::ReadLines($src)) {
  $i++
  if ($line -match 'strategy|failsafePath|independent') {
    if ($i -gt 1300 -and $i -lt 1500) {
      Write-Output ("{0}: {1}" -f $i, $line.Trim())
    }
  }
}
