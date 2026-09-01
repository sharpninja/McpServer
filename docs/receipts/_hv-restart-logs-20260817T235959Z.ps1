#Requires -Version 7.0
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Continue'

Write-Output ('UTC=' + [datetime]::UtcNow.ToString('o'))

Write-Output '=== EVENTLOG_BROAD ==='
try {
    $start = Get-Date '2026-08-17T18:20:00'
    $end = Get-Date '2026-08-17T19:10:00'
    $all = Get-WinEvent -FilterHashtable @{
        LogName = 'System'
        StartTime = $start
        EndTime = $end
    } -ErrorAction Stop
    Write-Output ('SystemEventsInWindow=' + @($all).Count)
    $svcish = $all | Where-Object {
        $m = $_.Message
        if (-not $m) { return $false }
        if ($m -match 'McpServer|Mcp Server|Support.Mcp|7147') { return $true }
        if ($_.ProviderName -eq 'Service Control Manager' -and $m -match 'entered the') { return $true }
        return $false
    }
    Write-Output ('FilteredHits=' + @($svcish).Count)
    foreach ($ev in @($svcish | Select-Object -First 40)) {
        $msg = (($ev.Message) -replace '\s+', ' ').Trim()
        if ($msg.Length -gt 350) { $msg = $msg.Substring(0, 350) }
        Write-Output ('EV Id={0} TimeLocal={1} TimeUtc={2} Provider={3}' -f $ev.Id, $ev.TimeCreated.ToString('o'), $ev.TimeCreated.ToUniversalTime().ToString('o'), $ev.ProviderName)
        Write-Output ('EV Msg=' + $msg)
    }
} catch {
    Write-Output ('EVENTLOG_ERROR=' + $_.Exception.Message)
}

Write-Output '=== EVENTLOG_SCM_ONLY ==='
try {
    $scm = Get-WinEvent -FilterHashtable @{
        LogName = 'System'
        ProviderName = 'Service Control Manager'
        StartTime = (Get-Date '2026-08-17T18:20:00')
        EndTime = (Get-Date '2026-08-17T19:10:00')
        Id = 7034,7035,7036,7040,7045
    } -ErrorAction Stop
    Write-Output ('ScmCount=' + @($scm).Count)
    foreach ($ev in @($scm | Select-Object -First 50)) {
        $msg = (($ev.Message) -replace '\s+', ' ').Trim()
        if ($msg.Length -gt 300) { $msg = $msg.Substring(0, 300) }
        Write-Output ('SCM Id={0} TimeUtc={1} Msg={2}' -f $ev.Id, $ev.TimeCreated.ToUniversalTime().ToString('o'), $msg)
    }
} catch {
    Write-Output ('SCM_ERROR=' + $_.Exception.Message)
}

Write-Output '=== LOG_SCAN_1838 ==='
$logPath = 'C:\ProgramData\McpServer\logs\mcp-20260817.log'
$fs = [System.IO.File]::Open($logPath, [System.IO.FileMode]::Open, [System.IO.FileAccess]::Read, [System.IO.FileShare]::ReadWrite)
try {
    $reader = [System.IO.StreamReader]::new($fs)
    $startup = New-Object System.Collections.Generic.List[string]
    $healthish = New-Object System.Collections.Generic.List[string]
    $keysBefore = New-Object System.Collections.Generic.HashSet[string]
    $keysAfter = New-Object System.Collections.Generic.HashSet[string]
    $lineNo = 0
    $windowHits = 0
    while ($null -ne ($line = $reader.ReadLine())) {
        $lineNo++
        $inWindow = $line.StartsWith('2026-08-17 18:3') -or $line.StartsWith('2026-08-17 18:4')
        if (-not $inWindow) { continue }
        $windowHits++
        $isStartup = $line -match 'unreachable|storage|backend|listening|Kestrel|Application started|Hosting|Now listening|SQLite|health|ready|nonce|pid|apiKey|rotated|marker'
        if ($isStartup) {
            if ($startup.Count -lt 120) { [void]$startup.Add($line) }
        }
        if ($line -match '/health|storage|unreachable|backend_unavailable') {
            if ($healthish.Count -lt 80) { [void]$healthish.Add($line) }
        }
        foreach ($m in [regex]::Matches($line, 'X-Api-Key=([A-Za-z0-9_-]+)')) {
            $k = $m.Groups[1].Value
            if ($line.StartsWith('2026-08-17 18:3') -and [int]$line.Substring(14,2) -lt 38) {
                [void]$keysBefore.Add($k)
            } else {
                [void]$keysAfter.Add($k)
            }
        }
    }
    Write-Output ('LogLineCount=' + $lineNo)
    Write-Output ('Window1830to1849Hits=' + $windowHits)
    Write-Output ('KeysBefore1838Count=' + $keysBefore.Count)
    foreach ($k in $keysBefore) {
        $suffix = $k.Substring([Math]::Max(0, $k.Length - 4))
        $prefix = $k.Substring(0, [Math]::Min(4, $k.Length))
        $hash = [System.BitConverter]::ToString([System.Security.Cryptography.SHA256]::HashData([System.Text.Encoding]::UTF8.GetBytes($k))).Replace('-', '')
        Write-Output ('KeyBefore prefix={0} suffix={1} len={2} sha256={3}' -f $prefix, $suffix, $k.Length, $hash)
    }
    Write-Output ('KeysAfter1838Count=' + $keysAfter.Count)
    foreach ($k in $keysAfter) {
        $suffix = $k.Substring([Math]::Max(0, $k.Length - 4))
        $prefix = $k.Substring(0, [Math]::Min(4, $k.Length))
        $hash = [System.BitConverter]::ToString([System.Security.Cryptography.SHA256]::HashData([System.Text.Encoding]::UTF8.GetBytes($k))).Replace('-', '')
        Write-Output ('KeyAfter prefix={0} suffix={1} len={2} sha256={3}' -f $prefix, $suffix, $k.Length, $hash)
    }
    Write-Output '--- startup-ish ---'
    foreach ($l in $startup) { Write-Output $l }
    Write-Output '--- health-ish ---'
    foreach ($l in $healthish) { Write-Output $l }
} finally {
    $fs.Dispose()
}

Write-Output '=== DEPLOY_JSON ==='
$dj = 'C:\ProgramData\McpServer\.mcpservice-deployment.json'
if (Test-Path -LiteralPath $dj) {
    $di = Get-Item -LiteralPath $dj
    Write-Output ('DeployJsonLw=' + $di.LastWriteTimeUtc.ToString('o'))
    Write-Output (Get-Content -LiteralPath $dj -Raw)
}

Write-Output '=== PROGRAMDATA_MARKER ==='
$pm = 'C:\ProgramData\McpServer\AGENTS-README-FIRST.yaml'
if (Test-Path -LiteralPath $pm) {
    $pi = Get-Item -LiteralPath $pm
    Write-Output ('ProgramDataMarkerLw=' + $pi.LastWriteTimeUtc.ToString('o'))
    $pt = Get-Content -LiteralPath $pm -Raw
    Write-Output ('ProgramDataMarkerPid=' + [regex]::Match($pt, '(?m)^pid:\s*(\d+)').Groups[1].Value)
    $pk = [regex]::Match($pt, '(?m)^apiKey:\s*(\S+)').Groups[1].Value
    $ph = [System.BitConverter]::ToString([System.Security.Cryptography.SHA256]::HashData([System.Text.Encoding]::UTF8.GetBytes($pk))).Replace('-', '')
    Write-Output ('ProgramDataApiKeySha256=' + $ph)
    Write-Output ('ProgramDataApiKeySuffix4=' + $pk.Substring([Math]::Max(0, $pk.Length - 4)))
}

Write-Output 'LOGS_DONE'
