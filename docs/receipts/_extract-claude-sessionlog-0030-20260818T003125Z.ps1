#Requires -Version 7.0
$ErrorActionPreference = 'Stop'

$path = 'C:\ProgramData\McpServer\logs\mcp-20260817.log'
$needles = @(
    'ClaudeCode-20260818T001231Z-plugin-session',
    'req-20260818T003048Z-prompt-3fc0',
    'planFile is omitted',
    'backend_unavailable'
)
$fs = [System.IO.File]::Open($path, [System.IO.FileMode]::Open, [System.IO.FileAccess]::Read, [System.IO.FileShare]::ReadWrite)
$hits = [System.Collections.Generic.List[string]]::new()
try {
    $reader = [System.IO.StreamReader]::new($fs)
    while ($null -ne ($line = $reader.ReadLine())) {
        if (-not ($line.StartsWith('2026-08-17 19:3'))) { continue }
        $keep = $false
        foreach ($needle in $needles) {
            if ($line.Contains($needle)) { $keep = $true; break }
        }
        if ($keep) {
            $hits.Add($line.Substring(0, [Math]::Min(500, $line.Length)))
        }
    }
} finally {
    $fs.Dispose()
}

Write-Output ('HitCount=' + $hits.Count)
$hits | Select-Object -Last 30 | ForEach-Object { Write-Output $_ }
