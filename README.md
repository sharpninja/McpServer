# MCP Server

Standalone repository for `McpServer.Support.Mcp`, the MCP context server used for todo management, session logs, context search, repository operations, and GitHub issue sync.

## What This Server Provides

- HTTP API with Swagger UI
- MCP over STDIO transport (`--transport stdio`)
- Multi-instance hosting from `appsettings` (`Mcp:Instances`)
- Per-instance todo storage backend (`yaml` file-backed or `sqlite` table-backed)
- Optional interaction logging and Parseable sink support

## Repository Layout

- `src/McpServer.Support.Mcp` - server application
- `tests/McpServer.Support.Mcp.Tests` - unit/integration tests
- `docs/MCP-SERVER.md` - detailed operational and configuration guide
- `scripts` - run, validate, test, migration, extension, and packaging scripts
- `.github/workflows/mcp-server-ci.yml` - CI pipeline (build/test/artifacts/MSIX/docs quality)

## Prerequisites

- .NET SDK from `global.json`
- PowerShell 7+
- Windows SDK tools (`makeappx.exe`) for MSIX packaging
- Optional: GitHub CLI (`gh`) for GitHub issue endpoints

## Quick Start

1. Restore and build:

```powershell
dotnet restore McpServer.sln
dotnet build McpServer.sln -c Staging
```

1. Run the default instance:

```powershell
.\scripts\Start-McpServer.ps1 -Configuration Staging -Instance default
```

1. Open Swagger:

```text
http://localhost:7147/swagger
```

## Run Modes

### HTTP mode

```powershell
dotnet run --project src\McpServer.Support.Mcp\McpServer.Support.Mcp.csproj -c Staging -- --instance default
```

### STDIO MCP mode

```powershell
dotnet run --project src\McpServer.Support.Mcp\McpServer.Support.Mcp.csproj -c Staging -- --transport stdio --instance default
```

## Configuration

Primary config section: `Mcp`.

Important keys:

- `Mcp:Port`
- `Mcp:RepoRoot`
- `Mcp:DataSource`
- `Mcp:TodoFilePath`
- `Mcp:TodoStorage:Provider` (`yaml` or `sqlite`)
- `Mcp:TodoStorage:SqliteDataSource`
- `Mcp:Instances:{name}:*` (per-instance overrides)

Environment overrides:

- `PORT` - highest-priority runtime port override
- `MCP_INSTANCE` - instance selection when `--instance` is not passed

### Example `Mcp:Instances`

```json
{
  "Mcp": {
    "Instances": {
      "default": {
        "Port": 7147,
        "RepoRoot": "E:\\github\\FunWasHad",
        "DataSource": "mcp.db",
        "TodoFilePath": "docs/Project/TODO.yaml",
        "TodoStorage": {
          "Provider": "yaml",
          "SqliteDataSource": "mcp.db"
        }
      },
      "alt-local": {
        "Port": 7157,
        "RepoRoot": "temp_test",
        "DataSource": "mcp-alt.db",
        "TodoFilePath": "docs/Project/TODO.yaml",
        "TodoStorage": {
          "Provider": "sqlite",
          "SqliteDataSource": "mcp-alt.db"
        }
      }
    }
  }
}
```

## Multi-Instance and Storage Validation

Run two configured instances:

```powershell
.\scripts\Start-McpServer.ps1 -Configuration Staging -Instance default
.\scripts\Start-McpServer.ps1 -Configuration Staging -Instance alt-local
```

Smoke test both instances:

```powershell
.\scripts\Test-McpMultiInstance.ps1 -Configuration Staging -FirstInstance default -SecondInstance alt-local
```

Migrate todo data between backends:

```powershell
.\scripts\Migrate-McpTodoStorage.ps1 -SourceBaseUrl http://localhost:7147 -TargetBaseUrl http://localhost:7157
```

## Common Scripts

- `scripts/Start-McpServer.ps1` - build/run server with optional `-Instance`
- `scripts/Run-McpServer.ps1` - direct local run helper
- `scripts/Validate-McpConfig.ps1` - config validation
- `scripts/Test-McpMultiInstance.ps1` - two-instance smoke test
- `scripts/Migrate-McpTodoStorage.ps1` - todo backend migration
- `scripts/Package-McpServerMsix.ps1` - publish and package MSIX

## Build and Test

```powershell
dotnet build McpServer.sln -c Staging
dotnet test tests\McpServer.Support.Mcp.Tests\McpServer.Support.Mcp.Tests.csproj -c Debug
```

## API Surface

Main endpoints:

- `/mcp/todo`
- `/mcp/sessionlog`
- `/mcp/context`
- `/mcp/repo`
- `/mcp/gh`
- `/mcp/sync`
- `/health`
- `/swagger`

## CI/CD

Workflow: `.github/workflows/mcp-server-ci.yml`

Pipeline jobs include:

- restore/build/test
- config validation
- OpenAPI artifact generation
- publish artifact upload
- Windows MSIX packaging
- markdown lint and link checking for docs

## VS Code / VS 2026 Extensions

Extension sources and packaging scripts live in:

- `extensions/fwh-mcp-todo` (legacy name)
- `extensions/McpServer-mcp-todo`
- `scripts/Package-Vsix.ps1`
- `scripts/Build-AndInstall-Vsix.ps1`

## Additional Documentation

- Full server guide: `docs/MCP-SERVER.md`
