#Requires -Version 7.0
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$jsonPath = 'F:\GitHub\McpServer\docs\receipts\hostile-validator-20260822T035559Z.json'
$obj = Get-Content -LiteralPath $jsonPath -Raw | ConvertFrom-Json
$obj.PersistenceProof = 'client.SessionLog.QueryAsync turn status completed; filesModified includes docs/receipts/hostile-validator-20260822T035559Z.md; CompleteTurnAsync turnId 42874'
$obj.TurnStatus = 'completed'
$obj | ConvertTo-Json -Depth 12 | Set-Content -LiteralPath $jsonPath -Encoding utf8
Write-Output 'JSON_PATCHED'
Get-Item -LiteralPath $jsonPath, 'F:\GitHub\McpServer\docs\receipts\hostile-validator-20260822T035559Z.md' |
    Select-Object Name, Length, LastWriteTimeUtc | Format-List
