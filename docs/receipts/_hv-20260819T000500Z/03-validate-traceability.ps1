#Requires -Version 7.0
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Continue'
Set-Location -LiteralPath 'F:\GitHub\McpServer'
$log = 'F:\GitHub\McpServer\docs\receipts\_hv-20260819T000500Z\validate-traceability.log'
& .\build.ps1 ValidateTraceability 2>&1 | Tee-Object -FilePath $log
$exit = $LASTEXITCODE
Write-Output ('VT_EXIT=' + $exit)
$d = Get-PSDrive -Name F
Write-Output ('DISK_FREE_GB=' + [math]::Round($d.Free / 1GB, 2))
exit $exit
