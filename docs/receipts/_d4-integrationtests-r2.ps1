$ErrorActionPreference = 'Continue'
Get-Process testhost, vstest.console -ErrorAction SilentlyContinue |
    Where-Object { $_.Id -ne $PID } |
    Stop-Process -Force -ErrorAction SilentlyContinue
Start-Sleep -Seconds 2
$ErrorActionPreference = 'Stop'
dotnet test tests/McpServer.Support.Mcp.IntegrationTests/McpServer.Support.Mcp.IntegrationTests.csproj -c Debug --logger 'trx;LogFileName=d4-integrationtests-r2.trx'
exit $LASTEXITCODE
