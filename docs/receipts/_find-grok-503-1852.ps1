#Requires -Version 7.0
$ErrorActionPreference = 'Stop'

$path = 'C:\ProgramData\McpServer\logs\mcp-20260817.log'
$fs = [System.IO.File]::Open($path, [System.IO.FileMode]::Open, [System.IO.FileAccess]::Read, [System.IO.FileShare]::ReadWrite)
$hits = [System.Collections.Generic.List[string]]::new()
try {
    $reader = [System.IO.StreamReader]::new($fs)
    while ($null -ne ($line = $reader.ReadLine())) {
        if (-not $line.StartsWith('2026-08-17 18:5')) { continue }
        if ($line.Contains('503') -or $line.Contains('backend_unavailable') -or $line.Contains('aab08889') -or $line.Contains('storage')) {
            $hits.Add($line.Substring(0, [Math]::Min(400, $line.Length)))
        }
    }
} finally {
    $fs.Dispose()
}

Write-Output ('HitCount=' + $hits.Count)
$hits | Select-Object -First 20 | ForEach-Object { Write-Output $_ }
