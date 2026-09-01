#Requires -Version 7.0
$ErrorActionPreference = 'Stop'

$path = 'C:\ProgramData\McpServer\logs\mcp-20260817.log'
$fs = [System.IO.File]::Open($path, [System.IO.FileMode]::Open, [System.IO.FileAccess]::Read, [System.IO.FileShare]::ReadWrite)
$emit = 0
try {
    $reader = [System.IO.StreamReader]::new($fs)
    while ($null -ne ($line = $reader.ReadLine())) {
        if ($line.Contains('00-aab0888980690d5c55a8af5c029f0bd1')) {
            Write-Output $line.Substring(0, [Math]::Min(500, $line.Length))
            $emit = 15
            continue
        }
        if ($emit -gt 0) {
            Write-Output $line.Substring(0, [Math]::Min(500, $line.Length))
            $emit--
        }
    }
} finally {
    $fs.Dispose()
}
