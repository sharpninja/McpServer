#Requires -Version 7.0
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$logPath = 'C:\ProgramData\McpServer\logs\mcp-20260817.log'
function Clip([string]$s, [int]$n = 1600) {
    if ($s.Length -le $n) { return $s }
    return $s.Substring(0, $n)
}

$fs = [System.IO.File]::Open($logPath, [System.IO.FileMode]::Open, [System.IO.FileAccess]::Read, [System.IO.FileShare]::ReadWrite)
try {
    $reader = [System.IO.StreamReader]::new($fs)
    $lineNo = 0
    while ($null -ne ($line = $reader.ReadLine())) {
        $lineNo++
        $inWin = $line.StartsWith('2026-08-17 18:52:2')
        if (-not $inWin) { continue }

        if ($line.Contains('ENTRY') -or $line.Contains('completed with') -or $line.Contains('Unhandled exception')) {
            Write-Output ('L' + $lineNo + ' LEN=' + $line.Length)
            Write-Output (Clip $line 1600)
            Write-Output '----'
        }
    }
} finally {
    $fs.Dispose()
}

# specifically dump replace_section output tail
Write-Output '=== REPLACE_SECTION_TAIL ==='
$fs2 = [System.IO.File]::Open($logPath, [System.IO.FileMode]::Open, [System.IO.FileAccess]::Read, [System.IO.FileShare]::ReadWrite)
try {
    $reader = [System.IO.StreamReader]::new($fs2)
    while ($null -ne ($line = $reader.ReadLine())) {
        if ($line.Contains('sessionlog_replace_section') -and $line.Contains('2026-08-17 18:52:23')) {
            $idx = $line.IndexOf('Output:')
            Write-Output ('LINE_LEN=' + $line.Length)
            Write-Output ('OUTPUT_IDX=' + $idx)
            if ($idx -ge 0) {
                $out = $line.Substring($idx)
                if ($out.Length -gt 2000) { $out = $out.Substring(0, 2000) }
                Write-Output $out
            }
            Write-Output ('HAS_503=' + $line.Contains('503'))
            Write-Output ('HAS_BACKEND=' + $line.Contains('backend_unavailable'))
            Write-Output ('HAS_ISERROR=' + $line.ToLowerInvariant().Contains('iserror'))
            Write-Output ('HAS_ERROR=' + $line.Contains('"error"'))
        }
    }
} finally {
    $fs2.Dispose()
}

# real completed-with-503 interaction lines after 18:38, not JSON body
Write-Output '=== REAL_COMPLETED_503 ==='
$fs3 = [System.IO.File]::Open($logPath, [System.IO.FileMode]::Open, [System.IO.FileAccess]::Read, [System.IO.FileShare]::ReadWrite)
try {
    $reader = [System.IO.StreamReader]::new($fs3)
    $n = 0
    while ($null -ne ($line = $reader.ReadLine())) {
        if (-not $line.StartsWith('2026-08-17 1')) { continue }
        if ($line.Contains('[INF] MCP interaction') -and $line.Contains('completed with 503')) {
            $n++
            Write-Output (Clip $line 400)
        }
    }
    Write-Output ('REAL_COMPLETED_503_COUNT=' + $n)
} finally {
    $fs3.Dispose()
}
