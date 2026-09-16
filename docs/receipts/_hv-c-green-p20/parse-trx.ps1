#Requires -Version 7.0
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Continue'

$out = 'F:\GitHub\McpServer\docs\receipts\_hv-c-green-p20'
function Parse-Trx([string]$path, [string]$name) {
    if (-not (Test-Path -LiteralPath $path)) {
        [ordered]@{ name = $name; exists = $false } | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $out ($name + '.json')) -Encoding utf8
        return
    }
    [xml]$xml = Get-Content -LiteralPath $path -Raw
    $ns = New-Object System.Xml.XmlNamespaceManager($xml.NameTable)
    $ns.AddNamespace('t', 'http://microsoft.com/schemas/VisualStudio/TeamTest/2010')
    $c = $xml.SelectSingleNode('//t:ResultSummary/t:Counters', $ns)
    $outcomes = @($xml.SelectNodes('//t:UnitTestResult', $ns) | ForEach-Object {
        $msg = $null
        $outNode = $_.SelectSingleNode('t:Output/t:ErrorInfo/t:Message', $ns)
        if ($outNode) { $msg = $outNode.InnerText }
        [ordered]@{ name = $_.testName; outcome = $_.outcome; duration = $_.duration; message = $msg }
    })
    [ordered]@{
        name = $name
        exists = $true
        path = $path
        total = [int]$c.total
        executed = [int]$c.executed
        passed = [int]$c.passed
        failed = [int]$c.failed
        notExecuted = [int]$c.notExecuted
        skippedAttr = $c.skipped
        outcomes = $outcomes
    } | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath (Join-Path $out ($name + '.json')) -Encoding utf8
}

Parse-Trx (Join-Path $out 'results-p20-green\p20-green.trx') 'independent-p20-trx-summary'
$rebuild = Join-Path $out 'results-p20-green-rebuild\p20-green-rebuild.trx'
if (Test-Path -LiteralPath $rebuild) {
    Parse-Trx $rebuild 'independent-p20-trx-rebuild-summary'
}
Write-Output 'PARSE_TRX_DONE'
