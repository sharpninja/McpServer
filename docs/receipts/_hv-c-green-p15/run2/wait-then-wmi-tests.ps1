#Requires -Version 7.0
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Continue'

$out = 'F:\GitHub\McpServer\docs\receipts\_hv-c-green-p15\run2'
New-Item -ItemType Directory -Force -Path $out | Out-Null

function Get-PluginIntProcs {
    @(Get-CimInstance Win32_Process | Where-Object {
        $n = [string]$_.Name
        $cmd = [string]$_.CommandLine
        (
            $cmd -match 'McpServer\.PluginIntegration' -or
            $cmd -match '_hv-c-green-p15\\tests-run' -or
            ($n -eq 'testhost.exe' -and $cmd -match 'PluginIntegration') -or
            ($n -eq 'McpServer.PluginIntegration.Tests.exe')
        )
    } | ForEach-Object {
        $cmd = [string]$_.CommandLine
        if ($cmd.Length -gt 400) { $cmd = $cmd.Substring(0, 400) }
        [ordered]@{
            ProcessId = $_.ProcessId
            ParentProcessId = $_.ParentProcessId
            Name = $_.Name
            CommandLine = $cmd
        }
    })
}

$deadline = [DateTime]::UtcNow.AddMinutes(12)
$waitLog = Join-Path $out 'wait-leftover.jsonl'
if (Test-Path -LiteralPath $waitLog) { Remove-Item -LiteralPath $waitLog -Force }
$started = [DateTime]::UtcNow
do {
    $procs = Get-PluginIntProcs
    $row = [ordered]@{
        TimestampUtc = [DateTime]::UtcNow.ToString('o')
        Count = $procs.Count
        Pids = @($procs | ForEach-Object { $_.ProcessId })
        Names = @($procs | ForEach-Object { $_.Name })
    }
    Add-Content -LiteralPath $waitLog -Value ($row | ConvertTo-Json -Compress) -Encoding utf8
    Write-Output ('WAIT_COUNT=' + $procs.Count + ' PIDS=' + ($row.Pids -join ','))
    if ($procs.Count -eq 0) { break }
    Start-Sleep -Seconds 15
} while ([DateTime]::UtcNow -lt $deadline)

$final = Get-PluginIntProcs
[ordered]@{
    TimestampUtc = [DateTime]::UtcNow.ToString('o')
    WaitedMs = [int]([DateTime]::UtcNow - $started).TotalMilliseconds
    RemainingCount = $final.Count
    Remaining = $final
    Drained = ($final.Count -eq 0)
} | ConvertTo-Json -Depth 6 | Set-Content -LiteralPath (Join-Path $out 'wait-leftover-final.json') -Encoding utf8

if ($final.Count -gt 0) {
    Write-Output 'LEFTOVER_STILL_RUNNING'
    Write-Output 'NOT_STARTING_INDEPENDENT_FILTER_WHILE_CONCURRENT_PLUGININT_EXISTS'
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
    Pwsh = $pwsh
} | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $out 'hv-tests-run.pid.json') -Encoding utf8
Write-Output ('WMI_RETURN=' + $result.ReturnValue)
Write-Output ('WMI_PID=' + $result.ProcessId)
if ([int]$result.ReturnValue -ne 0) { exit 1 }
Write-Output 'WMI_STARTED'
