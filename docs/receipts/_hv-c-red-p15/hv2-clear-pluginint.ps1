#Requires -Version 7.0
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Continue'

$out = 'F:\GitHub\McpServer\docs\receipts\_hv-c-red-p15\hv2'
$rounds = @()
for ($i = 0; $i -lt 6; $i++) {
    $procs = @(Get-CimInstance Win32_Process | Where-Object {
        $_.CommandLine -and (
            $_.CommandLine -match 'McpServer.PluginIntegration.Tests' -or
            $_.CommandLine -match 'mcp-pluginint-'
        ) -and $_.ProcessId -ne 16936
    })
    $killedThis = @()
    foreach ($p in $procs) {
        try {
            Stop-Process -Id $p.ProcessId -Force -ErrorAction Stop
            $killedThis += $p.ProcessId
        } catch {
        }
    }
    $rounds += [ordered]@{
        Round = $i
        Found = $procs.Count
        Killed = $killedThis
    }
    Start-Sleep -Seconds 2
    $remain = @(Get-CimInstance Win32_Process | Where-Object {
        $_.CommandLine -and (
            $_.CommandLine -match 'McpServer.PluginIntegration.Tests' -or
            $_.CommandLine -match 'mcp-pluginint-'
        )
    })
    if ($remain.Count -eq 0) { break }
}
$final = @(Get-CimInstance Win32_Process | Where-Object {
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
    Rounds = $rounds
    FinalCount = $final.Count
    Final = $final
} | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath (Join-Path $out 'clear-pluginint.json') -Encoding utf8
Write-Output ("FINAL=" + $final.Count)
