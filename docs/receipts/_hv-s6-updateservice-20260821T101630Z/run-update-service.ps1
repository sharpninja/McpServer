#Requires -Version 7.0
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

Set-Location -LiteralPath 'F:\GitHub\McpServer'
$outDir = 'F:\GitHub\McpServer\docs\receipts\_hv-s6-updateservice-20260821T101630Z'
New-Item -ItemType Directory -Force -Path $outDir | Out-Null
$logPath = Join-Path $outDir 'update-service.log'
$exitPath = Join-Path $outDir 'exit-code.txt'

$started = [DateTime]::UtcNow
"STARTED_UTC=$($started.ToString('o'))" | Out-File -FilePath $logPath -Encoding utf8
"HEAD=$(git rev-parse HEAD)" | Out-File -FilePath $logPath -Append -Encoding utf8
"BRANCH=$(git rev-parse --abbrev-ref HEAD)" | Out-File -FilePath $logPath -Append -Encoding utf8

& pwsh.exe -NoLogo -NoProfile -NonInteractive -File '.\build.ps1' UpdateService *>&1 |
    ForEach-Object { $_ | Out-File -FilePath $logPath -Append -Encoding utf8; $_ }
$code = $LASTEXITCODE
if ($null -eq $code) { $code = 0 }
$code | Out-File -FilePath $exitPath -Encoding utf8
"FINISHED_UTC=$([DateTime]::UtcNow.ToString('o'))" | Out-File -FilePath $logPath -Append -Encoding utf8
"EXIT=$code" | Out-File -FilePath $logPath -Append -Encoding utf8
exit $code
