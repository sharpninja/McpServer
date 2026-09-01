$ErrorActionPreference = 'Stop'
$trx = 'F:\GitHub\McpServer\docs\receipts\_hv-g1-closeout\named-unit.trx'
[xml]$doc = Get-Content -LiteralPath $trx
$ns = New-Object System.Xml.XmlNamespaceManager($doc.NameTable)
$ns.AddNamespace('t', 'http://microsoft.com/schemas/VisualStudio/TeamTest/2010')
$result = $doc.SelectSingleNode('//t:ResultSummary', $ns)
$counters = $doc.SelectSingleNode('//t:ResultSummary/t:Counters', $ns)
Write-Output ('outcome=' + $result.outcome)
Write-Output ('total=' + $counters.total)
Write-Output ('executed=' + $counters.executed)
Write-Output ('passed=' + $counters.passed)
Write-Output ('failed=' + $counters.failed)
Write-Output ('skipped=' + $counters.notExecuted)
Write-Output ('inconclusive=' + $counters.inconclusive)
