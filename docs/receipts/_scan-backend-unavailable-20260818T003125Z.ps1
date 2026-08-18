#Requires -Version 7.0
$ErrorActionPreference = 'Stop'

$candidates = @(
    'C:\ProgramData\McpServer\logs\mcp-20260818.log',
    'C:\ProgramData\McpServer\logs\mcp-20260817.log'
)
$path = $candidates | Where-Object { Test-Path -LiteralPath $_ } | Select-Object -First 1
Write-Output ('LogPath=' + $path)
$item = Get-Item -LiteralPath $path
Write-Output ('LogLength=' + $item.Length)
Write-Output ('LogUtc=' + $item.LastWriteTimeUtc.ToString('o'))

$fs = [System.IO.File]::Open($path, [System.IO.FileMode]::Open, [System.IO.FileAccess]::Read, [System.IO.FileShare]::ReadWrite)
try {
    $take = [Math]::Min(400000, $fs.Length)
    $fs.Seek(-$take, [System.IO.SeekOrigin]::End) | Out-Null
    $reader = [System.IO.StreamReader]::new($fs)
    $text = $reader.ReadToEnd()
} finally {
    $fs.Dispose()
}

$needles = @(
    'backend_unavailable',
    'storage backend',
    'Storage connectivity',
    'unreachable',
    'database is locked',
    'SQLite Error',
    'sessionlog',
    'SessionLog'
)
$lines = $text -split "`r?`n"
Write-Output ('TailLineCount=' + $lines.Count)

$hits = $lines | Where-Object {
    $_ -match 'backend_unavailable|storage backend|Storage connectivity|database is locked|SQLite Error|503'
}
Write-Output ('ErrorHitCount=' + @($hits).Count)
$hits | Select-Object -Last 40 | ForEach-Object { Write-Output $_ }

Write-Output '--- recent sessionlog status lines ---'
$sessionHits = $lines | Where-Object { $_ -match 'sessionlog|SessionLog' -and $_ -match 'completed with' }
$sessionHits | Select-Object -Last 20 | ForEach-Object { Write-Output $_ }
