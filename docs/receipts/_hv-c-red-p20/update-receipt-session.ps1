#Requires -Version 7.0
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$jsonPath = 'F:\GitHub\McpServer\docs\receipts\hostile-validator-20260822T132620Z.json'
$obj = Get-Content -LiteralPath $jsonPath -Raw | ConvertFrom-Json
$obj.Session.sessionId = 'GrokSubagentHostile-20260822T133042Z-c-red-p20-rebuild'
$obj.Session.requestId = 'req-20260822T133042Z-001-hostile-c-red-p20-rebuild'
$obj.Session.turnId = 42986
$obj.Session.queryProof = 'docs/receipts/_hv-c-red-p20/own-sl-query-sid-after.txt'
$obj.Session.completeProof = 'docs/receipts/_hv-c-red-p20/own-sl-complete.txt'
$obj | ConvertTo-Json -Depth 12 | Set-Content -LiteralPath $jsonPath -Encoding utf8
Write-Output 'UPDATED_JSON_SESSION'
