#Requires -Version 7.0
$ErrorActionPreference = 'Stop'

$path = 'C:\ProgramData\McpServer\logs\mcp-20260817.log'
$item = Get-Item -LiteralPath $path
Write-Output ('LogLength=' + $item.Length)
Write-Output ('LogUtc=' + $item.LastWriteTimeUtc.ToString('o'))

$fs = [System.IO.File]::Open($path, [System.IO.FileMode]::Open, [System.IO.FileAccess]::Read, [System.IO.FileShare]::ReadWrite)
try {
    $take = [Math]::Min(80000, $fs.Length)
    $fs.Seek(-$take, [System.IO.SeekOrigin]::End) | Out-Null
    $reader = [System.IO.StreamReader]::new($fs)
    $text = $reader.ReadToEnd()
} finally {
    $fs.Dispose()
}

$lines = $text -split "`r?`n"
Write-Output ('TailLines=' + $lines.Count)
$needles = @('storage', 'unreachable', 'backend', 'SQLite', 'Exception', 'fail', 'error', 'started', 'listening', 'Kestrel')
$hits = $lines | Where-Object {
    foreach ($needle in $needles) {
        if ($_ -match $needle) { return $true }
    }
    return $false
} | Select-Object -Last 40
Write-Output '--- matching tail ---'
$hits | ForEach-Object { Write-Output $_ }
