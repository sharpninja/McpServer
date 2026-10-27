$ErrorActionPreference = 'Stop'
$wt = 'F:\GitHub\McpServer\.worktrees\sessionlife-p2-contracts-5cb2'
$man = Get-Content -Raw (Join-Path $wt 'docs\receipts\sessionlife-completion\20260928-p0-r3\acceptance-manifest.json') | ConvertFrom-Json
$rows = @($man.requirementAcceptanceCoverage)
$csharp = $rows | Where-Object { @($_.criterionSpecificExistingTests) -match 'McpServer\.|NukeBuild\.' }
Write-Output ("CSHARP_ROWS=" + @($csharp).Count)
$csharp | Select-Object -First 6 | ForEach-Object {
  Write-Output ("===== " + $_.acceptanceCriterionId + " relation=" + $_.criterionSpecificRelation)
  @($_.criterionSpecificExistingTests) | Select-Object -First 8 | ForEach-Object { Write-Output ("  - " + $_) }
}
Write-Output '==== p2MappingRevision keys ===='
$man.p2MappingRevision | Get-Member -MemberType NoteProperty | ForEach-Object { Write-Output $_.Name }
Write-Output '==== revision sample ===='
$man.p2MappingRevision | ConvertTo-Json -Depth 4
