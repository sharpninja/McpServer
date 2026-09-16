#Requires -Version 7.0
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
$out = 'F:\GitHub\McpServer\docs\receipts\_hv-c-green-p20'
$path = Join-Path $out 'results-p20-green\p20-green.trx'
[xml]$xml = Get-Content -LiteralPath $path -Raw
$ns = New-Object System.Xml.XmlNamespaceManager($xml.NameTable)
$ns.AddNamespace('t', 'http://microsoft.com/schemas/VisualStudio/TeamTest/2010')
$c = $xml.SelectSingleNode('//t:ResultSummary/t:Counters', $ns)
$outcomes = @($xml.SelectNodes('//t:UnitTestResult', $ns) | ForEach-Object {
    [ordered]@{ name = [string]$_.testName; outcome = [string]$_.outcome; duration = [string]$_.duration }
})
$skippedAttr = $null
if ($c.HasAttribute('skipped')) { $skippedAttr = [string]$c.GetAttribute('skipped') }
[ordered]@{
    exists = $true
    path = $path
    total = [int]$c.GetAttribute('total')
    executed = [int]$c.GetAttribute('executed')
    passed = [int]$c.GetAttribute('passed')
    failed = [int]$c.GetAttribute('failed')
    notExecuted = [int]$c.GetAttribute('notExecuted')
    skippedAttr = $skippedAttr
    outcomes = $outcomes
} | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath (Join-Path $out 'independent-p20-trx-summary.json') -Encoding utf8
Write-Output 'PARSE2_DONE'
