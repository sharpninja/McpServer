$ErrorActionPreference = 'Stop'
$outDir = 'F:\GitHub\McpServer\docs\receipts\_hv-20260819T013000Z'
$testPath = 'C:\Users\kingd\.grok\sessions\F%3A%5CGitHub%5CMcpServer\01a017a3-736b-72d1-9b03-a0b131d4a579\mcp\call-31442869-083b-426f-a60f-7d44a8cd6348-105.json'
$raw = Get-Content -LiteralPath $testPath -Raw
$doc = $raw | ConvertFrom-Json
$items = @()
if ($doc.items) { $items = @($doc.items) }
$wanted = @($items | Where-Object { $_.Id -match 'TRIAGE|SESSIONLOGCTX' })
$summary = foreach ($t in $wanted) {
  $acs = @()
  if ($t.AcceptanceCriteria) {
    $acs = @($t.AcceptanceCriteria | ForEach-Object {
      [pscustomobject]@{ id = $_.id; text = $_.text; isSatisfied = $_.isSatisfied }
    })
  }
  [pscustomobject]@{
    Id = $t.Id
    Title = $t.Title
    Status = $t.Status
    Condition = $t.Condition
    AcCount = $acs.Count
    Ac1 = if ($acs.Count -gt 0) { $acs[0].text } else { $null }
    AcceptanceCriteria = $acs
  }
}
$summary | ConvertTo-Json -Depth 6 | Set-Content -LiteralPath (Join-Path $outDir 'test-triage.json') -Encoding utf8
Write-Output ('TEST_TOTAL=' + $items.Count)
Write-Output ('TEST_TRIAGE=' + $summary.Count)
$summary | ForEach-Object {
  $ac1len = 0
  if ($_.Ac1) { $ac1len = $_.Ac1.Length }
  Write-Output ($_.Id + ' status=' + $_.Status + ' ac=' + $_.AcCount + ' ac1len=' + $ac1len)
}
