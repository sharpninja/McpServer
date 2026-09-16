#Requires -Version 7.0
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$out = 'F:\GitHub\McpServer\docs\receipts\_hv-c-red-p14'
$mdPath = 'F:\GitHub\McpServer\docs\receipts\hostile-validator-20260822T041327Z.md'
$jsonPath = 'F:\GitHub\McpServer\docs\receipts\hostile-validator-20260822T041327Z.json'
$md = Get-Item -LiteralPath $mdPath
$json = Get-Item -LiteralPath $jsonPath
$mdText = Get-Content -LiteralPath $md.FullName -Raw
$jsonObj = Get-Content -LiteralPath $json.FullName -Raw | ConvertFrom-Json
$proof = [ordered]@{
    MdExists = $true
    MdLength = $md.Length
    MdLastWriteTimeUtc = $md.LastWriteTimeUtc.ToString('o')
    MdHasAgree = [bool]($mdText -match '(?m)^OverallVerdict:\s*AGREE\s*$')
    MdHasDisagree = [bool]($mdText -match '(?m)^OverallVerdict:\s*DISAGREE\s*$')
    MdEmDash = $mdText.Contains([char]0x2014)
    MdEnDash = $mdText.Contains([char]0x2013)
    JsonExists = $true
    JsonLength = $json.Length
    JsonLastWriteTimeUtc = $json.LastWriteTimeUtc.ToString('o')
    JsonOverallVerdict = [string]$jsonObj.OverallVerdict
    JsonFailCount = $jsonObj.FailCount
    JsonUnknownCount = $jsonObj.UnknownCount
    JsonPassCount = $jsonObj.PassCount
    JsonClaimCount = @($jsonObj.Claims).Count
    JsonPhase = [string]$jsonObj.Phase
    JsonRerunFailed = $jsonObj.TestResults.RerunFailed
    JsonRerunPassed = $jsonObj.TestResults.RerunPassed
    JsonRerunSkipped = $jsonObj.TestResults.RerunSkippedConsole
    JsonP14Present = $jsonObj.TestResults.P14NamedTestsPresent
    JsonP15Present = $jsonObj.TestResults.P15NamedTestsPresent
    JsonP16Present = $jsonObj.TestResults.P16NamedTestsPresent
    JsonAddProfileCount = $jsonObj.AddProfile.profileFilesRead
}
$proof | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $out 'receipt-on-disk.json') -Encoding utf8
$proof | ConvertTo-Json
Write-Output 'VERIFY_DONE'
