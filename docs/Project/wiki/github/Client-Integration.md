# Client Integration Guide

This guide explains how to connect VS Code extensions, Visual Studio VSIX packages,
and other MCP clients to the standalone MCP server.

## Endpoints

| Transport | Default | Configuration |
|-----------|---------|---------------|
| HTTP REST | `http://localhost:7147` | `Mcp:Port` in appsettings.json |
| STDIO | `dotnet run --project src/McpServer.Support.Mcp -- --transport stdio` | Command-line |

## VS Code / Cursor (MCP Streamable HTTP)

For VS Code Copilot, Cursor, and other MCP-compatible editors, configure the Streamable HTTP transport via `.vscode/mcp.json`:

```json
{
  "servers": {
    "mcp-server": {
      "type": "http",
      "url": "http://localhost:7147/mcp-transport"
    }
  }
}
```

## Agent Plugin Availability

MCP-compatible editor clients can connect directly through Streamable HTTP or STDIO. Audited agent workflows are stricter: when `AGENTS-README-FIRST.yaml` declares `agent_plugins.policy: required`, each agent must use its matching plugin for session log, TODO, requirements, import/export, and traceability operations.

See `docs/AGENT-PLUGIN-AVAILABILITY.md` for current plugin repositories, expected local roots, status wrappers, and failure behavior.

### Docker Mode

When running the MCP server in Docker, the extension connects to the same URL
(`http://localhost:7147`) since the Docker port is mapped to the host.

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

See `docs/stdio-tool-contract.json` for the complete machine-readable manifest of all STDIO tools.

Key tool categories:

- **Context**: `context_search`, `context_pack`, `context_sources`, `context_ingest_website`
- **Repository**: `repo_read`, `repo_list`, `repo_write`
- **Desktop**: `desktop_launch`
- **Sync**: `sync_run`, `sync_status`
- **TODO**: `todo_list`, `todo_get`, `todo_create`, `todo_update`, `todo_delete`
- **Session Logs**: `sessionlog_submit`, `sessionlog_query`, `sessionlog_dialog`, `sessionlog_open`, `sessionlog_begin_turn`, `sessionlog_complete_turn`, `sessionlog_fail_turn`
- **Session Logs (replace/remove)**: `sessionlog_replace_turn`, `sessionlog_replace_section`, `sessionlog_clear_section`, `sessionlog_delete_item`, `sessionlog_delete_turn`, `sessionlog_delete_session` (PUT=replace, DELETE=remove; see [session-log-workflow-api.md](context/session-log-workflow-api.md#replacing-and-removing-data-patch--put--delete))
- **GitHub**: `github_list_issues`, `github_list_pulls`, `github_create_issue`, `github_comment_issue`, `github_comment_pull`
- **Agent Help**: `agent_help_create_session`, `agent_help_submit_turn`, `agent_help_get_status` (see marker `## Agent Help (MCP Server issues)`)
- **Use cases**: `usecase_list`, `usecase_get`, `usecase_create`, `usecase_update`, `usecase_delete`, `usecase_link`, `usecase_diagram`, `usecase_coverage`, approval/product tools (see Swagger and plugin `usecase` skill)

## Typed client: Use Cases

`McpServerClient.UseCases` (`UseCaseClient`) covers `/mcpserver/usecases`:

```csharp
var uc = await client.UseCases.CreateAsync(new CreateUseCaseRequest
{
    Title = "Sign in",
    CreateBasicFlow = true,
});
var graph = await client.UseCases.GetDiagramGraphAsync(uc.UseCaseId);
// graph.SchemaVersion == 1; nodes/edges for UML canvas
await client.UseCases.PutDiagramGraphAsync(uc.UseCaseId, graph);
var umlMermaid = await client.UseCases.GetDiagramAsync(uc.UseCaseId, format: "mermaid", kind: "usecase");
var sequence = await client.UseCases.GetDiagramAsync(uc.UseCaseId, format: "mermaid", kind: "sequence");
var coverage = await client.UseCases.GetCoverageAsync();
```

Mermaid UML export uses project schema `%% mcp-usecase-diagram-schema:1` (see `docs/context/usecase-diagram-mermaid-schema-v1.md`). Sequence diagrams remain separate (`kind=sequence` from flows/steps).

## Workspace Targeting

All workspaces share a single port. To target a specific workspace, send the `X-Workspace-Path` header:

```bash
curl http://localhost:7147/mcpserver/todo \
  -H "X-Api-Key: <token>" \
  -H "X-Workspace-Path: E:\\github\\MyProject"
```

Resolution chain: `X-Workspace-Path` header → API key reverse lookup → default workspace.

### Typed Client Library

```csharp
var client = McpServerClientFactory.Create(new McpServerClientOptions
{
    BaseUrl = new Uri("http://localhost:7147"),
    ApiKey = "token-from-marker",
    DesktopLaunchToken = "desktop-launch-token-from-secure-config",
    WorkspacePath = @"E:\github\MyProject",
});
// All requests include both X-Api-Key and X-Workspace-Path headers
var todos = await client.Todo.QueryAsync();
var launch = await client.Desktop.LaunchAsync(new DesktopLaunchRequest
{
    ExecutablePath = @"C:\Windows\System32\cmd.exe",
    Arguments = "/c exit 0",
    CreateNoWindow = true,
    WaitForExit = true,
});
```

Agent Help for MCP Server issue diagnosis:

```csharp
var help = await client.AgentHelp.CreateSessionAsync(new AgentHelpSessionCreateRequest
{
    WorkspacePath = client.WorkspacePath,
    Topic = "marker trust failure",
});
var turn = await client.AgentHelp.SubmitTurnAsync(help.SessionId, new AgentHelpTurnRequest
{
    UserMessage = "POST /mcpserver/todo returns 401 after server restart.",
});
var status = await client.AgentHelp.GetStatusAsync(help.SessionId);
```

REST surface: `/mcpserver/agent-help/session`, `/mcpserver/agent-help/session/{id}`, `/mcpserver/agent-help/session/{id}/turn`, `/mcpserver/agent-help/session/{id}/transcript`, plus SSE/WebSocket streaming endpoints.

Remote desktop launch also requires the server-side `Mcp:DesktopLaunch:Enabled` feature gate,
the `Mcp:DesktopLaunch:AllowedExecutables` allowlist, and the privileged
`X-Desktop-Launch-Token` header supplied by `McpServerClientOptions.DesktopLaunchToken`.

Switch workspace at runtime:

```csharp
client.WorkspacePath = @"E:\github\OtherProject";
```

Admin-only configuration endpoints are also available through the typed client when you supply an admin JWT bearer token:

```csharp
client.BearerToken = adminJwt;
var values = await client.Configuration.GetValuesAsync();

var updated = await client.Configuration.PatchValuesAsync(new Dictionary<string, string?>
{
    ["VoiceConversation:CopilotModel"] = "gpt-5.4",
    ["VoiceConversation:ModelApiKey"] = null, // remove the persisted key
});
```

## Hosted .NET Agent Framework Library

Use `src\McpServer.McpAgent` when you want a .NET 9 host application to consume MCP Server session-log, TODO, repository, desktop-launch, and in-process PowerShell workflows through Microsoft Agent Framework-oriented registration instead of hand-assembling transport glue.

Typical registration:

```csharp
services.AddMcpServerMcpAgent(options =>
{
    options.BaseUrl = new Uri("http://localhost:7147");
    options.ApiKey = "token-from-marker";
    options.WorkspacePath = @"E:\github\MyProject";
    options.SourceType = "Codex";
});

using var serviceProvider = services.BuildServiceProvider();
var hostedAgentFactory = serviceProvider.GetRequiredService<IMcpHostedAgentFactory>();
var hostedAgent = hostedAgentFactory.CreateHostedAgent();
var registration = hostedAgent.Registration;
```

Built-in hosted services include:

- `ISessionLogWorkflow` for session bootstrap, turn lifecycle updates, and canonical session/request identifiers.
- `ITodoWorkflow` for TODO query/get/update plus buffered or streaming plan/status/implementation flows.
- built-in MCP tools for repository access, local desktop launch, and in-process PowerShell sessions (`mcp_repo_*`, `mcp_desktop_launch`, `mcp_powershell_session_*`).
- `IMcpHostedAgent` / `IMcpHostedAgentFactory` for creating `ChatClientAgent`-ready registrations and run options with the built-in MCP tool set attached, plus `IMcpHostedAgent.PowerShellSessions` for host-driven direct local PowerShell execution.

Reference implementations:

- Library source: `src\McpServer.McpAgent`
- Interactive preview host: `src\McpServer.McpAgent.SampleHost`
- Automated acceptance coverage: `tests\McpServer.McpAgent.Tests\HostedAgentWorkflowIntegrationTests.cs`

## Health Check

All clients should verify connectivity before making API calls:

```text
GET /health → { "status": "Healthy" }
```

## Swagger / OpenAPI

Interactive API documentation is available at:

- Swagger UI: `http://localhost:7147/swagger`
- OpenAPI JSON: `http://localhost:7147/swagger/v1/swagger.json`
