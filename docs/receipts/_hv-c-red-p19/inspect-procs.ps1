#Requires -Version 7.0
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Continue'
$out = 'F:\GitHub\McpServer\docs\receipts\_hv-c-red-p19'
$busy = @(Get-CimInstance Win32_Process -ErrorAction SilentlyContinue | Where-Object {
    $_.Name -match 'dotnet|testhost|vstest' -and $_.CommandLine -match 'PluginIntegration|PluginNativeSuite|McpServer.PluginIntegration'
} | ForEach-Object {
    [ordered]@{
        Pid = $_.ProcessId
        ParentPid = $_.ParentProcessId
        Name = $_.Name
        CreationDate = $_.CreationDate
        CommandLine = $_.CommandLine
    }
})
[ordered]@{
    TimestampUtc = [DateTime]::UtcNow.ToString('o')
    BusyCount = $busy.Count
    Processes = $busy
} | ConvertTo-Json -Depth 6 | Set-Content -LiteralPath (Join-Path $out 'inspect-procs.json') -Encoding utf8
Write-Output ('BUSY=' + $busy.Count)
$busy | ForEach-Object { Write-Output ("PID=$($_.Pid) NAME=$($_.Name) PARENT=$($_.ParentPid)") }
