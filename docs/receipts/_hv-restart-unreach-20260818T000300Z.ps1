#Requires -Version 7.0
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Continue'

$logPath = 'C:\ProgramData\McpServer\logs\mcp-20260817.log'
$fs = [System.IO.File]::Open($logPath, [System.IO.FileMode]::Open, [System.IO.FileAccess]::Read, [System.IO.FileShare]::ReadWrite)
$unreach = New-Object System.Collections.Generic.List[string]
$mcpWrite = New-Object System.Collections.Generic.List[string]
$ready = New-Object System.Collections.Generic.List[string]
$backend = New-Object System.Collections.Generic.List[string]
try {
    $reader = [System.IO.StreamReader]::new($fs)
    while ($null -ne ($line = $reader.ReadLine())) {
        if (-not ($line.StartsWith('2026-08-17 18:38') -or $line.StartsWith('2026-08-17 18:39') -or $line.StartsWith('2026-08-17 18:40') -or $line.StartsWith('2026-08-17 18:41') -or $line.StartsWith('2026-08-17 18:42'))) {
            continue
        }
        if ($line -match 'unreachable') {
            [void]$unreach.Add($line.Substring(0, [Math]::Min(400, $line.Length)))
        }
        if ($line -match 'Wrote MCP marker file: F:\\GitHub\\McpServer\\AGENTS-README-FIRST') {
            [void]$mcpWrite.Add($line)
        }
        if ($line -match 'GET /ready') {
            $outIdx = $line.IndexOf(', Output:')
            if ($outIdx -ge 0) {
                $tail = $line.Substring($outIdx)
                if ($tail.Length -gt 400) { $tail = $tail.Substring(0, 400) }
                [void]$ready.Add($line.Substring(0, 160) + ' || ' + $tail)
            } else {
                [void]$ready.Add($line.Substring(0, [Math]::Min(300, $line.Length)))
            }
        }
        if ($line -match 'backend_unavailable|503') {
            [void]$backend.Add($line.Substring(0, [Math]::Min(300, $line.Length)))
        }
    }
} finally {
    $fs.Dispose()
}

Write-Output ('UnreachableHits=' + $unreach.Count)
foreach ($l in $unreach) { Write-Output $l }
Write-Output ('McpServerMarkerWrites=' + $mcpWrite.Count)
foreach ($l in $mcpWrite) { Write-Output $l }
Write-Output ('ReadyHits=' + $ready.Count)
foreach ($l in $ready) { Write-Output $l }
Write-Output ('BackendOr503Hits=' + $backend.Count)
foreach ($l in $backend) { Write-Output $l }
Write-Output 'UNREACH_DONE'
