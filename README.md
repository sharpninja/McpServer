# MCP Server

Workspace-scoped AI agent infrastructure for .NET: context retrieval, TODO orchestration, session logging, repository operations, GitHub automation, GraphRAG, and agent orchestration over HTTP and MCP STDIO transports.

**Current line:** GitVersion `next-version` **1.4.25** (see `GitVersion.yml`). Live `/health` reports the build informational version from the deployed bits.

## Key Features

- **Dual transport** - HTTP REST with Swagger UI and MCP-over-STDIO for direct agent integration
- **Multi-tenant workspaces** - single port, workspace isolation via header, API key, or default resolution
- **Agent orchestration** - process-isolated agent pool with branch strategies, PowerShell sessions, and desktop automation
- **Semantic search** - ONNX-based vector embeddings with HNSW indexing, optional GraphRAG enhancement
- **Requirements traceability** - FR/TR/TEST document management with validation and Markdown/ZIP export
- **Multi-provider storage** - SQLite, SQL Server, and PostgreSQL with automatic migrations
- **REPL CLI tool** - `mcpserver-repl` for interactive use and agent STDIO access via single-line JSON request envelopes
- **Typed .NET client** - `SharpNinja.McpServer.Client` NuGet package covering all API endpoints

## Quick Start

```powershell
# Build
./build.ps1 Compile

# Run
./build.ps1 StartServer --instance default

# Test
./build.ps1 Test
```

Open Swagger at `http://localhost:7147/swagger`.

## Architecture

```
src/
  McpServer.Support.Mcp     ASP.NET Core server (controllers, STDIO host, auth)
  McpServer.Client           Typed REST client library (NuGet)
  McpServer.McpAgent         Microsoft Agent Framework integration
  McpServer.Repl.Core        REPL protocol, request envelopes, trust bootstrap
  McpServer.Repl.Host        mcpserver-repl CLI tool
  McpServer.SessionLog.Transcripts  Transcript detection, normalization, and canonical YAML (Claude, Codex, Grok, Cline, Copilot, OpenCode)
  McpServer.Services         Business logic (ingestion, indexing, TODO, GitHub, agents)
  McpServer.Storage          EF Core abstraction + vector indexing
  McpServer.GraphRag         Hybrid semantic search with GraphRAG
  McpServer.Cqrs             Lightweight async CQRS framework (NuGet)
  McpServer.Cqrs.Mvvm        MVVM extensions for CQRS
  McpServer.Launcher          Windows GUI launcher
  McpServer.ServiceDefaults  Aspire service defaults, OpenTelemetry, health checks
```

## Transports

### HTTP

```powershell
./build.ps1 StartServer --instance default
# Listens on http://localhost:7147
```

### MCP STDIO

```powershell
dotnet run --project src/McpServer.Support.Mcp -- --transport stdio --instance default
```

### REPL

```powershell
./build.ps1 InstallReplTool
mcpserver-repl --interactive              # interactive mode
mcpserver-repl --agent-stdio              # STDIO mode for agent integration
```

Direct `--agent-stdio` callers send one single-line JSON request envelope per stdin line. Do not send formatted YAML or a `type: batch` envelope.

## API Surface

| Route | Capability |
|---|---|
| `/mcpserver/todo` | TODO CRUD, audit history, priority/section filtering, prompt generation |
| `/mcpserver/sessionlog` | Session log upsert, query, full-text search, pagination, transcript import (six agent formats, size ceilings of `Int32.MaxValue`) |
| `/mcpserver/context` | Hybrid semantic search with GraphRAG, deterministic context packs |
| `/mcpserver/agents` | Agent definitions, workspace config, deployment status |
| `/mcpserver/agent-pool` | Pool lifecycle, health monitoring, process isolation |
| `/mcpserver/repo` | Repository read/list/write with allowlist enforcement |
| `/mcpserver/requirements` | FR/TR/TEST documents, validation, Markdown/ZIP export |
| `/mcpserver/workspace` | Multi-tenant workspace resolution and management |
| `/mcpserver/gh` | GitHub issues, PRs, workflows, repository metadata |
| `/mcpserver/tools` | Tool capability registration, discovery, schema validation |
| `/mcpserver/graphrag` | GraphRAG query with mode selection |
| `/mcpserver/events` | Server-sent events for real-time change notifications |
| `/mcpserver/templates` | Prompt template storage and rendering |
| `/mcpserver/voice` | Voice conversation management |
| `/mcpserver/desktop` | Desktop application launch (Windows) |
| `/mcpserver/diagnostic` | Health, version, database connectivity, index status |
| `/mcpserver/configuration` | Application configuration retrieval |
| `/mcpserver/tunnel` | Reverse proxy for agent communication |
| `/auth` | OIDC discovery, device authorization flow, token endpoint |
| `/health` | Health check |
| `/swagger` | OpenAPI documentation |

## Configuration

Primary config section: `Mcp`. Instance overrides under `Mcp:Instances:{name}`.

```json
{
  "Mcp": {
    "Port": 7147,
    "RepoRoot": ".",
    "DataSource": "mcp.db",
    "ApiKey": "your-api-key",
    "TodoStorage": { "Provider": "database" },
    "Instances": {
      "default": { "Port": 7147, "RepoRoot": "." },
      "alt-local": { "Port": 7157, "TodoStorage": { "Provider": "database" } }
    }
  }
}
```

Environment overrides: `PORT` (runtime port), `MCP_INSTANCE` (instance selection).

## Authentication

| Method | Use Case |
|---|---|
| **API key** | Server-to-server, per-workspace isolation via `X-Workspace-Path` header |
| **OIDC / Keycloak** | External identity provider with JWT Bearer validation and device authorization flow |
| **Embedded IdentityServer** | Local OIDC authority when `Mcp:IdentityServer:Enabled = true` |
| **Marker file trust** | Cryptographic signature validation for REPL protocol bootstrap |

## Storage

**Database providers** (EF Core with automatic migrations):

| Provider | Project |
|---|---|
| SQLite (default) | `McpServer.Storage.SqliteMigrations` |
| SQL Server | `McpServer.Storage.SqlServerMigrations` |
| PostgreSQL | `McpServer.Storage.PostgreSqlMigrations` |

TODO items live in the configured database (the sole source of truth); `docs/Project/TODO.yaml` is a read-only projection. The removed `yaml` provider fails fast, and `sqlite` is a deprecated alias for `database` (TR-MCP-CFG-007).

Vector indexing uses ONNX Runtime with Sentence Transformer embeddings and HNSW index for semantic search.

## Deployment

| Method | Details |
|---|---|
| **Standalone** | `./build.ps1 StartServer` or `dotnet run` |
| **Windows Service** | `./build.ps1 UpdateService` through the Nuke build; do not manually redeploy service files |
| **Docker** | Multi-stage build, volumes for `/data` and `/workspace` |
| **MSIX** | `./build.ps1 PackageMsix` for Windows app package |
| **Windows Launcher** | GUI application for starting/managing the server |

## Build System

[Nuke](https://nuke.build/) build orchestrator via `./build.ps1` (or `./build.sh` on Linux/macOS).

| Target | Description |
|---|---|
| `Compile` | Restore + build the solution (default) |
| `Test` | Run all unit tests |
| `Publish` | Publish server for deployment |
| `UpdateService` | Build/publish, backup config/data, update the Windows service, restore config/data, and health-check |
| `PackNuGet` | Pack McpServer.Client NuGet package |
| `PackReplTool` | Pack mcpserver-repl to local-packages/ |
| `PackageMsix` | Create MSIX package for Windows |
| `InstallReplTool` | Install mcpserver-repl as a global dotnet tool |
| `StartServer` | Build and run MCP server |
| `BumpVersion` | Increment patch version in GitVersion.yml |
| `ValidateConfig` | Validate appsettings instance configuration |
| `ValidateTraceability` | Check FR/TR/TEST requirements coverage |
| `TestMultiInstance` | Two-instance smoke test |
| `TestGraphRagSmoke` | GraphRAG endpoint smoke test |
| `Clean` | Clean artifacts and solution output |

## CI/CD

| Platform | File | Jobs |
|---|---|---|
| **Azure Pipelines** | `azure-pipelines.yml` | Build, test, publish, MSIX, docs lint, docs build, NuGet publish; optional Octopus LEGION2 release when `OCTOPUS_API_KEY` is set |
| **GitHub Actions** | `.github/workflows/build.yml` | Build & test, validate, package, MSIX, publish |

Versioning uses GitVersion (`GitVersion.yml`, `next-version: 1.4.25` at last docs refresh). See `docs/AZURE-PIPELINES.md` for pipeline variables and the optional Octopus Deploy integration.

## Client Library

```powershell
dotnet add package SharpNinja.McpServer.Client
```

```csharp
builder.Services.AddMcpServerClient(options =>
{
    options.BaseUrl = new Uri("http://localhost:7147");
    options.ApiKey = "your-api-key";
});
```

Covers: Todo, Context, SessionLog, GitHub, Repo, Workspace, ToolRegistry, Sync, and more.

Source: `src/McpServer.Client/` | [Package README](src/McpServer.Client/README.md)

## Agent Framework

`McpServer.McpAgent` integrates with the Microsoft Agent Framework:

```csharp
builder.Services.AddMcpServerMcpAgent();
```

Built-in MCP tools: `mcp_repo_read`, `mcp_repo_list`, `mcp_repo_write`, `mcp_desktop_launch`, `mcp_powershell_session_*`.
Workflows: session log lifecycle, TODO management, requirements ingestion.

Sample host: `src/McpServer.McpAgent.SampleHost/`

## Tests

21 test projects covering unit, integration, and Reqnroll validation:

- `Build.Tests` - build system and configuration
- `McpServer.Support.Mcp.Tests` / `.IntegrationTests` - server API and database
- `McpServer.Client.Tests` - REST client serialization
- `McpServer.McpAgent.Tests` - agent workflows and tool adapters
- `McpServer.Repl.Core.Tests` / `.IntegrationTests` - REPL protocol
- `McpServer.Cqrs.Tests` - CQRS dispatcher and pipeline
- `McpServer.QBAgent.Tests` - QuadBrain agent behavior
- `McpServer.Launcher.Tests` - launcher host
- `McpServer.Acid.IntegrationTests` - ACID turn-closure matrix
- `McpServer.TransactionSecurity.IntegrationTests` - durable transaction security storage
- `McpServer.PlanReview.Tests` / `McpServer.Review.Tests` - plan and AI review flows
- 7 Reqnroll validation projects (Context, GitHub, Repo, SessionLog, Todo, ToolRegistry, Workspace)

`./build.ps1 Test` runs the unit gate only: it excludes every `*.IntegrationTests` project and filters out
`Category=Integration` and `Category=AiReview` tests. Integration suites that need provisioned dependencies run
through `./build.ps1 MigrationIntegrationTests` or by targeting the project directly.

Integration tests provision what they need. The QuadBrain Ollama tests probe `http://localhost:11434` at fixture
startup, adopt a server that is already running, or start one from a discovered `ollama` executable and stop that
server again at teardown. Only a server the fixture started is stopped. When no executable is discoverable the
failure names the `InstallOllama` target, which stages the portable binaries and the required model.

## Prerequisites

- .NET SDK (version in `global.json`)
- PowerShell 7+ (`pwsh.exe`)
- Optional: Windows SDK (`makeappx.exe`) for MSIX, GitHub CLI (`gh`) for GitHub endpoints

## Documentation

| Document | Purpose |
|---|---|
| [User Guide](docs/USER-GUIDE.md) | End-user setup and usage |
| [Server Guide](docs/MCP-SERVER.md) | Operations and configuration |
| [Client Integration](docs/CLIENT-INTEGRATION.md) | NuGet client library usage |
| [REPL Migration Guide](docs/REPL-MIGRATION-GUIDE.md) | Migrating to mcpserver-repl |
| [FAQ](docs/FAQ.md) | Common questions |
| [Release Checklist](docs/RELEASE-CHECKLIST.md) | Pre-release verification |
| [Azure Pipelines](docs/AZURE-PIPELINES.md) | CI/CD variables and retention |

## License

See [LICENSE](LICENSE) for details.

## Shared Plugin Surfaces

This repository is the canonical home for the shared client surfaces used by all McpServer agent plugins:

- **PowerShell**: `tools/powershell/McpRepl` (published to PS Gallery as `McpRepl`)
- **TypeScript**: `tools/typescript/mcp-repl-ts` (published to npm as `@sharpninja/mcp-repl`)

See the respective READMEs in those directories and `GROK-USAGE.md` in the grok-plugin for usage details.
