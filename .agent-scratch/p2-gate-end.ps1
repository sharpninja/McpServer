$ErrorActionPreference = 'Stop'
$log = 'F:\GitHub\McpServer\.worktrees\sessionlife-p2-contracts-5cb2\docs\receipts\hv\p2-unit-legion-20261002T215154Z-gate.log'
$lines = [System.IO.File]::ReadAllLines($log)
Write-Output ("LINES=" + $lines.Length)
$start = [Math]::Max(0, $lines.Length - 40)
for ($i = $start; $i -lt $lines.Length; $i++) {
  $line = $lines[$i]
  if ($line.Length -gt 300) { $line = $line.Substring(0, 300) }
  Write-Output ("{0}: {1}" -f ($i+1), $line)
}
