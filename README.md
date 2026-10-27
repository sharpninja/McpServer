# QBrain.AI

Workspace-scoped AI agent infrastructure for .NET: context retrieval, TODO orchestration, session logging, durable agent memory, repository operations, GitHub automation, GraphRAG, host-local Products for shared requirements, and agent orchestration over HTTP and MCP STDIO transports.

**Versioning:** `GitVersion.yml` sets the next version to **1.4.39**. The deployed service version is reported by `/health`; a source checkout may be ahead of or behind that deployment. `/health` echoes a caller nonce for liveness and reports storage as `reachable` or `unreachable`.

## Key Features

- **Three transports** - HTTP REST with Swagger UI, MCP Streamable HTTP at `/mcp-transport`, and MCP-over-STDIO for direct agent integration
- **QuadBrain** - four-role multi-model decision engine (Creativity, Logic, CuriosityEngine, ArbiterOfTruth) behind an OpenAI-compatible chat-completions endpoint
- **Multi-tenant workspaces** - single port, workspace isolation via header, API key, or default resolution
- **Agent orchestration** - process-isolated agent pool with branch strategies, PowerShell sessions, and desktop automation
- **Semantic search** - ONNX-based vector embeddings with HNSW indexing, optional GraphRAG enhancement
- **Requirements traceability** - FR/TR/TEST document management with validation and Markdown/ZIP export
- **Products** - host-local `PROD-*` workspace groups that share FR/TR/TEST/layers into effective queries and `product-requirements` context without copying rows
- **Use cases** - workspace-scoped use-case modeling with FR Realizes links, coverage, UML canvas graph (schema v1), sequence diagrams, first-party UI at `/usecases/`, REST + MCP + typed client
- **Multi-provider storage** - SQLite, SQL Server, and PostgreSQL with startup migrations enabled by default; `Mcp:Database:AutoMigrate=false` requires an administrator to apply pending migrations before startup
- **REPL CLI tool** - `qbrain-ai-repl` for interactive use and agent STDIO access via single-line JSON request envelopes
- **Agent memory** - workspace-scoped remember/recall/explore/promote/consolidate/revert plus compat CRUD. All eight official plugins inject a raw `REQUIRED MEMORIES` block at host-supported request boundaries. Default CI bench stays Grok; `./build.ps1 BenchMemory -Plugin all` is unblocked after H7a `agree:true`. Multi-turn v2 uses tokens as the primary metric (`docs/benchmarks/`).
- **Typed .NET client** - `QBrainAI.Client` NuGet package covering all API endpoints

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

## Frontier agent setup prompt (draft)

Operator-facing copy-paste setup prompt for frontier coding agents (MCP-SETUPPROMPT-001). Status: **DRAFT** with hostile-validator overall **AGREE** (Accuracy 99, Completeness 99) on 2026-09-28 CT; live install dry-runs remain outside that draft acceptance.

- Draft: [docs/setup/2026-09-28-frontier-agent-setup-prompt.DRAFT.md](docs/setup/2026-09-28-frontier-agent-setup-prompt.DRAFT.md)
- Receipts: docs/receipts/setup/ and docs/receipts/hv/hostile-validator-setupprompt-*

## Architecture

```
src/
  QBrainAi.Support.Mcp     ASP.NET Core server (controllers, STDIO host, auth)
  QBrainAi.Client           Typed REST client library (NuGet)
  QBrainAi.McpAgent         Microsoft Agent Framework integration
  QBrainAi.Repl.Core        REPL protocol, request envelopes, trust bootstrap
  QBrainAi.Repl.Host        qbrain-ai-repl CLI tool
  QBrainAi.SessionLog.Transcripts  Transcript detection, normalization, and canonical YAML (Claude, Codex, Grok, Cline, Copilot, OpenCode)
  QBrainAi.Services         Business logic (ingestion, indexing, TODO, GitHub, agents)
  QBrainAi.Storage          EF Core abstraction + vector indexing
  QBrainAi.GraphRag         Hybrid semantic search with GraphRAG
  QBrainAi.Cqrs             Lightweight async CQRS framework (NuGet)
  QBrainAi.Cqrs.Mvvm        MVVM extensions for CQRS
  QBrainAi.Launcher          Windows GUI launcher
  QBrainAi.ServiceDefaults  Aspire service defaults, OpenTelemetry, health checks
```

## Transports

### HTTP

```powershell
./build.ps1 StartServer --instance default
# Listens on http://localhost:7147
```

### MCP Streamable HTTP

JSON-RPC MCP transport. No API key required.

```text
POST http://localhost:7147/mcp-transport
```

### MCP STDIO

```powershell
dotnet run --project src/QBrainAi.Support.Mcp -- --transport stdio --instance default
```

### REPL

```powershell
./build.ps1 InstallReplTool
qbrain-ai-repl --interactive              # interactive mode
qbrain-ai-repl --agent-stdio              # STDIO mode for agent integration
```

Direct `--agent-stdio` callers send one single-line JSON request envelope per stdin line. Do not send formatted YAML or a `type: batch` envelope.

## API Surface

| Route | Capability |
|---|---|
| `/qbrainai/todo` | TODO CRUD, audit history, priority/section filtering, prompt generation |
| `/qbrainai/sessionlog` | Session log upsert, query, full-text search, pagination, transcript import (six agent formats, size ceilings of `Int32.MaxValue`) |
| `/qbrainai/context` | Hybrid semantic search with GraphRAG, deterministic context packs |
| `/qbrainai/agents` | Agent definitions, workspace config, deployment status |
| `/qbrainai/agent-pool` | Pool lifecycle, health monitoring, process isolation |
| `/qbrainai/repo` | Repository read/list/write with allowlist enforcement |
| `/qbrainai/requirements` | FR/TR/TEST documents, validation, Markdown/ZIP export, `productScope` on effective |
| `/qbrainai/products` | Product CRUD and workspace membership (`PROD-*` keys) |
| `/qbrainai/usecases` | Use case CRUD, flows/steps/actors/FR links, diagram-graph, coverage, approval/product |
| `/usecases/` | First-party Use Case Manager UI (REST-only; UML canvas + secondary forms) |
| `/qbrainai/memory` | Remember, recall, explore, consolidate, promote, versions/revert, plus compat CRUD |
| `/memory/` | First-party Memory UI (REST-only; Effective set search/edit/revert) |
| `/qbrainai/workspace` | Multi-tenant workspace resolution and management |
| `/qbrainai/gh` | GitHub issues, PRs, workflows, repository metadata |
| `/qbrainai/tools` | Tool capability registration, discovery, schema validation |
| `/qbrainai/graphrag` | GraphRAG query with mode selection |
| `/qbrainai/events` | Server-sent events for real-time change notifications |
| `/qbrainai/templates` | Prompt template storage and rendering |
| `/qbrainai/voice` | Voice conversation management |
| `/qbrainai/desktop` | Desktop application launch (Windows) |
| `/qbrainai/diagnostic` | Health, version, database connectivity, index status |
| `/qbrainai/configuration` | Application configuration retrieval |
| `/qbrainai/tunnel` | Reverse proxy for agent communication |
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

**Database providers** (EF Core with configurable startup migrations):

| Provider | Project |
|---|---|
| SQLite (default) | `QBrainAi.Storage.SqliteMigrations` |
| SQL Server | `QBrainAi.Storage.SqlServerMigrations` |
| PostgreSQL | `QBrainAi.Storage.PostgreSqlMigrations` |

TODO items live in the configured database (the sole source of truth); `docs/Project/TODO.yaml` is a read-only projection. The removed `yaml` provider fails fast, and `sqlite` is a deprecated alias for `database` (TR-MCP-CFG-007).

Audit ledgers are permanent and local. New `DataAuditLogs` rows use versioned GZip payload columns; legacy text rows remain readable. TODO audit rows are excluded from federation and wiki exports. SQL Server deployments can use a restricted `mcp_runtime` role after an administrator applies the audit migration. See [Permanent local audit storage](docs/Operations/permanent-local-audit-sqlserver.md) for migration and runtime identity steps.

Vector indexing uses ONNX Runtime with Sentence Transformer embeddings and HNSW index for semantic search.

## Deployment

| Method | Details |
|---|---|
| **Standalone** | `./build.ps1 StartServer` or `dotnet run` |
| **Windows Service** | `./build.ps1 UpdateService` through the Nuke build; do not manually redeploy service files. Nuke `UpdateService` is Windows-only. |
| **Linux box** | Publish `develop` and swap into `/opt/mcpserver` (box equivalent; not `UpdateService`). PLAN-TXNKEYSERVER-001 closed on the box MCP at `8f30caf`. |
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
| `UpdateService` | Build/publish, backup config/data, update the Windows service, restore config/data, and health-check (Windows-only; Linux box uses publish/swap to `/opt/mcpserver`) |
| `PackNuGet` | Pack QBrainAi.Client NuGet package |
| `PackReplTool` | Pack qbrain-ai-repl to local-packages/ |
| `PackageMsix` | Create MSIX package for Windows |
| `InstallReplTool` | Install qbrain-ai-repl as a global dotnet tool |
| `StartServer` | Build and run MCP server |
| `BumpVersion` | Increment patch version in GitVersion.yml |
| `ValidateConfig` | Validate appsettings instance configuration |
| `ValidateTraceability` | Check FR/TR/TEST requirements coverage |
| `TestMultiInstance` | Two-instance smoke test |
| `TestGraphRagSmoke` | GraphRAG endpoint smoke test |
| `BenchMemory` | Memory bench (v1 smoke + v2 multi-turn; tokens primary). Default `-Plugin grok`; `-Plugin all` after H7a `agree:true` (S7b/H7b on develop) |
| `Clean` | Clean artifacts and solution output |

## CI/CD

| Platform | File | Jobs |
|---|---|---|
| **Azure Pipelines** | `azure-pipelines.yml` | Build, test, publish, MSIX, docs lint, docs build, NuGet publish; optional Octopus LEGION2 release when `OCTOPUS_API_KEY` is set |
| **GitHub Actions** | `.github/workflows/build.yml` | Build & test, validate, package, MSIX, publish |

Versioning uses GitVersion (`GitVersion.yml`, `next-version: 1.4.39`). See `docs/AZURE-PIPELINES.md` for pipeline variables and the optional Octopus Deploy integration.

## Client Library

```powershell
dotnet add package QBrainAI.Client
```

```csharp
builder.Services.AddQBrainAiClient(options =>
{
    options.BaseUrl = new Uri("http://localhost:7147");
    options.ApiKey = "your-api-key";
});
```

Covers: Todo, Context, SessionLog, Memory, GitHub, Repo, Workspace, ToolRegistry, Sync, and more.

Source: `src/QBrainAi.Client/` | [Package README](src/QBrainAi.Client/README.md)

## Agent Framework

`QBrainAi.McpAgent` integrates with the Microsoft Agent Framework:

```csharp
builder.Services.AddQBrainAiMcpAgent();
```

Built-in MCP tools: `mcp_repo_read`, `mcp_repo_list`, `mcp_repo_write`, `mcp_desktop_launch`, `mcp_powershell_session_*`.
Workflows: session log lifecycle, TODO management, requirements ingestion.

Sample host: `src/QBrainAi.McpAgent.SampleHost/`

## Tests

22 test projects covering unit, integration, and Reqnroll validation (plus the `QBrainAi.ProcessTree.TestHelper` support project):

- `Build.Tests` - build system and configuration
- `QBrainAi.Support.Mcp.Tests` / `.IntegrationTests` - server API and database
- `QBrainAi.Client.Tests` - REST client serialization
- `QBrainAi.McpAgent.Tests` - agent workflows and tool adapters
- `QBrainAi.Repl.Core.Tests` / `.IntegrationTests` - REPL protocol
- `QBrainAi.Cqrs.Tests` - CQRS dispatcher and pipeline
- `QBrainAi.QBAgent.Tests` - QuadBrain agent behavior
- `QBrainAi.Launcher.Tests` - launcher host
- `QBrainAi.Acid.IntegrationTests` - ACID turn-closure matrix
- `QBrainAi.TransactionSecurity.IntegrationTests` - durable transaction security storage
- `QBrainAi.PlanReview.Tests` / `QBrainAi.Review.Tests` - plan and AI review flows
- `QBrainAi.PluginIntegration.Tests` - plugin integration
- 7 Reqnroll validation projects (Context, GitHub, Repo, SessionLog, Todo, ToolRegistry, Workspace)

`./build.ps1 Test` runs the unit gate only: it excludes every `*.IntegrationTests` project and filters out
`Category=Integration` and `Category=AiReview` tests. Integration suites that need provisioned dependencies run
through `./build.ps1 MigrationIntegrationTests` or by targeting the project directly.

The audit payload migration integration tests cover SQLite, SQL Server (LocalDB by default), and PostgreSQL (ephemeral cluster by default). Run the focused provider set with:

```powershell
dotnet test tests/QBrainAi.Support.Mcp.Tests/QBrainAi.Support.Mcp.Tests.csproj --filter "FullyQualifiedName~AuditPayloadMigrationTests|FullyQualifiedName~SqlServerAuditPayloadMigrationTests|FullyQualifiedName~PostgreSqlAuditPayloadMigrationTests"
```

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
| [REPL Migration Guide](docs/REPL-MIGRATION-GUIDE.md) | Migrating to qbrain-ai-repl |
| [FAQ](docs/FAQ.md) | Common questions |
| [MCP Memories](docs/context/memory.md) | Remember/recall/promote/consolidate and REQUIRED MEMORIES injection |
| [Memory benchmarks](docs/benchmarks/README.md) | Token-primary bench; v2 multi-turn is the efficiency claim; `-Plugin all` after H7a |
| [Release Checklist](docs/RELEASE-CHECKLIST.md) | Pre-release verification |
| [Azure Pipelines](docs/AZURE-PIPELINES.md) | CI/CD variables and retention |
| [Permanent local audit storage](docs/Operations/permanent-local-audit-sqlserver.md) | Audit migration, SQL Server runtime role, and local-only export policy |

## License

See [LICENSE](LICENSE) for details.

## Shared Plugin Surfaces

This repository is the canonical home for the shared client surfaces used by all QBrainAi agent plugins:

- **PowerShell**: `tools/powershell/McpRepl` (published to PS Gallery as `McpRepl`)
- **TypeScript**: `tools/typescript/mcp-repl-ts` (published to npm as `@qbrainai/qbrain-ai-repl`)

See the respective READMEs in those directories and `GROK-USAGE.md` in the grok-plugin for usage details.
