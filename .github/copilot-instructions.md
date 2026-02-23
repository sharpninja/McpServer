# Copilot Instructions — McpServer

## Build, Test, Lint

```powershell
# Build
dotnet build src\McpServer.Support.Mcp -c Debug
dotnet build src\McpServer.Client -c Debug

# Run all tests
dotnet test tests\McpServer.Support.Mcp.Tests -c Debug
dotnet test tests\McpServer.Client.Tests -c Debug

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

- **HTTP REST API** — Controllers under `src/McpServer.Support.Mcp/Controllers/` (routes at `/mcp/*`).
- **MCP Streamable HTTP** — `app.MapMcp("/mcp-transport")` using ModelContextProtocol.AspNetCore.
- **MCP STDIO** — `--transport stdio` flag; same tools as HTTP via `McpStdio/FwhMcpTools.cs`.

**Workspace model**: The primary host manages multiple workspaces, each getting its own in-process Kestrel `WebApplication` (not child processes). Workspace config lives in `appsettings.json` under `Mcp:Workspaces` (not in the database). One workspace is the "primary" — served by the host process directly; others get child `WebApplication` instances via `WorkspaceAppFactory`.

**Search pipeline**: Hybrid search combines SQLite FTS5 full-text with HNSW vector similarity (384-dim all-MiniLM-L6-v2 ONNX embeddings). `HybridSearchService` fuses both with BM25 scoring.

**Storage**: EF Core with SQLite (`McpDbContext`). TODO items use a pluggable backend — either YAML file-backed (`TodoService`) or SQLite table-backed (`SqliteTodoService`), selected via `Mcp:TodoStorage:Provider`.

**Multi-instance**: `McpInstanceResolver` overlays per-instance config from `Mcp:Instances:{name}` onto base `Mcp:*` settings.

## Key Conventions

### XML Documentation Required

`TreatWarningsAsErrors` and `GenerateDocumentationFile` are enabled globally in `Directory.Build.props`. All public types and members must have XML doc comments or the build fails (CS1591). Use `/// <inheritdoc />` for interface implementations. Test projects are exempt.

### Requirement Traceability Comments

All source files reference their FR/TR requirement IDs in doc comments (e.g., `/// <summary>TR-PLANNED-013: Constructor.</summary>`). When adding new functionality, reference the relevant requirement ID from `docs/REQUIREMENTS.md`.

### DRY — No Duplication (TR-MCP-DRY-001)

Shared logic must be extracted to a single reusable location. No copy-pasted logic across files or scripts. See `docs/REQUIREMENTS.md` § TR-MCP-DRY-001.

### Async Patterns

All async methods use `.ConfigureAwait(false)`. Controllers and services accept `CancellationToken` parameters.

### Testing

- **Framework**: xUnit v3 with NSubstitute for mocking.
- **Integration tests** use `CustomWebApplicationFactory` (sets environment to `"Test"`, uses EF in-memory database).
- **Unit tests** use temp files or in-memory state; always clean up in `Dispose`.
- Test project has `InternalsVisibleTo` access to the main project.

### Controller Patterns

Controllers are `sealed`, use `[ApiController]` + `[Route("mcp/...")]`. Mutating endpoints return `TodoMutationResult`-style result objects. Not-found returns 404 with the result; validation errors return 400/409.

### Service Registration

Services follow interface + implementation pairs (`ITodoService`/`TodoService`). Strategy-pattern switching (TODO storage, tunnel providers) is done via factory delegates in `Program.cs` using `ActivatorUtilities.CreateInstance`.

### API Key Auth

`[ApiKeyAuthFilter]` protects mutating endpoints; `[SkipApiKeyAuth]` bypasses for read-only endpoints. When `Mcp:ApiKey` is empty, all requests pass (open mode).

### Marker File

On workspace start, `MarkerFileService` writes `AGENTS-README-FIRST.yaml` to the workspace root with port, endpoints, and connection prompt. Removed on stop. This is how AI agents discover the running server.

### Configuration Hierarchy

`PORT` env var → `Mcp:Instances:{name}:Port` → `Mcp:Port` → default 7147. Instance-level config always overrides base-level for all `Mcp:*` keys.

### Central Package Management

Package versions are managed in `Directory.Packages.props`. Project files use `<PackageReference Include="..." />` without version attributes.

### Logging

Serilog with console + optional Parseable HTTP sink + file fallback. Configuration in `Mcp:Parseable` section.
