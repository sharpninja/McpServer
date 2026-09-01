$ErrorActionPreference = 'Continue'
$outDir = 'F:\GitHub\McpServer\docs\receipts\_hv-20260819T013000Z'
$proj = 'F:\GitHub\McpServer\tests\McpServer.Support.Mcp.Tests\McpServer.Support.Mcp.Tests.csproj'
$filter = @(
  'FullyQualifiedName~SubmitTurnAsync_StrategyProgressOnlyOutput_ReturnsIncompleteAndDoesNotPersistFinalAssistantTranscript',
  'FullyQualifiedName~SubmitTurnAsync_StrategyPlanOnlyOutput_ReturnsIncompleteAndRetainsProgressOnly',
  'FullyQualifiedName~SetTestPlanAsync_DurableExecTodoMissingExecutionState_Rehydrates'
) -join '|'
& dotnet test $proj -c Debug --no-build --filter $filter --nologo | Tee-Object -FilePath (Join-Path $outDir 'help-todo.log')
Write-Output ('HELP_TODO_EXIT=' + $LASTEXITCODE)
Write-Output ('UTC=' + [datetime]::UtcNow.ToString('o'))
