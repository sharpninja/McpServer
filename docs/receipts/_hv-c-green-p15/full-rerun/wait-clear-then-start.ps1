#Requires -Version 7.0
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Continue'

$out = 'F:\GitHub\McpServer\docs\receipts\_hv-c-green-p15\full-rerun'
New-Item -ItemType Directory -Force -Path $out | Out-Null
$deadline = [DateTime]::UtcNow.AddMinutes(18)

function Get-PluginIntProcs {
    @(Get-CimInstance Win32_Process | Where-Object {
        $n = [string]$_.Name
        $cmd = [string]$_.CommandLine
        (
            ($n -eq 'testhost.exe' -and $cmd -match 'McpServer\.PluginIntegration\.Tests') -or
            ($n -eq 'McpServer.PluginIntegration.Tests.exe') -or
            ($cmd -match 'tests/McpServer.PluginIntegration.Tests' -and $n -eq 'dotnet.exe') -or
            ($cmd -match 'McpServer\.Support\.Mcp\.dll' -and $cmd -match 'PluginIntegration') -or
            ($cmd -match '_hv-c-green-p15\\run2\\tests-run\.ps1')
        )
    } | ForEach-Object {
        $cmd = [string]$_.CommandLine
        if ($cmd.Length -gt 300) { $cmd = $cmd.Substring(0, 300) }
        [ordered]@{ ProcessId = $_.ProcessId; Name = $_.Name; CommandLine = $cmd }
    })
}

while ([DateTime]::UtcNow -lt $deadline) {
    $procs = Get-PluginIntProcs
    [ordered]@{
        TimestampUtc = [DateTime]::UtcNow.ToString('o')
        Count = $procs.Count
        Processes = $procs
    } | ConvertTo-Json -Depth 6 | Set-Content -LiteralPath (Join-Path $out 'clear-poll.json') -Encoding utf8
    Write-Output ("CLEAR_POLL count=$($procs.Count) now=" + [DateTime]::UtcNow.ToString('o'))
    if ($procs.Count -eq 0) {
        Write-Output 'CLEAR'
        break
    }
    Start-Sleep -Seconds 15
}

$procs = Get-PluginIntProcs
if ($procs.Count -ne 0) {
    Write-Output 'NOT_CLEAR'
    exit 2
}

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
} | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $out 'hv-tests-run.pid.json') -Encoding utf8
Write-Output ('WMI_RETURN=' + $result.ReturnValue)
Write-Output ('WMI_PID=' + $result.ProcessId)
if ([int]$result.ReturnValue -ne 0) { exit 1 }
