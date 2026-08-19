$ErrorActionPreference = 'Continue'
$outDir = 'F:\GitHub\McpServer\docs\receipts\_hv-20260819T013000Z'
New-Item -ItemType Directory -Force -Path $outDir | Out-Null
$proj = 'F:\GitHub\McpServer\tests\McpServer.Support.Mcp.Tests\McpServer.Support.Mcp.Tests.csproj'
$filter = @(
  'FullyQualifiedName~SessionLogBeginTurn_MissingPlanFile_ReturnsStructuredError',
  'FullyQualifiedName~UpsertTurnAsync_NewTurnWithoutPlanFile_ThrowsAndDoesNotInsert',
  'FullyQualifiedName~SubmitAsync_NewTurnMissingFields_Throws',
  'FullyQualifiedName~ReplaceTurnAsync_OmittingFields_Throws',
  'FullyQualifiedName~Invoke_WorkflowBeginTurn_MissingFields_FailsValidation',
  'FullyQualifiedName~UpsertTurnAsync_OmittedPlanFileTodoId_WritesNoneAndCanceled',
  'FullyQualifiedName~SessionLogTurnContextValidatorTests'
) -join '|'
$leftoverLog = Join-Path $outDir 'leftover-store006-validator.log'
Write-Output 'START_LEFTOVER'
& dotnet test $proj -c Debug --no-build --filter $filter --nologo | Tee-Object -FilePath $leftoverLog
Write-Output ('LEFTOVER_EXIT=' + $LASTEXITCODE)

$namedFilter = @(
  'FullyQualifiedName~McpErrorClassifierTests',
  'FullyQualifiedName~McpToolErrorEnvelopeTests',
  'FullyQualifiedName~SessionLogControllerErrorTests',
  'FullyQualifiedName~SessionLogTriageStoreTests',
  'FullyQualifiedName~SessionLogSchemaGuardTests.QueryAsync_AfterColumnsPresent_Succeeds'
) -join '|'
$namedLog = Join-Path $outDir 'named-support.log'
Write-Output 'START_NAMED'
& dotnet test $proj -c Debug --no-build --filter $namedFilter --nologo | Tee-Object -FilePath $namedLog
Write-Output ('NAMED_EXIT=' + $LASTEXITCODE)
