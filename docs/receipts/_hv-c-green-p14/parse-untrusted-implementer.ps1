#Requires -Version 7.0
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Continue'

$out = 'F:\GitHub\McpServer\docs\receipts\_hv-c-green-p14'
$src = 'C:\Users\kingd\AppData\Local\Temp\grok-goal-678ba5b2f579\implementer\p14-green-filter.trx'
$log = 'C:\Users\kingd\AppData\Local\Temp\grok-goal-678ba5b2f579\implementer\p14-green-filter.log'
$obj = [ordered]@{
    Note = 'UNTRUSTED concurrent implementer artifact. Not this validator independent rerun.'
    TrxExists = (Test-Path -LiteralPath $src)
    LogExists = (Test-Path -LiteralPath $log)
}
if (Test-Path -LiteralPath $src) {
    $item = Get-Item -LiteralPath $src
    $obj.TrxLastWriteTimeUtc = $item.LastWriteTimeUtc.ToString('o')
    $obj.TrxLength = $item.Length
    [xml]$x = Get-Content -LiteralPath $src
    $ns = New-Object System.Xml.XmlNamespaceManager($x.NameTable)
    $ns.AddNamespace('t', 'http://microsoft.com/schemas/VisualStudio/TeamTest/2010')
    $c = $x.SelectSingleNode('//t:ResultSummary/t:Counters', $ns)
    $obj.trxTotal = if ($c) { [string]$c.Attributes['total'].Value } else { $null }
    $obj.trxPassed = if ($c) { [string]$c.Attributes['passed'].Value } else { $null }
    $obj.trxFailed = if ($c) { [string]$c.Attributes['failed'].Value } else { $null }
    $obj.trxExecuted = if ($c) { [string]$c.Attributes['executed'].Value } else { $null }
    $obj.trxNotExecuted = if ($c -and $c.Attributes['notExecuted']) { [string]$c.Attributes['notExecuted'].Value } else { $null }
}
if (Test-Path -LiteralPath $log) {
    $t = Get-Content -LiteralPath $log -Raw
    $obj.logPassedBang = ($t -match 'Passed!')
    $obj.logFailed = if ($t -match 'Failed:\s+(\d+)') { $Matches[1] } else { $null }
    $obj.logPassed = if ($t -match 'Passed:\s+(\d+)') { $Matches[1] } else { $null }
    $obj.logSkipped = if ($t -match 'Skipped:\s+(\d+)') { $Matches[1] } else { $null }
    $obj.logTotal = if ($t -match 'Total:\s+(\d+)') { $Matches[1] } else { $null }
}
$obj | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $out 'untrusted-implementer-p14-filter.json') -Encoding utf8
Write-Output 'UNTRUSTED_PARSE_DONE'
