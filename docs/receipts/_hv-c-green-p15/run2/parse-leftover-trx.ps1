#Requires -Version 7.0
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Continue'

$src = 'F:\GitHub\McpServer\docs\receipts\_hv-c-green-p15\trx-p15-hv\p15-filter.trx'
$dest = 'F:\GitHub\McpServer\docs\receipts\_hv-c-green-p15\run2\leftover-trx-summary.json'
if (-not (Test-Path -LiteralPath $src)) {
    [ordered]@{ exists = $false; path = $src } | ConvertTo-Json | Set-Content -LiteralPath $dest -Encoding utf8
    Write-Output 'LEFTOVER_TRX_MISSING'
    exit 0
}
[xml]$x = Get-Content -LiteralPath $src
$ns = New-Object System.Xml.XmlNamespaceManager($x.NameTable)
$ns.AddNamespace('t', 'http://microsoft.com/schemas/VisualStudio/TeamTest/2010')
$summary = $x.SelectSingleNode('//t:ResultSummary', $ns)
$c = $x.SelectSingleNode('//t:ResultSummary/t:Counters', $ns)
$results = $x.SelectNodes('//t:UnitTestResult', $ns)
function Get-Attr($node, [string]$name) {
    if ($null -eq $node) { return $null }
    $a = $node.Attributes[$name]
    if ($null -eq $a) { return $null }
    return [string]$a.Value
}
$names = @($results | ForEach-Object { Get-Attr $_ 'testName' })
$p16 = @($names | Where-Object { $_ -match 'AiTheory_|P16' })
[ordered]@{
    exists = $true
    path = $src
    lastWriteUtc = (Get-Item -LiteralPath $src).LastWriteTimeUtc.ToString('o')
    outcome = Get-Attr $summary 'outcome'
    total = Get-Attr $c 'total'
    executed = Get-Attr $c 'executed'
    passed = Get-Attr $c 'passed'
    failed = Get-Attr $c 'failed'
    skipped = Get-Attr $c 'skipped'
    notExecuted = Get-Attr $c 'notExecuted'
    unitCount = @($results).Count
    p16Count = $p16.Count
    note = 'Leftover 062054Z collector TRX. Corroboration only. Not this review independent proof.'
} | ConvertTo-Json | Set-Content -LiteralPath $dest -Encoding utf8
Write-Output 'LEFTOVER_TRX_PARSED'
