$ErrorActionPreference = 'Stop'
$wt = 'F:\GitHub\McpServer\.worktrees\sessionlife-p2-contracts-5cb2'
Write-Output '==== not_found in pester ===='
rg -n -g '*.ps1' 'not_found|auditDialog' (Join-Path $wt 'plugins\core\test-fixtures\pester')
Write-Output '==== CTX 002 text ===='
$m = Get-Content -LiteralPath (Join-Path $wt 'docs\receipts\sessionlife-completion\20260928-p0-r3\acceptance-manifest.json') -Raw | ConvertFrom-Json
$row = @($m.requirementAcceptanceCoverage) | Where-Object { $_.acceptanceCriterionId -eq 'AC-FR-MCP-SESSIONLOGCTX-001-002' }
Write-Output $row.text
Write-Output '==== TRX NAMES ===='
$trxRoot = Join-Path $wt 'TestResults\p2-unit-legion-20261002T202158Z'
$needles = @(
    'UpsertTurnAsync_NewTurnWithoutPlanFile',
    'SubmitAsync_NewTurnMissingFields',
    'UpsertTurnAsync_NewTurnWithNoneNone',
    'ValidateForNewEntry_NullPlanFile',
    'ValidateForNewEntry_OmittedTodoIdEmpty',
    'ValidateForNewEntry_WhitespacePlanFile',
    'ValidateForNewEntry_BothNone',
    'Validate_SharedInvalidArtifact_IsRejectedInUnitLane',
    'Validate_UnitOnlyInvalidArtifact_IsRejected',
    'Validate_RequiredCommandDefect_IsRejected',
    'ValidateSessionLifeUnitGate_DelegatesExactRequestExactlyOnce',
    'ValidateSessionLifeUnitGate_RejectsMissingRepeatedCopiedOrWrongTargetCall'
)
Get-ChildItem -LiteralPath $trxRoot -Recurse -Filter '*.trx' | ForEach-Object {
    $text = [System.IO.File]::ReadAllText($_.FullName)
    foreach ($n in $needles) {
        if ($text.Contains($n)) { Write-Output ($_.Name + ' HAS ' + $n) }
    }
}
