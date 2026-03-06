# Copilot Instructions — McpServer

**Agent Identity:** When posting to the MCP session log, use `sourceType: copilotcli`.
For specific operational instructions (session bootstrap, turn logging lifecycle, helper command order), follow `AGENTS-README-FIRST.yaml`.

## Response Formatting

- Do not use table-style output in responses.
- Use concise bullets or short paragraphs instead.

## Terminal Usage

- Do NOT set focus to the terminal window.
- Do NOT write multiline commands to the terminal; use temporary scripts instead.
- Always use non-login pwsh and PowerShell commands.

## Build, Test, Lint

```powershell
# Build
dotnet build src\McpServer.Support.Mcp -c Debug
dotnet build src\McpServer.Client -c Debug

# Run unit tests
dotnet test tests\McpServer.Support.Mcp.Tests -c Debug
dotnet test tests\McpServer.Client.Tests -c Debug

# Run integration tests (uses CustomWebApplicationFactory, in-memory EF)
dotnet test tests\McpServer.Support.Mcp.IntegrationTests -c Debug

# Run a single test by fully-qualified name
dotnet test tests\McpServer.Support.Mcp.Tests -c Debug --filter "FullyQualifiedName~TodoServiceTests.QueryAsync_NoFilters_ReturnsAllItems"

# Run tests in a single class
dotnet test tests\McpServer.Support.Mcp.Tests -c Debug --filter "FullyQualifiedName~TodoServiceTests"

# Validate appsettings config
pwsh ./scripts/Validate-McpConfig.ps1

# Markdown lint (docs only)
# CI uses markdownlint-cli2 with .markdownlint-cli2.yaml
```

## Architecture

**McpServer** is a standalone ASP.NET Core 9 server providing context retrieval, TODO management, session logging, repository operations, and GitHub issue sync for AI agents. It exposes functionality via two transports:

- **HTTP REST API** — Controllers under `src/McpServer.Support.Mcp/Controllers/` (routes at `/mcpserver/*`).
- **MCP Streamable HTTP** — `app.MapMcp("/mcp-transport")` using ModelContextProtocol.AspNetCore.
- **MCP STDIO** — `--transport stdio` flag; same tools as HTTP via `McpStdio/FwhMcpTools.cs`.

**Workspace model**: The primary host manages multiple workspaces, each getting its own in-process Kestrel `WebApplication` (not child processes). Workspace config lives in `appsettings.json` under `Mcp:Workspaces` (not in the database). One workspace is the "primary" — served by the host process directly; others get child `WebApplication` instances via `WorkspaceAppFactory`.

**Search pipeline**: Hybrid search combines SQLite FTS5 full-text with HNSW vector similarity (384-dim all-MiniLM-L6-v2 ONNX embeddings). `HybridSearchService` fuses both with BM25 scoring.

**Storage**: EF Core with SQLite (`McpDbContext`). TODO items use a pluggable backend — either YAML file-backed (`TodoService`) or SQLite table-backed (`SqliteTodoService`), selected via `Mcp:TodoStorage:Provider`.

**Multi-instance**: `McpInstanceResolver` overlays per-instance config from `Mcp:Instances:{name}` onto base `Mcp:*` settings.

## Key Conventions

### XML Documentation Required

`TreatWarningsAsErrors` and `GenerateDocumentationFile` are enabled globally in `Directory.Build.props`. All public types and members must have XML doc comments or the build fails (CS1591). Use `/// <inheritdoc />` for interface implementations. Test projects are not exempt: test classes and test methods must include XML docs that state what is being tested, what data/fixtures are used, why that data/fixtures are used, and which requirement IDs are validated.

### Requirement Traceability Comments

All source files reference their FR/TR requirement IDs in doc comments (e.g., `/// <summary>TR-PLANNED-013: Constructor.</summary>`). When adding new functionality, reference the relevant requirement ID from `docs/Project/Functional-Requirements.md` and `docs/Project/Technical-Requirements.md`.
