#Requires -Version 7.0
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$out = 'F:\GitHub\McpServer\docs\receipts\_hv-c-green-p15'
New-Item -ItemType Directory -Force -Path $out | Out-Null
$script = Join-Path $out 'tests-run.ps1'
$pwsh = (Get-Command pwsh.exe).Source
$cmd = '"' + $pwsh + '" -NoProfile -NonInteractive -File "' + $script + '"'
$result = Invoke-CimMethod -ClassName Win32_Process -MethodName Create -Arguments @{
    CommandLine = $cmd
    CurrentDirectory = 'F:\GitHub\McpServer'
}
[ordered]@{
    TimestampUtc = [DateTime]::UtcNow.ToString('o')
    ReturnValue = $result.ReturnValue
    ProcessId = $result.ProcessId
    CommandLine = $cmd
    Pwsh = $pwsh
} | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $out 'hv-tests-run.pid.json') -Encoding utf8
Write-Output ('WMI_RETURN=' + $result.ReturnValue)
Write-Output ('WMI_PID=' + $result.ProcessId)
Write-Output ('PWSH=' + $pwsh)
if ([int]$result.ReturnValue -ne 0) { exit 1 }
