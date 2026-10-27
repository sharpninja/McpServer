$ErrorActionPreference = 'Stop'
$manifest = 'F:\GitHub\McpServer\.worktrees\sessionlife-p2-contracts-5cb2\docs\receipts\sessionlife-completion\20260928-p0-r3\acceptance-manifest.json'
$doc = Get-Content -Raw -LiteralPath $manifest | ConvertFrom-Json
$ids = @(
  'FR-MCP-SESSIONLIFE-001-AC002',
  'TR-MCP-SESSIONLIFE-001-AC001',
  'TR-MCP-SESSIONLIFE-001-AC002',
  'TEST-MCP-SESSIONLIFE-001-AC001',
  'TEST-MCP-SESSIONLIFE-001-AC002',
  'FR-MCP-SESSIONLIFE-002-AC002',
  'TR-MCP-SESSIONLIFE-002-AC003',
  'TEST-MCP-SESSIONLIFE-002-AC001',
  'AC-FR-MCP-SESSIONLOGCTX-001-003'
)
$rows = @($doc.requirementAcceptanceCoverage)
foreach ($id in $ids) {
  $row = $rows | Where-Object { $_.requirementId -eq $id -or $_.id -eq $id -or $_.criterionId -eq $id -or $_.acceptanceCriterionId -eq $id }
  if (-not $row) {
    $row = $rows | Where-Object {
      ($_.PSObject.Properties.Value | ForEach-Object { "$_" }) -contains $id
    } | Select-Object -First 1
  }
  Write-Output "======== $id count=$(@($row).Count) ========"
  if (-not $row) { Write-Output 'NOT FOUND'; continue }
  $one = @($row)[0]
  $one.PSObject.Properties.Name | ForEach-Object { Write-Output $_ }
}
