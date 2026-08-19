$ErrorActionPreference = 'Stop'
$outDir = 'F:\GitHub\McpServer\docs\receipts\_hv-20260819T013000Z'
$pesterFile = 'F:\GitHub\McpServer\plugins\core\test-fixtures\pester\TriagePluginIdentity.Tests.ps1'
$cfg = New-PesterConfiguration
$cfg.Run.Path = $pesterFile
$cfg.Run.PassThru = $true
$cfg.Output.Verbosity = 'Detailed'
$r = Invoke-Pester -Configuration $cfg
$summary = [pscustomobject]@{
  Discovered = $r.TotalCount
  Passed = $r.PassedCount
  Failed = $r.FailedCount
  Skipped = $r.SkippedCount
  NotRun = $r.NotRunCount
}
$summary | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $outDir 'pester-summary.json') -Encoding utf8
Write-Output ('PESTER_DISCOVERED=' + $r.TotalCount)
Write-Output ('PESTER_PASSED=' + $r.PassedCount)
Write-Output ('PESTER_FAILED=' + $r.FailedCount)
Write-Output ('PESTER_SKIPPED=' + $r.SkippedCount)
Write-Output ('PESTER_NOTRUN=' + $r.NotRunCount)
if ($r.FailedCount -ne 0 -or $r.SkippedCount -ne 0) { exit 1 }
exit 0
