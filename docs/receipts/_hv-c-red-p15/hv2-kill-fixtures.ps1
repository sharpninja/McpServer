#Requires -Version 7.0
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Continue'

$out = 'F:\GitHub\McpServer\docs\receipts\_hv-c-red-p15\hv2'
$killed = @()
$kept = @()
$all = @(Get-CimInstance Win32_Process | Where-Object {
    $_.CommandLine -and (
        $_.CommandLine -match 'mcp-pluginint-' -or
        ($_.Name -eq 'dotnet.exe' -and $_.CommandLine -match 'PluginIntegration.Tests\\bin\\Debug\\net10.0\\McpServer.Support.Mcp.dll') -or
        ($_.Name -match 'testhost' -and $_.CommandLine -match 'PluginIntegration') -or
        $_.CommandLine -match 'hv2-rerun-filter'
    )
})
foreach ($p in $all) {
    $cmd = [string]$p.CommandLine
    if ($cmd.Length -gt 350) { $cmd = $cmd.Substring(0, 350) }
    $item = [ordered]@{ ProcessId = $p.ProcessId; Name = $p.Name; CommandLine = $cmd }
    if ($p.ProcessId -eq 16936) { $kept += $item; continue }
    try {
        Stop-Process -Id $p.ProcessId -Force -ErrorAction Stop
        $item.Killed = $true
        $killed += $item
    } catch {
        $item.Killed = $false
        $item.Error = $_.Exception.Message
        $kept += $item
    }
}
Start-Sleep -Seconds 3
$after = @(Get-CimInstance Win32_Process | Where-Object {
    $_.CommandLine -and (
        $_.CommandLine -match 'mcp-pluginint-' -or
        ($_.Name -eq 'dotnet.exe' -and $_.CommandLine -match 'PluginIntegration.Tests\\bin\\Debug')
    )
})
[ordered]@{
    TimestampUtc = [DateTime]::UtcNow.ToString('o')
    KilledCount = $killed.Count
    Killed = $killed
    Kept = $kept
    AfterCount = $after.Count
} | ConvertTo-Json -Depth 6 | Set-Content -LiteralPath (Join-Path $out 'kill-fixtures.json') -Encoding utf8
Write-Output ("KILLED=" + $killed.Count + " AFTER=" + $after.Count)
