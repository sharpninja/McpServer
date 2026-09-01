#Requires -Version 7.0
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$path = 'C:\ProgramData\McpServer\logs\mcp-20260817.log'
$restart = [datetime]'2026-08-17T18:38:00'
$implEnd = [datetime]'2026-08-17T19:35:30'
$windowStart = [datetime]'2026-08-17T19:31:00'
$windowEnd = [datetime]'2026-08-17T19:36:00'
$morningStart = [datetime]'2026-08-17T05:40:00'
$morningEnd = [datetime]'2026-08-17T05:50:00'
$utc1838 = [datetime]'2026-08-17T13:38:00'

function Get-LogTimestamp {
    param([string]$Line)
    if ($Line -match '^(\d{4}-\d{2}-\d{2} \d{2}:\d{2}:\d{2})') {
        return [datetime]::ParseExact($Matches[1], 'yyyy-MM-dd HH:mm:ss', [cultureinfo]::InvariantCulture)
    }
    return $null
}

$c = [ordered]@{
    PlanFileAll = 0
    PlanFileAfterUtc1838AsLocal = 0
    PlanFileAfterRestart = 0
    PlanFileThroughImplEnd = 0
    PlanFileWindow = 0
    PlanFileBareAfterRestart = 0
    PrefixSessionlog503AfterRestart = 0
    PrefixSessionlog503All = 0
    PrefixSessionlog500AfterRestart = 0
    PrefixTransport503AfterRestart = 0
    PrefixAny503AfterRestart = 0
    CompletedWith503SubstringAfterRestart = 0
    BackendAfterRestart = 0
    BackendThroughImplEnd = 0
    BackendAfterRestartStatus503 = 0
    MorningBackend = 0
    MorningSql77 = 0
    TraceAab088 = 0
    UnhealthyAfterRestart = 0
    NamedPipesAfterRestart = 0
}

$sessionlogStatus = @{}
$transportStatus = @{}
$prefix503 = New-Object System.Collections.Generic.List[string]
$backendClass = New-Object System.Collections.Generic.List[string]
$morningBackend = New-Object System.Collections.Generic.List[string]
$unhealthy = New-Object System.Collections.Generic.List[string]
$namedPipes = New-Object System.Collections.Generic.List[string]
$traceHits = New-Object System.Collections.Generic.List[string]
$implSessionHits = New-Object System.Collections.Generic.List[string]
$turn41593 = New-Object System.Collections.Generic.List[string]
$body503Examples = New-Object System.Collections.Generic.List[string]

$lineNo = 0
$fs = [System.IO.FileStream]::new($path, [System.IO.FileMode]::Open, [System.IO.FileAccess]::Read, [System.IO.FileShare]::ReadWrite)
$reader = [System.IO.StreamReader]::new($fs)
try {
    while ($null -ne ($line = $reader.ReadLine())) {
        $lineNo++
        $ts = Get-LogTimestamp -Line $line
        $afterRestart = $false
        $throughImpl = $false
        $inWindow = $false
        $inMorning = $false
        $afterUtcAsLocal = $false
        if ($null -ne $ts) {
            $afterRestart = ($ts -ge $restart)
            $throughImpl = ($ts -ge $restart -and $ts -le $implEnd)
            $inWindow = ($ts -ge $windowStart -and $ts -lt $windowEnd)
            $inMorning = ($ts -ge $morningStart -and $ts -lt $morningEnd)
            $afterUtcAsLocal = ($ts -ge $utc1838)
        }

        if ($line.Contains('planFile is omitted')) {
            $c.PlanFileAll++
            if ($afterUtcAsLocal) { $c.PlanFileAfterUtc1838AsLocal++ }
            if ($afterRestart) { $c.PlanFileAfterRestart++ }
            if ($throughImpl) { $c.PlanFileThroughImplEnd++ }
            if ($inWindow) { $c.PlanFileWindow++ }
        }
        if ($afterRestart -and $line.Contains('planFile')) { $c.PlanFileBareAfterRestart++ }

        $prefixStatus = $null
        $prefixKind = $null
        if ($line -match 'MCP interaction POST /mcpserver/sessionlog completed with (\d+)') {
            $prefixStatus = $Matches[1]
            $prefixKind = 'sessionlog'
            if (-not $sessionlogStatus.Contains($prefixStatus)) { $sessionlogStatus[$prefixStatus] = 0 }
            if ($afterRestart) { $sessionlogStatus[$prefixStatus]++ }
            if ($prefixStatus -eq '503') {
                $c.PrefixSessionlog503All++
                if ($afterRestart) {
                    $c.PrefixSessionlog503AfterRestart++
                    $prefix503.Add('L' + $lineNo + ' ' + $line.Substring(0, [Math]::Min(240, $line.Length)))
                }
            }
            if ($afterRestart -and $prefixStatus -eq '500') { $c.PrefixSessionlog500AfterRestart++ }
        } elseif ($line -match 'MCP interaction POST /mcp-transport completed with (\d+)') {
            $prefixStatus = $Matches[1]
            $prefixKind = 'transport'
            if (-not $transportStatus.Contains($prefixStatus)) { $transportStatus[$prefixStatus] = 0 }
            if ($afterRestart) { $transportStatus[$prefixStatus]++ }
            if ($afterRestart -and $prefixStatus -eq '503') {
                $c.PrefixTransport503AfterRestart++
                $prefix503.Add('L' + $lineNo + ' TRANSPORT ' + $line.Substring(0, [Math]::Min(240, $line.Length)))
            }
        } elseif ($afterRestart -and $line -match 'MCP interaction .+ completed with 503') {
            $c.PrefixAny503AfterRestart++
            if ($prefix503.Count -lt 20) {
                $prefix503.Add('L' + $lineNo + ' OTHER ' + $line.Substring(0, [Math]::Min(240, $line.Length)))
            }
        }

        if ($afterRestart -and $line.Contains('completed with 503')) {
            $c.CompletedWith503SubstringAfterRestart++
            if ($null -eq $prefixStatus -or $prefixStatus -ne '503') {
                if ($body503Examples.Count -lt 6) {
                    $clip = $line.Substring(0, [Math]::Min(220, $line.Length))
                    $body503Examples.Add('L' + $lineNo + ' prefixStatus=' + $prefixStatus + ' ' + $clip)
                }
            }
        }

        if ($line.Contains('backend_unavailable')) {
            if ($afterRestart) {
                $c.BackendAfterRestart++
                if ($throughImpl) { $c.BackendThroughImplEnd++ }
                if ($prefixStatus -eq '503') { $c.BackendAfterRestartStatus503++ }
                if ($backendClass.Count -lt 30) {
                    $backendClass.Add('L' + $lineNo + ' kind=' + $prefixKind + ' status=' + $prefixStatus + ' ' + $line.Substring(0, [Math]::Min(180, $line.Length)))
                }
            }
            if ($inMorning) {
                $c.MorningBackend++
                if ($morningBackend.Count -lt 12) {
                    $morningBackend.Add('L' + $lineNo + ' ' + $line.Substring(0, [Math]::Min(300, $line.Length)))
                }
            }
        }

        if ($inMorning -and $line.Contains('192.168.1.77')) { $c.MorningSql77++ }

        if ($line.Contains('aab0888980690d5c55a8af5c029f0bd1')) {
            $c.TraceAab088++
            if ($traceHits.Count -lt 8) { $traceHits.Add('L' + $lineNo + ' ' + $line.Substring(0, [Math]::Min(240, $line.Length))) }
        }
        if ($afterRestart -and $line.Contains('Unhealthy') -and $line.Contains('storage')) {
            $c.UnhealthyAfterRestart++
            if ($unhealthy.Count -lt 10) { $unhealthy.Add('L' + $lineNo + ' ' + $line.Substring(0, [Math]::Min(240, $line.Length))) }
        }
        if ($afterRestart -and ($line.Contains('Named Pipes') -or $line.Contains('Access is denied'))) {
            $c.NamedPipesAfterRestart++
            if ($namedPipes.Count -lt 8) { $namedPipes.Add('L' + $lineNo + ' ' + $line.Substring(0, [Math]::Min(240, $line.Length))) }
        }
        if ($line.Contains('GrokCode-20260817T120000Z-agent-help-grok-cli')) {
            if ($implSessionHits.Count -lt 8) { $implSessionHits.Add('L' + $lineNo + ' ' + $line.Substring(0, [Math]::Min(220, $line.Length))) }
        }
        if ($line.Contains('41593')) {
            if ($turn41593.Count -lt 8) { $turn41593.Add('L' + $lineNo + ' ' + $line.Substring(0, [Math]::Min(220, $line.Length))) }
        }
    }
} finally {
    $reader.Dispose()
    $fs.Dispose()
}

Write-Output ('LINE_COUNT=' + $lineNo)
$c.GetEnumerator() | ForEach-Object { Write-Output ($_.Key + '=' + $_.Value) }
Write-Output '---- SESSIONLOG_STATUS_AFTER_RESTART ----'
$sessionlogStatus.GetEnumerator() | Sort-Object Name | ForEach-Object { Write-Output ($_.Key + '=' + $_.Value) }
Write-Output '---- TRANSPORT_STATUS_AFTER_RESTART ----'
$transportStatus.GetEnumerator() | Sort-Object Name | ForEach-Object { Write-Output ($_.Key + '=' + $_.Value) }
Write-Output '---- PREFIX_503 ----'
$prefix503 | ForEach-Object { Write-Output $_ }
Write-Output '---- BODY_503_EXAMPLES ----'
$body503Examples | ForEach-Object { Write-Output $_ }
Write-Output '---- BACKEND_CLASS ----'
$backendClass | ForEach-Object { Write-Output $_ }
Write-Output '---- MORNING_BACKEND ----'
$morningBackend | ForEach-Object { Write-Output $_ }
Write-Output '---- UNHEALTHY ----'
$unhealthy | ForEach-Object { Write-Output $_ }
Write-Output '---- NAMED_PIPES ----'
$namedPipes | ForEach-Object { Write-Output $_ }
Write-Output '---- TRACE ----'
$traceHits | ForEach-Object { Write-Output $_ }
Write-Output '---- IMPL_SESSION ----'
$implSessionHits | ForEach-Object { Write-Output $_ }
Write-Output '---- TURN_41593 ----'
$turn41593 | ForEach-Object { Write-Output $_ }
