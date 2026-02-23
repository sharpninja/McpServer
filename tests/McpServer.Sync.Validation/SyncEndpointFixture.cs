using Xunit;

namespace McpServer.Sync.Validation;

[CollectionDefinition("SyncEndpoint")]
public sealed class SyncEndpointCollection : ICollectionFixture<SyncEndpointFixture> { }

public sealed class SyncEndpointFixture : IDisposable
{
    public const string BaseUrl = "http://localhost:7147";
    public const string SyncRoute = "/mcp/sync";
    public HttpClient Client { get; }

    public SyncEndpointFixture()
    {
        Client = new HttpClient { BaseAddress = new Uri(BaseUrl), Timeout = TimeSpan.FromMinutes(2) };
        var apiKey = Environment.GetEnvironmentVariable("MCPSERVER_APIKEY");
        if (!string.IsNullOrEmpty(apiKey))
            Client.DefaultRequestHeaders.Add("X-Api-Key", apiKey);
    }

    public void Dispose() => Client.Dispose();
}
