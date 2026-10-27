$ErrorActionPreference = 'Continue'
$logDir = 'F:\GitHub\McpServer\.worktrees\sessionlife-p2-contracts-5cb2\docs\receipts\hv'
$logs = @(Get-ChildItem -LiteralPath $logDir -Filter 'p2-unit-legion-20261002T*-gate.log' | Sort-Object LastWriteTimeUtc -Descending)
foreach ($log in $logs | Select-Object -First 3) {
    Write-Output ('LOG=' + $log.Name + ' BYTES=' + $log.Length + ' WRITE=' + $log.LastWriteTimeUtc.ToString('o'))
}
$newest = $logs | Select-Object -First 1
if ($newest) {
    Write-Output '=== TAIL ==='
    Get-Content -LiteralPath $newest.FullName -Tail 20
}
Write-Output '=== SHIM ==='
Write-Output ('SHIM_EXISTS=' + (Test-Path 'C:\Users\kingd\.dotnet\tools\pwsh.exe'))
Write-Output ('ASIDE_EXISTS=' + (Test-Path 'C:\Users\kingd\.dotnet\tools\pwsh.exe.p2gate-aside'))
