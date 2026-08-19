$ErrorActionPreference = 'Stop'
$log = 'F:\GitHub\McpServer\docs\receipts\_hv-233800Z\pester.log'
Import-Module Pester -MinimumVersion 5.0 -ErrorAction Stop
$cfg = New-PesterConfiguration
$cfg.Run.Path = 'F:\GitHub\McpServer\plugins\core\test-fixtures\pester\TriagePluginIdentity.Tests.ps1'
$cfg.Run.PassThru = $true
$cfg.Output.Verbosity = 'Detailed'
$result = Invoke-Pester -Configuration $cfg
$result | Out-String | Set-Content -LiteralPath $log -Encoding utf8
Write-Output ('Passed=' + $result.PassedCount)
Write-Output ('Failed=' + $result.FailedCount)
Write-Output ('Skipped=' + $result.SkippedCount)
Write-Output ('NotRun=' + $result.NotRunCount)
Write-Output ('Total=' + $result.TotalCount)
if ($result.FailedCount -ne 0) { exit 1 }
exit 0
