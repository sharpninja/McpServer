$ErrorActionPreference = 'Stop'
$env:DOTNET_MODIFIABLE_ASSEMBLIES = "debug"
Set-Location "E:\github\FunWasHad"
dotnet run --project src\FWH.Support.Mcp\FWH.Support.Mcp.csproj -c Staging --no-build 2>&1
