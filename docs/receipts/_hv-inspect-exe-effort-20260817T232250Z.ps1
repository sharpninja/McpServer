#Requires -Version 7.0
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$path = 'C:\ProgramData\McpServer\McpServer.Support.Mcp.exe'
$needles = @(
    '--effort',
    '--reasoning-effort',
    'HighestEffort',
    'GrokHighestEffort'
)
$counts = @{}
$contexts = @{}
foreach ($n in $needles) {
    $counts[$n] = 0
    $contexts[$n] = [System.Collections.Generic.List[string]]::new()
}

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
        $text = [System.Text.Encoding]::Unicode.GetString($buffer, 0, $length)
        foreach ($needle in $needles) {
            $ms = [regex]::Matches($text, [regex]::Escape($needle))
            $counts[$needle] += $ms.Count
            foreach ($m in $ms) {
                if ($contexts[$needle].Count -ge 3) { continue }
                $start = $m.Index
                $take = [Math]::Min(48, $text.Length - $start)
                $slice = $text.Substring($start, $take) -replace '[\u0000-\u001f]', '.'
                $contexts[$needle].Add($slice)
            }
        }
        if ($read -le 0) { break }
        $keep = [Math]::Min($overlap, $length)
        $carry = New-Object byte[] $keep
        [Array]::Copy($buffer, $length - $keep, $carry, 0, $keep)
    }
} finally {
    $fs.Dispose()
}

$item = Get-Item -LiteralPath $path
Write-Output ("ExeLength=" + $item.Length)
Write-Output ("ExeLastWriteUtc=" + $item.LastWriteTimeUtc.ToString('o'))
foreach ($n in $needles) {
    Write-Output ("UTF16 {0} hits={1}" -f $n, $counts[$n])
    foreach ($c in $contexts[$n]) {
        Write-Output ("CTX {0}: {1}" -f $n, $c)
    }
}
Write-Output 'EXE_SCAN_DONE'
