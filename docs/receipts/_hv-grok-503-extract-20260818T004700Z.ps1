#Requires -Version 7.0
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$logPath = 'C:\ProgramData\McpServer\logs\mcp-20260817.log'
function Clip([string]$s, [int]$n = 1200) {
    if ($s.Length -le $n) { return $s }
    return $s.Substring(0, $n)
}

$emit = 0
$mode = 'scan'
$counts = [ordered]@{
    completed_503_after_1838 = 0
    backend_after_1838 = 0
    access_denied_near_1852 = 0
    ensureboot_near_1852 = 0
    win32_5_near_1852 = 0
}

$fs = [System.IO.File]::Open($logPath, [System.IO.FileMode]::Open, [System.IO.FileAccess]::Read, [System.IO.FileShare]::ReadWrite)
try {
    $reader = [System.IO.StreamReader]::new($fs)
    $lineNo = 0
    $afterStart = $false
    $near1852 = $false
    while ($null -ne ($line = $reader.ReadLine())) {
        $lineNo++
        if ($line.StartsWith('2026-08-17 18:38:')) { $afterStart = $true }
        if ($line.StartsWith('2026-08-17 18:52:') -or $line.StartsWith('2026-08-17 18:51:') -or $line.StartsWith('2026-08-17 18:53:')) { $near1852 = $true }
        if ($line.StartsWith('2026-08-17 18:54:')) { $near1852 = $false }

        if ($afterStart) {
            if ($line.Contains('completed with 503')) {
                $counts.completed_503_after_1838++
                Write-Output ('COMPLETED_503=' + (Clip $line 500))
            }
            if ($line.Contains('backend_unavailable')) {
                $counts.backend_after_1838++
            }
        }

        if ($near1852 -or $emit -gt 0) {
            if ($line.Contains('Access is denied')) { $counts.access_denied_near_1852++ }
            if ($line.Contains('EnsureBootstrappedAsync')) { $counts.ensureboot_near_1852++ }
            if ($line.Contains('Win32Exception (5)')) { $counts.win32_5_near_1852++ }
        }

        if ($line.Contains('2026-08-17 18:52:13') -or ($line.StartsWith('2026-08-17 18:52:1') -and ($line.Contains('Health check') -or $line.Contains('timed out') -or $line.Contains('Unhealthy') -or $line.Contains('storage')))) {
            Write-Output ('H1852=' + (Clip $line 700))
        }

        if ($line.Contains('00-aab0888980690d5c55a8af5c029f0bd1')) {
            Write-Output ('TRACE_HIT_L' + $lineNo + '=' + (Clip $line 900))
            $emit = 45
            continue
        }

        if ($line.Contains('sessionlog_replace_section') -and $line.Contains('2026-08-17 18:52:23')) {
            Write-Output ('REPLACE_L' + $lineNo + '=' + (Clip $line 1400))
        }

        if ($emit -gt 0) {
            Write-Output ('EXC_L' + $lineNo + '=' + (Clip $line 900))
            $emit--
        }
    }
} finally {
    $fs.Dispose()
}

Write-Output '--- COUNTS ---'
foreach ($k in $counts.Keys) { Write-Output ($k + '=' + $counts[$k]) }
