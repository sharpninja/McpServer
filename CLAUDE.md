# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Build & Test Commands

```bash
# Build (Nuke orchestrator)
./build.ps1 Compile
# or: dotnet build src/McpServer.Support.Mcp -c Debug

# Run all unit tests (excludes integration tests)
./build.ps1 Test

# Run a specific test project
dotnet test tests/McpServer.Support.Mcp.Tests -c Debug

# Run a single test by name
dotnet test tests/McpServer.Support.Mcp.Tests -c Debug --filter "FullyQualifiedName~TodoServiceTests.QueryAsync_NoFilters_ReturnsAllItems"

# Run all tests in a class
dotnet test tests/McpServer.Support.Mcp.Tests -c Debug --filter "FullyQualifiedName~TodoServiceTests"

# Integration tests (CustomWebApplicationFactory, in-memory EF)
dotnet test tests/McpServer.Support.Mcp.IntegrationTests -c Debug

# Validate appsettings configuration
./build.ps1 ValidateConfig

# Validate requirements traceability (FR/TR/TEST mapping)
./build.ps1 ValidateTraceability

# Start server
./build.ps1 StartServer --instance default
# or: dotnet run --project src/McpServer.Support.Mcp/McpServer.Support.Mcp.csproj -c Staging -- --instance default
```

Swagger UI: `http://localhost:7147/swagger`

## Architecture

**McpServer** is an ASP.NET Core 9 server (.NET 9.0) providing workspace-scoped context retrieval, TODO management, session logging, repository operations, GraphRAG, and GitHub automation for AI agents.

### Transports

- **HTTP REST** — Controllers at `/mcpserver/*` in `src/McpServer.Support.Mcp/Controllers/`
- **MCP Streamable HTTP** — JSON-RPC wire protocol at `/mcp-transport`
- **MCP STDIO** — `--transport stdio` flag; tools in `McpStdio/FwhMcpTools.cs`

### Workspace Model

A single host process manages multiple workspaces. Each workspace gets its own in-process Kestrel `WebApplication` via `WorkspaceAppFactory` (not child processes). The primary workspace is served directly by the host. Workspace targeting uses the `X-Workspace-Path` header. Config lives in `appsettings.yaml` under `Mcp:Workspaces`. Per-instance overrides: `Mcp:Instances:{name}:*` resolved by `McpInstanceResolver`.

### Search Pipeline

Hybrid search combining SQLite FTS5 full-text with HNSW vector similarity (384-dim all-MiniLM-L6-v2 ONNX embeddings). `HybridSearchService` fuses results with BM25 scoring.

### Storage

EF Core with SQLite (`McpDbContext`), with migration projects for SQLite, PostgreSQL, and SQL Server. TODO items use a pluggable backend: YAML file-backed (`TodoService`) or SQLite table-backed (`SqliteTodoService`), selected via `Mcp:TodoStorage:Provider`.

### Key Projects

- `McpServer.Support.Mcp` — main server application
- `McpServer.Services` — ingestion, context search, models
- `McpServer.Storage` — EF Core with SQLite FTS5 + HNSW vector search
- `McpServer.Cqrs` — CQRS command/query handlers
- `McpServer.McpAgent` — agent hosting & tool execution framework
- `McpServer.Client` — typed REST client (published as NuGet `SharpNinja.McpServer.Client`)
- `McpServer.GraphRag` — GraphRAG indexing & query (workspace-scoped, disabled by default)
- `McpServer.Repl.Core` / `McpServer.Repl.Host` — REPL command interpreter and dotnet tool

### Test Projects

- Unit tests: `McpServer.Support.Mcp.Tests`, `McpServer.Client.Tests`, `McpServer.Cqrs.Tests`, `McpServer.Launcher.Tests`, `McpServer.McpAgent.Tests`, `McpServer.Repl.Core.Tests`, `Build.Tests`
- Integration: `McpServer.Support.Mcp.IntegrationTests`, `McpServer.Repl.IntegrationTests`
- BDD/Validation (Reqnroll): `McpServer.Context.Validation`, `McpServer.GitHub.Validation`, `McpServer.Repo.Validation`, `McpServer.SessionLog.Validation`, `McpServer.Todo.Validation`, `McpServer.ToolRegistry.Validation`, `McpServer.Workspace.Validation`

## Coding Conventions

### XML Documentation Required

`TreatWarningsAsErrors` and `GenerateDocumentationFile` are enabled globally in `Directory.Build.props`. **All public types and members must have XML doc comments** or the build fails (CS1591). Use `/// <inheritdoc />` for interface implementations. Test classes and methods also require XML docs stating what is tested, what data/fixtures are used, and which requirement IDs are validated.

### Requirement Traceability

All source files reference FR/TR requirement IDs in doc comments (e.g., `/// <summary>TR-PLANNED-013: Constructor.</summary>`). When adding new functionality, reference the relevant ID from `docs/Project/Functional-Requirements.md` and `docs/Project/Technical-Requirements.md`. Requirements traceability is validated in CI via `./build.ps1 ValidateTraceability`.

### Central Package Management

NuGet package versions are centralized in `Directory.Packages.props`. Use `<PackageReference Include="..." />` without `Version` in `.csproj` files.

### Configuration

Configuration uses YAML format (`appsettings.yaml`) via `NetEscapades.Configuration.Yaml`. Primary config section is `Mcp`.

### Shell

Use `pwsh.exe` (PowerShell 7+) for all scripts. Do not use `powershell.exe`.

## Key Documentation

- `docs/MCP-SERVER.md` — operational guide, configuration details
- `docs/USER-GUIDE.md` — user documentation
- `docs/CLIENT-INTEGRATION.md` — client library usage
- `docs/Development-Process.md` — development workflow
- `docs/Project/Functional-Requirements.md` — FR-MCP-* requirements
- `docs/Project/Technical-Requirements.md` — TR-MCP-* requirements
- `docs/Project/TODO.yaml` — canonical task list
- `AGENTS.md` — agent workspace policy and session logging conventions

## MCP Session Logging — Mandatory Precondition

**Speed is never more important than following workspace procedures.**

### Session Start (Run Once Per Session)

1. **Read `AGENTS-README-FIRST.yaml`** in the repo root for the current API key, endpoints, and base URL
2. **Bootstrap the required plugin interface** before any state-changing MCP call. For Claude Code, use the `mcpserver-claude-code-plugin` wrapper and its `workflow.*` / `client.*` methods. "Not in the visible tool list" does not mean unavailable; use the documented wrapper invocation form.
3. **Verify marker signature and health** through the required plugin status/bootstrap path. Use direct REST only for read-only diagnosis after the documented plugin path fails.
4. **Review recent session history and current TODOs** through the required plugin only after verification succeeds
5. **POST an initial session log turn** through the required plugin
6. **THEN** begin working on the user's request

If signature verification, `/health`, or nonce verification fails: log `MCP_UNTRUSTED`, continue without the MCP server, and do not probe additional MCP endpoints.

### Per User Message

1. POST a new session log turn BEFORE starting work
2. Complete the user's request
3. Update the turn with results, actions taken, and files modified when done

### Re-run Full Session Start Only If

- The user explicitly says "Start Session"
- Signature verification fails
- `/health` fails or nonce verification fails
- Any `/mcpserver/*` call returns 401
- The marker endpoint/key changes after a server restart

### Authentication

All `/mcpserver/*` endpoints require a per-workspace auth token (from `AGENTS-README-FIRST.yaml`). These details are for plugin internals, typed client integration, and read-only diagnosis after plugin failure; they are not permission to bypass the required plugin route for session log, TODO, requirements, import/export, or traceability operations:
- Header: `X-Api-Key: <token>`
- Or query param: `?api_key=<token>`
- If you receive a 401, re-read the marker file — the token rotates on each server restart

### Session Log Rules

- Use rich turn detail: interpretation, response, status, actions (type/status/filePath), contextList, filesModified, designDecisions, requirementsDiscovered, blockers, and key processingDialog
- Persist session log updates immediately after each meaningful change — do not defer saves
- Before any compaction step, persist the current session log state; after compaction, update again to record the outcome
- Agents must identify themselves accurately using their real agent identity in Pascal-Case (e.g., `ClaudeCode`). Do not use placeholder or misleading sourceType values

### Naming Conventions

- **TODO IDs**: uppercase canonical form `<SDLC-PHASE>-<AREA>-###` (e.g., `PLAN-NAMINGCONVENTIONS-001`) or `ISSUE-{number}`. Never write to `TODO.yaml` directly
- **Session IDs**: `<Agent>-<yyyyMMddTHHmmssZ>-<suffix>` with Pascal-Case agent prefix
- **Request IDs**: `req-<yyyyMMddTHHmmssZ>-<slugOrOrdinal>`, unique within a session

## Agent Conduct

- Do not fabricate information. Acknowledge mistakes. Distinguish facts from speculation
- Prioritize correctness over speed. Do not ship code you have not verified compiles
- Log every decision to the session log, including: what was decided, why, alternatives considered, what was rejected
- Document all web sources as actions with type `web_reference`
- Do not use table-style output in responses — use concise bullets or short paragraphs

## Requirements Tracking

When you discover or agree on new requirements during a session, update the files in `docs/Project/`:
- `Functional-Requirements.md` — append FR-MCP-* entries
- `Technical-Requirements.md` — append TR-MCP-* entries
- `TR-per-FR-Mapping.md` — append mapping rows
- `Requirements-Matrix.md` — append status rows
- `Testing-Requirements.md` — append TEST-MCP-* entries

Include the requirement ID in your session log turn's tags. Capture requirements as they emerge — do not defer.

## Context Loading by Task Type

- Session logging: `docs/context/session-log-schema.md` + `docs/context/module-bootstrap.md`
- TODO management: `docs/context/todo-schema.md` + `docs/context/module-bootstrap.md`
- API integration: `docs/context/api-capabilities.md` (or `GET /swagger/v1/swagger.json`)
- Adding dependencies: `docs/context/compliance-rules.md`
- Logging actions: `docs/context/action-types.md`
- New to workspace: this file + `docs/context/api-capabilities.md`

## Where Things Live

- `AGENTS-README-FIRST.yaml` — connection details, API key, workspace config (regenerated on server start)
- `AGENTS.md` — agent conduct, requirements tracking, session continuity, glossary
- `docs/context/` — on-demand reference docs (schemas, module docs, compliance rules, action types)
- `docs/Project/` — requirements docs, TODO.yaml, mapping matrices
- `templates/` — prompt templates (loaded on demand)
- `tools/powershell/` — PowerShell modules for MCP context operations

## CI/CD

GitHub Actions (`.github/workflows/build.yml`) on Windows-latest with .NET 9.0. Triggers on push/PR to main/develop. Jobs: build-test, validate, package, msix, publish. The Nuke `Test` target excludes `*.IntegrationTests` projects.
