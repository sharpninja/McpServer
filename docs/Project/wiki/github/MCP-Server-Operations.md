# MCP Server

Standalone repository for `McpServer.Support.Mcp`, the MCP context server used for todo management, session logs, context search, repository operations, and GitHub issue sync.

## What This Server Provides

- HTTP API with Swagger UI
- MCP over STDIO transport (`--transport stdio`)
- Single-port multi-tenant workspace hosting via `X-Workspace-Path` header
- Database-backed TODO storage following `Mcp:Database:Provider`; `docs/Project/TODO.yaml` is a read-only projection (TR-MCP-CFG-007)
- Use case domain: `/mcpserver/usecases` (CRUD, structure, FR Realizes links, coverage, diagram-graph, approval/product) and first-party UI at `/usecases/`
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
./build.ps1 Compile --configuration Staging
# or: dotnet restore McpServer.sln && dotnet build McpServer.sln -c Staging
```

1. Run the default instance:

```powershell
./build.ps1 StartServer --instance default
# or: dotnet run --project src\McpServer.Support.Mcp\McpServer.Support.Mcp.csproj -c Staging -- --instance default
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
- `Mcp:TodoStorage:Provider` (`database`; `sqlite` is a deprecated alias for `database`, and the removed `yaml` value fails fast per TR-MCP-CFG-007)
- `Mcp:TodoStorage:SqliteDataSource`
- `Mcp:GraphRag:*` (GraphRAG enablement, query defaults, backend command, concurrency)
- `Mcp:Triage:*` (asynchronous triage research runner: `AgentPath`, `ExecutionStrategy`, quiet period, fallback tiers). `AgentModel: auto` is a sentinel meaning "let the agent CLI pick its default model"; the Grok strategy omits `--model` for it and pins effort to `high` (current Grok CLIs reject `max`)
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
          "Provider": "database",
          "SqliteDataSource": "mcp.db"
        }
      },
      "alt-local": {
        "Port": 7157,
        "RepoRoot": "temp_test",
        "DataSource": "mcp-alt.db",
        "TodoFilePath": "docs/Project/TODO.yaml",
        "TodoStorage": {
          "Provider": "database",
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
./build.ps1 StartServer --instance default
./build.ps1 StartServer --instance alt-local
```

Smoke test both instances:

```powershell
./build.ps1 TestMultiInstance --first-instance default --second-instance alt-local
```

Migrate todo data between backends:

```powershell
.\scripts\Migrate-McpTodoStorage.ps1 -SourceBaseUrl http://localhost:7147 -TargetBaseUrl http://localhost:7157
```

## Build System

Build-related tasks are available as Nuke targets via `./build.ps1`. See the [Build System section in README.md](../README.md#build-system) for the full target list.

## Common Scripts And Service Updates

Windows service deployment and update must go through the Nuke build target:

```powershell
pwsh.exe -NoLogo -NoProfile -NonInteractive -File .\build.ps1 UpdateService
```

The following operational/admin scripts are lower-level helpers for local development, diagnostics, or migration tasks. Do not use them as the normal Windows service redeploy path:

- `scripts/Run-McpServer.ps1` - direct local run helper
- `scripts/Manage-McpService.ps1` - install/start/stop/remove Windows service
- `scripts/Migrate-McpTodoStorage.ps1` - todo backend migration

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
./build.ps1 Compile --configuration Staging
./build.ps1 Test
```

## API Surface

Main endpoints:

- `/mcpserver/todo`
- `/mcpserver/sessionlog`
- `/mcpserver/context`
- `/mcpserver/repo`
- `/mcpserver/gh`
- `/mcpserver/sync`
- `/mcpserver/usecases` — use case aggregates, structure, FR links, coverage, diagram-graph (UML canvas schema v1), sequence/UML diagram export, approval/product
- `/usecases/` — first-party Use Case Manager static UI (REST-only; deploy via Nuke `UpdateService`)
- `/mcpserver/agent-help` — Agent Help sessions for MCP Server issue diagnosis (create session, submit turn, status, transcript, SSE/WebSocket streaming)
- `/mcpserver/sessionlog/ingest/path` and `/mcpserver/sessionlog/ingest/upload` — provider transcript import
- `/health`
- `/swagger`

### Transcript Ingestion Limits

Transcript size ceilings are `Int32.MaxValue` (2,147,483,647) for the upload request body, expanded archive
content, per source file, per JSONL line, and records per bundle. JSONL sources stream line by line rather than
being read whole, so a large transcript does not have to fit in memory at once. Agent transcripts that carry a
full tool result on a single line therefore import without special handling.

Guards against hostile archives keep their original values and are not affected by those ceilings: a maximum of
10,000 archive entries, a decompression ratio ceiling of 20:1, rejection of ZIP symlink entries, and rejection of
paths that escape the upload root. Exceeded limits return 413; malformed or unsafe inputs return 400.

## Products

Host-local products (`PROD-*` keys such as `PROD-MCPSERVER`) map workspaces together so members can union FR/TR/TEST/layers into `GET /mcpserver/requirements/effective` (default `productScope=product`). Rows stay in the origin workspace and are tagged with `originWorkspaceId`. Context source `product-requirements` synthesizes those texts; sibling source files are never included. REST lives at `/mcpserver/products`. MCP tools are `product_*` plus `requirements_effective`. Typed client is `McpServerClient.Products`. Acceptance criteria travel with the effective union. `ProductClient.RemoveMemberAsync` deserializes the DELETE body (self-leave is 404 on a later GET).

## Requirements Wiki Export

`docs/wiki.yaml` uses schema `mcp-wiki-export/v1` to define the requirements wiki document tree for GitHub and Azure exports. When the file is absent, wiki generation falls back to the canonical generated Home, requirements, traceability, matrix, GitHub sidebar/footer, Azure order files, and manifests.

The optional `docfx` section is disabled by default with an empty workflow list:

```yaml
docfx:
  workflows: []
```

A workflow entry enables DocFX content for the export:

```yaml
docfx:
  workflows:
    - id: api
      executable: dotnet
      arguments:
        - tool
        - run
        - docfx
        - docfx.json
      workingDirectory: docs/docfx
      outputRoot: docs/docfx/_site
      targetRoot: api
      platforms:
        - github
        - azure
      timeoutSeconds: 120
```

Workflow paths are workspace-relative and must stay inside the active workspace. `workingDirectory` is where the process runs. `outputRoot` is a staging directory that is deleted before and after the workflow. `targetRoot` is the folder under each selected platform root where generated DocFX artifacts are published. `platforms` may contain `github`, `azure`, or both.

DocFX processes run through structured executable plus argument lists with `UseShellExecute=false`; command strings are not passed through a shell. All configured workflows must complete successfully before the requirements wiki writer receives any files. The writer then publishes one merged file set atomically and removes stale files under the managed `github` and `azure` roots.

Failure behavior is fail-closed: invalid YAML, invalid schema, duplicate workflow IDs, duplicate target roots for a platform, path traversal, absolute external paths, reparse-point escapes, timeout, non-zero process exit, missing output, duplicate publication paths, or unsupported arbitrary binary artifacts abort the export before partial wiki output is published. Built-in DocFX template binary assets that cannot be represented by the text-only writer, such as `favicon.ico` and default font files under `styles/`, are ignored; text artifacts such as HTML, Markdown, CSS, JavaScript, JSON, XML, YAML, SVG, and source maps are eligible for publication.

## CI/CD

Pipeline: `azure-pipelines.yml`

Pipeline jobs include:

- config validation
- restore/build/test
- publish artifact upload
- Windows MSIX packaging
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


