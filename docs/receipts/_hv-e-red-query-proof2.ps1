#Requires -Version 7.0
[CmdletBinding()]
param()
$ErrorActionPreference = 'Stop'
$path = 'F:\GitHub\McpServer\docs\receipts\_hv-e-red-20260822T151657Z\session-complete.json'
$obj = Get-Content -LiteralPath $path -Raw | ConvertFrom-Json
$query = $obj.queryText | ConvertFrom-Json
$s = @($query.items)[0]
Write-Output ('SESSION_KEYS=' + (($s.PSObject.Properties.Name) -join ','))
Write-Output ('SESSION_STATUS=' + $s.status)
Write-Output ('TURNCOUNT=' + $s.turnCount)
$turns = @($s.turns)
Write-Output ('TURNS=' + $turns.Count)
if ($turns.Count -gt 0) {
    $t = $turns[0]
    Write-Output ('TURN_KEYS=' + (($t.PSObject.Properties.Name) -join ','))
    $t | ConvertTo-Json -Depth 8 -Compress
}
# also check requests
if ($s.PSObject.Properties['requests']) {
    Write-Output ('REQUESTS=' + @($s.requests).Count)
    $r = @($s.requests)[0]
    Write-Output ('REQ_KEYS=' + (($r.PSObject.Properties.Name) -join ','))
    Write-Output ('REQ_STATUS=' + $r.status)
    Write-Output ('REQ_ID=' + $r.requestId)
}
if ($s.PSObject.Properties['entries']) {
    Write-Output ('ENTRIES=' + @($s.entries).Count)
}
