#Requires -Version 7.0
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
$md = 'F:\GitHub\McpServer\docs\receipts\hostile-validator-20260817T233618Z.md'
$json = 'F:\GitHub\McpServer\docs\receipts\hostile-validator-20260817T233618Z.json'
foreach ($p in @($md, $json)) {
    $i = Get-Item -LiteralPath $p
    Write-Output ($i.Name + ' exists=True len=' + $i.Length + ' utc=' + $i.LastWriteTimeUtc.ToString('o'))
}
$obj = Get-Content -LiteralPath $json -Raw | ConvertFrom-Json
Write-Output ('OverallVerdict=' + $obj.OverallVerdict)
Write-Output ('SessionId=' + $obj.SessionId)
Write-Output ('RequestId=' + $obj.RequestId)
Write-Output ('AddProfileExecuted=' + $obj.AddProfileExecuted)
Write-Output ('ProfileFilesRead=' + $obj.ProfileFilesRead)
Write-Output ('FailListCount=' + @($obj.FailList).Count)
$mdText = Get-Content -LiteralPath $md -Raw
$em = ([regex]::Matches($mdText, [char]0x2014)).Count
$en = ([regex]::Matches($mdText, [char]0x2013)).Count
Write-Output ('MdEmDashCount=' + $em)
Write-Output ('MdEnDashCount=' + $en)
$claimVerdicts = @($obj.Claims | ForEach-Object { $_.Id + '=' + $_.Verdict })
Write-Output ('ClaimVerdicts=' + ($claimVerdicts -join ','))
