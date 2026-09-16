$ErrorActionPreference = 'Stop'
dotnet test tests/McpServer.Support.Mcp.Tests/McpServer.Support.Mcp.Tests.csproj -c Debug --filter FullyQualifiedName~GetProductEffectiveRequirementsQueryHandlerTests --logger 'trx;LogFileName=d4-reqscope-handler-class.trx'
exit $LASTEXITCODE
