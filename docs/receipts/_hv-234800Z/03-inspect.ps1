#Requires -Version 7.0
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$workspace = 'F:\GitHub\McpServer'
$outDir = Join-Path $workspace 'docs\receipts\_hv-234800Z'
New-Item -ItemType Directory -Force -Path $outDir | Out-Null
Set-Location -LiteralPath $workspace

$needles = @(
    @{ Name = 'McpErrorClassifier.cs'; Path = 'src\McpServer.Support.Mcp\Services\McpErrorClassifier.cs'; Patterns = @('ReasonDetails', 'backend_unavailable', 'validation', 'not_found', 'persistence') },
    @{ Name = 'SessionLogController.ClassifiedError'; Path = 'src\McpServer.Support.Mcp\Controllers\SessionLogController.cs'; Patterns = @('ClassifiedError', 'McpErrorClassifier', 'retryable', 'details') },
    @{ Name = 'McpToolErrors.Serialize'; Path = 'src\McpServer.Support.Mcp\McpStdio\McpToolErrors.cs'; Patterns = @('Serialize', 'McpErrorClassifier', 'Details') },
    @{ Name = 'ReplMcpErrorClassifier'; Path = 'src\McpServer.Repl.Core\ReplMcpErrorClassifier.cs'; Patterns = @('FromException', 'backend_unavailable', 'retryable') },
    @{ Name = 'AgentStdioProtocol'; Path = 'src\McpServer.Repl.Core\AgentStdioProtocol.cs'; Patterns = @('ReplMcpErrorClassifier.FromException') },
    @{ Name = 'SchemaGuard product'; Path = 'src\McpServer.Support.Mcp\Services\SessionLogSchemaGuard.cs'; Patterns = @('pending-migration', 'AgentSession') },
    @{ Name = 'StorageCommandBudget'; Path = 'src\McpServer.Support.Mcp\Services\StorageCommandBudget.cs'; Patterns = @('FromSeconds(5)', 'ExecuteAsync') }
)

$hits = @()
foreach ($item in $needles) {
    $full = Join-Path $workspace $item.Path
    $exists = Test-Path -LiteralPath $full
    $row = [ordered]@{
        name = $item.Name
        path = $item.Path
        exists = $exists
        lastWriteUtc = $null
        matches = @()
    }
    if ($exists) {
        $fi = Get-Item -LiteralPath $full
        $row.lastWriteUtc = $fi.LastWriteTimeUtc.ToString('o')
        $text = Get-Content -LiteralPath $full -Raw
        foreach ($pat in $item.Patterns) {
            $row.matches += [ordered]@{
                pattern = $pat
                found = $text.Contains($pat)
            }
        }
    }
    $hits += [pscustomobject]$row
}

$testNeedles = @(
    'tests\McpServer.Support.Mcp.Tests\Controllers\SessionLogControllerErrorTests.cs',
    'tests\McpServer.Support.Mcp.Tests\McpStdio\McpToolErrorEnvelopeTests.cs',
    'tests\McpServer.Support.Mcp.Tests\McpStdio\McpToolBackendUnavailableErrorTests.cs',
    'tests\McpServer.Repl.Core.Tests\ReplMcpErrorClassifierTests.cs',
    'tests\McpServer.Support.Mcp.Tests\Services\McpErrorClassifierTests.cs',
    'tests\McpServer.Support.Mcp.Tests\Services\SessionLogSchemaGuardTests.cs',
    'tests\McpServer.Support.Mcp.Tests\Services\SessionLogTriageStoreTests.cs',
    'tests\McpServer.Support.Mcp.Tests\Services\StorageCommandBudgetTests.cs',
    'plugins\core\test-fixtures\pester\TriagePluginIdentity.Tests.ps1'
)

$methodNeedles = @(
    @{ File = 'tests\McpServer.Support.Mcp.Tests\Services\SessionLogTriageStoreTests.cs'; Methods = @(
        'SubmitAsync_HungSaveChanges',
        'SubmitAsync_IdenticalActions',
        'SubmitAsync_SessionTags',
        'ReplaceTurnAsync_MissingRequestId',
        'SubmitAsync_CanceledStatus',
        'UpsertTurnAsync_OmittedPlanFile'
    ) },
    @{ File = 'tests\McpServer.Support.Mcp.Tests\Services\SessionLogSchemaGuardTests.cs'; Methods = @(
        'EnsureAgentSessionHeaderColumns_MissingColumns_ThrowsPendingMigration',
        'QueryAsync_MissingAgentSessionColumns_FailsClosedWithNamedError',
        'QueryAsync_AfterColumnsPresent_Succeeds',
        'QueryAsync'
    ) },
    @{ File = 'tests\McpServer.Support.Mcp.Tests\Controllers\SessionLogControllerErrorTests.cs'; Methods = @(
        'details.reason',
        'validation',
        'backend_unavailable',
        'not_found'
    ) },
    @{ File = 'tests\McpServer.Support.Mcp.Tests\McpStdio\McpToolErrorEnvelopeTests.cs'; Methods = @(
        'details.reason',
        'validation',
        'not_found'
    ) },
    @{ File = 'tests\Build.Tests\BuildTargetTests.cs'; Methods = @(
        'ReplacePluginCache_OpenTurn_RetainsExistingCache',
        'ReplacePluginCache_ReplacesReadOnlyExistingCache'
    ) },
    @{ File = 'tests\McpServer.Support.Mcp.Tests\Services\TodoExecutionServiceTests.cs'; Methods = @(
        'SetTestPlanAsync_DurableExecTodoMissingExecutionState_Rehydrates',
        'GenerateNextTodoIdAsync_SkipsSoftDeletedDurableId',
        'CreateTodosFromPlanAsync_InvalidDependsOn_FailsBeforeInsert'
    ) },
    @{ File = 'tests\McpServer.Repl.Core.Tests\RequirementsWorkflowMetadataTests.cs'; Methods = @(
        'GetTrAsync_LegacyId_DoesNotRejectCanonicalFormat',
        'CreateTrAsync_LegacyId_StillRejected'
    ) },
    @{ File = 'tests\McpServer.Support.Mcp.Tests\Services\AgentHelpConversationServiceTests.cs'; Methods = @(
        'SubmitTurnAsync_StrategyReceivesFiniteHelperTimeout'
    ) }
)

$testFiles = @()
foreach ($rel in $testNeedles) {
    $full = Join-Path $workspace $rel
    $exists = Test-Path -LiteralPath $full
    $row = [ordered]@{ path = $rel; exists = $exists; lastWriteUtc = $null; length = $null }
    if ($exists) {
        $fi = Get-Item -LiteralPath $full
        $row.lastWriteUtc = $fi.LastWriteTimeUtc.ToString('o')
        $row.length = $fi.Length
    }
    $testFiles += [pscustomobject]$row
}

$methodHits = @()
foreach ($item in $methodNeedles) {
    $full = Join-Path $workspace $item.File
    $exists = Test-Path -LiteralPath $full
    $text = if ($exists) { Get-Content -LiteralPath $full -Raw } else { '' }
    foreach ($m in $item.Methods) {
        $methodHits += [pscustomobject]@{
            file = $item.File
            method = $m
            exists = $exists
            found = $exists -and $text.Contains($m)
        }
    }
}

# Schema text-filter: look for QueryAsync with text filter after columns present
$schemaFile = Join-Path $workspace 'tests\McpServer.Support.Mcp.Tests\Services\SessionLogSchemaGuardTests.cs'
$schemaText = if (Test-Path -LiteralPath $schemaFile) { Get-Content -LiteralPath $schemaFile -Raw } else { '' }
$schemaFacts = [ordered]@{
    exists = [bool]$schemaText
    hasAfterColumnsPresent = $schemaText.Contains('QueryAsync_AfterColumnsPresent_Succeeds')
    hasTextFilter = $schemaText -match 'text' -or $schemaText -match 'TextFilter' -or $schemaText -match 'QueryText'
}

$result = [ordered]@{
    product = $hits
    testFiles = $testFiles
    methods = $methodHits
    schemaFacts = $schemaFacts
}
$result | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath (Join-Path $outDir 'inspect.json') -Encoding utf8
Write-Output ('INSPECT_WROTE ' + (Join-Path $outDir 'inspect.json'))
foreach ($h in $hits) {
    Write-Output ('PRODUCT ' + $h.name + ' exists=' + $h.exists)
}
foreach ($m in $methodHits) {
    if (-not $m.found) {
        Write-Output ('MISSING ' + $m.file + ' :: ' + $m.method)
    }
}
Write-Output 'INSPECT_DONE'
