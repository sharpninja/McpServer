#Requires -Version 7.0
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Continue'

$out = 'F:\GitHub\McpServer\docs\receipts\_hv-c-green-p15'
$run2 = Join-Path $out 'run2'

$procs = @(Get-CimInstance Win32_Process | Where-Object {
    $n = [string]$_.Name
    $cmd = [string]$_.CommandLine
    $cmd -match 'PluginIntegration|_hv-c-green-p15' -or $n -match 'testhost'
} | ForEach-Object {
    $cmd = [string]$_.CommandLine
    if ($cmd.Length -gt 500) { $cmd = $cmd.Substring(0, 500) }
    [ordered]@{
        ProcessId = $_.ProcessId
        ParentProcessId = $_.ParentProcessId
        Name = $_.Name
        CreationDate = [string]$_.CreationDate
        CommandLine = $cmd
    }
})

[ordered]@{
    TimestampUtc = [DateTime]::UtcNow.ToString('o')
    Count = $procs.Count
    Processes = $procs
} | ConvertTo-Json -Depth 6 | Set-Content -LiteralPath (Join-Path $run2 'inspect-procs.json') -Encoding utf8

Write-Output ('PROC_COUNT=' + $procs.Count)
foreach ($p in $procs) {
    Write-Output ('PID=' + $p.ProcessId + ' PPID=' + $p.ParentProcessId + ' NAME=' + $p.Name + ' CMD=' + $p.CommandLine)
}

foreach ($name in @('hv-dotnet-p15-filter.log', 'hv-dotnet-pluginint-all.log', 'hv-tests-run.transcript.log')) {
    $p = Join-Path $out $name
    $item = Get-Item -LiteralPath $p -ErrorAction SilentlyContinue
    Write-Output ('FILE=' + $name + ' EXISTS=' + [bool]$item + ' LEN=' + $(if ($item) { $item.Length } else { 0 }) + ' MTIME=' + $(if ($item) { $item.LastWriteTimeUtc.ToString('o') } else { 'missing' }))
}

Write-Output ('LEFTOVER_TRX=' + (Test-Path -LiteralPath (Join-Path $out 'trx-p15-hv\p15-filter.trx')))
Write-Output ('LEFTOVER_FILTER_EXIT=' + (Test-Path -LiteralPath (Join-Path $out 'hv-dotnet-p15-filter-exit.json')))
Write-Output ('LEFTOVER_ALL_LOG=' + (Test-Path -LiteralPath (Join-Path $out 'hv-dotnet-pluginint-all.log')))
Write-Output ('PID86108=' + [bool](Get-Process -Id 86108 -ErrorAction SilentlyContinue))
Write-Output ('WAIT_STARTER_32996=' + [bool](Get-Process -Id 32996 -ErrorAction SilentlyContinue))

$filterLog = Join-Path $out 'hv-dotnet-p15-filter.log'
if (Test-Path -LiteralPath $filterLog) {
    $tail = Get-Content -LiteralPath $filterLog -Tail 15
    Write-Output '--- FILTER TAIL ---'
    $tail | ForEach-Object { Write-Output $_ }
}

$waitLog = Join-Path $run2 'wait-leftover.jsonl'
Write-Output ('WAIT_LOG_EXISTS=' + (Test-Path -LiteralPath $waitLog))
if (Test-Path -LiteralPath $waitLog) {
    Write-Output '--- WAIT LAST ---'
    Get-Content -LiteralPath $waitLog -Tail 3 | ForEach-Object { Write-Output $_ }
}
