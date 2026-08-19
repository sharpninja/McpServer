$ErrorActionPreference = 'Stop'
$outDir = 'F:\GitHub\McpServer\docs\receipts\_hv-20260819T013000Z'
New-Item -ItemType Directory -Force -Path $outDir | Out-Null
$frPath = 'C:\Users\kingd\.grok\sessions\F%3A%5CGitHub%5CMcpServer\01a017a3-736b-72d1-9b03-a0b131d4a579\mcp\call-2164c54c-10c2-4758-9717-2b7f968f49e1-99.json'
$raw = Get-Content -LiteralPath $frPath -Raw
$doc = $raw | ConvertFrom-Json
$items = @()
if ($doc.items) { $items = @($doc.items) }
elseif ($doc.result.items) { $items = @($doc.result.items) }
elseif ($doc.content) {
  # MCP wrapper sometimes puts text
}
$triage = @($items | Where-Object { $_.Id -match 'TRIAGE|SESSIONLOGCTX' })
$summary = foreach ($fr in $triage) {
  $acs = @()
  if ($fr.AcceptanceCriteria) {
    $acs = @($fr.AcceptanceCriteria | ForEach-Object {
      [pscustomobject]@{ id = $_.id; text = $_.text; isSatisfied = $_.isSatisfied }
    })
  }
  [pscustomobject]@{
    Id = $fr.Id
    Title = $fr.Title
    Status = $fr.Status
    Priority = $fr.Priority
    AcCount = $acs.Count
    AcceptanceCriteria = $acs
  }
}
$summary | ConvertTo-Json -Depth 6 | Set-Content -LiteralPath (Join-Path $outDir 'fr-triage.json') -Encoding utf8
Write-Output ('FR_TOTAL=' + $items.Count)
Write-Output ('FR_TRIAGE=' + $summary.Count)
$summary | ForEach-Object { Write-Output ($_.Id + ' status=' + $_.Status + ' ac=' + $_.AcCount) }
