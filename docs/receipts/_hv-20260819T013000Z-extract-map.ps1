$ErrorActionPreference = 'Stop'
$outDir = 'F:\GitHub\McpServer\docs\receipts\_hv-20260819T013000Z'
$mapPath = 'C:\Users\kingd\.grok\sessions\F%3A%5CGitHub%5CMcpServer\01a017a3-736b-72d1-9b03-a0b131d4a579\mcp\call-78757055-225c-46a4-a74b-d44a0b040724-111.json'
$raw = Get-Content -LiteralPath $mapPath -Raw
$doc = $raw | ConvertFrom-Json
$items = @()
if ($doc.items) { $items = @($doc.items) }
$wanted = @($items | Where-Object { $_.FrId -match 'TRIAGE|SESSIONLOGCTX' })
$wanted | ConvertTo-Json -Depth 6 | Set-Content -LiteralPath (Join-Path $outDir 'map-triage.json') -Encoding utf8
Write-Output ('MAP_TOTAL=' + $items.Count)
Write-Output ('MAP_TRIAGE=' + $wanted.Count)
$wanted | ForEach-Object {
  $tr = @($_.TrIds) -join ','
  $te = @($_.TestIds) -join ','
  Write-Output ($_.FrId + ' TR=[' + $tr + '] TEST=[' + $te + ']')
}
