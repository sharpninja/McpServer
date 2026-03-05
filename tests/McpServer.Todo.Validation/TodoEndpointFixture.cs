using Xunit;

namespace McpServer.Todo.Validation;

/// <summary>
/// Shared fixture that provides an HttpClient configured to hit the live MCP Server
/// on port 7147, plus helper methods for generating unique test TODO IDs.
/// </summary>
public sealed class TodoEndpointFixture : IDisposable
{
    /// <summary>Base URL of the running MCP Server.</summary>
    public const string BaseUrl = "http://localhost:7147";

    /// <summary>Route prefix for TODO endpoints.</summary>
    public const string TodoRoute = "/mcpserver/todo";

    /// <summary>Pre-configured HTTP client targeting the live service.</summary>
    public HttpClient Client { get; }

    public TodoEndpointFixture()
    {
        Client = new HttpClient { BaseAddress = new Uri(BaseUrl) };
    }

    /// <summary>Generate a unique test TODO item ID that won't collide with real data.</summary>
    public static string GenerateTestId()
    {
        // Use a short random suffix to keep IDs readable.
        var suffix = Guid.NewGuid().ToString("N")[..8].ToUpperInvariant();
        return $"AUDIT-{suffix}";
    }

    public void Dispose() => Client.Dispose();
}

/// <summary>xUnit collection definition so all todo tests share the same fixture.</summary>
[CollectionDefinition("TodoEndpoint")]
public sealed class TodoEndpointCollection : ICollectionFixture<TodoEndpointFixture>;
