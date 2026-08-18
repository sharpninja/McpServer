#Requires -Version 7.0
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$mdPath = 'F:\GitHub\McpServer\docs\receipts\hostile-validator-20260818T160833Z.md'
$jsonPath = 'F:\GitHub\McpServer\docs\receipts\hostile-validator-20260818T160833Z.json'
$utf8 = New-Object System.Text.UTF8Encoding $false
$correct = '2026-08-18T16:08:33Z'

$obj = Get-Content -LiteralPath $jsonPath -Raw | ConvertFrom-Json
$obj.TimestampUtc = $correct
[System.IO.File]::WriteAllText($jsonPath, (($obj | ConvertTo-Json -Depth 10) + "`n"), $utf8)

$md = Get-Content -LiteralPath $mdPath -Raw
$md = $md.Replace('TimestampUtc: 2026-08-18T11:08:33Z', 'TimestampUtc: 2026-08-18T16:08:33Z')
[System.IO.File]::WriteAllText($mdPath, $md, $utf8)

$md2 = Get-Content -LiteralPath $mdPath -Raw
$obj2 = Get-Content -LiteralPath $jsonPath -Raw | ConvertFrom-Json
Write-Output ('MD_TS=' + ([regex]::Match($md2, 'TimestampUtc: ([^\r\n]+)')).Groups[1].Value)
Write-Output ('JSON_TS=' + $obj2.TimestampUtc)
Write-Output ('MD_HAS_WRONG=' + $md2.Contains('11:08:33Z'))
Write-Output ('JSON_HAS_WRONG=' + $obj2.TimestampUtc.Contains('11:08:33'))
Write-Output ('MD_SHA256=' + (Get-FileHash -LiteralPath $mdPath -Algorithm SHA256).Hash)
Write-Output ('JSON_SHA256=' + (Get-FileHash -LiteralPath $jsonPath -Algorithm SHA256).Hash)
