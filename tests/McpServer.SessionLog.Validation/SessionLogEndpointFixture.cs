using Xunit;

namespace McpServer.SessionLog.Validation;

/// <summary>
/// Shared fixture providing an HttpClient targeting the live MCP Server on port 7147.
/// </summary>
public sealed class SessionLogEndpointFixture : IDisposable
{
    /// <summary>
    /// Defines <c>BaseUrl</c> constant used by validation tests.
    /// </summary>
    public const string BaseUrl = "http://localhost:7147";
    /// <summary>
    /// Defines <c>SessionLogRoute</c> constant used by validation tests.
    /// </summary>
    public const string SessionLogRoute = "/mcpserver/sessionlog";

    /// <summary>
    /// Gets or sets <c>Client</c> for validation payload/state handling.
    /// </summary>
    public HttpClient Client { get; }
    /// <summary>
    /// Gets or sets <c>ApiKey</c> for validation payload/state handling.
    /// </summary>
    public string? ApiKey { get; }

    /// <summary>
    /// Initializes a new instance of SessionLogEndpointFixture.
    /// </summary>
    /// <remarks>
    /// Requirement coverage: TEST-MCP-015, TEST-MCP-074, FR-MCP-003, TR-MCP-LOG-002.
    /// Test data: Generated session/request IDs plus submit/query/dialog payloads serialized as endpoint JSON bodies.
    /// Data rationale: These inputs verify session-log persistence/query behavior and canonical identifier validation paths.
    /// </remarks>
    public SessionLogEndpointFixture()
    {
        Client = new HttpClient { BaseAddress = new Uri(BaseUrl) };
        ApiKey = Environment.GetEnvironmentVariable("MCPSERVER_APIKEY");
        if (!string.IsNullOrWhiteSpace(ApiKey))
            Client.DefaultRequestHeaders.Add("X-Api-Key", ApiKey);
    }

    /// <summary>Generate a unique session ID for test isolation.</summary>
    public static string GenerateSessionId() => $"audit-test-{Guid.NewGuid():N}";

    /// <summary>Generate a unique request ID for dialog tests.</summary>
    public static string GenerateRequestId() => $"req-{Guid.NewGuid():N}";

    /// <summary>
    /// Releases resources used by validation tests.
    /// </summary>
    /// <remarks>
    /// Requirement coverage: TEST-MCP-015, TEST-MCP-074, FR-MCP-003, TR-MCP-LOG-002.
    /// Test data: Generated session/request IDs plus submit/query/dialog payloads serialized as endpoint JSON bodies.
    /// Data rationale: These inputs verify session-log persistence/query behavior and canonical identifier validation paths.
    /// </remarks>
    public void Dispose() => Client.Dispose();
}

/// <summary>
/// xUnit collection wiring for shared validation fixtures in <c>SessionLogEndpointCollection</c>.
/// </summary>
/// <remarks>
/// Requirement coverage: TEST-MCP-015, TEST-MCP-074, FR-MCP-003, TR-MCP-LOG-002.
/// Test data: Generated session/request IDs plus submit/query/dialog payloads serialized as endpoint JSON bodies.
/// Data rationale: These inputs verify session-log persistence/query behavior and canonical identifier validation paths.
/// </remarks>
[CollectionDefinition("SessionLogEndpoint")]
public sealed class SessionLogEndpointCollection : ICollectionFixture<SessionLogEndpointFixture>;
