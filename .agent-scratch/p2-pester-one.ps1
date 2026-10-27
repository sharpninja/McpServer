$ErrorActionPreference = 'Stop'
Set-Location 'F:\GitHub\McpServer\.worktrees\sessionlife-p2-contracts-5cb2'
$cfg = New-PesterConfiguration
$cfg.Run.Path = 'plugins/core/test-fixtures/pester/SessionLogLifecycle.Tests.ps1'
$cfg.Run.PassThru = $true
$cfg.Run.Exit = $false
$cfg.Filter.FullName = '*degraded 404 queues*'
$cfg.Output.Verbosity = 'Detailed'
$result = Invoke-Pester -Configuration $cfg
Write-Output ("TOTAL=" + $result.TotalCount)
Write-Output ("PASSED=" + $result.PassedCount)
Write-Output ("FAILED=" + $result.FailedCount)
if ($result.FailedCount -gt 0) {
  $result.Failed | ForEach-Object { Write-Output $_.ExpandedName; Write-Output $_.ErrorRecord }
  exit 1
}
exit 0
