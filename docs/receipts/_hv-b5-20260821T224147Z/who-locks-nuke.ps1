$ErrorActionPreference = 'Continue'
$out = 'F:\GitHub\McpServer\docs\receipts\_hv-b5-20260821T224147Z'
$procs = Get-CimInstance Win32_Process | Where-Object {
    $_.Name -match 'dotnet|nuke|build|_build|pwsh|testhost' -and
    $_.CommandLine -match 'McpServer|build.ps1|Nuke|_build'
} | Select-Object ProcessId, Name, CreationDate, CommandLine
$procs | ConvertTo-Json -Depth 4 | Set-Content -LiteralPath (Join-Path $out 'nuke-lock-procs.json') -Encoding utf8
$procs | ForEach-Object { Write-Output (('PID=' + $_.ProcessId + ' NAME=' + $_.Name + ' CMD=' + $_.CommandLine).Substring(0, [Math]::Min(400, (('PID=' + $_.ProcessId + ' NAME=' + $_.Name + ' CMD=' + $_.CommandLine).Length)))) }
if (-not $procs) { Write-Output 'NO_MATCHING_PROCS' }
# handle count on build.log
$log = 'F:\GitHub\McpServer\.nuke\temp\build.log'
if (Test-Path $log) {
    Write-Output ('LOG_EXISTS=True SIZE=' + (Get-Item $log).Length + ' MTIME=' + (Get-Item $log).LastWriteTimeUtc.ToString('o'))
} else {
    Write-Output 'LOG_EXISTS=False'
}
