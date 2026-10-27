$ErrorActionPreference = 'Stop'
$log = 'F:\GitHub\McpServer\.worktrees\sessionlife-p2-contracts-5cb2\docs\receipts\hv\p2-unit-legion-20261002T215154Z-gate.log'
$lines = [System.IO.File]::ReadAllLines($log)
Write-Output ("LINES=" + $lines.Length)
$needles = @('Failed', 'error ', 'Exception', 'GATE', 'Passed', 'CheckSession', 'exit', 'PluginIntegration', 'Total')
foreach ($line in $lines) {
  foreach ($n in $needles) {
    if ($line -like "*$n*") {
      if ($line.Length -lt 400) { Write-Output $line }
      else { Write-Output $line.Substring(0, 400) }
      break
    }
  }
}
