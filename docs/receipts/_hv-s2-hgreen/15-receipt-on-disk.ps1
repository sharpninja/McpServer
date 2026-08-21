#Requires -Version 7.0
[CmdletBinding()]
param()
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$md = 'F:\GitHub\McpServer\docs\receipts\hostile-validator-20260819T210624Z.md'
$js = 'F:\GitHub\McpServer\docs\receipts\hostile-validator-20260819T210624Z.json'
$mdItem = Get-Item -LiteralPath $md
$jsItem = Get-Item -LiteralPath $js
$mdText = Get-Content -LiteralPath $md -Raw
$j = Get-Content -LiteralPath $js -Raw | ConvertFrom-Json
$obj = [ordered]@{
    TimestampUtc = [datetime]::UtcNow.ToString('yyyy-MM-ddTHH:mm:ssZ')
    MdExists = $true
    JsonExists = $true
    MdBytes = $mdItem.Length
    JsonBytes = $jsItem.Length
    MdLastWriteUtc = $mdItem.LastWriteTimeUtc.ToString('o')
    JsonLastWriteUtc = $jsItem.LastWriteTimeUtc.ToString('o')
    MdVerdict = [regex]::Match($mdText, 'OverallVerdict:\s*(AGREE|DISAGREE)').Groups[1].Value
    JsonVerdict = [string]$j.OverallVerdict
    JsonFailCount = @($j.FailList).Count
    JsonPass = $j.Counts.PASS
    JsonFail = $j.Counts.FAIL
    JsonUnknown = $j.Counts.UNKNOWN
}
$out = 'F:\GitHub\McpServer\docs\receipts\_hv-s2-hgreen\15-receipt-on-disk.json'
$obj | ConvertTo-Json -Depth 5 | Set-Content -LiteralPath $out -Encoding utf8
Write-Output ("WROTE {0} md={1} json={2} failCount={3} pass={4}" -f $out, $obj.MdVerdict, $obj.JsonVerdict, $obj.JsonFailCount, $obj.JsonPass)
