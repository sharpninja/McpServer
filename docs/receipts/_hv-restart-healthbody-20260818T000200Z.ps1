#Requires -Version 7.0
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Continue'

$logPath = 'C:\ProgramData\McpServer\logs\mcp-20260817.log'
$fs = [System.IO.File]::Open($logPath, [System.IO.FileMode]::Open, [System.IO.FileAccess]::Read, [System.IO.FileShare]::ReadWrite)
$wanted = New-Object System.Collections.Generic.List[string]
try {
    $reader = [System.IO.StreamReader]::new($fs)
    while ($null -ne ($line = $reader.ReadLine())) {
        if (-not ($line.StartsWith('2026-08-17 18:38') -or $line.StartsWith('2026-08-17 18:39') -or $line.StartsWith('2026-08-17 18:40') -or $line.StartsWith('2026-08-17 18:41'))) {
            continue
        }
        $keep = $false
        if ($line -match 'GET /health|GET /ready|Application started|Now listening|Hosting environment|Content root path|PID=57744|pid: 57744|F:\\GitHub\\McpServer\\AGENTS-README-FIRST|Workspace registered|marker file at F:\\GitHub\\McpServer|storage.:.unreachable|storage.:.reachable|backend_unavailable|nonce=') {
            $keep = $true
        }
        if ($line.Length -lt 220 -and $line -match 'started|listening|Hosting|Kestrel|PID=|marker|unreachable|reachable|ready') {
            $keep = $true
        }
        if ($keep) { [void]$wanted.Add($line) }
    }
} finally {
    $fs.Dispose()
}

Write-Output ('WantedCount=' + $wanted.Count)
foreach ($line in $wanted) {
    if ($line.Length -gt 800) {
        # Prefer Output JSON tail for health/ready
        $outIdx = $line.IndexOf(', Output:')
        if ($outIdx -ge 0) {
            $head = $line.Substring(0, [Math]::Min(180, $line.Length))
            $tail = $line.Substring($outIdx)
            if ($tail.Length -gt 500) { $tail = $tail.Substring(0, 500) }
            Write-Output ('HEAD ' + $head)
            Write-Output ('OUT ' + $tail)
        } else {
            Write-Output ($line.Substring(0, 800))
        }
    } else {
        Write-Output $line
    }
}

Write-Output 'HEALTHBODY_DONE'
