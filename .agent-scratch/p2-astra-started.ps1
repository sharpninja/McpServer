$ErrorActionPreference = 'Continue'
$req = 'F:\GitHub\McpServer\.worktrees\sessionlife-p2-contracts-5cb2\docs\receipts\hv\20261002T203600Z-sessionlife-p2-contracts-hv.request.jsonl'
Write-Output ('REQ_EXISTS=' + (Test-Path -LiteralPath $req))
if (Test-Path -LiteralPath $req) {
    Get-Content -LiteralPath $req -TotalCount 1
}
Get-Process -Id 138084 -ErrorAction SilentlyContinue | Select-Object Id,StartTime,CPU | Format-List | Out-String
