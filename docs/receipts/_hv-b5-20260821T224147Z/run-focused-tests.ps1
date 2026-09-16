$ErrorActionPreference = 'Continue'
$out = 'F:\GitHub\McpServer\docs\receipts\_hv-b5-20260821T224147Z'
Set-Location 'F:\GitHub\McpServer'

Write-Output '=== COORDINATOR ==='
dotnet test tests/McpServer.Repl.Core.Tests -c Debug --filter 'FullyQualifiedName~SessionLogPersistenceCoordinatorIsolationTests' --logger "trx;LogFileName=hv-b5-coordinator.trx" --results-directory $out | Tee-Object -FilePath (Join-Path $out 'dotnet-coordinator.txt')
Write-Output ('COORDINATOR_EXIT=' + $LASTEXITCODE)

Write-Output '=== CHECKSUM ==='
dotnet test tests/Build.Tests -c Debug --filter 'FullyQualifiedName~SyncAgentPlugins_OfficialPluginLibChecksumsMatchCanonicalCore' --logger "trx;LogFileName=hv-b5-checksum.trx" --results-directory $out | Tee-Object -FilePath (Join-Path $out 'dotnet-checksum.txt')
Write-Output ('CHECKSUM_EXIT=' + $LASTEXITCODE)

Write-Output '=== PERSISTENCE REUSE ==='
dotnet test tests/McpServer.Repl.Core.Tests -c Debug --filter 'FullyQualifiedName~SessionLogPersistence' --logger "trx;LogFileName=hv-b5-persist.trx" --results-directory $out | Tee-Object -FilePath (Join-Path $out 'dotnet-persist.txt')
Write-Output ('PERSIST_EXIT=' + $LASTEXITCODE)

Write-Output '=== FOCUSED PESTER ==='
$cfg = New-PesterConfiguration
$cfg.Run.Path = 'F:\GitHub\McpServer\plugins\core\test-fixtures\pester\PluginPowerShellRuntime.Tests.ps1'
$cfg.Filter.FullName = @(
    '*Get-ReplMethodTimeoutSeconds_WhileDrainingSubmitAsync_IsNotTwoSeconds*',
    '*Get-ReplMethodTimeoutSeconds_CompleteTurnBeginTurn_RemainThirtySecondsWhileDrainSubmitUsesDrainTimeout*',
    '*Invoke-ReplFailsafeDrain_SuccessfulSubmitWithinDrainTimeout_RemovesYaml*',
    '*Invoke-ReplFailsafeDrainOnFirstSuccess_WhileReplRawInFlight_DoesNotRun*',
    '*Get-McpFailsafeDir_MatchesV4FailsafeAgentWorkspacesPending*',
    '*Get-McpFailsafeDir_MigratesLegacyPluginQueue_ChecksumMatchThenDeleteSource*'
)
$cfg.Output.Verbosity = 'Detailed'
$cfg.TestResult.Enabled = $true
$cfg.TestResult.OutputPath = Join-Path $out 'pester-focused.xml'
$cfg.TestResult.OutputFormat = 'NUnitXml'
$pester = Invoke-Pester -Configuration $cfg
$pester | ConvertTo-Json -Depth 4 | Set-Content -LiteralPath (Join-Path $out 'pester-focused.json') -Encoding utf8
Write-Output ('PESTER_FOCUSED_TOTAL=' + $pester.TotalCount)
Write-Output ('PESTER_FOCUSED_PASSED=' + $pester.PassedCount)
Write-Output ('PESTER_FOCUSED_FAILED=' + $pester.FailedCount)
Write-Output ('PESTER_FOCUSED_SKIPPED=' + $pester.SkippedCount)
if ($pester.FailedCount -gt 0) {
    $pester.Failed | ForEach-Object { Write-Output ('FAILED:' + $_.ExpandedName + ' :: ' + $_.ErrorRecord) }
}
Write-Output 'FOCUSED_TESTS_DONE'
