$ErrorActionPreference = 'Stop'
$wt = 'F:\GitHub\McpServer\.worktrees\sessionlife-p2-contracts-5cb2'
$root = Join-Path $wt 'TestResults\p2-unit-legion-20261002T202158Z'
$needles = @(
  'Coordinator_Open_PrimaryFailFailsafeOk',
  'Coordinator_Begin_PrimaryFailFailsafeOk',
  'Coordinator_Update_PrimaryFailFailsafeOk',
  'Coordinator_AppendDialog_PrimaryFailFailsafeOk',
  'Coordinator_AppendActions_PrimaryFailFailsafeOk',
  'Coordinator_CompleteTurnAndFailTurn_PrimaryFail',
  'Coordinator_PrimarySuccess_NoPendingFailsafeArtifact',
  'AppendDialogAsync_MissingTurn_ReturnsNotFoundRetryableFalse',
  'Envelope',
  'Failsafe'
)
Get-ChildItem $root -Recurse -Filter *.trx | ForEach-Object {
  if ($_.Name -notmatch 'Repl.Core|Support.Mcp') { return }
  [xml]$xml = Get-Content -Raw $_.FullName
  $ns = New-Object System.Xml.XmlNamespaceManager($xml.NameTable)
  $ns.AddNamespace('t', 'http://microsoft.com/schemas/VisualStudio/TeamTest/2010')
  foreach ($n in $xml.SelectNodes('//t:UnitTestResult', $ns)) {
    $name = [string]$n.testName
    if ($name -match 'Coordinator_|AppendDialogAsync_MissingTurn|FailsafeEnvelope|Replayable|SessionLogFailsafe') {
      Write-Output ($_.Name + ' ' + $n.outcome + ' ' + $name)
    }
  }
}
Write-Output '==== AC2-like tests in repo ===='
Get-ChildItem (Join-Path $wt 'tests') -Recurse -Filter *.cs | ForEach-Object {
  $n = 0
  foreach ($line in [System.IO.File]::ReadLines($_.FullName)) {
    $n++
    if ($line -match 'FR-MCP-REPL-009 AC2|replayable|EnvelopeContains|payloadFingerprint') {
      Write-Output ("{0}:{1}: {2}" -f $_.Name, $n, $line.Trim())
    }
  }
}
