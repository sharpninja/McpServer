$ErrorActionPreference = 'Stop'
$root = 'F:\GitHub\McpServer\.worktrees\sessionlife-p2-contracts-5cb2\TestResults\p2-unit-legion-20261002T202158Z'
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
Get-ChildItem $root -Recurse -Filter *.trx | ForEach-Object {
  $file = $_.FullName
  [xml]$xml = Get-Content -Raw $file
  $ns = New-Object System.Xml.XmlNamespaceManager($xml.NameTable)
  $ns.AddNamespace('t', 'http://microsoft.com/schemas/VisualStudio/TeamTest/2010')
  $nodes = $xml.SelectNodes('//t:UnitTestResult', $ns)
  foreach ($n in $nodes) {
    $name = [string]$n.testName
    foreach ($needle in $needles) {
      if ($name -like "*$needle*") {
        Write-Output ("TRX " + $_.Name + " outcome=" + $n.outcome)
        Write-Output $name
        Write-Output ""
      }
    }
  }
}
