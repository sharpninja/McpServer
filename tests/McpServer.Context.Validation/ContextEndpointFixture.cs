using Xunit;

namespace McpServer.Context.Validation;

[CollectionDefinition("ContextEndpoint")]
public sealed class ContextEndpointCollection : ICollectionFixture<ContextEndpointFixture> { }

public sealed class ContextEndpointFixture : IDisposable
{
    public const string BaseUrl = "http://localhost:7147";
    public const string ContextRoute = "/mcpserver/context";
    public HttpClient Client { get; }

    public ContextEndpointFixture()
    {
        Client = new HttpClient { BaseAddress = new Uri(BaseUrl) };
        var apiKey = Environment.GetEnvironmentVariable("MCPSERVER_APIKEY");
        if (!string.IsNullOrEmpty(apiKey))
            Client.DefaultRequestHeaders.Add("X-Api-Key", apiKey);
    }

    public void Dispose() => Client.Dispose();
}
