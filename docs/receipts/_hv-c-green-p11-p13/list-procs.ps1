#Requires -Version 7.0
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Continue'

$rows = @()
foreach ($p in Get-CimInstance Win32_Process) {
    if ($p.Name -notmatch 'dotnet|testhost|McpServer.Support.Mcp|pwsh') { continue }
    $cmd = [string]$p.CommandLine
    if ($cmd.Length -gt 300) { $cmd = $cmd.Substring(0, 300) }
    $keep = $false
    if ($p.Name -match 'dotnet|testhost|McpServer') { $keep = $true }
    if ($cmd -match 'PluginIntegration|tests\.ps1|_hv-c-green-p11-p13') { $keep = $true }
    if (-not $keep) { continue }
    $rows += [ordered]@{
        Pid = $p.ProcessId
        Name = $p.Name
        Created = [string]$p.CreationDate
        Cmd = $cmd
    }
}
$rows | ConvertTo-Json -Depth 4 | Set-Content -LiteralPath 'F:\GitHub\McpServer\docs\receipts\_hv-c-green-p11-p13\dotnet-procs.json' -Encoding utf8
Write-Output ("PROC_COUNT=" + @($rows).Count)
