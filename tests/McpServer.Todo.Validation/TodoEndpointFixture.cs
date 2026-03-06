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

    /// <summary>
    /// Initializes a new instance of TodoEndpointFixture.
    /// </summary>
    /// <remarks>
    /// Requirement coverage: TEST-MCP-002, TEST-MCP-074, FR-MCP-002, TR-MCP-TODO-002.
    /// Test data: Generated TODO IDs and endpoint payloads for create/update/query/error combinations.
    /// Data rationale: These inputs verify TODO endpoint contract stability, mutation behavior, and validation/error handling paths.
    /// </remarks>
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

    /// <summary>
    /// Releases resources used by validation tests.
    /// </summary>
    /// <remarks>
    /// Requirement coverage: TEST-MCP-002, TEST-MCP-074, FR-MCP-002, TR-MCP-TODO-002.
    /// Test data: Generated TODO IDs and endpoint payloads for create/update/query/error combinations.
    /// Data rationale: These inputs verify TODO endpoint contract stability, mutation behavior, and validation/error handling paths.
    /// </remarks>
    public void Dispose() => Client.Dispose();
}

/// <summary>xUnit collection definition so all todo tests share the same fixture.</summary>
[CollectionDefinition("TodoEndpoint")]
public sealed class TodoEndpointCollection : ICollectionFixture<TodoEndpointFixture>;
