$ErrorActionPreference = 'Stop'
$env:DOTNET_MODIFIABLE_ASSEMBLIES = "debug"
Set-Location "E:\github\McpServer"
dotnet run --project src\McpServer.Support.Mcp\McpServer.Support.Mcp.csproj -c Staging --no-build 2>&1
