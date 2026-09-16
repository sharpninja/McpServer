#Requires -Version 7.0
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Continue'

$out = 'F:\GitHub\McpServer\docs\receipts\_hv-c-green-p15\run2'
New-Item -ItemType Directory -Force -Path $out | Out-Null

$waiter = Get-Process -Id 32996 -ErrorAction SilentlyContinue
if ($waiter) {
    Write-Output 'KILL_HUNG_WAITER_32996'
    Stop-Process -Id 32996 -Force -ErrorAction Continue
}

$still = @(Get-CimInstance Win32_Process | Where-Object {
    $n = [string]$_.Name
    $cmd = [string]$_.CommandLine
    $n -eq 'McpServer.PluginIntegration.Tests.exe' -or
    ($n -eq 'testhost.exe' -and $cmd -match 'PluginIntegration') -or
    $cmd -match 'trx-all-hv' -or
    $cmd -match '_hv-c-green-p15\\tests-run\.ps1'
})
Write-Output ('LEFTOVER_PLUGININT=' + $still.Count)

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
    HungWaiterKilled = $true
    LeftoverPluginIntAfterKillWaiter = $still.Count
} | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $out 'hv-tests-run.pid.json') -Encoding utf8
Write-Output ('WMI_RETURN=' + $result.ReturnValue)
Write-Output ('WMI_PID=' + $result.ProcessId)
if ([int]$result.ReturnValue -ne 0) { exit 1 }
