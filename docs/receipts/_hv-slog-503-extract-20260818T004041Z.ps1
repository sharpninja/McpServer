#Requires -Version 7.0
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$path = 'C:\ProgramData\McpServer\logs\mcp-20260817.log'
$wanted = [System.Collections.Generic.HashSet[int]]::new()
@(
    326496,326583,326611,326843,
    373059,373060,373596,
    384080,384081,
    53720,53721,53802,53803,53804,53805,53806,53807,53808
) | ForEach-Object { [void]$wanted.Add($_) }

# Also capture first post-restart sessionlog 400 and morning backend-ish data lines
$lineNo = 0
$first400 = $null
$morningPayloads = New-Object System.Collections.Generic.List[string]
$fs = [System.IO.FileStream]::new($path, [System.IO.FileMode]::Open, [System.IO.FileAccess]::Read, [System.IO.FileShare]::ReadWrite)
$reader = [System.IO.StreamReader]::new($fs)
$lastTs = $null
try {
    while ($null -ne ($line = $reader.ReadLine())) {
        $lineNo++
        if ($line -match '^(\d{4}-\d{2}-\d{2} \d{2}:\d{2}:\d{2})') {
            $lastTs = [datetime]::ParseExact($Matches[1], 'yyyy-MM-dd HH:mm:ss', [cultureinfo]::InvariantCulture)
        }
        if ($wanted.Contains($lineNo)) {
            Write-Output ('==== L' + $lineNo + ' len=' + $line.Length + ' ====')
            if ($line.Length -gt 1500) { Write-Output ($line.Substring(0, 1500)); Write-Output ('... len=' + $line.Length) }
            else { Write-Output $line }
        }
        if ($null -eq $first400 -and $line -match 'MCP interaction POST /mcpserver/sessionlog completed with 400') {
            $first400 = $lineNo
            Write-Output ('==== FIRST400 L' + $lineNo + ' ====')
            if ($line.Length -gt 1200) { Write-Output ($line.Substring(0, 1200)); Write-Output ('... len=' + $line.Length) }
            else { Write-Output $line }
        }
        $inMorning = $false
        if ($null -ne $lastTs) {
            $inMorning = ($lastTs -ge [datetime]'2026-08-17T05:42:00' -and $lastTs -le [datetime]'2026-08-17T05:43:00')
        }
        if ($inMorning -and ($line.Contains('backend_unavailable') -or $line.Contains('requirements_update') -or $line.Contains('192.168.1.77'))) {
            if ($morningPayloads.Count -lt 15) {
                $clip = if ($line.Length -gt 400) { $line.Substring(0, 400) } else { $line }
                $morningPayloads.Add('L' + $lineNo + ' ' + $clip)
            }
        }
    }
} finally {
    $reader.Dispose()
    $fs.Dispose()
}
Write-Output '---- MORNING_PAYLOADS ----'
$morningPayloads | ForEach-Object { Write-Output $_ }
Write-Output ('FIRST400_LINE=' + $first400)
