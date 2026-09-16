$ErrorActionPreference = 'Continue'
Get-Process testhost, vstest.console -ErrorAction SilentlyContinue | Stop-Process -Force -ErrorAction SilentlyContinue
Start-Sleep -Seconds 2
$ErrorActionPreference = 'Stop'
Write-Output '--- list tests ---'
dotnet test tests/McpServer.Support.Mcp.Tests/McpServer.Support.Mcp.Tests.csproj -c Debug --filter FullyQualifiedName~GetProductEffectiveRequirementsQueryHandlerTests --list-tests --no-restore
Write-Output '--- run tests ---'
dotnet test tests/McpServer.Support.Mcp.Tests/McpServer.Support.Mcp.Tests.csproj -c Debug --filter FullyQualifiedName~GetProductEffectiveRequirementsQueryHandlerTests --no-restore --logger 'trx;LogFileName=d4-reqscope-handler-class-r2.trx'
exit $LASTEXITCODE
