# MCP Server (Standalone)

Standalone extraction of `McpServer.Support.Mcp` from the FunWasHad monorepo.

## Project root
`E:\github\McpServer`

## Build
```powershell
dotnet build McpServer.sln -c Staging
```

## Test
```powershell
dotnet test tests\McpServer.Support.Mcp.Tests\McpServer.Support.Mcp.Tests.csproj -c Debug
```
