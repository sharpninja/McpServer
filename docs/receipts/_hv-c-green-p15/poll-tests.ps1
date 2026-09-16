#Requires -Version 7.0
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Continue'

$out = 'F:\GitHub\McpServer\docs\receipts\_hv-c-green-p15'
$pidFile = Join-Path $out 'hv-tests-run.pid.json'
$wmiPid = $null
if (Test-Path -LiteralPath $pidFile) {
    $j = Get-Content -LiteralPath $pidFile -Raw | ConvertFrom-Json
    $wmiPid = [int]$j.ProcessId
}
$alive = $false
if ($wmiPid) {
    $alive = $null -ne (Get-Process -Id $wmiPid -ErrorAction SilentlyContinue)
}
$children = @()
if ($wmiPid) {
    $children = @(Get-CimInstance Win32_Process | Where-Object { $_.ParentProcessId -eq $wmiPid -or ($_.CommandLine -and $_.CommandLine -match 'PluginIntegration') } | ForEach-Object {
        $cmd = [string]$_.CommandLine
        if ($cmd.Length -gt 240) { $cmd = $cmd.Substring(0, 240) }
        [ordered]@{ ProcessId = $_.ProcessId; ParentProcessId = $_.ParentProcessId; Name = $_.Name; CommandLine = $cmd }
    })
}
$files = @('hv-tests-run.transcript.log','hv-dotnet-list-tests.log','hv-dotnet-list-tests-exit.json','hv-dotnet-p15-filter.log','hv-dotnet-p15-filter-exit.json','hv-dotnet-pluginint-all.log','hv-dotnet-pluginint-all-exit.json','trx-p15-hv\p15-filter.trx','trx-all-hv\pluginint-all.trx')
$meta = foreach ($f in $files) {
    $p = Join-Path $out $f
    if (Test-Path -LiteralPath $p) {
        $i = Get-Item -LiteralPath $p
        [pscustomobject]@{ Path = $f; Exists = $true; Length = $i.Length; LastWriteTimeUtc = $i.LastWriteTimeUtc.ToString('o') }
    } else {
        [pscustomobject]@{ Path = $f; Exists = $false; Length = 0; LastWriteTimeUtc = $null }
    }
}
$tail = $null
$tlog = Join-Path $out 'hv-tests-run.transcript.log'
if (Test-Path -LiteralPath $tlog) {
    $tail = @(Get-Content -LiteralPath $tlog -Tail 12)
}
[ordered]@{
    TimestampUtc = [DateTime]::UtcNow.ToString('o')
    WmiPid = $wmiPid
    WmiAlive = $alive
    ChildCount = $children.Count
    Children = $children
    Files = $meta
    TranscriptTail = $tail
} | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath (Join-Path $out 'poll-tests.json') -Encoding utf8
Write-Output ("ALIVE=$alive PID=$wmiPid CHILDREN=$($children.Count)")
$meta | ForEach-Object { Write-Output ("FILE exists=$($_.Exists) len=$($_.Length) $($_.Path) $($_.LastWriteTimeUtc)") }
if ($tail) { $tail | ForEach-Object { Write-Output $_ } }
