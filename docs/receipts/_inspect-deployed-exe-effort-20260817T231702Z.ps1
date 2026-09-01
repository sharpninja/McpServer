#Requires -Version 7.0
$ErrorActionPreference = 'Stop'

$path = 'C:\ProgramData\McpServer\McpServer.Support.Mcp.exe'
$bytes = [System.IO.File]::ReadAllBytes($path)
$ascii = [System.Text.Encoding]::ASCII.GetString($bytes)

Write-Output ("ExeLength=" + $bytes.Length)
Write-Output '--- unique effort flags ---'
[regex]::Matches($ascii, '--(?:reasoning-)?effort') | ForEach-Object { $_.Value } | Select-Object -Unique
Write-Output '--- unknown effort level snippets ---'
[regex]::Matches($ascii, 'unknown effort level.{0,40}') | ForEach-Object { $_.Value } | Select-Object -First 10
Write-Output '--- HighestEffort / GrokHighestEffort ---'
[regex]::Matches($ascii, 'HighestEffort|GrokHighestEffort') | ForEach-Object { $_.Value } | Select-Object -Unique
Write-Output '--- context windows around --effort ---'
$matches = [regex]::Matches($ascii, '.{20}--effort.{20}')
$matches | Select-Object -First 8 | ForEach-Object { $_.Value -replace '[^\x20-\x7E]', '.' }
Write-Output '--- context windows around --reasoning-effort ---'
$matches2 = [regex]::Matches($ascii, '.{16}--reasoning-effort.{16}')
$matches2 | Select-Object -First 8 | ForEach-Object { $_.Value -replace '[^\x20-\x7E]', '.' }
