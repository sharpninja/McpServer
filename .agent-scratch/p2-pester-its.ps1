$ErrorActionPreference = 'Stop'
$wt = 'F:\GitHub\McpServer\.worktrees\sessionlife-p2-contracts-5cb2'
$files = @(
  'plugins\core\test-fixtures\pester\SessionLogP2Contracts.Tests.ps1',
  'plugins\core\test-fixtures\pester\SessionLogLifecycleMetadata.Tests.ps1',
  'plugins\core\test-fixtures\pester\SessionLogLifecycle.Tests.ps1'
)
foreach ($rel in $files) {
  Write-Output "===== $rel ====="
  $lines = [System.IO.File]::ReadAllLines((Join-Path $wt $rel))
  for ($i = 0; $i -lt $lines.Length; $i++) {
    if ($lines[$i] -match "^\s*It '") {
      Write-Output ("{0}: {1}" -f ($i + 1), $lines[$i].Trim())
    }
  }
}
