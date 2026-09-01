#Requires -Version 7.0
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Continue'
Set-Location 'F:\GitHub\McpServer'
$out = 'F:\GitHub\McpServer\docs\receipts\_hv-wrapup-test-20260818.txt'
Write-Output ("UTC_START=" + (Get-Date -AsUTC -Format o))
& .\build.ps1 Test *>&1 | Tee-Object -FilePath $out
Write-Output ("TEST_EXIT=" + $LASTEXITCODE)
Write-Output ("UTC_END=" + (Get-Date -AsUTC -Format o))
"TEST_EXIT=$LASTEXITCODE" | Add-Content -LiteralPath $out
