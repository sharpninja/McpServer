#Requires -Version 7.0
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
$json = 'F:\GitHub\McpServer\docs\receipts\hostile-validator-20260818T163120Z.json'
$text = [System.IO.File]::ReadAllText($json)
if (-not $text.Contains('"TimestampUtc": "2026-08-18T11:31:20Z"')) { throw 'json stamp text not found' }
$text = $text.Replace('"TimestampUtc": "2026-08-18T11:31:20Z"', '"TimestampUtc": "2026-08-18T16:31:20Z"')
[System.IO.File]::WriteAllText($json, $text)
$line = Select-String -Path $json -Pattern 'TimestampUtc'
Write-Output ('JSON_LINE=' + $line.Line)
