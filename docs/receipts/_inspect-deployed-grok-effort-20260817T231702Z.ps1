#Requires -Version 7.0
$ErrorActionPreference = 'Stop'

$path = 'C:\ProgramData\McpServer\McpServer.Services.dll'
$bytes = [System.IO.File]::ReadAllBytes($path)
$utf16 = [System.Text.Encoding]::Unicode.GetString($bytes)
$ascii = [System.Text.Encoding]::ASCII.GetString($bytes)

Write-Output '--- ASCII effort flags ---'
[regex]::Matches($ascii, '--(?:reasoning-)?effort') | ForEach-Object { $_.Value } | Select-Object -Unique

Write-Output '--- ASCII nearby tokens ---'
[regex]::Matches($ascii, 'HighestEffort|reasoning-effort|--effort|unknown effort level') | ForEach-Object { $_.Value } | Select-Object -Unique

Write-Output '--- UTF16 nearby tokens ---'
[regex]::Matches($utf16, 'HighestEffort|reasoning-effort|--effort|unknown effort level') | ForEach-Object { $_.Value } | Select-Object -Unique

Write-Output '--- ASCII quoted effort values around flags ---'
[regex]::Matches($ascii, '(?:--effort|--reasoning-effort).{0,12}') | ForEach-Object { $_.Value } | Select-Object -First 20
