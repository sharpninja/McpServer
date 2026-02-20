# Client Integration Guide

This guide explains how to connect VS Code extensions, Visual Studio VSIX packages,
and other MCP clients to the standalone MCP server.

## Endpoints

| Transport | Default | Configuration |
|-----------|---------|---------------|
| HTTP REST | `http://localhost:7147` | `Mcp:Port` in appsettings.json |
| STDIO | `dotnet run --project src/McpServer.Support.Mcp -- --transport stdio` | Command-line |

## VS Code Extension

The `McpServer-mcp-todo` VS Code extension communicates with the MCP server via HTTP REST.

### Configuration

In VS Code `settings.json`:

```json
{
  "mcpServer.url": "http://localhost:7147",
  "mcpServer.todoEndpoint": "/mcp/todo"
}
```

### Installation

```bash
code --install-extension extensions/McpServer-mcp-todo/McpServer-mcp-todo-0.7.0.vsix
```

Or use the deployment script:

```powershell
./scripts/Deploy-McpTodoExtension.ps1
```

### Docker Mode

When running the MCP server in Docker, the extension connects to the same URL
(`http://localhost:7147`) since the Docker port is mapped to the host.

## Visual Studio VSIX

The `McpServer.VsExtension.McpTodo.Vsix` project provides a Visual Studio 2022+ extension.

### Building

```bash
dotnet build src/McpServer.VsExtension.McpTodo.Vsix/McpServer.VsExtension.McpTodo.Vsix.csproj -c Release
```

### Configuration

The VSIX reads the MCP server URL from VS settings or defaults to `http://localhost:7147`.

## STDIO Transport (Cursor / MCP Clients)

For MCP-compatible clients (e.g., Cursor), configure the STDIO transport:

### Cursor `.cursor/mcp.json`

```json
{
  "mcpServers": {
    "fwh-mcp": {
      "command": "dotnet",
      "args": ["run", "--project", "E:\\github\\McpServer\\src\\McpServer.Support.Mcp", "--", "--transport", "stdio"]
    }
  }
}
```

### Available STDIO Tools

See `docs/stdio-tool-contract.json` for the complete machine-readable manifest of all 21 tools.

Key tool categories:
- **Context**: `context_search`, `context_pack`, `context_sources`
- **Repository**: `repo_read`, `repo_list`, `repo_write`
- **Sync**: `sync_run`, `sync_status`
- **TODO**: `todo_list`, `todo_get`, `todo_create`, `todo_update`, `todo_delete`
- **Session Logs**: `sessionlog_submit`, `sessionlog_query`, `sessionlog_dialog`
- **GitHub**: `github_list_issues`, `github_list_pulls`, `github_create_issue`, `github_comment_issue`, `github_comment_pull`

## Multi-Instance

When running multiple MCP instances (e.g., for different workspaces), each instance
runs on a different port. Update client configuration to point to the correct port:

```json
{
  "mcpServer.url": "http://localhost:7157"
}
```

See `appsettings.json` → `Mcp:Instances` for instance configuration.

## Health Check

All clients should verify connectivity before making API calls:

```
GET /health → { "status": "Healthy" }
```

## Swagger / OpenAPI

Interactive API documentation is available at:
- Swagger UI: `http://localhost:7147/swagger`
- OpenAPI JSON: `http://localhost:7147/swagger/v1/swagger.json`
