#Requires -Version 7.0
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Continue'

$out = 'F:\GitHub\McpServer\docs\receipts\_hv-c-red-p15\hv2'
$killed = @()
$kept = @()
$candidates = @(Get-CimInstance Win32_Process | Where-Object {
    $_.CommandLine -and (
        $_.CommandLine -match 'hv2-wait-then-tests|hv2-tests.ps1' -or
        ($_.Name -match 'testhost|vstest' -and $_.CommandLine -match 'PluginIntegration') -or
        ($_.Name -eq 'dotnet.exe' -and $_.CommandLine -match 'PluginIntegration.Tests' -and $_.CommandLine -match 'dotnet.exe\" test ') -or
        ($_.Name -eq 'dotnet.exe' -and $_.CommandLine -match 'mcp-pluginint-')
    )
})
foreach ($p in $candidates) {
    $cmd = [string]$p.CommandLine
    if ($cmd.Length -gt 300) { $cmd = $cmd.Substring(0, 300) }
    $item = [ordered]@{ ProcessId = $p.ProcessId; Name = $p.Name; CommandLine = $cmd }
    if ($p.ProcessId -eq 16936) {
        $kept += $item
        continue
    }
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
[ordered]@{
    TimestampUtc = [DateTime]::UtcNow.ToString('o')
    KilledCount = $killed.Count
    KeptCount = $kept.Count
    Killed = $killed
    Kept = $kept
} | ConvertTo-Json -Depth 6 | Set-Content -LiteralPath (Join-Path $out 'kill-hung.json') -Encoding utf8
Write-Output ("KILLED=" + $killed.Count + " KEPT=" + $kept.Count)
