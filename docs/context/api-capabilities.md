# API Capabilities Reference

Load this file when you need endpoint details, protocol information, or workspace resolution rules.

## Protocols

- **REST API**: All `/mcpserver/*` endpoints (requires `X-Api-Key` header). Full OpenAPI spec at `GET /swagger/v1/swagger.json`. Interactive Swagger UI at `/swagger`.
- **MCP Streamable HTTP**: `POST /mcp-transport` — Model Context Protocol transport for tool-calling agents. No API key required.
- **Health Check**: `GET /health` — returns `{"status":"healthy"}`. No API key required.

## Workspace Resolution

All workspaces share a single server port. Resolution priority:

1. **`X-Workspace-Path` header** (highest priority): explicitly targets a workspace.
2. **API key reverse lookup**: `X-Api-Key` is unique per workspace — the server resolves automatically.
3. **Default workspace**: if neither header nor key is present, the primary workspace is used.

For most agents, including `X-Api-Key` from the marker file is sufficient.

## Stale Marker Detection

To detect a stale marker without auth:

1. `GET /server-startup-utc` — compare `serverStartedAtUtc` to marker's value. If different, re-read the marker.
2. `GET /marker-file-timestamp?repoPath=<workspacePath>` — compare `lastWriteTimeUtc` to marker's `markerWrittenAtUtc`. If newer, re-read.
3. If both match, the marker is current.

## Available Endpoints

- **Context Search**: `POST /mcpserver/context/search` — semantic + full-text hybrid search over indexed project documents
- **Context Pack**: `POST /mcpserver/context/pack` — retrieve ordered context chunks for a topic
- **Context Sources**: `GET /mcpserver/context/sources` — list all indexed document sources
- **Todo Management**: `GET/POST/PUT/DELETE /mcpserver/todo` — query, create, update, delete project tasks
- **Repo Files**: `GET /mcpserver/repo/file`, `POST /mcpserver/repo/file`, `GET /mcpserver/repo/list` — read, write, and list repository files
- **GitHub Integration**: `/mcpserver/gh/issues`, `/mcpserver/gh/pulls`, `/mcpserver/gh/labels` — issue, PR, and label management
- **Tool Registry**: `GET /mcpserver/tools/search` — discover available tools; `GET/POST /mcpserver/tools` — manage tool definitions
- **Session Log**: `POST /mcpserver/sessionlog`, `GET /mcpserver/sessionlog` — session logging
- **MCP Protocol**: `/mcp-transport` — Model Context Protocol streamable HTTP transport endpoint

## Server Health

Before making API calls, verify the server is running: `GET /health` returns `{"status":"healthy"}`.
