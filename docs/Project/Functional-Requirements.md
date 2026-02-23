# Functional Requirements (MCP Server)

## FR-MCP-001 Configurable workspace root and paths

The server shall support configurable `RepoRoot`, `TodoFilePath`, `DataDirectory`, and index paths.

## FR-MCP-002 TODO management API

The server shall provide CRUD/query operations for TODO items over REST and STDIO.

## FR-MCP-003 Session log ingestion and query

The server shall ingest session logs and support searchable queries.

## FR-MCP-004 Hybrid context search

The server shall support FTS and vector search over indexed content.

## FR-MCP-005 GitHub issue sync

The server shall support GitHub issue lifecycle integration and ISSUE-* TODO synchronization.

## FR-MCP-006 Multi-source ingestion

The server shall ingest repository files, session logs, external docs, and issue content.

## FR-MCP-007 Dual transport

The server shall support HTTP and STDIO MCP transports.

## FR-MCP-008 Containerized deployment

The server shall support containerized deployment and packaged distribution.

## FR-MCP-009 Workspace Management

The server shall support dynamic workspace registration, configuration, and lifecycle management — replacing static instance configuration — with per-workspace port assignment, directory scaffolding, and Base64URL-encoded path keys.

## FR-MCP-011 Workspace Process Orchestration

The server shall spawn and manage in-process Kestrel hosts per workspace, with full process lifecycle tracking (start, stop, status), graceful shutdown of all workspace hosts on exit, and automatic startup of all registered workspaces when the service starts.

## FR-MCP-012 Tool Registry

Agents shall be able to discover tools by keyword search across global and workspace-scoped tool definitions, and install tool definitions from GitHub-backed bucket repositories.

## FR-MCP-013 Per-Workspace Auth Tokens

The server shall protect all `/mcp/*` API endpoints with per-workspace cryptographic tokens that rotate on each service restart. Tokens are discoverable via the `AGENTS-README-FIRST.yaml` marker file, checked via the `X-Api-Key` header or `api_key` query parameter, and enforced by `WorkspaceAuthMiddleware` at the pipeline level.

## FR-MCP-014 Pairing Web UI

The server shall provide a browser-based login flow for authorized users to retrieve the server API key for MCP client configuration, backed by SHA-256 constant-time password verification and HttpOnly session cookies.

## FR-MCP-015 Tunnel Providers

The server shall expose its HTTP interface to the internet via pluggable tunnel providers (ngrok, Cloudflare, FRP) configured through a strategy pattern and registered as hosted services.

## FR-MCP-016 MCP Streamable HTTP Transport

The server shall expose a native MCP protocol endpoint at `/mcp-transport` coexisting with the REST API on the same port, enabling standard MCP client connections via `ModelContextProtocol.AspNetCore`.

## FR-MCP-017 Windows Service

The server shall run as a Windows service with automatic startup, failure recovery (restart on failure with 60 s delay), and PowerShell-based install/update/uninstall management.

## FR-MCP-018 Marker File Agent Discovery

When a workspace Kestrel host starts, the server shall write a `.mcp-server.yaml` marker file to the workspace root containing the port, all endpoint paths, a machine-readable prompt, and PID, so that agents can discover and connect to the correct server instance without manual configuration. The marker shall be removed when the host stops.

## FR-MCP-019 Workspace Host Controller Isolation

Workspace-scoped Kestrel hosts shall expose all API controllers except `WorkspaceController`. Workspace lifecycle management (create, delete, start, stop) shall be available only on the primary host.

## FR-MCP-020 Workspace Auto-Start on Service Startup

On service startup, the server shall automatically start Kestrel host instances for all workspaces already registered in the database, restoring availability without manual intervention.

## FR-MCP-021 Workspace Auto-Init and Auto-Start on Creation

When a new workspace is registered, the server shall automatically initialize the workspace directory scaffold (todo.yaml, mcp.db, docs structure) and start its Kestrel host in the same request, so the workspace is immediately operational.

## FR-MCP-022 Tool Registry Default Bucket Seeding

On first startup, the server shall seed default tool buckets from configuration (`Mcp:ToolRegistry:DefaultBuckets`) if they are not already registered, ensuring new installations have the primary tool repository available without manual setup.

## FR-MCP-023 AI-Assisted Requirements Analysis

The server shall provide a requirements analysis capability that invokes the Copilot CLI to examine a TODO item's title, description, and technical details, identify matching existing FR/TR IDs from the project docs, create new FR/TR entries for unaddressed functionality, and persist the assigned IDs back to the TODO item.

## FR-MCP-024 Markdown Session Log Ingestion

The ingestion pipeline shall parse legacy Markdown session log files (matching a `# Session Log – {title}` header pattern) into the unified session log schema alongside JSON session logs, enabling retroactive indexing of pre-existing agent session records.
