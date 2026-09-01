#Requires -Version 7.0
[CmdletBinding()]
param()
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$wt = 'F:\GitHub\McpServer\.worktrees\triage-plugin-core'
$outDir = 'F:\GitHub\McpServer\docs\receipts\_hv-s2-hgreen'
if (-not (Test-Path -LiteralPath $outDir)) {
    [void][System.IO.Directory]::CreateDirectory($outDir)
}
$out = Join-Path $outDir '03-pester-named.json'
$nunit = Join-Path $outDir '03-pester-named.xml'
$log = Join-Path $outDir '03-pester-named.log'

$identity = Join-Path $wt 'plugins\core\test-fixtures\pester\TriagePluginIdentity.Tests.ps1'
$runtime = Join-Path $wt 'plugins\core\test-fixtures\pester\PluginPowerShellRuntime.Tests.ps1'

Import-Module Pester -MinimumVersion 5.0 -ErrorAction Stop
$config = New-PesterConfiguration
$config.Run.Path = @($identity, $runtime)
$config.Run.PassThru = $true
$config.Run.Exit = $false
$config.Output.Verbosity = 'Detailed'
$config.Filter.FullName = @(
    '*TEST-MCP-STRICTCOUNT-001*',
    '*TEST-MCP-FAILSAFE-001*',
    '*TEST-MCP-SESSIONEND-001*',
    '*TEST-MCP-XAGENT-001*',
    '*TEST-MCP-VERIFYWRAP-001*',
    '*TEST-MCP-TRIAGEPLUGIN-004*'
)
$config.TestResult.Enabled = $true
$config.TestResult.OutputPath = $nunit
$config.TestResult.OutputFormat = 'NUnitXml'

$sw = [System.Diagnostics.Stopwatch]::StartNew()
$result = Invoke-Pester -Configuration $config
$sw.Stop()

$obj = [ordered]@{
    TimestampUtc = [datetime]::UtcNow.ToString('yyyy-MM-ddTHH:mm:ssZ')
    Worktree = $wt
    Paths = @($identity, $runtime)
    IdentityExists = (Test-Path -LiteralPath $identity)
    RuntimeExists = (Test-Path -LiteralPath $runtime)
    Filter = @(
        '*TEST-MCP-STRICTCOUNT-001*',
        '*TEST-MCP-FAILSAFE-001*',
        '*TEST-MCP-SESSIONEND-001*',
        '*TEST-MCP-XAGENT-001*',
        '*TEST-MCP-VERIFYWRAP-001*',
        '*TEST-MCP-TRIAGEPLUGIN-004*'
    )
    ElapsedSec = [math]::Round($sw.Elapsed.TotalSeconds, 3)
    Result = [string]$result.Result
    TotalCount = $result.TotalCount
    PassedCount = $result.PassedCount
    FailedCount = $result.FailedCount
    SkippedCount = $result.SkippedCount
    NotRunCount = $result.NotRunCount
    ExitCodeImplied = $(if ($result.FailedCount -eq 0 -and $result.SkippedCount -eq 0 -and $result.PassedCount -gt 0) { 0 } else { 1 })
    Failed = @($result.Failed | ForEach-Object {
        [ordered]@{
            ExpandedName = $_.ExpandedName
            Name = $_.Name
            Result = [string]$_.Result
            ErrorRecord = @($_.ErrorRecord | ForEach-Object { [string]$_ })
        }
    })
    PassedNames = @($result.Passed | ForEach-Object { $_.ExpandedName })
    SkippedNames = @($result.Skipped | ForEach-Object { $_.ExpandedName })
}
$obj | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath $out -Encoding utf8
$summary = "WROTE $out Result=$($result.Result) Passed=$($result.PassedCount) Failed=$($result.FailedCount) Skipped=$($result.SkippedCount) Total=$($result.TotalCount) NotRun=$($result.NotRunCount) Elapsed=$($obj.ElapsedSec)"
Set-Content -LiteralPath $log -Value $summary -Encoding utf8
Write-Output $summary
