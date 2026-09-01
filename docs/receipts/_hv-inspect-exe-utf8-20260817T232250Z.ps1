#Requires -Version 7.0
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
$path = 'C:\ProgramData\McpServer\McpServer.Support.Mcp.exe'
$needles = @('HighestEffort', 'GrokHighestEffort', '--effort', '--reasoning-effort')
$counts = @{}
foreach ($n in $needles) { $counts[$n] = 0 }
$fs = [System.IO.File]::OpenRead($path)
try {
    $chunkSize = 4MB
    $overlap = 256
    $buffer = New-Object byte[] ($chunkSize + $overlap)
    $carry = New-Object byte[] 0
    while ($true) {
        if ($carry.Length -gt 0) { [Array]::Copy($carry, $buffer, $carry.Length) }
        $read = $fs.Read($buffer, $carry.Length, $chunkSize)
        if ($read -le 0 -and $carry.Length -eq 0) { break }
        $length = $carry.Length + $read
        $text = [System.Text.Encoding]::UTF8.GetString($buffer, 0, $length)
        foreach ($needle in $needles) {
            $counts[$needle] += [regex]::Matches($text, [regex]::Escape($needle)).Count
        }
        if ($read -le 0) { break }
        $keep = [Math]::Min($overlap, $length)
        $carry = New-Object byte[] $keep
        [Array]::Copy($buffer, $length - $keep, $carry, 0, $keep)
    }
} finally { $fs.Dispose() }
foreach ($n in $needles) { Write-Output ("UTF8 {0} hits={1}" -f $n, $counts[$n]) }
