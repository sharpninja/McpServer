#Requires -Version 7.0
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$logDir = 'C:\ProgramData\McpServer\logs'
$restartLocal = [datetime]'2026-08-17T18:38:00'
$windowStart = [datetime]'2026-08-17T19:31:00'
$windowEnd = [datetime]'2026-08-17T19:36:00'

function Get-LogTimestamp {
    param([string]$Line)
    if ($Line -match '^(\d{4}-\d{2}-\d{2} \d{2}:\d{2}:\d{2})') {
        return [datetime]::ParseExact($Matches[1], 'yyyy-MM-dd HH:mm:ss', [cultureinfo]::InvariantCulture)
    }
    return $null
}

$files = @(Get-ChildItem -LiteralPath $logDir -File -Filter 'mcp-*.log' | Sort-Object Name)
Write-Output ('LOG_FILE_COUNT=' + $files.Count)
foreach ($f in $files) {
    Write-Output ('LOG_FILE=' + $f.Name + ' len=' + $f.Length + ' utc=' + $f.LastWriteTimeUtc.ToString('o'))
}

$patterns = @(
    'planFile is omitted',
    'POST /mcpserver/sessionlog completed with 503',
    'sessionlog completed with 503',
    'backend_unavailable',
    '192.168.1.77',
    'requirements_update',
    'vice-sharp'
)

foreach ($f in $files) {
    Write-Output ('==== SCAN ' + $f.Name + ' ====')
    $counts = @{}
    foreach ($p in $patterns) { $counts[$p] = 0 }
    $windowPlanFile = New-Object System.Collections.Generic.List[string]
    $window503 = New-Object System.Collections.Generic.List[string]
    $postRestart503Exact = New-Object System.Collections.Generic.List[string]
    $postRestart503Loose = New-Object System.Collections.Generic.List[string]
    $postRestartBackend = New-Object System.Collections.Generic.List[string]
    $earlyBackend = New-Object System.Collections.Generic.List[string]
    $postRestartPlanFileCount = 0
    $lineNo = 0
    $reader = [System.IO.StreamReader]::new($f.FullName)
    try {
        while ($null -ne ($line = $reader.ReadLine())) {
            $lineNo++
            $ts = Get-LogTimestamp -Line $line
            $inWindow = $false
            $postRestart = $false
            if ($null -ne $ts) {
                $inWindow = ($ts -ge $windowStart -and $ts -lt $windowEnd)
                $postRestart = ($ts -ge $restartLocal)
            }

            if ($line.Contains('planFile is omitted')) {
                $counts['planFile is omitted']++
                if ($inWindow -and $windowPlanFile.Count -lt 12) {
                    $windowPlanFile.Add(('L' + $lineNo + ' ' + $line.Substring(0, [Math]::Min(240, $line.Length))))
                }
                if ($postRestart) { $postRestartPlanFileCount++ }
            }
            if ($line.Contains('POST /mcpserver/sessionlog completed with 503')) {
                $counts['POST /mcpserver/sessionlog completed with 503']++
                $msg = 'L' + $lineNo + ' ' + $line.Substring(0, [Math]::Min(300, $line.Length))
                if ($inWindow) { $window503.Add($msg) }
                if ($postRestart) { $postRestart503Exact.Add($msg) }
            }
            if ($line.Contains('sessionlog completed with 503')) {
                $counts['sessionlog completed with 503']++
            }
            if ($line.Contains('backend_unavailable')) {
                $counts['backend_unavailable']++
                $msg = 'L' + $lineNo + ' ' + $line.Substring(0, [Math]::Min(320, $line.Length))
                if ($postRestart) {
                    if ($postRestartBackend.Count -lt 40) { $postRestartBackend.Add($msg) }
                } else {
                    if ($earlyBackend.Count -lt 20) { $earlyBackend.Add($msg) }
                }
            }
            if ($line.Contains('192.168.1.77')) { $counts['192.168.1.77']++ }
            if ($line.Contains('requirements_update')) { $counts['requirements_update']++ }
            if ($line.Contains('vice-sharp')) { $counts['vice-sharp']++ }

            $looks503 = $false
            if ($line.Contains(' 503 ') -or $line.Contains('completed with 503') -or $line.Contains('StatusCode: 503') -or $line.Contains('HTTP 503')) {
                if ($line.Contains('sessionlog') -or $line.Contains('/mcpserver/sessionlog')) {
                    $looks503 = $true
                }
            }
            if ($looks503 -and $postRestart) {
                if ($postRestart503Loose.Count -lt 30) {
                    $postRestart503Loose.Add('L' + $lineNo + ' ' + $line.Substring(0, [Math]::Min(300, $line.Length)))
                }
            }
        }
    } finally {
        $reader.Dispose()
    }

    Write-Output ('LINE_COUNT=' + $lineNo)
    foreach ($p in $patterns) {
        Write-Output ('COUNT[' + $p + ']=' + $counts[$p])
    }
    Write-Output ('POST_RESTART_PLANFILE_OMITTED=' + $postRestartPlanFileCount)
    Write-Output ('POST_RESTART_503_EXACT_COUNT=' + $postRestart503Exact.Count)
    Write-Output ('POST_RESTART_503_LOOSE_COUNT=' + $postRestart503Loose.Count)
    Write-Output ('POST_RESTART_BACKEND_LISTED=' + $postRestartBackend.Count)
    Write-Output ('WINDOW_PLANFILE_SAMPLES=')
    $windowPlanFile | ForEach-Object { Write-Output $_ }
    Write-Output ('WINDOW_503_SAMPLES=')
    $window503 | ForEach-Object { Write-Output $_ }
    Write-Output ('POST_RESTART_503_EXACT=')
    $postRestart503Exact | ForEach-Object { Write-Output $_ }
    Write-Output ('POST_RESTART_503_LOOSE=')
    $postRestart503Loose | ForEach-Object { Write-Output $_ }
    Write-Output ('POST_RESTART_BACKEND_SAMPLES=')
    $postRestartBackend | ForEach-Object { Write-Output $_ }
    Write-Output ('PRE_RESTART_BACKEND_SAMPLES=')
    $earlyBackend | ForEach-Object { Write-Output $_ }
}

Write-Output '==== TARGETED 05:42 vice-sharp / SQL ===='
$aug17 = Join-Path $logDir 'mcp-20260817.log'
if (Test-Path -LiteralPath $aug17) {
    $reader = [System.IO.StreamReader]::new($aug17)
    $lineNo = 0
    $hits = 0
    try {
        while ($null -ne ($line = $reader.ReadLine())) {
            $lineNo++
            $ts = Get-LogTimestamp -Line $line
            $inMorning = $false
            if ($null -ne $ts) {
                $inMorning = ($ts.Hour -eq 5 -and $ts.Minute -ge 40 -and $ts.Minute -le 50)
            }
            if ($inMorning -and ($line.Contains('backend_unavailable') -or $line.Contains('192.168.1.77') -or $line.Contains('vice-sharp') -or $line.Contains('requirements_update'))) {
                $hits++
                if ($hits -le 25) {
                    Write-Output ('L' + $lineNo + ' ' + $line.Substring(0, [Math]::Min(360, $line.Length)))
                }
            }
        }
    } finally {
        $reader.Dispose()
    }
    Write-Output ('MORNING_HITS=' + $hits)
}
