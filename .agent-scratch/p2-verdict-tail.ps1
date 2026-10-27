$ErrorActionPreference = 'Stop'
$md = 'F:\GitHub\McpServer\.worktrees\sessionlife-p2-contracts-5cb2\docs\receipts\hv\hostile-validator-sessionlife-p2-20261002T223800Z.md'
$lines = [System.IO.File]::ReadAllLines($md)
Write-Output ("MD_LINES=" + $lines.Length)
$start = [Math]::Max(0, $lines.Length - 40)
for ($i = $start; $i -lt $lines.Length; $i++) { Write-Output $lines[$i] }
