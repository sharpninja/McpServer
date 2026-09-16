#Requires -Version 7.0
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Continue'

$out = 'F:\GitHub\McpServer\docs\receipts\_hv-c-red-p15\hv2'
New-Item -ItemType Directory -Force -Path $out | Out-Null

$all = @(Get-CimInstance Win32_Process | Where-Object {
    $_.Name -match 'testhost|vstest|dotnet|pwsh' -and
    $_.CommandLine -match 'PluginIntegration|hv-c-red-p15|hv2-tests'
})
$items = foreach ($p in $all) {
    $cmd = [string]$p.CommandLine
    if ($cmd.Length -gt 400) { $cmd = $cmd.Substring(0, 400) }
    [ordered]@{ ProcessId = $p.ProcessId; Name = $p.Name; CommandLine = $cmd }
}
$pid71784 = $null -ne (Get-Process -Id 71784 -ErrorAction SilentlyContinue)
[ordered]@{
    TimestampUtc = [DateTime]::UtcNow.ToString('o')
    Count = $items.Count
    Pid71784Alive = $pid71784
    Items = $items
} | ConvertTo-Json -Depth 6 | Set-Content -LiteralPath (Join-Path $out 'procs.json') -Encoding utf8
Write-Output ("PROC_COUNT=" + $items.Count)
Write-Output ("PID71784_ALIVE=$pid71784")
