#Requires -Version 7.0
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Continue'

$workspace = 'F:\GitHub\McpServer'
$outDir = Join-Path $workspace 'docs\receipts\_hv-20260819T000500Z'
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

# Combined Support named subset: envelope, schema, store, budget, triage, exec, help, 139, health
$supportFilter = @(
    'FullyQualifiedName~SessionLogControllerErrorTests',
    'FullyQualifiedName~McpToolErrorEnvelopeTests',
    'FullyQualifiedName~McpToolBackendUnavailableErrorTests',
    'FullyQualifiedName~McpErrorClassifierTests',
    'FullyQualifiedName~SessionLogSchemaGuardTests',
    'FullyQualifiedName~SessionLogTriageStoreTests',
    'FullyQualifiedName~StorageCommandBudgetTests',
    'FullyQualifiedName~TriageServiceTests.SubmitReportAsync_UnreachableSql',
    'FullyQualifiedName~HealthEndpointStoragePayloadTests.HealthPayload_UnreachableStorage',
    'FullyQualifiedName~TodoExecutionServiceTests.SetTestPlanAsync_DurableExecTodoMissingExecutionState_Rehydrates',
    'FullyQualifiedName~TodoExecutionServiceTests.GenerateNextTodoIdAsync_SkipsSoftDeletedDurableId',
    'FullyQualifiedName~TodoExecutionServiceTests.CreateTodosFromPlanAsync_InvalidDependsOn_FailsBeforeInsert',
    'FullyQualifiedName~TodoExecutionServiceTests.CreateTodosFromPlanAsync_WhenLaterLegacyCreateFails_DeletesAlreadyCreatedTodo',
    'FullyQualifiedName~EfTodoServiceTests.CreateAsync_SoftDeletedId_RevivesOrSkips',
    'FullyQualifiedName~AgentHelpConversationServiceTests.SubmitTurnAsync_StrategyReceivesFiniteHelperTimeout',
    'FullyQualifiedName~AgentHelpConversationServiceTests.SubmitTurnAsync_StrategyProgressOnlyOutput',
    'FullyQualifiedName~AgentHelpConversationServiceTests.SubmitTurnAsync_StrategyFailureWithEchoFallback',
    'FullyQualifiedName~UseCaseCqrsTests.CreateUseCase_WithoutPreSeededWorkspace',
    'FullyQualifiedName~UseCasesControllerTests.CreateAsync_DbUpdateException_ReturnsClassifiedEnvelope'
) -join '|'
Invoke-NamedTest -Project $support -Filter $supportFilter -LogName 'support-named.log'

$replFilter = @(
    'FullyQualifiedName~ReplMcpErrorClassifierTests',
    'FullyQualifiedName~RequirementsWorkflowMetadataTests'
) -join '|'
Invoke-NamedTest -Project $repl -Filter $replFilter -LogName 'repl-named.log'

$pesterLog = Join-Path $outDir 'pester.log'
Write-Output 'BEGIN_PESTER'
$pester = Invoke-Pester -Path 'plugins\core\test-fixtures\pester\TriagePluginIdentity.Tests.ps1' -Output Detailed -PassThru
$pester | Out-String | Set-Content -LiteralPath $pesterLog -Encoding utf8
Write-Output ('PESTER Passed=' + $pester.PassedCount + ' Failed=' + $pester.FailedCount + ' Skipped=' + $pester.SkippedCount + ' NotRun=' + $pester.NotRunCount + ' Total=' + $pester.TotalCount)

Invoke-NamedTest -Project $build -Filter 'FullyQualifiedName~ReplacePluginCache_OpenTurn_RetainsExistingCache|FullyQualifiedName~ReplacePluginCache_ReplacesReadOnlyExistingCache' -LogName 'build-cache.log'

$drive = Get-PSDrive -Name F
Write-Output ('DISK_FREE_GB_AFTER_TESTS=' + [math]::Round($drive.Free / 1GB, 2))
Write-Output 'ALL_TESTS_DONE'
