#Requires -Version 7.0
[CmdletBinding()]
param()
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$wt = 'F:\GitHub\McpServer\.worktrees\triage-plugin-core'
$outDir = 'F:\GitHub\McpServer\docs\receipts\_hv-s2-hgreen'
$out = Join-Path $outDir '03-pester-s2-only.json'
$nunit = Join-Path $outDir '03-pester-s2-only.xml'

$identity = Join-Path $wt 'plugins\core\test-fixtures\pester\TriagePluginIdentity.Tests.ps1'
$runtime = Join-Path $wt 'plugins\core\test-fixtures\pester\PluginPowerShellRuntime.Tests.ps1'

Import-Module Pester -MinimumVersion 5.0 -ErrorAction Stop
$config = New-PesterConfiguration
$config.Run.Path = @($identity, $runtime)
$config.Run.PassThru = $true
$config.Run.Exit = $false
$config.Output.Verbosity = 'Detailed'
$config.Filter.FullName = '*TEST-MCP-STRICTCOUNT-001*','*TEST-MCP-FAILSAFE-001*','*TEST-MCP-SESSIONEND-001*','*TEST-MCP-XAGENT-001*','*TEST-MCP-VERIFYWRAP-001*','*SubmitAsyncTimeout*','*command_timeout*','*updateTurn omitted*','*session-end*','*wrapper template*','*BeginTurn.SubmitTimeoutAfterFailsafe*'
$config.TestResult.Enabled = $true
$config.TestResult.OutputPath = $nunit
$config.TestResult.OutputFormat = 'NUnitXml'

$sw = [System.Diagnostics.Stopwatch]::StartNew()
$result = Invoke-Pester -Configuration $config
$sw.Stop()

$obj = [ordered]@{
    TimestampUtc = [datetime]::UtcNow.ToString('yyyy-MM-ddTHH:mm:ssZ')
    Filter = @($config.Filter.FullName)
    ElapsedSec = [math]::Round($sw.Elapsed.TotalSeconds, 3)
    TotalCount = $result.TotalCount
    PassedCount = $result.PassedCount
    FailedCount = $result.FailedCount
    SkippedCount = $result.SkippedCount
    NotRunCount = $result.NotRunCount
    Failed = @($result.Failed | ForEach-Object { $_.ExpandedName })
    PassedNames = @($result.Passed | ForEach-Object { $_.ExpandedName })
}
$obj | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath $out -Encoding utf8
Write-Output ("WROTE {0} Passed={1} Failed={2} Skipped={3} Total={4} NotRun={5}" -f $out, $result.PassedCount, $result.FailedCount, $result.SkippedCount, $result.TotalCount, $result.NotRunCount)
