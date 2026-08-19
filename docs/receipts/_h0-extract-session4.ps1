#Requires -Version 7.0
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$outDir = 'F:\GitHub\McpServer\docs\receipts\_h0-hostile-raw'
$path = Join-Path $outDir '20-sessionlog-query-implementer-turn.txt'
$raw = Get-Content -LiteralPath $path -Raw
$i = $raw.IndexOf('requestId: req-20260818T191655Z-004-s0-triagecluster-reqs', [System.StringComparison]::Ordinal)
$from = [Math]::Max(0, $i - 250)
$slice = $raw.Substring($from, [Math]::Min(3500, $raw.Length - $from))
$out = Join-Path $outDir '24g-implementer-turn-exact.txt'
Set-Content -LiteralPath $out -Value $slice -Encoding utf8
Write-Output "WROTE $out length=$($slice.Length) idx=$i"
Write-Output $slice
