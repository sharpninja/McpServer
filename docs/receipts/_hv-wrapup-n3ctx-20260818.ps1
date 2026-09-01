#Requires -Version 7.0
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Continue'
foreach ($p in @(
    'F:\GitHub\McpServer\.mcpServer\grok\current-turn.yaml',
    'F:\GitHub\McpServer\.mcpServer\claude\current-turn.yaml'
)) {
    Write-Output "FILE=$p EXISTS=$(Test-Path -LiteralPath $p)"
    if (-not (Test-Path -LiteralPath $p)) { continue }
    $text = Get-Content -LiteralPath $p -Raw
    $idx = $text.IndexOf('N3fWcoY')
    Write-Output "N3_INDEX=$idx LEN=$($text.Length)"
    if ($idx -ge 0) {
        $start = [Math]::Max(0, $idx - 80)
        $len = [Math]::Min(160, $text.Length - $start)
        $clip = $text.Substring($start, $len) -replace 'N3fWcoY[A-Za-z0-9_\-]*', 'N3fWcoY[REDACTED]'
        Write-Output "N3_CONTEXT=$clip"
    }
}

# Narrow wrap-up-time test logs
$cutoff = [datetime]'2026-08-18T17:50:00Z'
foreach ($root in @(
    'C:\Users\kingd\AppData\Local\Temp\grok-goal-18747a5af710',
    'F:\GitHub\McpServer\docs\receipts'
)) {
    if (-not (Test-Path -LiteralPath $root)) { Write-Output "MISSING $root"; continue }
    Get-ChildItem -Path $root -File -Recurse -ErrorAction SilentlyContinue |
        Where-Object { $_.LastWriteTimeUtc -ge $cutoff -and $_.Name -match 'test|nuke|trace' } |
        Sort-Object LastWriteTimeUtc |
        ForEach-Object { Write-Output ("CAND {0:o} {1} {2}" -f $_.LastWriteTimeUtc, $_.Length, $_.FullName) }
}
