$ErrorActionPreference = 'Stop'
$run = 'p2-unit-legion-20261002T220847Z'
$root = "F:\GitHub\McpServer\.worktrees\sessionlife-p2-contracts-5cb2\TestResults\$run"
$log = "F:\GitHub\McpServer\.worktrees\sessionlife-p2-contracts-5cb2\docs\receipts\hv\$run-gate.log"
Write-Output '==== command-results ===='
Get-Content -Raw (Join-Path $root 'command-results.json')
Write-Output '==== gate log tail ===='
$lines = [System.IO.File]::ReadAllLines($log)
Write-Output ("LOG_LINES=" + $lines.Length)
$start = [Math]::Max(0, $lines.Length - 40)
for ($i = $start; $i -lt $lines.Length; $i++) { Write-Output $lines[$i] }
Write-Output '==== props/rsp left ===='
Write-Output ("PROPS=" + (Test-Path 'F:\GitHub\McpServer\.worktrees\sessionlife-p2-contracts-5cb2\build\Directory.Build.props'))
Write-Output ("RSP=" + (Test-Path 'F:\GitHub\McpServer\.worktrees\sessionlife-p2-contracts-5cb2\build\Directory.Build.rsp'))
$bt = Join-Path $root 'logs\build-tests.log'
if (Test-Path $bt) {
  Write-Output '==== build-tests errors ===='
  Select-String -LiteralPath $bt -Pattern 'error |Failed!|MSB3027|Passed!|Total tests' | Select-Object -Last 30 | ForEach-Object { $_.Line.Trim() }
}
