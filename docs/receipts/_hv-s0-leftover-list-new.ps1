#Requires -Version 7.0
Set-StrictMode -Version Latest
$cutoff = [datetime]::Parse('2026-08-19T18:34:00Z').ToUniversalTime()
Get-ChildItem -LiteralPath 'F:\GitHub\McpServer\docs\receipts' | Where-Object {
    $_.LastWriteTimeUtc -gt $cutoff
} | Sort-Object LastWriteTimeUtc -Descending | Select-Object -First 25 Name, LastWriteTimeUtc, Mode | Format-Table -AutoSize
