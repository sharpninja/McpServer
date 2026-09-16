#Requires -Version 7.0
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$jsonPath = 'F:\GitHub\McpServer\docs\receipts\hostile-validator-20260822T081750Z.json'
$mdPath = 'F:\GitHub\McpServer\docs\receipts\hostile-validator-20260822T081750Z.md'
$out = 'F:\GitHub\McpServer\docs\receipts\_hv-c-green-p16-p18'

$obj = Get-Content -LiteralPath $jsonPath -Raw | ConvertFrom-Json
$claimCount = @($obj.Claims).Count
$pass = @($obj.Claims | Where-Object { $_.Verdict -eq 'PASS' }).Count
$fail = @($obj.Claims | Where-Object { $_.Verdict -eq 'FAIL' }).Count
$unk = @($obj.Claims | Where-Object { $_.Verdict -eq 'UNKNOWN' }).Count
if ($pass -ne 18 -or $fail -ne 2 -or $claimCount -ne 20) {
    throw "count mismatch pass=$pass fail=$fail claims=$claimCount"
}
$obj.PassCount = 18
$obj | ConvertTo-Json -Depth 20 | Set-Content -LiteralPath $jsonPath -Encoding utf8

$md = Get-Item -LiteralPath $mdPath
$js = Get-Item -LiteralPath $jsonPath
$mdText = Get-Content -LiteralPath $md.FullName -Raw
$jsObj = Get-Content -LiteralPath $js.FullName -Raw | ConvertFrom-Json
[ordered]@{
    MdExists = $true
    MdLength = $md.Length
    MdLastWriteTimeUtc = $md.LastWriteTimeUtc.ToString('o')
    MdHasAgree = [bool]($mdText -match '(?m)^OverallVerdict:\s*AGREE\s*$')
    MdHasDisagree = [bool]($mdText -match '(?m)^OverallVerdict:\s*DISAGREE\s*$')
    MdEmDash = $mdText.Contains([char]0x2014)
    MdEnDash = $mdText.Contains([char]0x2013)
    JsonExists = $true
    JsonLength = $js.Length
    JsonLastWriteTimeUtc = $js.LastWriteTimeUtc.ToString('o')
    JsonOverallVerdict = [string]$jsObj.OverallVerdict
    JsonFailCount = $jsObj.FailCount
    JsonPassCount = $jsObj.PassCount
    JsonUnknownCount = $jsObj.UnknownCount
    ClaimCount = @($jsObj.Claims).Count
    Pass = $pass
    Fail = $fail
    Unknown = $unk
} | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $out 'receipt-on-disk-finish.json') -Encoding utf8

Write-Output ('CLAIMS=' + $claimCount + ' PASS=' + $pass + ' FAIL=' + $fail + ' JSON_PASSCOUNT=' + $jsObj.PassCount)
Write-Output 'FIX_DONE'
