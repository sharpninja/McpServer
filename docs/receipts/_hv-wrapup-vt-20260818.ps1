#Requires -Version 7.0
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Continue'
Set-Location 'F:\GitHub\McpServer'
$out = 'F:\GitHub\McpServer\docs\receipts\_hv-wrapup-vt-20260818.txt'
Write-Output ('UTC_START=' + (Get-Date -AsUTC -Format o))
& .\build.ps1 ValidateTraceability *>&1 | Tee-Object -FilePath $out
$code = 0
if (Test-Path variable:LASTEXITCODE) { $code = $LASTEXITCODE }
Write-Output ('VT_EXIT=' + $code)
Write-Output ('UTC_END=' + (Get-Date -AsUTC -Format o))
Add-Content -LiteralPath $out -Value ('VT_EXIT=' + $code)
