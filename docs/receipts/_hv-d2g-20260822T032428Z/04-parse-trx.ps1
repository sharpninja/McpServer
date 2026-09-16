#Requires -Version 7.0
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
$outDir = 'F:\GitHub\McpServer\docs\receipts\_hv-d2g-20260822T032428Z'
$files = Get-ChildItem -LiteralPath $outDir -Filter '*.trx'
$results = foreach ($f in $files) {
    [xml]$xml = Get-Content -LiteralPath $f.FullName
    $ns = New-Object System.Xml.XmlNamespaceManager($xml.NameTable)
    $ns.AddNamespace('t', 'http://microsoft.com/schemas/VisualStudio/TeamTest/2010')
    $counters = $xml.SelectSingleNode('//t:Counters', $ns)
    $defs = @($xml.SelectNodes('//t:UnitTest/t:TestMethod', $ns) | ForEach-Object { $_.className + '.' + $_.name })
    [ordered]@{
        file = $f.Name
        total = [int]$counters.total
        executed = [int]$counters.executed
        passed = [int]$counters.passed
        failed = [int]$counters.failed
        notExecuted = [int]$counters.notExecuted
        timeout = [int]$counters.timeout
        error = [int]$counters.error
        methods = $defs
    }
}
$results | ConvertTo-Json -Depth 6 | Set-Content -LiteralPath (Join-Path $outDir 'trx-parse.json') -Encoding utf8
$results | ForEach-Object { Write-Output ($_.file + ' total=' + $_.total + ' exec=' + $_.executed + ' pass=' + $_.passed + ' fail=' + $_.failed + ' skip=' + $_.notExecuted) }
