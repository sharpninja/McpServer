#Requires -Version 7.0
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Continue'

$out = 'F:\GitHub\McpServer\docs\receipts\_hv-c-green-p1-p3'
New-Item -ItemType Directory -Force -Path $out | Out-Null
Set-Location -LiteralPath 'F:\GitHub\McpServer'

function Get-TrxSummary {
    param([string]$TrxPath)
    if (-not (Test-Path -LiteralPath $TrxPath)) {
        return [ordered]@{ exists = $false }
    }
    [xml]$x = Get-Content -LiteralPath $TrxPath
    $c = $x.TestRun.ResultSummary.Counters
    return [ordered]@{
        exists = $true
        outcome = [string]$x.TestRun.ResultSummary.outcome
        total = [string]$c.total
        executed = [string]$c.executed
        passed = [string]$c.passed
        failed = [string]$c.failed
        skipped = [string]$c.skipped
        notExecuted = [string]$c.notExecuted
        inconclusive = [string]$c.inconclusive
    }
}

$buildTrx = Join-Path $out 'build-pluginsessionlog.trx'
$pluginTrx = Join-Path $out 'pluginintegration.trx'
$buildLog = Join-Path $out 'dotnet-build-tests.log'
$pluginLog = Join-Path $out 'dotnet-pluginintegration-tests.log'

Write-Output 'START_BUILD_TESTS'
$buildArgs = @(
    'test'
    'tests/Build.Tests'
    '-c'
    'Debug'
    '--filter'
    'FullyQualifiedName~PluginSessionLogIntegrationTargetTests'
    '--logger'
    "trx;LogFileName=build-pluginsessionlog.trx"
    '--results-directory'
    $out
    '--nologo'
)
$buildProc = Start-Process -FilePath 'dotnet' -ArgumentList $buildArgs -Wait -PassThru -NoNewWindow -RedirectStandardOutput $buildLog -RedirectStandardError (Join-Path $out 'dotnet-build-tests.err.log')
$buildExit = $buildProc.ExitCode
Write-Output ("BUILD_TESTS_EXIT=$buildExit")

Write-Output 'START_PLUGININT_TESTS'
$pluginArgs = @(
    'test'
    'tests/McpServer.PluginIntegration.Tests'
    '-c'
    'Debug'
    '--logger'
    "trx;LogFileName=pluginintegration.trx"
    '--results-directory'
    $out
    '--nologo'
)
$pluginProc = Start-Process -FilePath 'dotnet' -ArgumentList $pluginArgs -Wait -PassThru -NoNewWindow -RedirectStandardOutput $pluginLog -RedirectStandardError (Join-Path $out 'dotnet-pluginintegration-tests.err.log')
$pluginExit = $pluginProc.ExitCode
Write-Output ("PLUGININT_TESTS_EXIT=$pluginExit")

$buildSummary = Get-TrxSummary -TrxPath $buildTrx
$pluginSummary = Get-TrxSummary -TrxPath $pluginTrx
$summary = [ordered]@{
    TimestampUtc = [DateTime]::UtcNow.ToString('o')
    BuildExitCode = $buildExit
    PluginExitCode = $pluginExit
    BuildTrx = $buildSummary
    PluginTrx = $pluginSummary
}
$summary | ConvertTo-Json -Depth 6 | Set-Content -LiteralPath (Join-Path $out 'trx-summary.json') -Encoding utf8
Write-Output ('TRX_SUMMARY=' + ($summary | ConvertTo-Json -Compress))
Write-Output 'TESTS_DONE'
