$ErrorActionPreference = 'Continue'
$nuke = Get-CimInstance Win32_Process | Where-Object { $_.CommandLine -match 'build.ps1 Test|_build.exe.*Test' }
if ($nuke) {
    $nuke | ForEach-Object { Write-Output ('RUNNING PID=' + $_.ProcessId + ' NAME=' + $_.Name) }
} else {
    Write-Output 'TEST_NOT_RUNNING'
}
$log = 'F:\GitHub\McpServer\.nuke\temp\build.log'
if (Test-Path $log) {
    $item = Get-Item $log
    Write-Output ('LOG_MTIME_UTC=' + $item.LastWriteTimeUtc.ToString('o') + ' SIZE=' + $item.Length)
    Get-Content -LiteralPath $log -Tail 15
}
