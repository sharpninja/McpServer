#Requires -Version 7.0
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Continue'

Write-Output ('UTC=' + [datetime]::UtcNow.ToString('o'))

Write-Output '=== EVENTLOG_RECENT ==='
try {
    $recent = Get-WinEvent -LogName System -MaxEvents 30 -ErrorAction Stop
    Write-Output ('RecentSystemCount=' + @($recent).Count)
    foreach ($ev in @($recent | Select-Object -First 15)) {
        $msg = if ($ev.Message) { (($ev.Message) -replace '\s+', ' ').Trim() } else { '' }
        if ($msg.Length -gt 220) { $msg = $msg.Substring(0, 220) }
        Write-Output ('RECENT Id={0} TimeUtc={1} Provider={2} Msg={3}' -f $ev.Id, $ev.TimeCreated.ToUniversalTime().ToString('o'), $ev.ProviderName, $msg)
    }
} catch {
    Write-Output ('RECENT_ERROR=' + $_.Exception.Message)
}

Write-Output '=== LOG_TIGHT_1838 ==='
$logPath = 'C:\ProgramData\McpServer\logs\mcp-20260817.log'
$fs = [System.IO.File]::Open($logPath, [System.IO.FileMode]::Open, [System.IO.FileAccess]::Read, [System.IO.FileShare]::ReadWrite)
$startup = New-Object System.Collections.Generic.List[string]
$health = New-Object System.Collections.Generic.List[string]
$short = New-Object System.Collections.Generic.List[string]
$reqKeys = New-Object System.Collections.Generic.List[string]
$transport = New-Object System.Collections.Generic.List[string]
try {
    $reader = [System.IO.StreamReader]::new($fs)
    while ($null -ne ($line = $reader.ReadLine())) {
        if (-not ($line.StartsWith('2026-08-17 18:38') -or $line.StartsWith('2026-08-17 18:39') -or $line.StartsWith('2026-08-17 18:40') -or $line.StartsWith('2026-08-17 18:41') -or $line.StartsWith('2026-08-17 18:42'))) {
            continue
        }
        if ($line.Length -lt 400 -and $short.Count -lt 80) {
            [void]$short.Add($line)
        }
        if ($line -match 'Application started|Now listening|Hosting environment|Content root|Kestrel.*started|Listening on|pid |ProcessId|marker written|Workspace marker|unreachable|backend_unavailable|storage is|Storage backend') {
            if ($startup.Count -lt 80) { [void]$startup.Add($line) }
        }
        if ($line -match 'GET /health|GET /ready|POST /mcp-transport|backend_unavailable|storage.:.unreachable|storage.:.reachable') {
            if ($health.Count -lt 80) { [void]$health.Add(($line.Substring(0, [Math]::Min(500, $line.Length)))) }
        }
        if ($line -match 'MCP interaction (GET|POST) /mcp-transport|503|backend') {
            if ($transport.Count -lt 40) { [void]$transport.Add(($line.Substring(0, [Math]::Min(400, $line.Length)))) }
        }
        $hdr = [regex]::Match($line, 'RequestHeaders:.*?X-Api-Key=([A-Za-z0-9_-]+).*?X-Workspace-Path=([^,;]+)')
        if ($hdr.Success) {
            $k = $hdr.Groups[1].Value
            $ws = $hdr.Groups[2].Value
            $stamp = $line.Substring(0, 23)
            $path = [regex]::Match($line, ' (GET|POST) ([^ ]+) ').Groups[2].Value
            [void]$reqKeys.Add(('{0} path={1} keySuffix={2} keyPrefix={3} ws={4}' -f $stamp, $path, $k.Substring($k.Length-4), $k.Substring(0,4), $ws))
        }
    }
} finally {
    $fs.Dispose()
}

Write-Output ('ShortLineCount=' + $short.Count)
Write-Output '--- short lines ---'
foreach ($l in $short) { Write-Output $l }
Write-Output '--- startup matches ---'
Write-Output ('StartupCount=' + $startup.Count)
foreach ($l in $startup) { Write-Output ($l.Substring(0, [Math]::Min(500, $l.Length))) }
Write-Output '--- health/ready/transport snippets ---'
Write-Output ('HealthCount=' + $health.Count)
foreach ($l in $health) { Write-Output $l }
Write-Output '--- transport snippets ---'
Write-Output ('TransportCount=' + $transport.Count)
foreach ($l in $transport) { Write-Output $l }
Write-Output '--- request-header keys ---'
Write-Output ('ReqKeyCount=' + $reqKeys.Count)
foreach ($l in $reqKeys) { Write-Output $l }

Write-Output '=== PRE_RESTART_WORKSPACE_KEY ==='
$fs2 = [System.IO.File]::Open($logPath, [System.IO.FileMode]::Open, [System.IO.FileAccess]::Read, [System.IO.FileShare]::ReadWrite)
$pre = New-Object System.Collections.Generic.List[string]
try {
    $reader = [System.IO.StreamReader]::new($fs2)
    while ($null -ne ($line = $reader.ReadLine())) {
        if (-not $line.StartsWith('2026-08-17 18:3')) { continue }
        $minute = [int]$line.Substring(14, 2)
        if ($minute -ge 38) { continue }
        $hdr = [regex]::Match($line, 'RequestHeaders:.*?X-Api-Key=([A-Za-z0-9_-]+).*?X-Workspace-Path=([^,;]+)')
        if ($hdr.Success -and $hdr.Groups[2].Value -match 'F:\\GitHub\\McpServer') {
            $k = $hdr.Groups[1].Value
            $stamp = $line.Substring(0, 23)
            $path = [regex]::Match($line, ' (GET|POST) ([^ ]+) ').Groups[2].Value
            $item = ('{0} path={1} prefix={2} suffix={3}' -f $stamp, $path, $k.Substring(0,4), $k.Substring($k.Length-4))
            if (-not $pre.Contains($item) -and $pre.Count -lt 20) { [void]$pre.Add($item) }
        }
    }
} finally {
    $fs2.Dispose()
}
Write-Output ('PreRestartWorkspaceReqKeys=' + $pre.Count)
foreach ($l in $pre) { Write-Output $l }

Write-Output 'TIGHT_DONE'
