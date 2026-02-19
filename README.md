# MCP Server (Standalone)

Standalone extraction of `FWH.Support.Mcp` from the FunWasHad monorepo.

## Project root
`E:\github\McpServer`

## Build
```powershell
dotnet build McpServer.sln -c Staging
```

## Test
```powershell
dotnet test tests\FWH.Support.Mcp.Tests\FWH.Support.Mcp.Tests.csproj -c Debug
```
