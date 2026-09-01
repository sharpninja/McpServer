#Requires -Version 7.0
$ErrorActionPreference = 'Stop'

$path = 'C:\ProgramData\McpServer\logs\mcp-20260818.log'
if (-not (Test-Path -LiteralPath $path)) {
    $path = 'C:\ProgramData\McpServer\logs\mcp-20260817.log'
}

Write-Output ('LogPath=' + $path)
$item = Get-Item -LiteralPath $path
Write-Output ('LogLength=' + $item.Length)
Write-Output ('LogUtc=' + $item.LastWriteTimeUtc.ToString('o'))

$fs = [System.IO.File]::Open($path, [System.IO.FileMode]::Open, [System.IO.FileAccess]::Read, [System.IO.FileShare]::ReadWrite)
try {
    $take = [Math]::Min(200000, $fs.Length)
    $fs.Seek(-$take, [System.IO.SeekOrigin]::End) | Out-Null
    $reader = [System.IO.StreamReader]::new($fs)
    $text = $reader.ReadToEnd()
} finally {
    $fs.Dispose()
}

$needles = @(
    'help-20260818001213',
    'grok-4.5',
    '--effort',
    'GrokCli',
    'agent-help',
    'Agent Help'
)
$lines = $text -split "`r?`n"
$hits = $lines | Where-Object {
    foreach ($needle in $needles) {
        if ($_.Contains($needle)) { return $true }
    }
    return $false
} | Select-Object -Last 30

Write-Output ('HitCount=' + @($hits).Count)
$hits | ForEach-Object { Write-Output $_ }
