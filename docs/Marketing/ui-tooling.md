# McpServer — Supported UI Tooling

## Blazor Web UI (`McpServer.Web`)
Full-featured Blazor Server dashboard — Dashboard, Workspaces, Todos, Sessions, Templates, Agents, Context pages. Built with GitHub Primer CSS and Octicons.

## Director CLI (`director`)
.NET global tool. Install: `dotnet tool install --global SharpNinja.McpServer.Director`
Docs: https://github.com/sharpninja/McpServer/blob/develop/docs/Marketing/ui-tooling.md
Commands: `director health`, `director list`, `director agents defs`, `director sync status`, `director login`, `director ui`
Auto-discovers workspace via `AGENTS-README-FIRST.yaml` marker file.

## Director TUI (`director ui`)
Interactive Terminal UI — role-filtered tabs (Health, Workspaces, Agents, TODOs, Sessions, Templates, Context, Policy), keyboard navigation, auto-refresh. Ideal for SSH sessions.

## VS / VS Code Extension (`McpServer-mcp-todo`)
VSIX for Visual Studio 2022/2026 and VS Code. Browse, create, and complete TODOs from the IDE sidebar without context switching.
Source: `extensions/McpServer-mcp-todo/`

## Client NuGet (`SharpNinja.McpServer.Client`)
Typed C# REST client. Install: `dotnet add package SharpNinja.McpServer.Client`
Docs: https://github.com/sharpninja/McpServer/blob/develop/src/McpServer.Client/README.md

```csharp
builder.Services.AddMcpServerClient(options => {
    options.BaseUrl = new Uri("http://localhost:7147");
    options.ApiKey = "your-api-key";
});
```
Covers all endpoints: Todo, Context, SessionLog, GitHub, Repo, Sync, Workspace, Tools, Requirements, Templates.

## MCP STDIO / Streamable HTTP
- MCP Streamable HTTP: `POST <your-mcpserver-base-url>/mcp-transport`
- MCP STDIO: `McpServer.Support.Mcp --transport stdio`
- Compatible: GitHub Copilot, Cursor, Codex, Claude Desktop, any MCP-spec agent

Agent config:
```json
{ "mcpServers": { "mcpserver": { "url": "<your-mcpserver-base-url>/mcp-transport" } } }
```

## Swagger / OpenAPI
Interactive docs at `http://localhost:7147/swagger` — every endpoint with request/response schemas and try-it-now.
