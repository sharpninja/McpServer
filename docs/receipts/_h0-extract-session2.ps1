#Requires -Version 7.0
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$outDir = 'F:\GitHub\McpServer\docs\receipts\_h0-hostile-raw'
$needleSess = 'GrokCode-20260818T182741Z-plugin-session'
$needleTurn = 'req-20260818T191655Z-004-s0-triagecluster-reqs'

function Write-Slice {
    param([string]$Path, [string]$Needle, [string]$OutName)
    if (-not (Test-Path -LiteralPath $Path)) {
        Write-Output "MISSING $Path"
        return
    }
    $raw = Get-Content -LiteralPath $Path -Raw
    $has = $raw.Contains($Needle)
    Write-Output "FILE $([IO.Path]::GetFileName($Path)) length=$($raw.Length) contains($Needle)=$has"
    if ($has) {
        $idx = $raw.IndexOf($Needle)
        $start = [Math]::Max(0, $idx - 400)
        $slice = $raw.Substring($start, [Math]::Min(9000, $raw.Length - $start))
        Set-Content -LiteralPath (Join-Path $outDir $OutName) -Value $slice -Encoding utf8
        Write-Output "WROTE $OutName"
    }
}

Write-Slice -Path (Join-Path $outDir '20-sessionlog-query-implementer-turn.txt') -Needle $needleSess -OutName '24c-dump-session-slice.txt'
Write-Slice -Path (Join-Path $outDir '20-sessionlog-query-implementer-turn.txt') -Needle $needleTurn -OutName '24d-dump-turn-slice.txt'
Write-Slice -Path (Join-Path $outDir '21-sessionlog-query-implementer-session.txt') -Needle $needleSess -OutName '24e-dump21-session-slice.txt'
Write-Slice -Path (Join-Path $outDir '22-queryHistory-GrokCode.txt') -Needle $needleSess -OutName '24f-history-session-slice.txt'
