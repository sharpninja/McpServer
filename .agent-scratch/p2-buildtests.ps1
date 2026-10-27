$ErrorActionPreference = 'Stop'
$root = 'F:\GitHub\McpServer\.worktrees\sessionlife-p2-contracts-5cb2\TestResults\p2-unit-legion-20261002T215154Z'
Write-Output '==== tree top ===='
Get-ChildItem $root -Recurse -File | Select-Object -First 40 | ForEach-Object { Write-Output $_.FullName.Substring($root.Length) }
$cmd = Join-Path $root 'command-results.json'
if (Test-Path $cmd) {
  Write-Output '==== command-results ===='
  Get-Content -Raw $cmd
}
$logs = Get-ChildItem $root -Recurse -Filter '*.log' | Where-Object { $_.Name -match 'build' }
foreach ($log in $logs) {
  Write-Output ("==== LOG " + $log.Name + " len=" + $log.Length)
}
