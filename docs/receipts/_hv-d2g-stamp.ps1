#Requires -Version 7.0
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$now = [DateTime]::UtcNow
$utc = $now.ToString('yyyyMMddTHHmmssZ')
$iso = $now.ToString('o')
$out = Join-Path 'F:\GitHub\McpServer\docs\receipts' ('_hv-d2g-' + $utc)
New-Item -ItemType Directory -Force -Path $out | Out-Null
$ids = [ordered]@{
    utc = $utc
    iso = $iso
    agent = 'GrokSubagentHostile'
    sessionId = 'GrokSubagentHostile-' + $utc + '-pluginhandoff-d2g'
    requestId = 'req-' + $utc + '-001-d2-green-pluginhandoff'
    outDir = $out
}
$ids | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $out 'ids.json') -Encoding utf8
Write-Output $utc
Write-Output $out
