#Requires -Version 7.0
$ErrorActionPreference = 'Stop'

$path = 'C:\ProgramData\McpServer\logs\mcp-20260817.log'
$cutoff = [DateTimeOffset]::Parse('2026-08-17T18:38:00-05:00')
$fs = [System.IO.File]::Open($path, [System.IO.FileMode]::Open, [System.IO.FileAccess]::Read, [System.IO.FileShare]::ReadWrite)
$backend = 0
$planFileOmitted = 0
$sessionlogUnhandled = 0
$transport503 = 0
$recentBackend = [System.Collections.Generic.List[string]]::new()
$recentUnhandled = [System.Collections.Generic.List[string]]::new()
try {
    $reader = [System.IO.StreamReader]::new($fs)
    while ($null -ne ($line = $reader.ReadLine())) {
        if ($line.Length -lt 32) { continue }
        if (-not ($line.StartsWith('2026-08-17 18:') -or $line.StartsWith('2026-08-17 19:') -or $line.StartsWith('2026-08-18'))) {
            continue
        }
        if ($line.Contains('backend_unavailable')) {
            $backend++
            if ($recentBackend.Count -lt 15) { $recentBackend.Add($line.Substring(0, [Math]::Min(400, $line.Length))) }
        }
        if ($line.Contains('planFile is omitted')) { $planFileOmitted++ }
        if ($line.Contains('Unhandled exception in middleware pipeline: POST /mcpserver/sessionlog')) {
            $sessionlogUnhandled++
            if ($recentUnhandled.Count -lt 10) { $recentUnhandled.Add($line) }
        }
        if ($line.Contains('completed with 503') -and $line.Contains('sessionlog')) { $transport503++ }
    }
} finally {
    $fs.Dispose()
}

Write-Output ('SinceCutoffApprox=18:38 local')
Write-Output ('backend_unavailableLines=' + $backend)
Write-Output ('planFileOmittedLines=' + $planFileOmitted)
Write-Output ('sessionlogUnhandledLines=' + $sessionlogUnhandled)
Write-Output ('sessionlogCompleted503=' + $transport503)
Write-Output '--- recent backend_unavailable ---'
$recentBackend | ForEach-Object { Write-Output $_ }
Write-Output '--- recent sessionlog unhandled ---'
$recentUnhandled | ForEach-Object { Write-Output $_ }
