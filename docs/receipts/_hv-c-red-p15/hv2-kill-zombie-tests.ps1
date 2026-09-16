#Requires -Version 7.0
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Continue'

$out = 'F:\GitHub\McpServer\docs\receipts\_hv-c-red-p15\hv2'
$killed = @()
foreach ($id in @(57188, 77404)) {
    $proc = Get-CimInstance Win32_Process -Filter ("ProcessId=" + $id) -ErrorAction SilentlyContinue
    if ($null -eq $proc) {
        $killed += [ordered]@{ ProcessId = $id; Found = $false }
        continue
    }
    $cmd = [string]$proc.CommandLine
    $item = [ordered]@{ ProcessId = $id; Found = $true; Name = $proc.Name; CommandLine = $cmd }
    if ($cmd -match 'TCS2' -or $id -eq 16936) {
        $item.Killed = $false
        $item.Reason = 'protected'
        $killed += $item
        continue
    }
    try {
        Stop-Process -Id $id -Force -ErrorAction Stop
        $item.Killed = $true
    } catch {
        $item.Killed = $false
        $item.Error = $_.Exception.Message
    }
    $killed += $item
}
Start-Sleep -Seconds 5
$after = @(Get-CimInstance Win32_Process | Where-Object {
    $_.CommandLine -and (
        $_.CommandLine -match 'McpServer.PluginIntegration.Tests' -or
        $_.CommandLine -match 'mcp-pluginint-'
    )
} | ForEach-Object {
    $cmd = [string]$_.CommandLine
    if ($cmd.Length -gt 300) { $cmd = $cmd.Substring(0, 300) }
    [ordered]@{ ProcessId = $_.ProcessId; Name = $_.Name; CommandLine = $cmd }
})
[ordered]@{
    TimestampUtc = [DateTime]::UtcNow.ToString('o')
    Actions = $killed
    AfterCount = $after.Count
    After = $after
} | ConvertTo-Json -Depth 6 | Set-Content -LiteralPath (Join-Path $out 'kill-zombie.json') -Encoding utf8
Write-Output ("AFTER=" + $after.Count)
