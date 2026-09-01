#Requires -Version 7.0
[CmdletBinding()]
param()
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$wt = 'F:\GitHub\McpServer\.worktrees\triage-plugin-core'
$outDir = 'F:\GitHub\McpServer\docs\receipts\_hv-s2-hgreen'
$out = Join-Path $outDir '03-pester-full.json'
$nunit = Join-Path $outDir '03-pester-full.xml'
$log = Join-Path $outDir '03-pester-full.log'

$identity = Join-Path $wt 'plugins\core\test-fixtures\pester\TriagePluginIdentity.Tests.ps1'
$runtime = Join-Path $wt 'plugins\core\test-fixtures\pester\PluginPowerShellRuntime.Tests.ps1'

Import-Module Pester -MinimumVersion 5.0 -ErrorAction Stop

$config = New-PesterConfiguration
$config.Run.Path = @($identity, $runtime)
$config.Run.PassThru = $true
$config.Run.Exit = $false
$config.Output.Verbosity = 'Normal'
$config.TestResult.Enabled = $true
$config.TestResult.OutputPath = $nunit
$config.TestResult.OutputFormat = 'NUnitXml'

$sw = [System.Diagnostics.Stopwatch]::StartNew()
$result = Invoke-Pester -Configuration $config
$sw.Stop()

$failed = @($result.Failed | ForEach-Object { [ordered]@{ Name = $_.ExpandedName; Result = [string]$_.Result; Error = [string]$_.ErrorRecord } })
$skipped = @($result.Skipped | ForEach-Object { [ordered]@{ Name = $_.ExpandedName } })

$obj = [ordered]@{
    TimestampUtc = [datetime]::UtcNow.ToString('yyyy-MM-ddTHH:mm:ssZ')
    ElapsedSec = [math]::Round($sw.Elapsed.TotalSeconds, 3)
    TotalCount = $result.TotalCount
    PassedCount = $result.PassedCount
    FailedCount = $result.FailedCount
    SkippedCount = $result.SkippedCount
    NotRunCount = $result.NotRunCount
    Failed = $failed
    Skipped = $skipped
    NUnitXml = $nunit
}
$obj | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath $out -Encoding utf8
$summary = "WROTE $out Passed=$($result.PassedCount) Failed=$($result.FailedCount) Skipped=$($result.SkippedCount) Total=$($result.TotalCount) Elapsed=$($obj.ElapsedSec)"
Set-Content -LiteralPath $log -Value $summary -Encoding utf8
Write-Output $summary
