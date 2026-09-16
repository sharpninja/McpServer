#Requires -Version 7.0
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$script = 'F:\GitHub\McpServer\docs\receipts\_hv-c-green-p15\run2\wait-then-wmi-tests.ps1'
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
} | ConvertTo-Json | Set-Content -LiteralPath 'F:\GitHub\McpServer\docs\receipts\_hv-c-green-p15\run2\wait-wmi-starter.json' -Encoding utf8
Write-Output ('STARTER_PID=' + $result.ProcessId)
Write-Output ('RETURN=' + $result.ReturnValue)
if ([int]$result.ReturnValue -ne 0) { exit 1 }
