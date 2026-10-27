$ErrorActionPreference = 'Stop'
$p = 'F:\GitHub\McpServer\.worktrees\sessionlife-p2-contracts-5cb2\plugins\core\test-fixtures\pester\PluginPowerShellRuntime.Tests.ps1'
$i = 0
$on = $false
foreach ($line in [System.IO.File]::ReadLines($p)) {
  $i++
  if ($line -match "appendDialog uses AppendDialogAsync") { $on = $true }
  if ($on) {
    Write-Output ("{0}: {1}" -f $i, $line)
    if ($i -gt 0 -and $line -match '^\s*It ' -and $line -notmatch 'appendDialog uses AppendDialogAsync') { break }
  }
}
