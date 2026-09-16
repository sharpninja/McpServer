#Requires -Version 7.0
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Continue'

$out = 'F:\GitHub\McpServer\docs\receipts\_hv-c-green-p15\run2'

function Get-TargetProcs {
    @(Get-CimInstance Win32_Process | Where-Object {
        $n = [string]$_.Name
        $cmd = [string]$_.CommandLine
        (
            $cmd -match '_hv-c-green-p15\\tests-run\.ps1' -or
            $cmd -match 'trx-all-hv\\pluginint-all\.trx' -or
            ($n -eq 'McpServer.PluginIntegration.Tests.exe') -or
            ($n -eq 'testhost.exe' -and $cmd -match 'McpServer\.PluginIntegration\.Tests') -or
            ($cmd -match 'McpServer\.Support\.Mcp\.dll' -and $cmd -match 'PluginIntegration')
        )
    } | ForEach-Object {
        $cmd = [string]$_.CommandLine
        if ($cmd.Length -gt 500) { $cmd = $cmd.Substring(0, 500) }
        [ordered]@{
            ProcessId = $_.ProcessId
            ParentProcessId = $_.ParentProcessId
            Name = $_.Name
            CommandLine = $cmd
        }
    })
}

$before = Get-TargetProcs
[ordered]@{
    TimestampUtc = [DateTime]::UtcNow.ToString('o')
    Count = $before.Count
    Processes = $before
} | ConvertTo-Json -Depth 6 | Set-Content -LiteralPath (Join-Path $out 'kill-leftover-before.json') -Encoding utf8

Write-Output ('BEFORE_COUNT=' + $before.Count)
$killed = @()
$errors = @()
foreach ($p in $before) {
    Write-Output ('KILL PID=' + $p.ProcessId + ' NAME=' + $p.Name)
    try {
        Stop-Process -Id $p.ProcessId -Force -ErrorAction Stop
        $killed += $p.ProcessId
    } catch {
        $errors += ('PID=' + $p.ProcessId + ' ERR=' + $_.Exception.Message)
        Write-Output ('KILL_FAIL PID=' + $p.ProcessId + ' ' + $_.Exception.Message)
    }
}

Start-Sleep -Seconds 2
$after = Get-TargetProcs
[ordered]@{
    TimestampUtc = [DateTime]::UtcNow.ToString('o')
    BeforeCount = $before.Count
    KilledPids = $killed
    Errors = $errors
    AfterCount = $after.Count
    After = $after
} | ConvertTo-Json -Depth 6 | Set-Content -LiteralPath (Join-Path $out 'kill-leftover-after.json') -Encoding utf8
Write-Output ('AFTER_COUNT=' + $after.Count)
Write-Output ('KILLED=' + ($killed -join ','))
