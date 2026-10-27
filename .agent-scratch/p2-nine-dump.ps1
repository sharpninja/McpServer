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
$out = Join-Path $env:USERPROFILE '.grok\p2-nine-dump.txt'
$sb = New-Object System.Text.StringBuilder
foreach ($id in $ids) {
  $one = @($doc.requirementAcceptanceCoverage | Where-Object { $_.acceptanceCriterionId -eq $id })[0]
  [void]$sb.AppendLine("======== $id ========")
  [void]$sb.AppendLine("TEXT: $($one.text)")
  [void]$sb.AppendLine("REAL: $($one.realCodeAssertion)")
  [void]$sb.AppendLine("STATE: $($one.acceptanceState) store=$($one.storeSatisfied)")
  [void]$sb.AppendLine('EXISTING:')
  foreach ($n in @($one.criterionSpecificExistingTests)) { [void]$sb.AppendLine("  - $n") }
  [void]$sb.AppendLine('PATHS:')
  foreach ($n in @($one.testSourcePaths)) { [void]$sb.AppendLine("  - $n") }
  [void]$sb.AppendLine('')
}
[System.IO.File]::WriteAllText($out, $sb.ToString())
Write-Output $out
Write-Output ("CHARS=" + $sb.Length)
