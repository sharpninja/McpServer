#Requires -Version 7.0
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
$trx = 'F:\GitHub\McpServer\docs\receipts\_hv-c-green-p19-r2\results-p19-green\p19-green.trx'
[xml]$x = Get-Content -LiteralPath $trx -Raw
$ns = New-Object System.Xml.XmlNamespaceManager($x.NameTable)
$ns.AddNamespace('t', 'http://microsoft.com/schemas/VisualStudio/TeamTest/2010')
$c = $x.SelectSingleNode('//t:ResultSummary/t:Counters', $ns)
$results = @($x.SelectNodes('//t:UnitTestResult', $ns) | ForEach-Object {
    [ordered]@{ name = $_.testName; outcome = $_.outcome }
})
$skippedAttr = $null
if ($c.HasAttribute('skipped')) { $skippedAttr = $c.GetAttribute('skipped') }
[ordered]@{
    total = $c.total
    executed = $c.executed
    passed = $c.passed
    failed = $c.failed
    notExecuted = $c.notExecuted
    skippedAttr = $skippedAttr
    names = @($results | ForEach-Object { $_.name })
    outcomes = $results
} | ConvertTo-Json -Depth 6 | Set-Content -LiteralPath 'F:\GitHub\McpServer\docs\receipts\_hv-c-green-p19-r2\p19-trx-summary.json' -Encoding utf8
Write-Output ('TRX total=' + $c.total + ' executed=' + $c.executed + ' passed=' + $c.passed + ' failed=' + $c.failed + ' notExecuted=' + $c.notExecuted)
