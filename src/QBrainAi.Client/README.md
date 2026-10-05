# QBrainAI.Client

Typed REST client for [QBrainAi](https://github.com/sharpninja/McpServer) — the MCP context server for AI agent integration.

## Installation

```shell
dotnet add package QBrainAI.Client
```

## Quick Start

### With Dependency Injection (recommended)

```csharp
builder.Services.AddQBrainAiClient(options =>
{
    options.BaseUrl = new Uri("http://localhost:7147");
    options.ApiKey = "your-api-key"; // optional
});

// Inject QBrainAiClient anywhere
public class MyService(QBrainAiClient mcp)
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
var client = QBrainAiClientFactory.Create(new QBrainAiClientOptions
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
| `AgentPool` | Pooled agent lifecycle, one-shot queueing, and SSE monitoring |
| `Triage` | Incidental bug report intake, queue dashboards, AI run history, and triage-created TODO lookup |

## Endpoint Contract

The package includes `docs/ENDPOINTS.md`, which lists every `QBrainAI.Client` REST/SSE endpoint, the client method that calls it, the request DTO/body, the response DTO/body, and the JSON DTO property names declared by the client models.

## Target Framework

- `net10.0`
