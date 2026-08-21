$ErrorActionPreference = 'Stop'
$path = 'F:\GitHub\McpServer\docs\receipts\hostile-validator-20260819T233252Z.json'
$obj = Get-Content -LiteralPath $path -Raw | ConvertFrom-Json
$obj | Add-Member -NotePropertyName SessionPersistence -NotePropertyValue ([ordered]@{
    OpenCreated = $true
    TurnId = 42130
    CompleteStatus = 'completed'
    QueryTotalCount = 1
    QuerySessionId = 'GrokCode-20260819T232048Z-hostile-g9s4'
    QueryRequestId = 'req-20260819T232048Z-001-hostile-validate-bug-triage-122'
    QueryTurnStatus = 'completed'
    QueryActionCount = 4
    QueryDialogCount = 5
    QueryDesignDecisionCount = 3
    QueryResponseStartsWith = 'OverallVerdict DISAGREE'
}) -Force
($obj | ConvertTo-Json -Depth 8) | Set-Content -LiteralPath $path -Encoding utf8
Write-Output 'PATCHED'
Get-Item -LiteralPath $path | Select-Object Length, LastWriteTimeUtc
