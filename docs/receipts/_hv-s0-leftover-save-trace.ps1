#Requires -Version 7.0
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
Set-Location -LiteralPath 'F:\GitHub\McpServer'
$out = 'F:\GitHub\McpServer\docs\receipts\_hv-s0-leftover\validate-traceability.txt'
& ./build.ps1 ValidateTraceability *>&1 | Out-File -FilePath $out -Encoding utf8
$exit = $LASTEXITCODE
Add-Content -LiteralPath $out -Value ('EXIT=' + $exit) -Encoding utf8
Write-Output ('TRACE_EXIT=' + $exit)
