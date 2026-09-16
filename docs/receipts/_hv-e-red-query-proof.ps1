#Requires -Version 7.0
[CmdletBinding()]
param()
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
$path = 'F:\GitHub\McpServer\docs\receipts\_hv-e-red-20260822T151657Z\session-complete.json'
$obj = Get-Content -LiteralPath $path -Raw | ConvertFrom-Json
Write-Output ('COMPLETE_TEXT=' + $obj.completeText)
$query = $obj.queryText | ConvertFrom-Json
$query | ConvertTo-Json -Depth 12 | Set-Content -LiteralPath 'F:\GitHub\McpServer\docs\receipts\_hv-e-red-20260822T151657Z\query-parsed.json' -Encoding utf8
$items = @()
if ($query.items) { $items = @($query.items) }
elseif ($query.Items) { $items = @($query.Items) }
Write-Output ('ITEM_COUNT=' + $items.Count)
foreach ($s in $items) {
    Write-Output ('SESSION=' + $s.sessionId + '|status=' + $s.status + '|turnCount=' + $s.turnCount)
    $turns = @()
    if ($s.turns) { $turns = @($s.turns) }
    elseif ($s.Turns) { $turns = @($s.Turns) }
    foreach ($t in $turns) {
        Write-Output ('TURN=' + $t.requestId + '|status=' + $t.status + '|turnId=' + $t.turnId + '|title=' + $t.queryTitle)
    }
}
Write-Output ('TODO_PLAN_HAS_DONEFALSE=' + ($obj.todoPlan -match '"Done":false'))
Write-Output ('TODO_HR_HAS_DONEFALSE=' + ($obj.todoHr -match '"Done":false'))
