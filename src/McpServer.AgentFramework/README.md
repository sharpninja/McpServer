# McpServer.AgentFramework

`SharpNinja.McpServer.AgentFramework` is a .NET 9 library for hosting MCP-aware agents inside external Microsoft Agent Framework applications.

## What it provides

- DI-friendly registration through `AddMcpServerAgentFramework(...)`
- canonical session and request identifier generation
- built-in session-log workflow operations
- built-in TODO query, update, and plan/status/implementation workflow operations
- a hosted-agent registration surface that attaches MCP-backed tools to `ChatClientAgent` run options

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

## Sample host

See `src\McpServer.AgentFramework.SampleHost` for a preview executable that:

- reads MCP configuration from environment variables
- creates a hosted agent through `IMcpHostedAgentFactory`
- constructs `ChatClientAgent`-ready registrations
- prints the attached MCP tool surface in safe preview mode

## Requirements

- FR-MCP-066
- TR-MCP-AGENT-006
- TR-MCP-AGENT-007
- TEST-MCP-089
