$ErrorActionPreference = 'Stop'
$logDir = 'F:\GitHub\McpServer\.worktrees\sessionlife-p2-contracts-5cb2\docs\receipts\hv'
Get-ChildItem -LiteralPath $logDir -Filter 'p2-unit-legion-20261002T22*-gate.log' |
  Sort-Object LastWriteTime -Descending |
  Select-Object -First 3 |
  ForEach-Object { Write-Output ($_.Name + ' ' + $_.Length + ' ' + $_.LastWriteTimeUtc.ToString('o')) }
$targets = 'F:\GitHub\McpServer\.worktrees\sessionlife-p2-contracts-5cb2\build\Directory.Build.targets'
Write-Output ("TARGETS=" + (Test-Path -LiteralPath $targets))
$ref = 'F:\GitHub\McpServer\.worktrees\sessionlife-p2-contracts-5cb2\build\obj\Debug\ref\_build.dll'
Write-Output ("REF=" + (Test-Path -LiteralPath $ref))
