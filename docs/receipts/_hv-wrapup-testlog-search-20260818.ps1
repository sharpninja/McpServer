#Requires -Version 7.0
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Continue'
$out = 'F:\GitHub\McpServer\docs\receipts\_hv-wrapup-testlog-search-20260818.txt'
$lines = [System.Collections.Generic.List[string]]::new()
function W([string]$s) { $script:lines.Add($s); Write-Output $s }

W "UTC=$(Get-Date -AsUTC -Format o)"
$cutoff = [datetime]'2026-08-18T17:50:00Z'
$roots = @(
    'C:\Users\kingd\AppData\Local\Temp',
    'F:\GitHub\McpServer\docs\receipts',
    'F:\GitHub\McpServer'
)
$patterns = @('*test*.txt','*test*.log','*nuke*.log','*build-test*')
foreach ($root in $roots) {
    if (-not (Test-Path -LiteralPath $root)) { continue }
    Get-ChildItem -Path $root -File -Recurse -ErrorAction SilentlyContinue |
        Where-Object { $_.LastWriteTimeUtc -ge $cutoff -and ($_.Name -match 'test|nuke|build') } |
        Sort-Object LastWriteTimeUtc |
        Select-Object -First 80 |
        ForEach-Object { W ("CAND {0:o} {1} {2}" -f $_.LastWriteTimeUtc, $_.Length, $_.FullName) }
}

# Current-turn N3 context (do not print the key)
foreach ($p in @(
    'F:\GitHub\McpServer\.mcpServer\grok\current-turn.yaml',
    'F:\GitHub\McpServer\.mcpServer\claude\current-turn.yaml'
)) {
    W "FILE=$p EXISTS=$(Test-Path -LiteralPath $p)"
    if (Test-Path -LiteralPath $p) {
        $text = Get-Content -LiteralPath $p -Raw
        $idx = $text.IndexOf('N3fWcoY')
        W "N3_INDEX=$idx LEN=$($text.Length)"
        if ($idx -ge 0) {
            $start = [Math]::Max(0, $idx - 80)
            $len = [Math]::Min(160, $text.Length - $start)
            $clip = $text.Substring($start, $len) -replace 'N3fWcoY[A-Za-z0-9_\-]*', 'N3fWcoY[REDACTED]'
            W "N3_CONTEXT=$clip"
        }
    }
}

W "UTC_END=$(Get-Date -AsUTC -Format o)"
$lines | Set-Content -LiteralPath $out -Encoding utf8
W "WROTE=$out"
