$ErrorActionPreference = 'Stop'
$hv = 'F:\GitHub\McpServer\.worktrees\sessionlife-p2-contracts-5cb2\docs\receipts\hv'
$cut = [DateTime]::Parse('2026-10-02T20:34:00Z').ToUniversalTime()
Get-ChildItem -LiteralPath $hv -File | Where-Object { $_.LastWriteTimeUtc -ge $cut -and $_.Extension -ne '.log' } | Sort-Object Name | ForEach-Object {
    Write-Output ('{0}`t{1}' -f $_.Length, $_.Name)
}
