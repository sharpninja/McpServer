$ErrorActionPreference = 'Stop'
$wt = 'F:\GitHub\McpServer\.worktrees\sessionlife-p2-contracts-5cb2'
$m = Get-Content -LiteralPath (Join-Path $wt 'docs\receipts\sessionlife-completion\20260928-p0-r3\acceptance-manifest.json') -Raw | ConvertFrom-Json
$want = @(
    'FR-MCP-SESSIONLIFE-002-AC002',
    'TR-MCP-SESSIONLIFE-002-AC003',
    'TEST-MCP-SESSIONLIFE-002-AC001',
    'TEST-MCP-SESSIONLIFE-002-AC004',
    'FR-MCP-170-AC002',
    'FR-MCP-170-AC003'
)
foreach ($row in @($m.requirementAcceptanceCoverage)) {
    if ($want -notcontains [string]$row.acceptanceCriterionId) { continue }
    Write-Output ('==== ' + $row.acceptanceCriterionId)
    Write-Output ('TEXT=' + $row.text)
    Write-Output ('EXISTING=' + (($row.criterionSpecificExistingTests | ConvertTo-Json -Compress)))
    Write-Output ('PROPOSED=' + [string]$row.criterionSpecificProposedTests)
    Write-Output ('RELATION=' + [string]$row.criterionSpecificRelation)
}
Write-Output '--- TEST NAMES ---'
$patterns = @(
    'class SessionLogServiceTurnContextTests',
    'class SessionLogTurnContextValidatorTests',
    'class SessionLifeUnitGateValidatorTests',
    'class SessionLifeUnitGateConsumerTests'
)
foreach ($pat in $patterns) {
    $hits = rg -n $pat -g '*.cs' $wt
    Write-Output $hits
}
