$ErrorActionPreference = 'Continue'
Get-Process testhost, vstest.console -ErrorAction SilentlyContinue | Stop-Process -Force -ErrorAction SilentlyContinue
Start-Sleep -Seconds 2
$ErrorActionPreference = 'Stop'

Write-Output '--- observer tests ---'
dotnet test tests/McpServer.Support.Mcp.IntegrationTests/McpServer.Support.Mcp.IntegrationTests.csproj -c Debug --filter FullyQualifiedName~MarkerFileChangeObserverTests --logger 'trx;LogFileName=d4-observer.trx'
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

Write-Output '--- marker class ---'
dotnet test tests/McpServer.Support.Mcp.IntegrationTests/McpServer.Support.Mcp.IntegrationTests.csproj -c Debug --filter FullyQualifiedName~MarkerRegenerationIntegrationTests --no-restore --logger 'trx;LogFileName=d4-marker-r3.trx'
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

Write-Output '--- full IntegrationTests ---'
dotnet test tests/McpServer.Support.Mcp.IntegrationTests/McpServer.Support.Mcp.IntegrationTests.csproj -c Debug --no-restore --logger 'trx;LogFileName=d4-integrationtests-r3.trx'
exit $LASTEXITCODE
