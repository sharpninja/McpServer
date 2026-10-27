$ErrorActionPreference = 'Stop'
$wt = 'F:\GitHub\McpServer\.worktrees\sessionlife-p2-contracts-5cb2'
$man = Get-Content -Raw (Join-Path $wt 'docs\receipts\sessionlife-completion\20260928-p0-r3\acceptance-manifest.json') | ConvertFrom-Json
$ids = @(
  'FR-MCP-SESSIONLIFE-002-AC002',
  'TR-MCP-SESSIONLIFE-002-AC003',
  'TEST-MCP-SESSIONLIFE-002-AC001',
  'TEST-MCP-SESSIONLIFE-002-AC004',
  'FR-MCP-170-AC002',
  'FR-MCP-170-AC003',
  'AC-FR-MCP-SESSIONLOGCTX-001-002',
  'AC-FR-MCP-SESSIONLOGCTX-001-003',
  'FR-MCP-REPL-009-AC001',
  'FR-MCP-REPL-009-AC002',
  'FR-MCP-REPL-009-AC003',
  'FR-MCP-REPL-009-AC004'
)
$rows = @($man.requirementAcceptanceCoverage)
foreach ($id in $ids) {
  $row = $rows | Where-Object { $_.acceptanceCriterionId -eq $id } | Select-Object -First 1
  if (-not $row) { Write-Output "MISSING $id"; continue }
  Write-Output "===== $id ====="
  Write-Output ("state=" + $row.acceptanceState)
  Write-Output ("relation=" + $row.criterionSpecificRelation)
  Write-Output ("proposedKind=" + $row.proposedTestNameKind)
  Write-Output ("existingKind=" + $row.existingCandidateTestNameKind)
  Write-Output ("proposed=" + $row.criterionSpecificProposedTests)
  Write-Output "existing:"
  @($row.criterionSpecificExistingTests) | ForEach-Object { Write-Output ("  - " + $_) }
  Write-Output ("TEXT: " + $row.text)
  Write-Output ""
}
