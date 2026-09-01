#Requires -Version 7.0
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$path = 'C:\ProgramData\McpServer\logs\mcp-20260817.log'
$restartLocal = [datetime]'2026-08-17T18:38:00'
$windowStart = [datetime]'2026-08-17T19:31:00'
$windowEnd = [datetime]'2026-08-17T19:36:00'
$morningStart = [datetime]'2026-08-17T05:40:00'
$morningEnd = [datetime]'2026-08-17T05:50:00'

function Get-LogTimestamp {
    param([string]$Line)
    if ($Line -match '^(\d{4}-\d{2}-\d{2} \d{2}:\d{2}:\d{2})') {
        return [datetime]::ParseExact($Matches[1], 'yyyy-MM-dd HH:mm:ss', [cultureinfo]::InvariantCulture)
    }
    return $null
}

$item = Get-Item -LiteralPath $path
Write-Output ('FILE=' + $item.FullName)
Write-Output ('LEN=' + $item.Length)
Write-Output ('LASTWRITE_UTC=' + $item.LastWriteTimeUtc.ToString('o'))

$c = [ordered]@{
    PlanFileOmitted = 0
    PlanFileOmittedWindow = 0
    PlanFileOmittedPostRestart = 0
    ArgExPlanFile = 0
    ArgExPlanFileWindow = 0
    Sessionlog503Exact = 0
    Sessionlog503ExactPost = 0
    Sessionlog503LoosePost = 0
    Sessionlog500Post = 0
    Transport503Post = 0
    Backend = 0
    BackendPost = 0
    BackendPre = 0
    Ip77 = 0
    Ip77Morning = 0
    ReqUpdate = 0
    ReqUpdateMorning = 0
    ViceSharp = 0
    ViceSharpMorning = 0
}

$windowPlan = New-Object System.Collections.Generic.List[string]
$windowArg = New-Object System.Collections.Generic.List[string]
$post503Exact = New-Object System.Collections.Generic.List[string]
$post503Loose = New-Object System.Collections.Generic.List[string]
$post500 = New-Object System.Collections.Generic.List[string]
$postBackend = New-Object System.Collections.Generic.List[string]
$morningHits = New-Object System.Collections.Generic.List[string]
$preBackend = New-Object System.Collections.Generic.List[string]

$lineNo = 0
$fs = [System.IO.FileStream]::new($path, [System.IO.FileMode]::Open, [System.IO.FileAccess]::Read, [System.IO.FileShare]::ReadWrite)
$reader = [System.IO.StreamReader]::new($fs)
try {
    while ($null -ne ($line = $reader.ReadLine())) {
        $lineNo++
        $ts = Get-LogTimestamp -Line $line
        $inWindow = $false
        $postRestart = $false
        $inMorning = $false
        if ($null -ne $ts) {
            $inWindow = ($ts -ge $windowStart -and $ts -lt $windowEnd)
            $postRestart = ($ts -ge $restartLocal)
            $inMorning = ($ts -ge $morningStart -and $ts -lt $morningEnd)
        }

        if ($line.Contains('planFile is omitted')) {
            $c.PlanFileOmitted++
            if ($inWindow) {
                $c.PlanFileOmittedWindow++
                if ($windowPlan.Count -lt 8) {
                    $windowPlan.Add('L' + $lineNo + ' ' + $line.Substring(0, [Math]::Min(260, $line.Length)))
                }
            }
            if ($postRestart) { $c.PlanFileOmittedPostRestart++ }
        }

        if ($line.Contains('ArgumentException') -and $line.Contains('planFile')) {
            $c.ArgExPlanFile++
            if ($inWindow) {
                $c.ArgExPlanFileWindow++
                if ($windowArg.Count -lt 8) {
                    $windowArg.Add('L' + $lineNo + ' ' + $line.Substring(0, [Math]::Min(260, $line.Length)))
                }
            }
        }

        if ($line.Contains('POST /mcpserver/sessionlog completed with 503')) {
            $c.Sessionlog503Exact++
            if ($postRestart) {
                $c.Sessionlog503ExactPost++
                $post503Exact.Add('L' + $lineNo + ' ' + $line.Substring(0, [Math]::Min(280, $line.Length)))
            }
        }

        $sessionlogish = $line.Contains('/mcpserver/sessionlog') -or ($line.Contains('sessionlog') -and $line.Contains('completed with'))
        if ($postRestart -and $sessionlogish) {
            if ($line.Contains('completed with 503') -or $line.Contains('HTTP 503') -or $line.Contains('StatusCode: 503')) {
                $c.Sessionlog503LoosePost++
                if ($post503Loose.Count -lt 20) {
                    $post503Loose.Add('L' + $lineNo + ' ' + $line.Substring(0, [Math]::Min(280, $line.Length)))
                }
            }
            if ($line.Contains('completed with 500')) {
                $c.Sessionlog500Post++
                if ($post500.Count -lt 12) {
                    $post500.Add('L' + $lineNo + ' ' + $line.Substring(0, [Math]::Min(280, $line.Length)))
                }
            }
        }

        if ($postRestart -and $line.Contains('POST /mcp-transport completed with 503')) {
            $c.Transport503Post++
        }

        if ($line.Contains('backend_unavailable')) {
            $c.Backend++
            $sample = 'L' + $lineNo + ' ' + $line.Substring(0, [Math]::Min(320, $line.Length))
            if ($postRestart) {
                $c.BackendPost++
                if ($postBackend.Count -lt 20) { $postBackend.Add($sample) }
            } else {
                $c.BackendPre++
                if ($preBackend.Count -lt 15) { $preBackend.Add($sample) }
            }
        }

        if ($line.Contains('192.168.1.77')) {
            $c.Ip77++
            if ($inMorning -and $morningHits.Count -lt 25) {
                $morningHits.Add('L' + $lineNo + ' IP ' + $line.Substring(0, [Math]::Min(300, $line.Length)))
            }
        }
        if ($line.Contains('requirements_update')) {
            $c.ReqUpdate++
            if ($inMorning -and $morningHits.Count -lt 25) {
                $morningHits.Add('L' + $lineNo + ' REQ ' + $line.Substring(0, [Math]::Min(300, $line.Length)))
            }
        }
        if ($line.Contains('vice-sharp')) {
            $c.ViceSharp++
            if ($inMorning -and $morningHits.Count -lt 25) {
                $morningHits.Add('L' + $lineNo + ' VICE ' + $line.Substring(0, [Math]::Min(300, $line.Length)))
            }
        }
        if ($inMorning) {
            $c.Ip77Morning += [int]$line.Contains('192.168.1.77')
            $c.ReqUpdateMorning += [int]$line.Contains('requirements_update')
            $c.ViceSharpMorning += [int]$line.Contains('vice-sharp')
        }
    }
} finally {
    $reader.Dispose()
    $fs.Dispose()
}

Write-Output ('LINE_COUNT=' + $lineNo)
$c.GetEnumerator() | ForEach-Object { Write-Output ($_.Key + '=' + $_.Value) }

Write-Output '---- WINDOW_PLANFILE ----'
$windowPlan | ForEach-Object { Write-Output $_ }
Write-Output '---- WINDOW_ARGEX ----'
$windowArg | ForEach-Object { Write-Output $_ }
Write-Output '---- POST_503_EXACT ----'
$post503Exact | ForEach-Object { Write-Output $_ }
Write-Output '---- POST_503_LOOSE ----'
$post503Loose | ForEach-Object { Write-Output $_ }
Write-Output '---- POST_500_SESSIONLOG ----'
$post500 | ForEach-Object { Write-Output $_ }
Write-Output '---- POST_BACKEND ----'
$postBackend | ForEach-Object { Write-Output $_ }
Write-Output '---- PRE_BACKEND ----'
$preBackend | ForEach-Object { Write-Output $_ }
Write-Output '---- MORNING ----'
$morningHits | ForEach-Object { Write-Output $_ }
