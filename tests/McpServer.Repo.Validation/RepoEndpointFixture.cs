using Xunit;

namespace McpServer.Repo.Validation;

[CollectionDefinition("RepoEndpoint")]
public sealed class RepoEndpointCollection : ICollectionFixture<RepoEndpointFixture> { }

public sealed class RepoEndpointFixture : IDisposable
{
    public const string BaseUrl = "http://localhost:7147";
    public const string RepoRoute = "/mcpserver/repo";
    public HttpClient Client { get; }

    public RepoEndpointFixture()
    {
        Client = new HttpClient { BaseAddress = new Uri(BaseUrl) };
        var apiKey = Environment.GetEnvironmentVariable("MCPSERVER_APIKEY");
        if (!string.IsNullOrEmpty(apiKey))
            Client.DefaultRequestHeaders.Add("X-Api-Key", apiKey);
    }

    public void Dispose() => Client.Dispose();
}
