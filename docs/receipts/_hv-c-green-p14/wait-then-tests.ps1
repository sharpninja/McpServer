#Requires -Version 7.0
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Continue'

$out = 'F:\GitHub\McpServer\docs\receipts\_hv-c-green-p14'
New-Item -ItemType Directory -Force -Path $out | Out-Null

function Get-PluginIntProcs {
    @(Get-CimInstance Win32_Process | Where-Object {
        $_.Name -match 'testhost|vstest|dotnet' -and
        $null -ne $_.CommandLine -and
        $_.CommandLine -match 'PluginIntegration|McpServer.PluginIntegration'
    })
}

$deadline = [DateTime]::UtcNow.AddMinutes(25)
$snapshots = @()
do {
    $rows = Get-PluginIntProcs
    $snap = [ordered]@{
        TimestampUtc = [DateTime]::UtcNow.ToString('o')
        Count = $rows.Count
        ProcessIds = @($rows | ForEach-Object { $_.ProcessId })
        CommandHints = @($rows | ForEach-Object {
            $cmd = [string]$_.CommandLine
            if ($cmd.Length -gt 160) { $cmd = $cmd.Substring(0, 160) }
            $cmd
        })
    }
    $snapshots += $snap
    Write-Output ("WAIT_COUNT=" + $rows.Count + " UTC=" + $snap.TimestampUtc)
    if ($rows.Count -eq 0) { break }
    Start-Sleep -Seconds 20
} while ([DateTime]::UtcNow -lt $deadline)

$final = Get-PluginIntProcs
[ordered]@{
    RemainingCount = $final.Count
    RemainingPids = @($final | ForEach-Object { $_.ProcessId })
    SnapshotCount = $snapshots.Count
    Snapshots = $snapshots
    TimedOut = ($final.Count -gt 0)
    FinishedUtc = [DateTime]::UtcNow.ToString('o')
} | ConvertTo-Json -Depth 6 | Set-Content -LiteralPath (Join-Path $out 'wait-p14-hosts.json') -Encoding utf8

if ($final.Count -gt 0) {
    Write-Output 'WAIT_TIMEOUT_REMAINING_PROCS'
}

Write-Output 'START_TESTS_AFTER_WAIT'
& pwsh.exe -NoProfile -NonInteractive -File 'F:\GitHub\McpServer\docs\receipts\_hv-c-green-p14\tests.ps1'
Write-Output ("TESTS_SCRIPT_EXIT=" + $LASTEXITCODE)
