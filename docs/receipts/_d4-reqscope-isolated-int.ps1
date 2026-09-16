$ErrorActionPreference = 'Stop'
$filter = 'FullyQualifiedName~RequirementScopeLayer_ReplWorkflow_ExercisesCurrentEffectiveRequirementsBeforeAndAfterLayer'
dotnet test tests/McpServer.Support.Mcp.IntegrationTests/McpServer.Support.Mcp.IntegrationTests.csproj -c Debug --filter $filter --logger 'trx;LogFileName=d4-reqscope-isolated.trx'
exit $LASTEXITCODE
