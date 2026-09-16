#Requires -Version 7.0
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Continue'
$out = 'F:\GitHub\McpServer\docs\receipts\_hv-c-green-p15\full-rerun'
New-Item -ItemType Directory -Force -Path $out | Out-Null
$procs = @(Get-CimInstance Win32_Process | Where-Object {
    $n = [string]$_.Name
    $cmd = [string]$_.CommandLine
    (
        $cmd -match 'PluginIntegration' -or
        $cmd -match '_hv-c-green-p15' -or
        ($n -eq 'testhost.exe')
    )
} | ForEach-Object {
    $cmd = [string]$_.CommandLine
    if ($cmd.Length -gt 320) { $cmd = $cmd.Substring(0, 320) }
    [ordered]@{ ProcessId = $_.ProcessId; ParentProcessId = $_.ParentProcessId; Name = $_.Name; CommandLine = $cmd }
})
$n = @($procs).Count
[ordered]@{
    TimestampUtc = [DateTime]::UtcNow.ToString('o')
    Count = $n
    Processes = $procs
} | ConvertTo-Json -Depth 6 | Set-Content -LiteralPath (Join-Path $out 'inspect-now.json') -Encoding utf8
Write-Output ("COUNT=$n")
foreach ($p in $procs) { Write-Output ("PID=$($p.ProcessId) NAME=$($p.Name) CMD=$($p.CommandLine)") }
