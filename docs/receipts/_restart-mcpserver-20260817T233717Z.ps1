#Requires -Version 7.0
$ErrorActionPreference = 'Stop'

$before = Get-Service -Name McpServer
$beforeProc = Get-Process -Name 'McpServer.Support.Mcp' -ErrorAction SilentlyContinue
Write-Output ('BEFORE Status=' + $before.Status)
Write-Output ('BEFORE Pid=' + (($beforeProc | Select-Object -ExpandProperty Id) -join ','))

Restart-Service -Name McpServer -Force

$deadline = [DateTime]::UtcNow.AddSeconds(90)
do {
    Start-Sleep -Seconds 2
    $after = Get-Service -Name McpServer
} while ($after.Status -ne 'Running' -and [DateTime]::UtcNow -lt $deadline)

$afterProc = Get-Process -Name 'McpServer.Support.Mcp' -ErrorAction SilentlyContinue
Write-Output ('AFTER Status=' + $after.Status)
Write-Output ('AFTER Pid=' + (($afterProc | Select-Object -ExpandProperty Id) -join ','))
Write-Output ('AFTER StartTimeUtc=' + (($afterProc | ForEach-Object { $_.StartTime.ToUniversalTime().ToString('o') }) -join ','))

if ($after.Status -ne 'Running') {
    throw 'McpServer service did not return to Running within 90 seconds.'
}
