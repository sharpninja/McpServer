#Requires -Version 7.0
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Continue'

$out = 'F:\GitHub\McpServer\docs\receipts\_hv-c-green-p15'
New-Item -ItemType Directory -Force -Path $out | Out-Null

$procs = @(Get-CimInstance Win32_Process | Where-Object {
    $n = [string]$_.Name
    $cmd = [string]$_.CommandLine
    ($n -match 'testhost|dotnet|VBCSCompiler|pwsh|PluginIntegration') -and (
        $cmd -match 'PluginIntegration' -or
        $cmd -match '_hv-c-green-p15' -or
        $cmd -match '_hv-c-red-p15' -or
        $n -match 'testhost'
    )
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
} | ConvertTo-Json -Depth 6 | Set-Content -LiteralPath (Join-Path $out 'inspect-procs-now.json') -Encoding utf8

Write-Output ('PROC_COUNT=' + $procs.Count)
foreach ($p in $procs) {
    Write-Output ('PID=' + $p.ProcessId + ' PPID=' + $p.ParentProcessId + ' NAME=' + $p.Name + ' CMD=' + $p.CommandLine)
}

$filterLog = Join-Path $out 'hv-dotnet-p15-filter.log'
$filterItem = Get-Item -LiteralPath $filterLog -ErrorAction SilentlyContinue
Write-Output ('FILTER_LOG_LEN=' + $(if ($filterItem) { $filterItem.Length } else { 0 }))
Write-Output ('FILTER_LOG_MTIME=' + $(if ($filterItem) { $filterItem.LastWriteTimeUtc.ToString('o') } else { 'missing' }))
Write-Output ('PID86108_EXISTS=' + [bool](Get-Process -Id 86108 -ErrorAction SilentlyContinue))
Write-Output ('TRX_EXISTS=' + (Test-Path -LiteralPath (Join-Path $out 'trx-p15-hv\p15-filter.trx')))

$py = @(Get-CimInstance Win32_Process | Where-Object { [string]$_.Name -match 'python' } | ForEach-Object {
    $cmd = [string]$_.CommandLine
    if ($cmd.Length -gt 200) { $cmd = $cmd.Substring(0, 200) }
    [ordered]@{ ProcessId = $_.ProcessId; Name = $_.Name; CommandLine = $cmd }
})
[ordered]@{ TimestampUtc = [DateTime]::UtcNow.ToString('o'); Count = $py.Count; Processes = $py } |
    ConvertTo-Json -Depth 6 | Set-Content -LiteralPath (Join-Path $out 'python-procs.json') -Encoding utf8
Write-Output ('PYTHON_COUNT=' + $py.Count)
foreach ($p in $py) {
    Write-Output ('PY PID=' + $p.ProcessId + ' NAME=' + $p.Name + ' CMD=' + $p.CommandLine)
}
