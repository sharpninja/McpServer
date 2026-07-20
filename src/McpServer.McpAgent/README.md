# McpServer.McpAgent

`SharpNinja.McpServer.McpAgent` is a .NET 10 library for hosting MCP-aware agents as MCP Agent integrations built on Microsoft Agent Framework.

## What it provides

- DI-friendly registration through `AddMcpServerMcpAgent(...)`
- canonical session and request identifier generation
- built-in session-log workflow operations
- built-in TODO query, update, and plan/status/implementation workflow operations
- built-in repository read/list/write, local desktop-launch, and in-process PowerShell session operations
- a hosted-agent registration surface that attaches MCP-backed tools to `ChatClientAgent` run options
- a host-facing `IMcpHostedAgent.PowerShellSessions` manager for direct local PowerShell execution
- a Quad Brain coding-agent tool that routes coding prompts through MCP Server brain-slot orchestration
- an ACID tightly coupled Agent Framework profile that exposes a fail-closed, audited, serialized tool surface

## Basic registration

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
var powerShellSessions = hostedAgent.PowerShellSessions;
```

## ACID tightly coupled profile

Use the ACID profile when a host needs a named Microsoft Agent Framework definition with authenticated workspace binding, durable audit expectations, serialized function invocation, and a restricted model-visible MCP tool surface.

```csharp
services.AddMcpServerMcpAgent(options =>
{
    options.UseAcidTightlyCoupledProfile();
    options.BaseUrl = new Uri("http://localhost:7147");
    options.ApiKey = "token-from-marker";
    options.WorkspacePath = @"E:\github\MyProject";
});

var hostedAgent = serviceProvider.GetRequiredService<IMcpHostedAgent>();
var runtime = hostedAgent.CreateAcidTightlyCoupledRuntime(chatClient);
```

The ACID profile extends Microsoft Agent Framework through `Microsoft.Agents.AI.ChatClientAgent`. It does not certify every MCP endpoint as transactional. By default it exposes only the built-in session/audit, read-only TODO, repository read/list, requirements read, and GraphRAG read/list/get tools. Generic passthrough, desktop launch, local PowerShell, repository writes, TODO mutations, and GraphRAG mutations remain hidden until a separate transaction and audit contract authorizes them.

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

See `src\McpServer.McpAgent.SampleHost` for an interactive preview executable that:

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
- FR-MCP-136
- TR-MCP-AGENT-015
- TEST-MCP-186
- FR-MCP-137
- TR-MCP-AGENT-016
- TEST-MCP-187
