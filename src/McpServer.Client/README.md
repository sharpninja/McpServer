# SharpNinja.McpServer.Client

Typed REST client for [McpServer](https://github.com/sharpninja/McpServer) — the MCP context server for AI agent integration.

## Installation

```shell
dotnet add package SharpNinja.McpServer.Client
```

## Quick Start

### With Dependency Injection (recommended)

```csharp
builder.Services.AddMcpServerClient(options =>
{
    options.BaseUrl = new Uri("http://localhost:7147");
    options.ApiKey = "your-api-key"; // optional
});

// Inject McpServerClient anywhere
public class MyService(McpServerClient mcp)
{
    public async Task Example()
    {
        var todos = await mcp.Todo.QueryAsync();
        var results = await mcp.Context.SearchAsync("authentication");
    }
}
```

### Without DI

```csharp
var client = McpServerClientFactory.Create(new McpServerClientOptions
{
    BaseUrl = new Uri("http://localhost:7147"),
});

var todos = await client.Todo.QueryAsync();
```

## Available Clients

| Client | Description |
|--------|-------------|
| `Todo` | Query, create, update, delete TODO items |
| `Context` | Semantic + full-text hybrid search, context packs |
| `SessionLog` | Submit and query agent session logs |
| `Repo` | Read, write, and list repository files |
| `GitHub` | Issues, PRs, labels, bidirectional sync |
| `Workspace` | Manage workspace lifecycle |
| `Tools` | Tool registry search, CRUD, bucket management |
| `Sync` | Trigger and monitor ingestion sync |

## Target Frameworks

- `net9.0`
- `netstandard2.0` (for broad compatibility)
