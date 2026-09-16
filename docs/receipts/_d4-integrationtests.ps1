$ErrorActionPreference = 'Continue'
Get-Process testhost, vstest.console -ErrorAction SilentlyContinue | Stop-Process -Force -ErrorAction SilentlyContinue
Start-Sleep -Seconds 2
$ErrorActionPreference = 'Stop'
dotnet test tests/McpServer.Support.Mcp.IntegrationTests/McpServer.Support.Mcp.IntegrationTests.csproj -c Debug --logger 'trx;LogFileName=d4-integrationtests.trx'
exit $LASTEXITCODE
