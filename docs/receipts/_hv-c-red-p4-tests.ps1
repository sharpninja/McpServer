#Requires -Version 7.0
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Continue'

$out = 'F:\GitHub\McpServer\docs\receipts\_hv-c-red-p4'
New-Item -ItemType Directory -Force -Path $out | Out-Null
Set-Location -LiteralPath 'F:\GitHub\McpServer'

function Get-TrxSummary {
    param([string]$TrxPath)
    if (-not (Test-Path -LiteralPath $TrxPath)) {
        return [ordered]@{ exists = $false; path = $TrxPath }
    }
    [xml]$x = Get-Content -LiteralPath $TrxPath
    $c = $x.TestRun.ResultSummary.Counters
    $outcomes = @()
    foreach ($u in @($x.TestRun.Results.UnitTestResult)) {
        $outcomes += [ordered]@{
            name = [string]$u.testName
            outcome = [string]$u.outcome
            duration = [string]$u.duration
            output = [string]$u.Output.ErrorInfo.Message
        }
    }
    return [ordered]@{
        exists = $true
        path = $TrxPath
        outcome = [string]$x.TestRun.ResultSummary.outcome
        total = [string]$c.total
        executed = [string]$c.executed
        passed = [string]$c.passed
        failed = [string]$c.failed
        skipped = $(if ($null -ne $c.skipped) { [string]$c.skipped } else { 'ATTR_MISSING' })
        notExecuted = $(if ($null -ne $c.notExecuted) { [string]$c.notExecuted } else { 'ATTR_MISSING' })
        unitOutcomes = $outcomes
    }
}

$filterLog = Join-Path $out 'dotnet-catalog-filter.log'
$filterErr = Join-Path $out 'dotnet-catalog-filter.err.log'
$allLog = Join-Path $out 'dotnet-pluginintegration-all.log'
$allErr = Join-Path $out 'dotnet-pluginintegration-all.err.log'

Write-Output 'START_CATALOG_FILTER'
$filterArgs = @(
    'test'
    'tests/McpServer.PluginIntegration.Tests'
    '-c'
    'Debug'
    '--filter'
    'FullyQualifiedName~PluginSessionLogCatalogTests'
    '--logger'
    'trx;LogFileName=catalog-filter.trx'
    '--results-directory'
    $out
    '--nologo'
)
$filterProc = Start-Process -FilePath 'dotnet' -ArgumentList $filterArgs -Wait -PassThru -NoNewWindow -RedirectStandardOutput $filterLog -RedirectStandardError $filterErr
$filterExit = $filterProc.ExitCode
Write-Output ("CATALOG_FILTER_EXIT=$filterExit")

Write-Output 'START_PLUGININT_ALL'
$allArgs = @(
    'test'
    'tests/McpServer.PluginIntegration.Tests'
    '-c'
    'Debug'
    '--logger'
    'trx;LogFileName=pluginintegration-all.trx'
    '--results-directory'
    $out
    '--nologo'
)
$allProc = Start-Process -FilePath 'dotnet' -ArgumentList $allArgs -Wait -PassThru -NoNewWindow -RedirectStandardOutput $allLog -RedirectStandardError $allErr
$allExit = $allProc.ExitCode
Write-Output ("PLUGININT_ALL_EXIT=$allExit")

$filterSummary = Get-TrxSummary -TrxPath (Join-Path $out 'catalog-filter.trx')
$allSummary = Get-TrxSummary -TrxPath (Join-Path $out 'pluginintegration-all.trx')
[ordered]@{
    TimestampUtc = [DateTime]::UtcNow.ToString('o')
    CatalogFilterExitCode = $filterExit
    PluginAllExitCode = $allExit
    CatalogFilterTrx = $filterSummary
    PluginAllTrx = $allSummary
} | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath (Join-Path $out 'trx-summary.json') -Encoding utf8
Write-Output 'TESTS_DONE'
