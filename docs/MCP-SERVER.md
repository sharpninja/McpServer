# MCP Server Guide

## Overview

## Documentation

- [User Documentation](USER-GUIDE.md)
- [Documentation Index](README.md)
`McpServer.Support.Mcp` is the local MCP context server for todo data,
session logs, context search, repo file operations, and GitHub issue sync.

Supported transports:

- HTTP REST + Swagger
- STDIO MCP (`--transport stdio`)

## Quick Start

Build and run:

```powershell
.\scripts\Start-McpServer.ps1 -Configuration Staging
```

Run a named instance from `appsettings`:

```powershell
.\scripts\Start-McpServer.ps1 -Configuration Staging -Instance default
```

Run in STDIO mode:

```powershell
dotnet run --project src\McpServer.Support.Mcp\McpServer.Support.Mcp.csproj `
  -c Staging -- --transport stdio --instance default
```

## Configuration

Primary section: `Mcp`.

Common keys:

- `Mcp:Port`
- `Mcp:DataSource`
- `Mcp:DataDirectory`
- `Mcp:RepoRoot`
- `Mcp:TodoFilePath`
- `Mcp:TodoStorage:Provider` (`yaml` or `sqlite`)
- `Mcp:TodoStorage:SqliteDataSource`
- `Mcp:SessionsPath`
- `Mcp:ExternalDocsPath`
- `VoiceConversation:CopilotModel`
- `VoiceConversation:DefaultExecutionStrategy` (`hosted-agentframework` or `copilot-cli`)
- `VoiceConversation:ModelApiKey`
- `VoiceConversation:ModelApiKeyEnvironmentVariableName`

### Config Reference

- `Mcp:Port` (default `7147`): HTTP port when `PORT` is not set.
- `Mcp:DataSource` (default `mcp.db`): main SQLite DB filename or path.
- `Mcp:DataDirectory` (default `.`): base directory for relative DB paths.
- `Mcp:RepoRoot` (default `.`): root folder for repo-aware operations.
- `Mcp:TodoFilePath` (default `docs/Project/TODO.yaml`):
  YAML path relative to `RepoRoot` unless absolute.
- `Mcp:TodoStorage:Provider` (default `yaml`):
  todo backend (`yaml` or `sqlite`).
- `Mcp:TodoStorage:SqliteDataSource` (default `mcp.db`):
  SQLite path for `sqlite` todo backend.
- `Mcp:SessionsPath` (default `docs/sessions`):
  session log folder under `RepoRoot`.
- `Mcp:UnifiedModelSchemaPath`
  (default `docs/schemas/UnifiedModel.schema.json`):
  schema file path.
- `Mcp:ExternalDocsPath` (default `docs/external`):
  external-doc cache folder under `RepoRoot`.
- `VoiceConversation:CopilotModel` (default `gpt-5.3-codex`):
  model used for voice sessions when the request does not override it.
- `VoiceConversation:DefaultExecutionStrategy` (default `copilot-cli`):
  voice backend used when the request does not specify `executionStrategy`.
- `VoiceConversation:ModelApiKey` (default unset):
  optional API key injected into the voice agent/model process.
- `VoiceConversation:ModelApiKeyEnvironmentVariableName` (default `OPENAI_API_KEY`):
  environment variable name used to expose `ModelApiKey` to the underlying process.
- `Mcp:InteractionLogging:*`: request/response interaction logging controls.
- `Mcp:Parseable:*`: Parseable sink controls.
- `Mcp:Instances:{name}:*`: per-instance overrides (static, config-file-only instances).
- `Mcp:Workspaces`: dynamic workspace list managed via API (see [Workspaces](#workspaces)).

Environment overrides:

- `PORT` (highest-priority runtime port override)
- `MCP_INSTANCE` (instance selector when `--instance` is not passed)

## Workspaces

Workspaces are dynamic MCP instances tied to local project folders. They are stored in
`Mcp:Workspaces` inside the active `appsettings.json` (never in the database) and managed
entirely through the REST API — no manual config editing needed.

Each workspace entry has:

| Field | Required | Description |
|-------|----------|-------------|
| `WorkspacePath` | ✅ | Absolute path to the project folder |
| `Name` | auto | Defaults to the last path segment |
| `WorkspacePort` | auto | Shared with host port |
| `TodoPath` | auto | Defaults to `docs/todo.yaml` within `WorkspacePath` |
| `TunnelProvider` | optional | `ngrok`, `cloudflare`, or `frp` |

Create a workspace:

```bash
curl -X POST http://localhost:7147/mcpserver/workspace \
  -H "Content-Type: application/json" \
  -d '{"workspacePath": "E:\\github\\MyProject"}'
```

The workspace is immediately accessible on the shared host port. Target it with the `X-Workspace-Path` header:

```bash
curl http://localhost:7147/mcpserver/todo \
  -H "X-Api-Key: <token>" \
  -H "X-Workspace-Path: E:\\github\\MyProject"
```

### Workspace Resolution

All workspaces share a single port. Per-request workspace identity is resolved via a three-tier chain:

1. **`X-Workspace-Path` header** — highest priority. Send the absolute workspace path.
2. **API key reverse lookup** — the `X-Api-Key` token maps back to its workspace.
3. **Default workspace** — falls back to the primary workspace from configuration.

This eliminates per-workspace ports and simplifies agent connectivity.

Workspace state is written to `{ContentRootPath}/appsettings.json` by the running process.
For the Windows service this is `C:\ProgramData\McpServer\appsettings.json`.

## Production Deployment (Windows Service)

`C:\ProgramData\McpServer\appsettings.json` is the canonical Windows service configuration.
Environment-specific appsettings files (such as `appsettings.Production.json`) are not used by
the Windows service and should not be relied on for runtime configuration.

Update the service in-place (preserves `appsettings.json` and `*.db*` files):

```powershell
gsudo .\scripts\Update-McpService.ps1
```

The script performs: stop → backup all `*.json`/`*.db*` → publish Debug build → copy binaries →
restore backup → start → health-check → archive backup to `%USERPROFILE%\McpServer-Backups\`.

## Diagnostic Endpoints (Debug / Staging only)

Available in Debug builds and `Staging` environment; excluded in Production Release builds.

| Method | Route | Returns |
|--------|-------|---------|
| `GET` | `/mcpserver/diagnostic/execution-path` | `{ processPath, baseDirectory }` |
| `GET` | `/mcpserver/diagnostic/appsettings-path` | `{ environmentName, contentRootPath, files[] }` |

Use these to verify which binary and which `appsettings.json` a running instance has loaded.

## Administrative Configuration Endpoints

These endpoints require a JWT Bearer token with the `admin` role and stay unavailable when OIDC auth is not configured.

- `GET /mcpserver/configuration` — returns the current effective configuration as flattened `section:key` pairs.
- `PATCH /mcpserver/configuration` — accepts a JSON dictionary of flattened keys and patches only those values into `appsettings.yaml`, then reloads the active configuration.

Use `Mcp:Instances:{name}` to define isolated instances with unique ports,
roots, and storage backends.

Example:

```json
{
  "Mcp": {
    "Instances": {
      "default": {
        "Port": 7147,
        "RepoRoot": ".",
        "DataSource": "mcp.db",
        "TodoStorage": {
          "Provider": "yaml",
          "SqliteDataSource": "mcp.db"
        }
      },
      "alt-local": {
        "Port": 7157,
        "RepoRoot": "temp_test",
        "DataSource": "mcp-alt.db",
        "TodoStorage": {
          "Provider": "sqlite",
          "SqliteDataSource": "mcp-alt.db"
        }
      }
    }
  }
}
```

Selection:

- CLI: `--instance <name>`
- ENV: `MCP_INSTANCE=<name>`

Validation:

- Duplicate instance ports are rejected at startup.
- Missing `RepoRoot` or non-numeric `Port` is rejected at startup.

Run two servers concurrently:

```powershell
.\scripts\Start-McpServer.ps1 -Configuration Staging -Instance default
.\scripts\Start-McpServer.ps1 -Configuration Staging -Instance alt-local
```

Automated two-instance smoke test:

```powershell
.\scripts\Test-McpMultiInstance.ps1 -Configuration Staging `
  -FirstInstance default -SecondInstance alt-local
```

Expected endpoints:

- `default` -> `http://localhost:7147/swagger`
- `alt-local` -> `http://localhost:7157/swagger`

## Todo Storage Backends

Backends:

- `yaml`: reads and writes configured `TodoFilePath`
- `sqlite`: stores todo items in SQLite (`todo_items` table)

Backend is selected per instance via `Mcp:Instances:{name}:TodoStorage`.

Migrate between backends:

```powershell
.\scripts\Migrate-McpTodoStorage.ps1 `
  -SourceBaseUrl http://localhost:7147 `
  -TargetBaseUrl http://localhost:7157
```

## API Surface

Primary controllers:

- `/mcpserver/todo`
- `/mcpserver/sessionlog`
- `/mcpserver/context`
- `/mcpserver/repo`
- `/mcpserver/gh`
- `/mcpserver/sync`

Swagger:

- `/swagger`

## Operations Runbook

Update service in-place:

```powershell
gsudo .\scripts\Update-McpService.ps1
```

Health checks:

1. Open `/swagger` and `/health`.
2. Test todo read/write with `/mcpserver/todo`.
3. Test context search with `/mcpserver/context/search`.
4. For GitHub integration, run `gh auth status` on the host.

Log signals:

- Startup shows selected mode and configured sinks.
- Interaction logging middleware captures request/response metadata.

## Troubleshooting

- Port already in use:
  change `Mcp:Port` (or instance `Port`) or stop conflicting process.
- Wrong root folder:
  verify `RepoRoot` on the selected instance.
- Todo not found:
  - YAML: verify `TodoFilePath` exists relative to `RepoRoot`.
  - SQLite: verify `SqliteDataSource` path and file permissions.
- STDIO tools unavailable:
  ensure server started with `--transport stdio`.

## Build and CI

Workflow: `.github/workflows/mcp-server-ci.yml`.

Pipeline responsibilities:

- Restore, build, and test server + tests
- Publish build artifact
- Run markdown and link checks

## Packaging (MSIX)

Script:

- `scripts/Package-McpServerMsix.ps1`

The script publishes output, writes a minimal Appx manifest, and creates an
`.msix` package with `makeappx.exe`.

