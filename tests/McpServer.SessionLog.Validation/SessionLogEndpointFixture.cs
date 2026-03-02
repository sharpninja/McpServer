using Xunit;

namespace McpServer.SessionLog.Validation;

/// <summary>
/// Shared fixture providing an HttpClient targeting the live MCP Server on port 7147.
/// </summary>
public sealed class SessionLogEndpointFixture : IDisposable
{
    public const string BaseUrl = "http://localhost:7147";
    public const string SessionLogRoute = "/mcpserver/sessionlog";

    public HttpClient Client { get; }
    public string? ApiKey { get; }

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

    public void Dispose() => Client.Dispose();
}

[CollectionDefinition("SessionLogEndpoint")]
public sealed class SessionLogEndpointCollection : ICollectionFixture<SessionLogEndpointFixture>;
