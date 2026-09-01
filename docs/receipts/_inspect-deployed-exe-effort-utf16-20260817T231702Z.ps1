#Requires -Version 7.0
$ErrorActionPreference = 'Stop'

$path = 'C:\ProgramData\McpServer\McpServer.Support.Mcp.exe'
$needles = @('--effort', '--reasoning-effort', 'unknown effort level')
$counts = @{}
foreach ($needle in $needles) { $counts[$needle] = 0 }

$fs = [System.IO.File]::OpenRead($path)
try {
    $chunkSize = 4MB
    $overlap = 128
    $buffer = New-Object byte[] ($chunkSize + $overlap)
    $offset = 0
    $carry = New-Object byte[] 0
    while ($true) {
        if ($carry.Length -gt 0) {
            [Array]::Copy($carry, $buffer, $carry.Length)
        }
        $read = $fs.Read($buffer, $carry.Length, $chunkSize)
        if ($read -le 0 -and $carry.Length -eq 0) { break }
        $length = $carry.Length + $read
        $text = [System.Text.Encoding]::Unicode.GetString($buffer, 0, $length)
        foreach ($needle in $needles) {
            $counts[$needle] += [regex]::Matches($text, [regex]::Escape($needle)).Count
        }
        if ($read -le 0) { break }
        $keep = [Math]::Min($overlap, $length)
        $carry = New-Object byte[] $keep
        [Array]::Copy($buffer, $length - $keep, $carry, 0, $keep)
        $offset += $read
    }
} finally {
    $fs.Dispose()
}

foreach ($needle in $needles) {
    Write-Output ($needle + ' utf16Hits=' + $counts[$needle])
}
