# MCP Server Guide

## Overview
`McpServer.Support.Mcp` is the local MCP context server for Todo, session log, context search, repo file ops, and GitHub issue sync.

Transports:
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

Run STDIO mode:
```powershell
dotnet run --project src\McpServer.Support.Mcp\McpServer.Support.Mcp.csproj -c Staging -- --transport stdio --instance default
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

### CONFIG-REFERENCE
| Key | Default | Description |
|---|---|---|
| `Mcp:Port` | `7147` | HTTP listen port when `PORT` is not set. |
| `Mcp:DataSource` | `mcp.db` | Primary SQLite DB filename/path. |
| `Mcp:DataDirectory` | `.` | Base directory for relative DB paths. |
| `Mcp:RepoRoot` | `.` | Root folder for repo-aware operations. |
| `Mcp:TodoFilePath` | `docs/Project/TODO.yaml` | YAML TODO path relative to `RepoRoot` unless absolute. |
| `Mcp:TodoStorage:Provider` | `yaml` | TODO backend: `yaml` or `sqlite`. |
| `Mcp:TodoStorage:SqliteDataSource` | `mcp.db` | SQLite TODO database path for `sqlite` provider. |
| `Mcp:SessionsPath` | `docs/sessions` | Session log folder under `RepoRoot`. |
| `Mcp:UnifiedModelSchemaPath` | `docs/schemas/UnifiedModel.schema.json` | Schema file reference path. |
| `Mcp:ExternalDocsPath` | `docs/external` | External-doc cache path under `RepoRoot`. |
| `Mcp:InteractionLogging:*` | see `appsettings.json` | Request/response interaction logging controls. |
| `Mcp:Parseable:*` | see `appsettings.json` | Parseable sink controls. |
| `Mcp:Instances:{name}:*` | n/a | Per-instance overrides for running multiple servers. |

Environment overrides:
- `PORT` (highest-priority runtime port override)
- `MCP_INSTANCE` (instance selector if `--instance` not passed)

## Multi-Instance Support
Use `Mcp:Instances:{name}` to define isolated instances with distinct ports, roots, and storage backends.

Example:
```json
{
  "Mcp": {
    "Instances": {
      "default": {
        "Port": 7147,
        "RepoRoot": ".",
        "DataSource": "mcp.db",
        "TodoStorage": { "Provider": "yaml", "SqliteDataSource": "mcp.db" }
      },
      "alt-local": {
        "Port": 7157,
        "RepoRoot": "temp_test",
        "DataSource": "mcp-alt.db",
        "TodoStorage": { "Provider": "sqlite", "SqliteDataSource": "mcp-alt.db" }
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
- Missing `RepoRoot` or invalid/non-numeric `Port` is rejected at startup.

Running two servers concurrently:
```powershell
.\scripts\Start-McpServer.ps1 -Configuration Staging -Instance default
.\scripts\Start-McpServer.ps1 -Configuration Staging -Instance alt-local
```

Automated two-instance smoke test:
```powershell
.\scripts\Test-McpMultiInstance.ps1 -Configuration Staging -FirstInstance default -SecondInstance alt-local
```

Expected endpoints:
- `default` -> `http://localhost:7147/swagger`
- `alt-local` -> `http://localhost:7157/swagger`

## TODO Storage Backends
Backends:
- `yaml`: reads/writes configured `TodoFilePath`
- `sqlite`: stores TODO items in SQLite (`todo_items` table), preserving existing API contract

Backend is selected per instance via `Mcp:Instances:{name}:TodoStorage`.

Data migration between backends:
```powershell
.\scripts\Migrate-McpTodoStorage.ps1 -SourceBaseUrl http://localhost:7147 -TargetBaseUrl http://localhost:7157
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
Health checks:
1. Open `/swagger` and `/health`.
2. Test TODO read/write with `/mcp/todo`.
3. Test context search with `/mcp/context/search`.
4. For GitHub integration, verify `gh auth status` on host.

Log signals:
- Startup shows selected mode and configured sinks.
- Interaction logging middleware captures request/response metadata.

## Troubleshooting
- Port already in use:
  - Change `Mcp:Port` (or instance `Port`) or stop conflicting process.
- Wrong root folder:
  - Verify `RepoRoot` on selected instance.
- TODO not found:
  - YAML: verify `TodoFilePath` exists relative to `RepoRoot`.
  - SQLite: verify configured `SqliteDataSource` and file permissions.
- STDIO tools unavailable:
  - Ensure server started with `--transport stdio`.

## Build and CI
GitHub workflow: `.github/workflows/mcp-server-ci.yml`

Pipeline responsibilities:
- Restore/build/test MCP server + tests
- Publish build artifact
- Run markdown + link checks for docs quality gates

## Packaging (MSIX)
Script:
- `scripts/Package-McpServerMsix.ps1`

This script builds publish output, generates a minimal Appx manifest, and creates an `.msix` using `makeappx.exe` when available.
