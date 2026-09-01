#Requires -Version 7.0
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$mdPath = 'F:\GitHub\McpServer\docs\receipts\hostile-validator-20260818T160833Z.md'
$jsonPath = 'F:\GitHub\McpServer\docs\receipts\hostile-validator-20260818T160833Z.json'
$md = Get-Content -LiteralPath $mdPath -Raw
$jsonText = Get-Content -LiteralPath $jsonPath -Raw
Write-Output ('MD_EXISTS=' + (Test-Path -LiteralPath $mdPath))
Write-Output ('JSON_EXISTS=' + (Test-Path -LiteralPath $jsonPath))
Write-Output ('MD_BYTES=' + (Get-Item -LiteralPath $mdPath).Length)
Write-Output ('JSON_BYTES=' + (Get-Item -LiteralPath $jsonPath).Length)
Write-Output ('MD_SHA256=' + (Get-FileHash -LiteralPath $mdPath -Algorithm SHA256).Hash)
Write-Output ('JSON_SHA256=' + (Get-FileHash -LiteralPath $jsonPath -Algorithm SHA256).Hash)
Write-Output ('MD_TS=' + ([regex]::Match($md, 'TimestampUtc: ([^\r\n]+)')).Groups[1].Value)
Write-Output ('JSON_TS_LINE=' + ([regex]::Match($jsonText, '"TimestampUtc":\s*"[^"]+"')).Value)
Write-Output ('MD_VERDICT=' + ([regex]::Match($md, 'OverallVerdict: (\w+)')).Groups[1].Value)
Write-Output ('JSON_VERDICT_LINE=' + ([regex]::Match($jsonText, '"OverallVerdict":\s*"[^"]+"')).Value)
Write-Output ('MD_QUERY_PROOF=' + $md.Contains('Persistence proved by sessionlog_query'))
Write-Output ('JSON_QUERY=' + $jsonText.Contains('"queryTotalCount": 1'))
Write-Output ('MD_EMDASH=' + $md.Contains([string][char]0x2014))
Write-Output ('JSON_FAIL_EMPTY=' + $jsonText.Contains('"failList": []'))
