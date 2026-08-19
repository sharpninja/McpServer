#Requires -Version 7.0
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$outDir = 'F:\GitHub\McpServer\docs\receipts\_h0-hostile-raw'
$path = Join-Path $outDir '20-sessionlog-query-implementer-turn.txt'
$raw = Get-Content -LiteralPath $path -Raw

$patterns = @(
    'sessionId: GrokCode-20260818T182741Z-plugin-session'
    'requestId: req-20260818T191655Z-004-s0-triagecluster-reqs'
    'req-20260818T191655Z-004-s0-triagecluster-reqs'
)
foreach ($p in $patterns) {
    $idxs = [System.Collections.Generic.List[int]]::new()
    $start = 0
    while ($true) {
        $i = $raw.IndexOf($p, $start, [System.StringComparison]::Ordinal)
        if ($i -lt 0) { break }
        $idxs.Add($i)
        $start = $i + $p.Length
        if ($idxs.Count -ge 8) { break }
    }
    Write-Output ("PATTERN count={0} '{1}'" -f $idxs.Count, $p)
    $n = 0
    foreach ($i in $idxs) {
        $n++
        $from = [Math]::Max(0, $i - 180)
        $ctx = $raw.Substring($from, [Math]::Min(500, $raw.Length - $from))
        Write-Output ("HIT {0} at {1}" -f $n, $i)
        Write-Output $ctx
        Write-Output '-----'
    }
}
