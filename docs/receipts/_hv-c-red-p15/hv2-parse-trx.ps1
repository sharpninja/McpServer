#Requires -Version 7.0
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$out = 'F:\GitHub\McpServer\docs\receipts\_hv-c-red-p15\hv2'
$trxDir = Join-Path $out 'trx-p15'
$log = Join-Path $out 'dotnet-p15-filter.log'
$listLog = Join-Path $out 'dotnet-list-tests.log'

function Get-ConsoleCounts([string]$path) {
    $obj = [ordered]@{ Exists = (Test-Path -LiteralPath $path); Passed = $null; Failed = $null; Skipped = $null; Total = $null; Line = $null }
    if (-not $obj.Exists) { return $obj }
    $text = Get-Content -LiteralPath $path -Raw
    $m = [regex]::Match($text, 'Failed!\s+- Failed:\s+(\d+),\s+Passed:\s+(\d+),\s+Skipped:\s+(\d+),\s+Total:\s+(\d+)')
    if (-not $m.Success) {
        $m = [regex]::Match($text, 'Passed!\s+- Failed:\s+(\d+),\s+Passed:\s+(\d+),\s+Skipped:\s+(\d+),\s+Total:\s+(\d+)')
    }
    if ($m.Success) {
        $obj.Failed = [int]$m.Groups[1].Value
        $obj.Passed = [int]$m.Groups[2].Value
        $obj.Skipped = [int]$m.Groups[3].Value
        $obj.Total = [int]$m.Groups[4].Value
        $obj.Line = $m.Value
    }
    $obj.HasP16 = ($text -match 'AiTheory_')
    $obj.FailNames = @([regex]::Matches($text, 'Failed McpServer\.PluginIntegration\.Tests\.PluginSessionLogWorkflowAdapterTests\.(Theory_Agent_[A-Za-z0-9_]+)\(hostKind: ([A-Za-z0-9]+)\)') | ForEach-Object { $_.Groups[1].Value + '/' + $_.Groups[2].Value } | Select-Object -Unique)
    $obj.PassNames = @([regex]::Matches($text, 'Passed McpServer\.PluginIntegration\.Tests\.PluginSessionLogWorkflowAdapterTests\.(Theory_Agent_[A-Za-z0-9_]+)\(hostKind: ([A-Za-z0-9]+)\)') | ForEach-Object { $_.Groups[1].Value + '/' + $_.Groups[2].Value } | Select-Object -Unique)
    return $obj
}

function Get-ListCounts([string]$path) {
    $obj = [ordered]@{ Exists = (Test-Path -LiteralPath $path); TotalListed = 0; P15Success = 0; P15FailedSubmit = 0; P15Retry = 0; P16 = 0; SkipListed = 0 }
    if (-not $obj.Exists) { return $obj }
    $lines = Get-Content -LiteralPath $path
    $obj.TotalListed = @($lines | Where-Object { $_ -match 'McpServer\.PluginIntegration\.Tests\.' }).Count
    $obj.P15Success = @($lines | Where-Object { $_ -match 'Theory_Agent_Success_NoPendingFailsafe' }).Count
    $obj.P15FailedSubmit = @($lines | Where-Object { $_ -match 'Theory_Agent_FailedSubmit_RetainsRootIdPending' }).Count
    $obj.P15Retry = @($lines | Where-Object { $_ -match 'Theory_Agent_RetrySuccess_DeletesOnlyMatchingPending' }).Count
    $obj.P16 = @($lines | Where-Object { $_ -match 'AiTheory_' }).Count
    $obj.SkipListed = @($lines | Where-Object { $_ -match '\[SKIP\]|skipped' }).Count
    return $obj
}

function Get-TrxSummary([string]$dir) {
    $obj = [ordered]@{ TrxExists = $false; Path = $null; Executed = $null; Passed = $null; Failed = $null; NotExecuted = $null; OutcomeNames = @() }
    if (-not (Test-Path -LiteralPath $dir)) { return $obj }
    $trx = Get-ChildItem -LiteralPath $dir -Filter *.trx -Recurse | Sort-Object LastWriteTimeUtc -Descending | Select-Object -First 1
    if ($null -eq $trx) { return $obj }
    $obj.TrxExists = $true
    $obj.Path = $trx.FullName
    $obj.LastWriteTimeUtc = $trx.LastWriteTimeUtc.ToString('o')
    [xml]$xml = Get-Content -LiteralPath $trx.FullName
    $counters = $xml.TestRun.ResultSummary.Counters
    if ($null -ne $counters) {
        $obj.Executed = [int]$counters.executed
        $obj.Passed = [int]$counters.passed
        $obj.Failed = [int]$counters.failed
        if ($null -ne $counters.notExecuted) { $obj.NotExecuted = [int]$counters.notExecuted } else { $obj.NotExecuted = $null }
        if ($null -ne $counters.total) { $obj.Total = [int]$counters.total }
    }
    $obj.Outcome = [string]$xml.TestRun.ResultSummary.outcome
    $units = @($xml.TestRun.TestDefinitions.UnitTest)
    $results = @($xml.TestRun.Results.UnitTestResult)
    $obj.UnitCount = $units.Count
    $obj.ResultCount = $results.Count
    $obj.PassedNames = @($results | Where-Object { $_.outcome -eq 'Passed' } | ForEach-Object { $_.testName })
    $obj.FailedNames = @($results | Where-Object { $_.outcome -eq 'Failed' } | ForEach-Object { $_.testName })
    $obj.SkippedNames = @($results | Where-Object { $_.outcome -eq 'NotExecuted' -or $_.outcome -eq 'Skipped' } | ForEach-Object { $_.testName })
    $obj.P15NameCount = @($results | Where-Object { $_.testName -match 'Success_NoPendingFailsafe|FailedSubmit_RetainsRootIdPending|RetrySuccess_DeletesOnlyMatchingPending' }).Count
    $obj.P16NameCount = @($results | Where-Object { $_.testName -match 'AiTheory_' }).Count
    return $obj
}

$summary = [ordered]@{
    TimestampUtc = [DateTime]::UtcNow.ToString('o')
    Console = Get-ConsoleCounts $log
    List = Get-ListCounts $listLog
    Trx = Get-TrxSummary $trxDir
    ListExit = $null
    FilterExit = $null
}
$listExitPath = Join-Path $out 'dotnet-list-tests-exit.json'
$filterExitPath = Join-Path $out 'dotnet-p15-filter-exit.json'
if (Test-Path -LiteralPath $listExitPath) {
    $summary.ListExit = Get-Content -LiteralPath $listExitPath -Raw | ConvertFrom-Json
}
if (Test-Path -LiteralPath $filterExitPath) {
    $summary.FilterExit = Get-Content -LiteralPath $filterExitPath -Raw | ConvertFrom-Json
}
$json = $summary | ConvertTo-Json -Depth 8
Set-Content -LiteralPath (Join-Path $out 'p15-trx-summary.json') -Value $json -Encoding utf8
Write-Output $json
