$ErrorActionPreference = 'Stop'
$filter = 'FullyQualifiedName~MarkerRegenerationIntegrationTests'
dotnet test tests/McpServer.Support.Mcp.IntegrationTests/McpServer.Support.Mcp.IntegrationTests.csproj -c Debug --filter $filter --logger 'trx;LogFileName=d4-marker-rerun.trx'
exit $LASTEXITCODE
