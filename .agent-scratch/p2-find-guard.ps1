$ErrorActionPreference = 'Stop'
$root = 'F:\GitHub\McpServer\.worktrees\sessionlife-p2-contracts-5cb2'
$hits = Get-ChildItem -LiteralPath (Join-Path $root 'src') -Recurse -Filter 'SessionLogSchemaGuard.cs' -File |
  Where-Object { $_.FullName -notmatch '\\(bin|obj)\\' }
foreach ($f in $hits) { Write-Output $f.FullName }
