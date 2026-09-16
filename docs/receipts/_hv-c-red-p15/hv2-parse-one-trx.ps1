#Requires -Version 7.0
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$trxPath = $args[0]
$dest = $args[1]
[xml]$xml = Get-Content -LiteralPath $trxPath
$counters = $xml.TestRun.ResultSummary.Counters
$results = @()
if ($null -ne $xml.TestRun.Results -and $null -ne $xml.TestRun.Results.UnitTestResult) {
    $results = @($xml.TestRun.Results.UnitTestResult)
}
$obj = [ordered]@{
    Path = $trxPath
    Outcome = [string]$xml.TestRun.ResultSummary.outcome
    Executed = if ($null -ne $counters.executed) { [int]$counters.executed } else { $null }
    Passed = if ($null -ne $counters.passed) { [int]$counters.passed } else { $null }
    Failed = if ($null -ne $counters.failed) { [int]$counters.failed } else { $null }
    Total = if ($null -ne $counters.total) { [int]$counters.total } else { $null }
    NotExecuted = if ($null -ne $counters.notExecuted) { [int]$counters.notExecuted } else { $null }
    ResultCount = $results.Count
    PassedNames = @($results | Where-Object { $_.outcome -eq 'Passed' } | ForEach-Object { $_.testName })
    FailedNames = @($results | Where-Object { $_.outcome -eq 'Failed' } | ForEach-Object { $_.testName })
    Other = @($results | Where-Object { $_.outcome -ne 'Passed' -and $_.outcome -ne 'Failed' } | ForEach-Object { $_.testName + ':' + $_.outcome })
}
$json = $obj | ConvertTo-Json -Depth 6
Set-Content -LiteralPath $dest -Value $json -Encoding utf8
Write-Output $json
