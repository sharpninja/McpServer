#Requires -Version 7.0
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Continue'

$workspace = 'F:\GitHub\McpServer'
$outDir = Join-Path $workspace 'docs\receipts\_hv-234800Z'
New-Item -ItemType Directory -Force -Path $outDir | Out-Null
Set-Location -LiteralPath $workspace

function Invoke-NamedTest {
    param(
        [Parameter(Mandatory)][string]$Project,
        [Parameter(Mandatory)][string]$Filter,
        [Parameter(Mandatory)][string]$LogName
    )
    $log = Join-Path $outDir $LogName
    Write-Output ('BEGIN_TEST ' + $LogName + ' filter=' + $Filter)
    & dotnet test $Project -c Debug --filter $Filter --nologo --verbosity minimal | Tee-Object -FilePath $log
    Write-Output ('EXIT_' + $LogName + '=' + $LASTEXITCODE)
    return $LASTEXITCODE
}

$support = 'tests\McpServer.Support.Mcp.Tests\McpServer.Support.Mcp.Tests.csproj'
$repl = 'tests\McpServer.Repl.Core.Tests\McpServer.Repl.Core.Tests.csproj'
$build = 'tests\Build.Tests\Build.Tests.csproj'

# A1 four-field envelope
Invoke-NamedTest -Project $support -Filter 'FullyQualifiedName~SessionLogControllerErrorTests' -LogName 'rest.log'
Invoke-NamedTest -Project $support -Filter 'FullyQualifiedName~McpToolErrorEnvelopeTests' -LogName 'tool-envelope.log'
Invoke-NamedTest -Project $support -Filter 'FullyQualifiedName~McpToolBackendUnavailableErrorTests' -LogName 'tool-backend.log'
Invoke-NamedTest -Project $repl -Filter 'FullyQualifiedName~ReplMcpErrorClassifierTests' -LogName 'repl-classifier.log'
Invoke-NamedTest -Project $support -Filter 'FullyQualifiedName~McpErrorClassifierTests' -LogName 'classifier.log'

# A2 schema
Invoke-NamedTest -Project $support -Filter 'FullyQualifiedName~SessionLogSchemaGuardTests' -LogName 'schema.log'

# A3 budget + hung SaveChanges + unreachable SQL
Invoke-NamedTest -Project $support -Filter 'FullyQualifiedName~SessionLogTriageStoreTests.SubmitAsync_HungSaveChanges' -LogName 'hung-save.log'
Invoke-NamedTest -Project $support -Filter 'FullyQualifiedName~StorageCommandBudgetTests' -LogName 'budget.log'
Invoke-NamedTest -Project $support -Filter 'FullyQualifiedName~TriageServiceTests.SubmitReportAsync_UnreachableSql' -LogName 'triage-unreach.log'

# A4 session store
Invoke-NamedTest -Project $support -Filter 'FullyQualifiedName~SessionLogTriageStoreTests' -LogName 'store.log'

# A5 plugin Pester
$pesterLog = Join-Path $outDir 'pester.log'
Write-Output 'BEGIN_PESTER'
$pester = Invoke-Pester -Path 'plugins\core\test-fixtures\pester\TriagePluginIdentity.Tests.ps1' -Output Detailed -PassThru
$pester | Out-String | Set-Content -LiteralPath $pesterLog -Encoding utf8
Write-Output ('PESTER Passed=' + $pester.PassedCount + ' Failed=' + $pester.FailedCount + ' Skipped=' + $pester.SkippedCount + ' NotRun=' + $pester.NotRunCount + ' Total=' + $pester.TotalCount)

# A5 Build cache retain
Invoke-NamedTest -Project $build -Filter 'FullyQualifiedName~ReplacePluginCache_OpenTurn_RetainsExistingCache|FullyQualifiedName~ReplacePluginCache_ReplacesReadOnlyExistingCache' -LogName 'build-cache.log'

# A6 prior EXEC/TR/HELP
Invoke-NamedTest -Project $support -Filter 'FullyQualifiedName~TodoExecutionServiceTests.SetTestPlanAsync_DurableExecTodoMissingExecutionState_Rehydrates|FullyQualifiedName~TodoExecutionServiceTests.GenerateNextTodoIdAsync_SkipsSoftDeletedDurableId|FullyQualifiedName~TodoExecutionServiceTests.CreateTodosFromPlanAsync_InvalidDependsOn_FailsBeforeInsert|FullyQualifiedName~AgentHelpConversationServiceTests.SubmitTurnAsync_StrategyReceivesFiniteHelperTimeout' -LogName 'exec-help.log'
Invoke-NamedTest -Project $repl -Filter 'FullyQualifiedName~RequirementsWorkflowMetadataTests.GetTrAsync_LegacyId_DoesNotRejectCanonicalFormat|FullyQualifiedName~RequirementsWorkflowMetadataTests.CreateTrAsync_LegacyId_StillRejected' -LogName 'req-tr.log'

Write-Output 'ALL_TESTS_DONE'
