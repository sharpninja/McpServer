# Frequently Asked Questions

## Getting Started

### What is MCP Server?

MCP Server is a local AI-agent integration server that exposes project context — TODO items, repository files, GitHub issues, session logs, and semantic search — to MCP-compatible clients (Claude Desktop, VS Code Copilot, Cursor) via both HTTP REST and MCP Streamable HTTP transports, and optionally over STDIO.

### How do I run the server?

**As a Windows service:**

```powershell
.\scripts\Manage-McpService.ps1 -Action Install
.\scripts\Manage-McpService.ps1 -Action Start
```

**From the command line (development):**

```bash
dotnet run --project src/McpServer.Support.Mcp -- --instance default
```

**Over STDIO (for MCP clients that prefer stdin/stdout):**

```bash
dotnet run --project src/McpServer.Support.Mcp -- --transport stdio
```

### What port does the server use?

The default port is **7147**. Configure it with:

- `Mcp:Port` in `appsettings.json`
- `PORT` environment variable
- `--urls http://+:PORT` command-line argument

Workspace instances are hosted as in-process Kestrel listeners starting at port **7147**.

### How do I connect an MCP client?

**Claude Desktop** (`claude_desktop_config.json`):

```json
{
  "mcpServers": {
    "mcp-server": {
      "url": "http://localhost:7147/mcp-transport"
    }
  }
}
```

**VS Code / Cursor** (`.vscode/mcp.json`):

```json
{
  "servers": {
    "mcp-server": {
      "type": "sse",
      "url": "http://localhost:7147/mcp-transport"
    }
  }
}
```

The MCP endpoint requires the header `Accept: application/json, text/event-stream`.

---

## TODO Management

### What TODO backends are supported?

Two backends are available, configured via `Mcp:TodoStorage:Provider`:

| Provider | Storage | Best For |
|----------|---------|----------|
| `yaml` (default) | `docs/Project/TODO.yaml` file | Human-readable, version-controlled |
| `sqlite` | `mcp.db` SQLite database | High-volume, concurrent access |

### How are TODO IDs structured?

IDs follow a `SECTION-NNN` pattern (e.g., `APP-001`, `SUPPORT-042`). GitHub-synced items use `ISSUE-{number}` (e.g., `ISSUE-17`).

### Can I sync TODOs with GitHub Issues?

Yes. Bidirectional sync is available:

- **GitHub → TODO**: `POST /mcpserver/gh/issues/sync/from-github`
- **TODO → GitHub**: `POST /mcpserver/gh/issues/sync/to-github`
- **Single issue**: `POST /mcpserver/gh/issues/{number}/sync`

Synced items get `ISSUE-{number}` IDs. Status changes (done ↔ closed) propagate in both directions.

---

## Workspaces

### What is a workspace?

A workspace maps a local folder (e.g., `E:\github\MyProject`) to a managed MCP instance with
its own port, TODO storage, and optional tunnel. Workspace configuration is stored in
`Mcp:Workspaces` within `appsettings.json` — not in the database — and is managed entirely
via the REST API.

### How do I create a workspace?

```bash
curl -X POST http://localhost:7147/mcpserver/workspace \
  -H "Content-Type: application/json" \
  -H "X-Api-Key: YOUR_KEY" \
  -d '{"workspacePath": "E:\\github\\MyProject"}'
```

Defaults are applied automatically: name from last path segment, port auto-assigned from 7147+, TodoPath defaults to `docs/todo.yaml`.

### What does the init endpoint do?

`POST /mcpserver/workspace/{key}/init` scaffolds the workspace directory with:

- Creates directories as needed
- Creates an empty `todo.yaml` at the configured TodoPath
- Prepares the workspace for a child MCP instance

### How is the workspace key encoded?

The `{key}` URL parameter is the Base64URL-encoded `WorkspacePath`. For example, `E:\github\MyProject` encodes to `RTogaXRodWJcTXlQcm9qZWN0`.

---

## Tool Registry

### How does tool search work?

`GET /mcpserver/tools/search?keyword=screenshot` searches across:

1. **Tags** — bidirectional contains match (handles singular/plural, e.g., `screenshot` matches `screenshots`)
2. **Tool name** — case-insensitive contains
3. **Tool description** — case-insensitive contains

Results include both **global** tools (no workspace scope) and **workspace-specific** tools.

### What are tool buckets?

Buckets are GitHub repositories that serve as package registries for tool definitions, similar to Scoop buckets. They contain JSON manifest files describing tools. You can:

- **Add a bucket**: `POST /mcpserver/tools/buckets` with `{owner, repo, branch, path}`
- **Browse tools**: `GET /mcpserver/tools/buckets/{name}/browse`
- **Install a tool**: `POST /mcpserver/tools/buckets/{name}/install?tool=mytool`
- **Sync all**: `POST /mcpserver/tools/buckets/{name}/sync`

Buckets use the `gh` CLI to read repository contents.

---

## Security & Authentication

### How does API key authentication work?

Per-workspace auth tokens are generated on each service restart and written into the `AGENTS-README-FIRST.yaml` marker file in each workspace root. All `/mcpserver/*` endpoints require the token via:

- Header: `X-Api-Key: YOUR_TOKEN`
- Query parameter: `?api_key=YOUR_TOKEN`

Agents read the token from the marker file and include it in requests. Tokens are not persisted — they rotate automatically on restart.

### What is the pairing web UI?

Navigate to `http://localhost:7147/pair` in a browser. This provides a login form for authorized users (configured in `Mcp:PairingUsers`). After authentication, the API key is displayed for copy-paste into MCP client configs.

Passwords are stored as SHA-256 hashes and verified with constant-time comparison to prevent timing attacks. Session cookies are HttpOnly and Secure (when HTTPS).

### Which endpoints are public (no API key)?

| Controller | Public Endpoints |
|------------|------------------|
| Workspace | `GET /mcpserver/workspace`, `GET /mcpserver/workspace/{key}`, `GET /mcpserver/workspace/{key}/status` |
| Tool Registry | `GET /mcpserver/tools/search`, `GET /mcpserver/tools`, `GET /mcpserver/tools/{id}` |
| Health | `GET /health`, `GET /alive` |

---

## Tunneling

### Which tunnel providers are supported?

| Provider | Config Key | Use Case |
|----------|-----------|----------|
| **ngrok** | `ngrok` | SaaS, instant public URLs, zero infrastructure |
| **Cloudflare Tunnel** | `cloudflare` | Free tier, integrates with Cloudflare DNS |
| **FRP** | `frp` | Fully self-hosted, open-source (Apache-2.0) |

Configure via `Mcp:Tunnel:Provider` in `appsettings.json`.

Detailed runbooks:

- `docs/Operations/Tunnel-Ngrok.md`
- `docs/Operations/Tunnel-Cloudflare.md`
- `docs/Operations/Tunnel-FRP-Railway.md`

### How do I self-host with FRP?

1. Deploy `frps` on a VPS: `docker compose -f docker-compose.frps.yml up -d`
2. Set `Mcp:Tunnel:Provider` to `frp`
3. Configure `Mcp:Tunnel:Frp` with your server address, port, and token
4. The MCP server starts `frpc` automatically on launch

### Do tunnels start automatically?

Yes. When `Mcp:Tunnel:Provider` is set to a non-empty value, the tunnel starts as a hosted service with the application lifecycle and stops on shutdown.

### Is the ngrok auth token secure?

Yes. The auth token is passed via the `NGROK_AUTHTOKEN` environment variable, not as a CLI argument, preventing exposure in process listings.

---

## Context & Search

### How does hybrid search work?

The context search endpoint (`POST /mcpserver/context/search`) combines:

1. **FTS5 full-text search** — BM25-ranked SQLite FTS5 with snippet extraction
2. **HNSW vector search** — cosine-similarity nearest-neighbor using all-MiniLM-L6-v2 embeddings (384 dimensions)
3. **Reciprocal Rank Fusion** — merges both result sets with configurable weights

If embeddings are unavailable, search degrades gracefully to FTS5-only.

### What content is indexed?

The ingestion pipeline indexes:

- Repository files (under configured allowlist)
- Session logs (Markdown and JSON formats)
- GitHub issues and PRs (via `gh` CLI)
- External docs (from cached `docs/external/` path)

Trigger a full re-index with `POST /mcpserver/sync/run`.

### What is a context pack?

`POST /mcpserver/context/pack` produces a deterministic collection of ranked context chunks — a curated bundle of relevant content for an AI agent's prompt context.

---

## MCP Transport

### What's the difference between REST and MCP transport?

| Feature | REST API (`/mcpserver/*`) | MCP Transport (`/mcp-transport`) |
|---------|--------------------|---------------------------------|
| Protocol | Standard HTTP/JSON | MCP Streamable HTTP (JSON-RPC) |
| Clients | Any HTTP client, curl, Swagger | Claude Desktop, VS Code Copilot, Cursor |
| Tools | N/A (endpoints) | `todo_*`, `context_*`, `repo_*`, `gh_*`, `sync_*`, `sessionlog_*` |
| Discovery | OpenAPI/Swagger | MCP tool listing |

Both share the same backend services and run on the same port.

### What MCP tools are available?

| Group | Tools |
|-------|-------|
| TODO | `todo_list`, `todo_get`, `todo_create`, `todo_update`, `todo_delete` |
| Context | `context_search`, `context_pack` |
| Repository | `repo_read`, `repo_write`, `repo_list` |
| Sync | `sync_run`, `sync_status` |
| GitHub | `gh_list_issues`, `gh_get_issue`, `gh_create_issue`, `gh_comment_issue`, `gh_list_pulls`, `gh_comment_pull` |
| Session Log | `sessionlog_submit`, `sessionlog_query` |

---

## Windows Service

### How do I install as a Windows service?

```powershell
.\scripts\Manage-McpService.ps1 -Action Install
.\scripts\Manage-McpService.ps1 -Action Start
```

The script uses `gsudo` for UAC elevation and `sc.exe` for service management. The service is configured with auto-start and automatic recovery on failure.

### Where is the service installed?

Published to `C:\ProgramData\McpServer\` as a self-contained single-file executable. Configuration is at `C:\ProgramData\McpServer\appsettings.json`.

### How do I update the service?

```powershell
gsudo .\scripts\Update-McpService.ps1
```

This stops the service, publishes a fresh Debug build, restores all `*.json` and `*.db*` files
(preserving config and data), restarts the service, and verifies health. A timestamped archive
is saved to `%USERPROFILE%\McpServer-Backups\` for rollback.

### What actions are available in the management script?

| Action | Description |
|--------|-------------|
| `Install` | Publish + create Windows service |
| `Uninstall` | Stop + remove service |
| `Start` | Start the service |
| `Stop` | Stop the service |
| `Restart` | Stop + Start |
| `Status` | Show service info + health check |
| `Publish` | Build and publish without service changes |

---

## Troubleshooting

### The server fails to start with "address already in use"

Another process is listening on port 7147. Find and stop it:

```powershell
Get-NetTCPConnection -LocalPort 7147 | Select-Object OwningProcess
Stop-Process -Id <PID>
```

Or change the port in `appsettings.json` under `Mcp:Port`.

### MCP client gets 406 Not Acceptable

The `/mcp-transport` endpoint requires the header:

```text
Accept: application/json, text/event-stream
```

Ensure your MCP client sends this header. Standard REST clients should use the `/mcpserver/*` endpoints instead.

### The health endpoint returns unhealthy

Check that the database is accessible and the configured `DataDirectory` exists. Review logs at the configured Serilog file sink path (default: `logs/mcp-.log`).

### Tool search returns no results

Ensure tools have been registered with appropriate tags. Search matches tags bidirectionally — `screenshot` matches tools tagged `screenshots` and vice versa. Check both global tools and workspace-scoped tools.

### GitHub CLI commands fail

Ensure `gh` is installed and authenticated:

```bash
gh auth status
gh auth login
```

The server uses the local `gh` CLI authentication — no API tokens are configured separately.

## Multi-Tenant Workspace Resolution

### How does workspace resolution work?

The server uses a three-tier resolution chain to determine which workspace a request targets:

1. **`X-Workspace-Path` header** (highest priority) — Send the absolute workspace path in this header. Returns 400 if the path is not a registered workspace.
2. **API key reverse lookup** — The `X-Api-Key` token is mapped back to its generating workspace via `WorkspaceTokenService`.
3. **Default workspace** (lowest priority) — Falls back to the primary workspace from configuration.

### Can I still use per-workspace ports?

No. All workspaces are served on a single shared port. Use the `X-Workspace-Path` header to target a specific workspace. Each workspace still gets its own API key in its marker file, which can also be used for workspace resolution.

### How does the Director switch workspaces?

The Director TUI reuses the same base URL for all workspaces and changes only the `X-Workspace-Path` header when switching workspaces. No need to re-read marker files for workspace switching after the initial connection.

### How is workspace data isolated?

All workspace data is stored in a single shared SQLite database. Each entity table has a `WorkspaceId` column, and EF Core global query filters automatically scope all queries to the active workspace. Admin operations can use `IgnoreQueryFilters()` for cross-workspace queries.
