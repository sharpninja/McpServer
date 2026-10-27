$ErrorActionPreference = 'Stop'
$p = 'F:\GitHub\McpServer\.worktrees\sessionlife-p2-contracts-5cb2\docs\receipts\sessionlife-completion\20260928-p0-r3\acceptance-manifest.json'
$before = [System.Text.Json.Nodes.JsonNode]::Parse([System.IO.File]::ReadAllText($p))

$raw = @(
  'McpServer.Support.Mcp.Tests.Services.SessionLogTurnContextValidatorTests.ValidateForNewEntry_NullPlanFile_ThrowsArgumentException',
  'McpServer.Support.Mcp.Tests.Services.SessionLogTurnContextValidatorTests.ValidateForNewEntry_OmittedTodoIdEmpty_ThrowsArgumentException',
  'McpServer.Support.Mcp.Tests.Services.SessionLogTurnContextValidatorTests.ValidateForNewEntry_WhitespacePlanFile_ThrowsArgumentException',
  'McpServer.Support.Mcp.Tests.Services.SessionLogTurnContextValidatorTests.ValidateForNewEntry_BothNone_Succeeds',
  'McpServer.Support.Mcp.Tests.Services.SessionLogServiceTurnContextTests.UpsertTurnAsync_NewTurnWithoutPlanFile_ThrowsAndDoesNotInsert',
  'McpServer.Support.Mcp.Tests.Services.SessionLogServiceTurnContextTests.SubmitAsync_NewTurnMissingFields_Throws',
  'McpServer.Support.Mcp.Tests.Services.SessionLogServiceTurnContextTests.UpsertTurnAsync_NewTurnWithNoneNone_PersistsNone'
)
$gate = @(
  'NukeBuild.Tests.SessionLifeUnitGateValidatorTests.Validate_SharedInvalidArtifact_IsRejectedInUnitLane(invalidFixture: ZeroDiscoveredTests)',
  'NukeBuild.Tests.SessionLifeUnitGateValidatorTests.Validate_SharedInvalidArtifact_IsRejectedInUnitLane(invalidFixture: FailedTrxTests)',
  'NukeBuild.Tests.SessionLifeUnitGateValidatorTests.Validate_SharedInvalidArtifact_IsRejectedInUnitLane(invalidFixture: MissingProjectReport)',
  'NukeBuild.Tests.SessionLifeUnitGateValidatorTests.Validate_UnitOnlyInvalidArtifact_IsRejected(invalidFixture: FailedPesterTests)',
  'NukeBuild.Tests.SessionLifeUnitGateValidatorTests.Validate_UnitOnlyInvalidArtifact_IsRejected(invalidFixture: SkippedPesterTests)',
  'NukeBuild.Tests.SessionLifeUnitGateValidatorTests.Validate_UnitOnlyInvalidArtifact_IsRejected(invalidFixture: MissingPesterReport)',
  'NukeBuild.Tests.SessionLifeUnitGateValidatorTests.Validate_UnitOnlyInvalidArtifact_IsRejected(invalidFixture: MissingBuildTestsReport)',
  'NukeBuild.Tests.SessionLifeUnitGateValidatorTests.Validate_RequiredCommandDefect_IsRejected(lane: "unit", commandName: "pester", defect: NonZero)',
  'NukeBuild.Tests.SessionLifeUnitGateValidatorTests.Validate_RequiredCommandDefect_IsRejected(lane: "unit", commandName: "nuke-test", defect: NonZero)',
  'NukeBuild.Tests.SessionLifeUnitGateValidatorTests.Validate_RequiredCommandDefect_IsRejected(lane: "unit", commandName: "build-tests", defect: NonZero)',
  'NukeBuild.Tests.SessionLifeUnitGateConsumerTests.ValidateSessionLifeUnitGate_DelegatesExactRequestExactlyOnce',
  'NukeBuild.Tests.SessionLifeUnitGateConsumerTests.ValidateSessionLifeUnitGate_RejectsMissingRepeatedCopiedOrWrongTargetCall'
)
$coord = 'McpServer.Repl.Core.Tests.SessionLogPersistenceCoordinatorIsolationTests.'
$openVerbs = @(
  "${coord}Coordinator_Open_PrimaryFailFailsafeOk_PluginSeesSuccess",
  "${coord}Coordinator_Begin_PrimaryFailFailsafeOk_PluginSeesSuccess",
  "${coord}Coordinator_Update_PrimaryFailFailsafeOk_PluginSeesSuccess",
  "${coord}Coordinator_AppendDialog_PrimaryFailFailsafeOk_PluginSeesSuccess",
  "${coord}Coordinator_AppendActions_PrimaryFailFailsafeOk_PluginSeesSuccess",
  'FR-MCP-SESSIONLIFE P2 cache identity metadata and outcomes.covers primary queued and lost outcomes for every session verb'
)
$srcValidator = 'tests/McpServer.Support.Mcp.Tests/Services/SessionLogTurnContextValidatorTests.cs'
$srcService = 'tests/McpServer.Support.Mcp.Tests/Services/SessionLogServiceTurnContextTests.cs'
$srcConsumer = 'tests/Build.Tests/SessionLifeUnitGateConsumerTests.cs'
$srcPester195 = 'plugins/core/test-fixtures/pester/PluginPowerShellRuntime.Tests.ps1'
$srcLife = 'plugins/core/test-fixtures/pester/SessionLogLifecycle.Tests.ps1'
$srcP2 = 'plugins/core/test-fixtures/pester/SessionLogP2Contracts.Tests.ps1'
$srcCoord = 'tests/McpServer.Repl.Core.Tests/SessionLogPersistenceCoordinatorIsolationTests.cs'
$srcStrategy = 'tests/McpServer.Repl.Core.Tests/SessionLogPersistenceStrategyTests.cs'
$srcBegin = 'plugins/core/test-fixtures/pester/SessionLogTurnContextBeginTurn.Tests.ps1'
$kind = 'observed passing TRX names and Pester display labels'

$plan = @{
  'FR-MCP-SESSIONLIFE-002-AC002' = @{ Add = $raw; Paths = @($srcValidator, $srcService, $srcP2) }
  'TR-MCP-SESSIONLIFE-002-AC003' = @{ Add = $raw; Paths = @($srcValidator, $srcService, $srcP2) }
  'TEST-MCP-SESSIONLIFE-002-AC001' = @{ Add = $raw; Paths = @($srcValidator, $srcService, $srcP2) }
  'TEST-MCP-SESSIONLIFE-002-AC004' = @{ Add = $gate; Paths = @($srcConsumer) }
  'FR-MCP-170-AC002' = @{ Add = @('TEST-MCP-195 session-log incremental persist and failsafe drain.TEST-MCP-195 appendDialog uses AppendDialogAsync not full SubmitAsync when current-turn exists'); Paths = @($srcPester195) }
  'FR-MCP-170-AC003' = @{ Add = @(
      'McpServer.Support.Mcp.Tests.Controllers.SessionLogControllerErrorTests.AppendDialogAsync_MissingTurn_ReturnsNotFoundRetryableFalse',
      'FR-MCP-SESSIONLIFE-001 degraded begin keeps the turn cache.degraded 404 queues a session_dialog failsafe and does not say failsafe not used'
    ); Paths = @($srcLife) }
  'AC-FR-MCP-SESSIONLOGCTX-001-002' = @{ Add = @(
      'McpServer.Support.Mcp.Tests.Services.SessionLogTurnContextValidatorTests.ValidateForNewEntry_BothNone_Succeeds',
      'McpServer.Support.Mcp.Tests.Services.SessionLogTurnContextValidatorTests.ValidateForNewEntry_LowercaseNone_ThrowsArgumentException',
      'McpServer.Support.Mcp.Tests.Services.SessionLogServiceTurnContextTests.UpsertTurnAsync_NewTurnWithNoneNone_PersistsNone',
      'FR-MCP-SESSIONLIFE P2 cache identity metadata and outcomes.stores canceled with exact None when a new request supersedes an open turn',
      'FR-MCP-SESSIONLIFE P2 cache identity metadata and outcomes.sends exact None on updateTurn when caller clears planFile and todoId',
      'MCP-SESSIONLOG-002 workflow.sessionlog.beginTurn planFile/todoId.Invoke-WorkflowBeginTurn_FirstTurn_SendsNoneWhenNoPlanMap'
    ); Paths = @($srcValidator, $srcService, $srcP2, $srcBegin) }
  'AC-FR-MCP-SESSIONLOGCTX-001-003' = @{ Add = $raw; Paths = @($srcValidator, $srcService, $srcP2) }
  'FR-MCP-REPL-009-AC001' = @{ Add = $openVerbs; Paths = @($srcCoord, $srcP2) }
  'FR-MCP-REPL-009-AC002' = @{ Add = @(
      'McpServer.Repl.Core.Tests.SessionLogPersistenceStrategyTests.FilesystemPersistAsync_WritesReplayableV4ScopedEnvelope',
      'TEST-MCP-REPL-031 failsafe queue drain.TEST-MCP-REPL-031 replays queued records oldest-first and clears each one after a confirmed submit'
    ); Paths = @($srcStrategy, $srcPester195) }
  'FR-MCP-REPL-009-AC003' = @{ Add = @(
      "${coord}Coordinator_CompleteTurnAndFailTurn_PrimaryFail_ReportsDegradedStrategyNameAndAbsolutePath",
      'FR-MCP-SESSIONLIFE P2 cache identity metadata and outcomes.covers primary queued and lost outcomes for every session verb'
    ); Paths = @($srcCoord, $srcP2) }
  'FR-MCP-REPL-009-AC004' = @{ Add = @(
      "${coord}Coordinator_PrimarySuccess_NoPendingFailsafeArtifact",
      'TEST-MCP-REPL-025 PowerShell REPL persistence boundary.writes a YAML failsafe before submit and clears it after durable success',
      'FR-MCP-SESSIONLIFE P2 cache identity metadata and outcomes.covers primary queued and lost outcomes for every session verb'
    ); Paths = @($srcCoord, $srcPester195, $srcP2) }
}

function Format-JsonString([string]$Value) {
  return ($Value | ConvertTo-Json -Compress)
}

function Find-CoverageLine([string[]]$Lines, [string]$Id) {
  $needle = '"acceptanceCriterionId": "' + $Id + '"'
  for ($i = 0; $i -lt $Lines.Length; $i++) {
    if ($Lines[$i].Trim() -ne ($needle + ',')) { continue }
    $limit = [Math]::Min($Lines.Length - 1, $i + 90)
    for ($j = $i; $j -le $limit; $j++) {
      if ($Lines[$j] -match '"criterionSpecificExistingTests"') { return $i }
    }
  }
  throw "coverage row not found for $Id"
}

function Find-ArraySpan([string[]]$Lines, [int]$From, [string]$Key) {
  $keyLine = -1
  for ($i = $From; $i -lt [Math]::Min($Lines.Length, $From + 80); $i++) {
    if ($Lines[$i] -match ('"' + [regex]::Escape($Key) + '"')) { $keyLine = $i; break }
  }
  if ($keyLine -lt 0) { throw "key $Key not found after $From" }
  if ($Lines[$keyLine] -match '\[\s*\]') { return @{ Start = $keyLine; End = $keyLine; Empty = $true } }
  for ($i = $keyLine + 1; $i -lt $Lines.Length; $i++) {
    if ($Lines[$i] -match '^\s+\],$') { return @{ Start = $keyLine; End = $i; Empty = $false } }
  }
  throw "array end not found for $Key"
}

function New-ArrayLines([string]$Key, [string[]]$Values) {
  $lines = New-Object System.Collections.Generic.List[string]
  [void]$lines.Add('      "' + $Key + '": [')
  for ($i = 0; $i -lt $Values.Count; $i++) {
    $comma = if ($i -lt ($Values.Count - 1)) { ',' } else { '' }
    [void]$lines.Add('        ' + (Format-JsonString $Values[$i]) + $comma)
  }
  [void]$lines.Add('      ],')
  return $lines.ToArray()
}

$nl = "`r`n"
$rawText = [System.IO.File]::ReadAllText($p)
if ($rawText -notmatch "`r`n") { $nl = "`n" }
$lines = [System.Collections.Generic.List[string]]::new()
$lines.AddRange([System.IO.File]::ReadAllLines($p))

$snapshot = @{}
$rows = $before['requirementAcceptanceCoverage'].AsArray()
foreach ($row in $rows) {
  $id = $row['acceptanceCriterionId'].GetValue[string]()
  if (-not $plan.ContainsKey($id)) { continue }
  $existing = @()
  foreach ($item in $row['criterionSpecificExistingTests'].AsArray()) { $existing += $item.GetValue[string]() }
  $proposed = @()
  foreach ($item in $row['criterionSpecificProposedTests'].AsArray()) { $proposed += $item.GetValue[string]() }
  $paths = @()
  foreach ($item in $row['testSourcePaths'].AsArray()) { $paths += $item.GetValue[string]() }
  $snapshot[$id] = @{
    State = $row['acceptanceState'].GetValue[string]()
    Proposed = $proposed
    Existing = $existing
    Paths = $paths
  }
}

# Apply from the bottom so earlier indexes stay valid.
$ordered = $plan.Keys | ForEach-Object {
  $idx = Find-CoverageLine -Lines $lines.ToArray() -Id $_
  [pscustomobject]@{ Id = $_; Index = $idx }
} | Sort-Object Index -Descending

foreach ($item in $ordered) {
  $id = $item.Id
  $start = $item.Index
  $add = @($plan[$id].Add)
  $merged = [System.Collections.Generic.List[string]]::new()
  foreach ($name in $snapshot[$id].Existing) { if (-not $merged.Contains($name)) { [void]$merged.Add($name) } }
  foreach ($name in $add) { if (-not $merged.Contains($name)) { [void]$merged.Add($name) } }
  $span = Find-ArraySpan -Lines $lines.ToArray() -From $start -Key 'criterionSpecificExistingTests'
  $replacement = New-ArrayLines -Key 'criterionSpecificExistingTests' -Values $merged.ToArray()
  $lines.RemoveRange($span.Start, ($span.End - $span.Start + 1))
  for ($k = $replacement.Length - 1; $k -ge 0; $k--) { $lines.Insert($span.Start, $replacement[$k]) }

  $current = $lines.ToArray()
  $rel = -1
  for ($i = $start; $i -lt [Math]::Min($current.Length, $start + 120); $i++) {
    if ($current[$i] -match '"criterionSpecificRelation"') { $rel = $i; break }
  }
  if ($rel -lt 0) { throw "relation missing for $id" }
  $lines[$rel] = '      "criterionSpecificRelation": "executed-criterion-specific",'
  $current = $lines.ToArray()
  $kindLine = -1
  for ($i = $start; $i -lt [Math]::Min($current.Length, $start + 120); $i++) {
    if ($current[$i] -match '"existingCandidateTestNameKind"') { $kindLine = $i; break }
  }
  if ($kindLine -lt 0) { throw "kind missing for $id" }
  $lines[$kindLine] = '      "existingCandidateTestNameKind": ' + (Format-JsonString $kind) + ','

  $pathSpan = Find-ArraySpan -Lines $lines.ToArray() -From $start -Key 'testSourcePaths'
  $pathValues = [System.Collections.Generic.List[string]]::new()
  foreach ($path in $snapshot[$id].Paths) { if (-not $pathValues.Contains($path)) { [void]$pathValues.Add($path) } }
  foreach ($path in @($plan[$id].Paths)) { if (-not $pathValues.Contains($path)) { [void]$pathValues.Add($path) } }
  $pathReplacement = New-ArrayLines -Key 'testSourcePaths' -Values $pathValues.ToArray()
  $lines.RemoveRange($pathSpan.Start, ($pathSpan.End - $pathSpan.Start + 1))
  for ($k = $pathReplacement.Length - 1; $k -ge 0; $k--) { $lines.Insert($pathSpan.Start, $pathReplacement[$k]) }
}

for ($i = 0; $i -lt $lines.Count; $i++) {
  if ($lines[$i] -match '"updatedAtUtc":' -and $i -gt 0 -and $lines[$i - 1] -match '"p2MappingRevision"') {
    $lines[$i] = '    "updatedAtUtc": ' + (Format-JsonString ([DateTime]::UtcNow.ToString('o'))) + ','
  }
  if ($lines[$i] -match '"note": "HV19/HV20') {
    $lines[$i] = '    "note": ' + (Format-JsonString 'Map executed raw metadata, unit-gate rejection, and legacy failsafe tests. Acceptance stays not-accepted.') + ','
  }
  if ($lines[$i] -match '"priorDisagree": "20261001T014059Z"') {
    $lines[$i] = '    "priorDisagree": "20261002T203600Z"'
  }
}

$updated = ($lines -join $nl) + $nl
$after = [System.Text.Json.Nodes.JsonNode]::Parse($updated)
if ($after['requirementAcceptanceCoverage'].AsArray().Count -ne $before['requirementAcceptanceCoverage'].AsArray().Count) {
  throw 'row count changed'
}
$beforeRows = @{}
foreach ($row in $before['requirementAcceptanceCoverage'].AsArray()) {
  $beforeRows[$row['acceptanceCriterionId'].GetValue[string]()] = $row
}
foreach ($row in $after['requirementAcceptanceCoverage'].AsArray()) {
  $id = $row['acceptanceCriterionId'].GetValue[string]()
  $old = $beforeRows[$id]
  if ($row['acceptanceState'].GetValue[string]() -ne $old['acceptanceState'].GetValue[string]()) { throw "$id acceptance changed" }
  if ($row['storeSatisfied'].ToJsonString() -ne $old['storeSatisfied'].ToJsonString()) { throw "$id storeSatisfied changed" }
  $oldProposed = @($old['criterionSpecificProposedTests'].AsArray() | ForEach-Object { $_.GetValue[string]() })
  $newProposed = @($row['criterionSpecificProposedTests'].AsArray() | ForEach-Object { $_.GetValue[string]() })
  if (($oldProposed -join '|') -ne ($newProposed -join '|')) { throw "$id proposed tests changed" }
  if ($plan.ContainsKey($id)) {
    if ($row['criterionSpecificRelation'].GetValue[string]() -ne 'executed-criterion-specific') { throw "$id relation" }
    $names = @($row['criterionSpecificExistingTests'].AsArray() | ForEach-Object { $_.GetValue[string]() })
    foreach ($need in @($plan[$id].Add)) {
      if ($names -notcontains $need) { throw "$id missing $need" }
    }
    Write-Output ("MAPPED $id count=$($names.Count) relation=executed-criterion-specific state=not-accepted")
  } else {
    $oldNames = @($old['criterionSpecificExistingTests'].AsArray() | ForEach-Object { $_.GetValue[string]() })
    $newNames = @($row['criterionSpecificExistingTests'].AsArray() | ForEach-Object { $_.GetValue[string]() })
    if (($oldNames -join '|') -ne ($newNames -join '|')) { throw "untargeted $id tests changed" }
    if ($old['criterionSpecificRelation'].GetValue[string]() -ne $row['criterionSpecificRelation'].GetValue[string]()) {
      throw "untargeted $id relation changed"
    }
  }
}
if ($after['p2MappingRevision']['priorDisagree'].GetValue[string]() -ne '20261002T203600Z') { throw 'revision stamp missing' }
$utf8 = New-Object System.Text.UTF8Encoding $false
[System.IO.File]::WriteAllText($p, $updated, $utf8)
Write-Output 'WROTE manifest'
