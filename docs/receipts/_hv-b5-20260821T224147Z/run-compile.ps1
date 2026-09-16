$ErrorActionPreference = 'Continue'
$out = 'F:\GitHub\McpServer\docs\receipts\_hv-b5-20260821T224147Z'
Set-Location 'F:\GitHub\McpServer'
pwsh.exe -NoProfile -File .\build.ps1 Compile *>&1 | Tee-Object -FilePath (Join-Path $out 'compile.txt')
Write-Output ('COMPILE_EXIT=' + $LASTEXITCODE)
