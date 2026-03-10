# McpServer.AgentFramework

`SharpNinja.McpServer.AgentFramework` is a .NET 9 library for hosting MCP-aware agents inside external Microsoft Agent Framework applications.

## What it provides

- DI-friendly registration through `AddMcpServerAgentFramework(...)`
- canonical session and request identifier generation
- built-in session-log workflow operations
- built-in TODO query, update, and plan/status/implementation workflow operations
- built-in repository read/list/write, local desktop-launch, and in-process PowerShell session operations
- a hosted-agent registration surface that attaches MCP-backed tools to `ChatClientAgent` run options
- a host-facing `IMcpHostedAgent.PowerShellSessions` manager for direct local PowerShell execution

## Basic registration

```csharp
services.AddMcpServerAgentFramework(options =>
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
var powerShellSessions = hostedAgent.PowerShellSessions;
```

## Built-in workflows

- `ISessionLogWorkflow`
  - bootstrap session logs
  - begin, update, complete, and fail turns
  - append dialog and action state
- `ITodoWorkflow`
  - query and get TODO items
  - update TODO items
  - run plan/status/implementation flows with streaming or buffered helpers
- hosted-agent MCP tools
  - `mcp_repo_read`, `mcp_repo_list`, `mcp_repo_write`
  - `mcp_desktop_launch`
  - `mcp_powershell_session_create`, `mcp_powershell_session_command`, `mcp_powershell_session_close`
- host-facing PowerShell sessions
  - `IMcpHostedAgent.PowerShellSessions.CreateSession(...)`
  - `IMcpHostedAgent.PowerShellSessions.ExecuteCommandAsync(...)`
  - `IMcpHostedAgent.PowerShellSessions.ExecuteInteractiveCommandAsync(...)`
  - `IMcpHostedAgent.PowerShellSessions.CloseSession(...)`

## Sample host

See `src\McpServer.AgentFramework.SampleHost` for an interactive console executable that:

- reads MCP configuration from the marker file, environment variables, or `appsettings.yaml`
- creates a hosted agent through `IMcpHostedAgentFactory`
- constructs an OpenAI-backed `ChatClientAgent`
- runs a real conversational loop with the built-in MCP tool surface attached
- shows a PowerShell-style prompt and routes lines prefixed with `! ` directly into the hosted local PowerShell session

## Requirements

- FR-MCP-066
- TR-MCP-AGENT-006
- TR-MCP-AGENT-007
- TEST-MCP-089
