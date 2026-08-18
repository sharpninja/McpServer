#Requires -Version 7.0
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
$md = 'F:\GitHub\McpServer\docs\receipts\hostile-validator-20260818T163120Z.md'
$json = 'F:\GitHub\McpServer\docs\receipts\hostile-validator-20260818T163120Z.json'
$mdText = [System.IO.File]::ReadAllText($md)
if (-not $mdText.Contains('TimestampUtc: 2026-08-18T11:31:20Z')) { throw 'md stamp not found' }
$mdText = $mdText.Replace('TimestampUtc: 2026-08-18T11:31:20Z', 'TimestampUtc: 2026-08-18T16:31:20Z')
[System.IO.File]::WriteAllText($md, $mdText)
$obj = Get-Content -LiteralPath $json -Raw | ConvertFrom-Json
if ($obj.TimestampUtc -ne '2026-08-18T11:31:20Z') { throw 'json stamp not found' }
$obj.TimestampUtc = '2026-08-18T16:31:20Z'
($obj | ConvertTo-Json -Depth 12) | Set-Content -LiteralPath $json -Encoding utf8
$mdCheck = Select-String -Path $md -Pattern '^TimestampUtc:'
$jsonCheck = (Get-Content -LiteralPath $json -Raw | ConvertFrom-Json).TimestampUtc
Write-Output ('MD_STAMP=' + $mdCheck.Line)
Write-Output ('JSON_STAMP=' + $jsonCheck)
