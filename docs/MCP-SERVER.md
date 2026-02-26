# MCP Server Guide

## Overview

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
| `WorkspacePort` | auto | Auto-assigned from 7148+ |
| `TodoPath` | auto | Defaults to `docs/todo.yaml` within `WorkspacePath` |
| `TunnelProvider` | optional | `ngrok`, `cloudflare`, or `frp` |
| `RunAs` | optional | User account for the child process |

Create a workspace:

```bash
curl -X POST http://localhost:7147/mcp/workspace \
  -H "Content-Type: application/json" \
  -d '{"workspacePath": "E:\\github\\MyProject"}'
```

The child MCP instance starts automatically and is accessible at the assigned port (e.g., `http://localhost:7148`).

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
| `GET` | `/mcp/diagnostic/execution-path` | `{ processPath, baseDirectory }` |
| `GET` | `/mcp/diagnostic/appsettings-path` | `{ environmentName, contentRootPath, files[] }` |

Use these to verify which binary and which `appsettings.json` a running instance has loaded.

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

- `/mcp/todo`
- `/mcp/sessionlog`
- `/mcp/context`
- `/mcp/repo`
- `/mcp/gh`
- `/mcp/sync`

Swagger:

- `/swagger`

## Operations Runbook

Update service in-place:

```powershell
gsudo .\scripts\Update-McpService.ps1
```

Health checks:

1. Open `/swagger` and `/health`.
2. Test todo read/write with `/mcp/todo`.
3. Test context search with `/mcp/context/search`.
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
