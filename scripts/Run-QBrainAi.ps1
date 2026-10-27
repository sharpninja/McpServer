$ErrorActionPreference = 'Stop'
$env:DOTNET_MODIFIABLE_ASSEMBLIES = "debug"
Set-Location "E:\github\QBrainAi"
dotnet run --project src\QBrainAi.Support.Mcp\QBrainAi.Support.Mcp.csproj -c Staging --no-build 2>&1
