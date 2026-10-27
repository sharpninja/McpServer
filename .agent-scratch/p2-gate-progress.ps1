$ErrorActionPreference = 'Continue'
$logDir = 'F:\GitHub\McpServer\.worktrees\sessionlife-p2-contracts-5cb2\docs\receipts\hv'
$logs = Get-ChildItem -LiteralPath $logDir -Filter 'p2-unit-legion-20261002T2*-gate.log' | Sort-Object LastWriteTime -Descending
$log = $logs | Select-Object -First 1
if (-not $log) { Write-Output 'NO_LOG'; exit 1 }
Write-Output ('LOG=' + $log.FullName)
Write-Output ('BYTES=' + $log.Length)
Write-Output ('WRITE=' + $log.LastWriteTimeUtc.ToString('o'))
$shim = Join-Path $env:USERPROFILE '.dotnet\tools\pwsh.exe'
$aside = Join-Path $env:USERPROFILE '.dotnet\tools\pwsh.exe.p2gate-aside'
Write-Output ('SHIM=' + (Test-Path -LiteralPath $shim))
Write-Output ('ASIDE=' + (Test-Path -LiteralPath $aside))
Write-Output '--- HEAD ---'
Get-Content -LiteralPath $log.FullName -TotalCount 12
Write-Output '--- TAIL ---'
Get-Content -LiteralPath $log.FullName -Tail 25
