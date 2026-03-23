# MCP Server

Standalone repository for `McpServer.Support.Mcp`, the MCP context server used for todo management, session logs, context search, repository operations, and GitHub issue sync.

## What This Server Provides

- HTTP API with Swagger UI
- MCP over STDIO transport (`--transport stdio`)
- Single-port multi-tenant workspace hosting via `X-Workspace-Path` header
- Per-workspace todo storage backend (`yaml` file-backed or `sqlite` table-backed)
- Three-tier workspace resolution: header → API key reverse lookup → default
- Optional interaction logging and Parseable sink support

## Repository Layout

- `src/McpServer.Support.Mcp` - server application
- `tests/McpServer.Support.Mcp.Tests` - unit/integration tests
- `MCP-SERVER.md` - detailed operational and configuration guide
- `AZURE-PIPELINES.md` - Azure DevOps CI/CD variables and retention notes
- `scripts` - run, validate, test, migration, extension, and packaging scripts
- `azure-pipelines.yml` - Azure DevOps pipeline (build/test/artifacts/MSIX/docs quality/package publish)

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
- `Mcp:GraphRag:*` (GraphRAG enablement, query defaults, backend command, concurrency)
- `Mcp:Instances:{name}:*` (per-instance overrides)

Environment overrides:

- `PORT` - highest-priority runtime port override
- `MCP_INSTANCE` - instance selection when `--instance` is not passed
- Do not keep Parseable, OAuth, or other runtime secrets in shared repo config. Inject them through environment variables, secure host configuration, or machine-local overrides.

### Example `Mcp:Instances`

```json
{
  "Mcp": {
    "Instances": {
      "default": {
        "Port": 7147,
        "RepoRoot": ".",
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
- `scripts/Update-McpService.ps1` - stop, publish Debug build, restore config/data, restart, health-check Windows service
- `scripts/Validate-McpConfig.ps1` - config validation
- `scripts/Test-McpMultiInstance.ps1` - two-instance smoke test
- `scripts/Test-GraphRagSmoke.ps1` - GraphRAG status/index/query smoke validation
- `scripts/Migrate-McpTodoStorage.ps1` - todo backend migration
- `scripts/Package-McpServerMsix.ps1` - publish and package MSIX

## GraphRAG

GraphRAG is workspace-scoped and disabled by default. When enabled, it can enhance `/mcpserver/context/search` and is also exposed directly through:

- `GET /mcpserver/graphrag/status`
- `POST /mcpserver/graphrag/index`
- `POST /mcpserver/graphrag/query`

Key behavior:

- Per-workspace GraphRAG state under `Mcp:GraphRag:RootPath`
- Index locking per workspace (single active index job by default)
- Explicit status lifecycle fields (`state`, `activeJobId`, failure metadata, artifact version)
- Fallback to context search when GraphRAG is disabled, uninitialized, not indexed, or backend execution fails
- Do not store backend secrets in repo config; inject runtime secrets via environment or secure host configuration

Example config:

```json
{
  "Mcp": {
    "GraphRag": {
      "Enabled": true,
      "EnhanceContextSearch": true,
      "RootPath": "mcp-data/graphrag",
      "DefaultQueryMode": "local",
      "DefaultMaxChunks": 20,
      "IndexTimeoutSeconds": 600,
      "QueryTimeoutSeconds": 120,
      "BackendCommand": "",
      "BackendArgs": "{operation} --graphRoot {graphRoot} --workspace {workspacePath}",
      "MaxConcurrentIndexJobsPerWorkspace": 1,
      "ArtifactVersion": "v1"
    }
  }
}
```

### GraphRAG Observability

Track these operational indicators during rollout:

- Index duration (`lastIndexDurationMs`) and active job contention (`index_conflict`)
- Fallback rate (`fallbackUsed` and `fallbackReason`) per query mode
- Failure categories (`failureCode`) and backend stderr patterns
- Indexed corpus drift (`lastIndexedDocumentCount` vs expected input volume)

### GraphRAG Rollout Checklist

1. Keep `Mcp:GraphRag:Enabled=false` in shared defaults.
2. Enable GraphRAG in one pilot workspace and run `scripts/Test-GraphRagSmoke.ps1`.
3. Verify fallback rate and failure codes remain acceptable under real workload.
4. Expand enablement workspace-by-workspace.
5. Keep external backend optional; if unavailable, ensure fallback path remains healthy.

## Build and Test

```powershell
dotnet build McpServer.sln -c Staging
dotnet test tests\McpServer.Support.Mcp.Tests\McpServer.Support.Mcp.Tests.csproj -c Debug
```

## API Surface

Main endpoints:

- `/mcpserver/todo`
- `/mcpserver/sessionlog`
- `/mcpserver/context`
- `/mcpserver/repo`
- `/mcpserver/gh`
- `/mcpserver/sync`
- `/health`
- `/swagger`

## CI/CD

Pipeline: `azure-pipelines.yml`

Pipeline jobs include:

- config validation
- restore/build/test
- publish artifact upload
- Windows MSIX packaging
- markdown lint and link checking for docs
- DocFX docs artifact build
- client NuGet pack and branch-conditional feed publish

## VS Code / VS 2026 Extensions

Extension sources and packaging scripts live in:

- `extensions/fwh-mcp-todo` (legacy name)
- `extensions/McpServer-mcp-todo`
- `scripts/Package-Vsix.ps1`
- `scripts/Build-AndInstall-Vsix.ps1`

## Client Library

A typed REST client is available as a NuGet package for consuming the MCP Server API:

```powershell
dotnet add package SharpNinja.McpServer.Client
```

```csharp
// With DI
builder.Services.AddMcpServerClient(options =>
{
    options.BaseUrl = new Uri("http://localhost:7147");
    options.ApiKey = "your-api-key"; // optional
});

// Without DI
var client = McpServerClientFactory.Create(new McpServerClientOptions
{
    BaseUrl = new Uri("http://localhost:7147"),
});
```

Covers all API endpoints: Todo, Context, SessionLog, GitHub, Repo, Sync, Workspace, and Tools.

Source: `src/McpServer.Client/` — see the [package README](https://github.com/sharpninja/McpServer/blob/develop/src/McpServer.Client/README.md) for full usage.

## Additional Documentation

- User documentation: `USER-GUIDE.md`
- Documentation index: `README.md`
- FAQ: `FAQ.md`


