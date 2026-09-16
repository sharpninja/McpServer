$ErrorActionPreference = 'Continue'
$out = 'F:\GitHub\McpServer\docs\receipts\_hv-b5-20260821T224147Z'
Set-Location 'F:\GitHub\McpServer'
$cfg = New-PesterConfiguration
$cfg.Run.Path = 'F:\GitHub\McpServer\plugins\core\test-fixtures\pester\PluginPowerShellRuntime.Tests.ps1'
$cfg.Output.Verbosity = 'Normal'
$cfg.TestResult.Enabled = $true
$cfg.TestResult.OutputPath = Join-Path $out 'pester-full.xml'
$cfg.TestResult.OutputFormat = 'NUnitXml'
$cfg.Run.Exit = $true
Invoke-Pester -Configuration $cfg
Write-Output ('PESTER_FULL_EXIT=' + $LASTEXITCODE)
