#Requires -Version 7.0
$ErrorActionPreference = 'Stop'

$path = 'C:\ProgramData\McpServer\logs\mcp-20260817.log'
$needles = @(
    'aab0888980690d5c55a8af5c029f0bd1',
    'GrokCode-20260818T001225Z-plugin-session',
    'backend_unavailable',
    'sessionlog_replace_section'
)
$fs = [System.IO.File]::Open($path, [System.IO.FileMode]::Open, [System.IO.FileAccess]::Read, [System.IO.FileShare]::ReadWrite)
$hits = [System.Collections.Generic.List[string]]::new()
try {
    $reader = [System.IO.StreamReader]::new($fs)
    while ($null -ne ($line = $reader.ReadLine())) {
        if (-not ($line.StartsWith('2026-08-17 18:5') -or $line.StartsWith('2026-08-17 19:'))) { continue }
        $keep = $false
        foreach ($needle in $needles) {
            if ($line.Contains($needle)) { $keep = $true; break }
        }
        if ($keep -and ($line.Contains('503') -or $line.Contains('backend_unavailable') -or $line.Contains('aab08889') -or $line.Contains('GrokCode-20260818T001225Z'))) {
            $hits.Add($line.Substring(0, [Math]::Min(450, $line.Length)))
        }
    }
} finally {
    $fs.Dispose()
}

Write-Output ('HitCount=' + $hits.Count)
$hits | Select-Object -Last 25 | ForEach-Object { Write-Output $_ }
