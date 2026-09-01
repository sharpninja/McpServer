#Requires -Version 7.0
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$path = 'C:\ProgramData\McpServer\logs\mcp-20260817.log'
$sessionNeedle = 'help-20260818001213-0aa9f6de59d2403296130363aa94bb75'
$needles = @(
    $sessionNeedle,
    'latencyMs":55827',
    'latencyMs=55827',
    '55909.97',
    'agent_help_submit_turn completed',
    '"name":"agent_help_submit_turn"',
    '"name":"agent_help_create_session"',
    '"name":"agent_help_get_status"',
    '"name":"agent_help_get_transcript"',
    'guardResult'
)

Write-Output ('LogPath=' + $path)
$item = Get-Item -LiteralPath $path
Write-Output ('LogLength=' + $item.Length)
Write-Output ('LogUtc=' + $item.LastWriteTimeUtc.ToString('o'))

$fs = [System.IO.File]::Open($path, [System.IO.FileMode]::Open, [System.IO.FileAccess]::Read, [System.IO.FileShare]::ReadWrite)
$reader = [System.IO.StreamReader]::new($fs)
$hit = 0
$lineNo = 0
$sessionHits = 0
$submitHits = 0
try {
    while ($null -ne ($line = $reader.ReadLine())) {
        $lineNo++
        if ($line.Contains('"result":{"tools":[')) { continue }
        if ($line.Contains('MCP interaction POST /mcp-transport') -eq $false -and
            $line.Contains($sessionNeedle) -eq $false -and
            $line.Contains('55909.97') -eq $false -and
            $line.Contains('latencyMs":55827') -eq $false) {
            $isToolCall = $false
            foreach ($n in @(
                '"name":"agent_help_submit_turn"',
                '"name":"agent_help_create_session"',
                '"name":"agent_help_get_status"',
                '"name":"agent_help_get_transcript"'
            )) {
                if ($line.Contains($n)) { $isToolCall = $true; break }
            }
            if (-not $isToolCall) { continue }
        }

        $matched = $false
        foreach ($n in $needles) {
            if ($line.Contains($n)) { $matched = $true; break }
        }
        if (-not $matched) { continue }

        $hit++
        if ($line.Contains($sessionNeedle)) { $sessionHits++ }
        if ($line.Contains('agent_help_submit_turn')) { $submitHits++ }
        $shown = $line
        if ($shown.Length -gt 2200) { $shown = $shown.Substring(0, 2200) + '...TRUNC' }
        Write-Output ('L' + $lineNo + ' ' + $shown)
        if ($hit -ge 40) {
            Write-Output 'HIT_CAP=40'
            break
        }
    }
} finally {
    $reader.Dispose()
    $fs.Dispose()
}

Write-Output ('HitCount=' + $hit)
Write-Output ('SessionHits=' + $sessionHits)
Write-Output ('SubmitHits=' + $submitHits)
Write-Output ('LinesRead=' + $lineNo)
Write-Output 'LOGSCAN2_DONE'
