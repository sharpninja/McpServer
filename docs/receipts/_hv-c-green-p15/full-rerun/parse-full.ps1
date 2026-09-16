#Requires -Version 7.0
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Continue'
$out = 'F:\GitHub\McpServer\docs\receipts\_hv-c-green-p15\full-rerun'
$root = 'F:\GitHub\McpServer\docs\receipts\_hv-c-green-p15'

function Get-Attr {
    param($node, [string]$name)
    if ($null -eq $node) { return $null }
    $a = $node.Attributes[$name]
    if ($null -eq $a) { return $null }
    return [string]$a.Value
}

$trxPath = Join-Path $out 'trx-all-hv\pluginint-all.trx'
$dest = Join-Path $root 'full-trx-summary.json'
if (-not (Test-Path -LiteralPath $trxPath)) {
    [ordered]@{ exists = $false; path = $trxPath } | ConvertTo-Json | Set-Content -LiteralPath $dest -Encoding utf8
    Write-Output 'TRX_MISSING'
    exit 2
}
[xml]$x = Get-Content -LiteralPath $trxPath
$ns = New-Object System.Xml.XmlNamespaceManager($x.NameTable)
$ns.AddNamespace('t', 'http://microsoft.com/schemas/VisualStudio/TeamTest/2010')
$summary = $x.SelectSingleNode('//t:ResultSummary', $ns)
$c = $x.SelectSingleNode('//t:ResultSummary/t:Counters', $ns)
$results = $x.SelectNodes('//t:UnitTestResult', $ns)
$outcomes = @()
foreach ($u in @($results)) {
    $msgNode = $u.SelectSingleNode('t:Output/t:ErrorInfo/t:Message', $ns)
    $outcomes += [ordered]@{
        name = Get-Attr $u 'testName'
        outcome = Get-Attr $u 'outcome'
        duration = Get-Attr $u 'duration'
        message = $(if ($msgNode) { [string]$msgNode.InnerText } else { $null })
    }
}
$p15 = @($outcomes | Where-Object { $_.name -match 'Success_NoPendingFailsafe|FailedSubmit_RetainsRootIdPending|RetrySuccess_DeletesOnlyMatchingPending' })
$p16 = @($outcomes | Where-Object { $_.name -match 'AiTheory_' })
$skipped = @($outcomes | Where-Object { $_.outcome -in @('NotExecuted','Skipped','Inconclusive') })
$failed = @($outcomes | Where-Object { $_.outcome -eq 'Failed' })
$passed = @($outcomes | Where-Object { $_.outcome -eq 'Passed' })
$obj = [ordered]@{
    exists = $true
    path = $trxPath
    lastWriteTimeUtc = (Get-Item -LiteralPath $trxPath).LastWriteTimeUtc.ToString('o')
    length = (Get-Item -LiteralPath $trxPath).Length
    outcome = Get-Attr $summary 'outcome'
    total = Get-Attr $c 'total'
    executed = Get-Attr $c 'executed'
    passed = Get-Attr $c 'passed'
    failed = Get-Attr $c 'failed'
    skipped = Get-Attr $c 'skipped'
    notExecuted = Get-Attr $c 'notExecuted'
    inconclusive = Get-Attr $c 'inconclusive'
    unitCount = @($results).Count
    failedNames = @($failed | ForEach-Object { $_.name })
    skippedNames = @($skipped | ForEach-Object { $_.name })
    p15Count = @($p15).Count
    p15Passed = @($p15 | Where-Object { $_.outcome -eq 'Passed' }).Count
    p15Failed = @($p15 | Where-Object { $_.outcome -eq 'Failed' }).Count
    p15Skipped = @($p15 | Where-Object { $_.outcome -in @('NotExecuted','Skipped','Inconclusive') }).Count
    p16Count = @($p16).Count
    passedCount = @($passed).Count
    failedCount = @($failed).Count
    skippedCount = @($skipped).Count
}
$obj | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath $dest -Encoding utf8
Copy-Item -LiteralPath $dest -Destination (Join-Path $out 'full-trx-summary.json') -Force

$log = Join-Path $out 'hv-dotnet-pluginint-all.log'
$t = Get-Content -LiteralPath $log -Raw
$console = [ordered]@{
    exists = $true
    path = $log
    lastWriteTimeUtc = (Get-Item -LiteralPath $log).LastWriteTimeUtc.ToString('o')
    length = (Get-Item -LiteralPath $log).Length
    failedBang = ($t -match 'Failed!')
    passedBang = ($t -match 'Passed!')
    summaryLine = $null
    failed = $null
    passed = $null
    skipped = $null
    total = $null
    duration = $null
}
$m = [regex]::Match($t, '(?m)^(?:Passed|Failed)!  - Failed:\s+(\d+), Passed:\s+(\d+), Skipped:\s+(\d+), Total:\s+(\d+), Duration:\s+(.+)$')
if ($m.Success) {
    $console.summaryLine = $m.Value.Trim()
    $console.failed = [int]$m.Groups[1].Value
    $console.passed = [int]$m.Groups[2].Value
    $console.skipped = [int]$m.Groups[3].Value
    $console.total = [int]$m.Groups[4].Value
    $console.duration = $m.Groups[5].Value.Trim()
}
$console | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $root 'hv-dotnet-pluginint-all-console.json') -Encoding utf8
Copy-Item -LiteralPath (Join-Path $root 'hv-dotnet-pluginint-all-console.json') -Destination (Join-Path $out 'hv-dotnet-pluginint-all-console.json') -Force
Write-Output ($console.summaryLine)
Write-Output ('TRX passed=' + $obj.passed + ' failed=' + $obj.failed + ' skipped=' + $obj.skipped + ' notExecuted=' + $obj.notExecuted + ' unitCount=' + $obj.unitCount + ' p15=' + $obj.p15Count + ' p16=' + $obj.p16Count)
