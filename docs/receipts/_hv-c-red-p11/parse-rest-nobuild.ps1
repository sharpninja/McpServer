#Requires -Version 7.0
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Continue'

$out = 'F:\GitHub\McpServer\docs\receipts\_hv-c-red-p11'

function Get-Attr {
    param($node, [string]$name)
    if ($null -eq $node) { return $null }
    $a = $node.Attributes[$name]
    if ($null -eq $a) { return $null }
    return [string]$a.Value
}

$TrxPath = Join-Path $out 'dotnet-pluginintegration-rest-nobuild.trx'
if (-not (Test-Path -LiteralPath $TrxPath)) {
    [ordered]@{ exists = $false } | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $out 'rest-nobuild-trx-summary.json') -Encoding utf8
    Write-Output 'NO_TRX'
    exit 0
}

[xml]$x = Get-Content -LiteralPath $TrxPath
$ns = New-Object System.Xml.XmlNamespaceManager($x.NameTable)
$ns.AddNamespace('t', 'http://microsoft.com/schemas/VisualStudio/TeamTest/2010')
$summary = $x.SelectSingleNode('//t:ResultSummary', $ns)
$c = $x.SelectSingleNode('//t:ResultSummary/t:Counters', $ns)
$results = $x.SelectNodes('//t:UnitTestResult', $ns)
$outcomes = @()
foreach ($u in @($results)) {
    $outcomes += [ordered]@{
        name = Get-Attr $u 'testName'
        outcome = Get-Attr $u 'outcome'
    }
}
$p11 = @($outcomes | Where-Object { $_.name -match 'Theory_EachScenario_FailsUntilAdapterOperational' })
$obj = [ordered]@{
    exists = $true
    outcome = Get-Attr $summary 'outcome'
    total = Get-Attr $c 'total'
    executed = Get-Attr $c 'executed'
    passed = Get-Attr $c 'passed'
    failed = Get-Attr $c 'failed'
    skipped = Get-Attr $c 'skipped'
    notExecuted = Get-Attr $c 'notExecuted'
    unitCount = @($results).Count
    failedNames = @($outcomes | Where-Object { $_.outcome -eq 'Failed' } | ForEach-Object { $_.name })
    skippedNames = @($outcomes | Where-Object { $_.outcome -in @('NotExecuted','Skipped','Inconclusive') } | ForEach-Object { $_.name })
    p11Count = $p11.Count
    p12Count = @($outcomes | Where-Object { $_.name -match 'BootstrapBeginAppendComplete' }).Count
    p13Count = @($outcomes | Where-Object { $_.name -match 'ServerQuery_SourceType' }).Count
}
$obj | ConvertTo-Json -Depth 6 | Set-Content -LiteralPath (Join-Path $out 'rest-nobuild-trx-summary.json') -Encoding utf8

$t = Get-Content -LiteralPath (Join-Path $out 'dotnet-pluginintegration-rest-nobuild.log') -Raw
$console = [ordered]@{
    restFailed = if ($t -match 'Failed:\s+(\d+)') { $Matches[1] } else { $null }
    restPassed = if ($t -match 'Passed:\s+(\d+)') { $Matches[1] } else { $null }
    restSkipped = if ($t -match 'Skipped:\s+(\d+)') { $Matches[1] } else { $null }
    restTotal = if ($t -match 'Total:\s+(\d+)') { $Matches[1] } else { $null }
    restPassedBang = ($t -match 'Passed!')
    restFailedBang = ($t -match 'Failed!')
}
$console | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $out 'dotnet-rest-nobuild-console-counts.json') -Encoding utf8
Write-Output ('UTC=' + [DateTime]::UtcNow.ToString('yyyyMMddTHHmmssZ'))
Write-Output ('REST_PASSED=' + $obj.passed + ' FAILED=' + $obj.failed + ' NOTEXEC=' + $obj.notExecuted + ' P11=' + $obj.p11Count)
