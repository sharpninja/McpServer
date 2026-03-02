using Xunit;

namespace McpServer.GitHub.Validation;

[CollectionDefinition("GitHubEndpoint")]
public sealed class GitHubEndpointCollection : ICollectionFixture<GitHubEndpointFixture> { }

public sealed class GitHubEndpointFixture : IDisposable
{
    public const string BaseUrl = "http://localhost:7147";
    public const string GhRoute = "/mcpserver/gh";
    public HttpClient Client { get; }

    public GitHubEndpointFixture()
    {
        Client = new HttpClient { BaseAddress = new Uri(BaseUrl) };
        var apiKey = Environment.GetEnvironmentVariable("MCPSERVER_APIKEY");
        if (!string.IsNullOrEmpty(apiKey))
            Client.DefaultRequestHeaders.Add("X-Api-Key", apiKey);
    }

    public void Dispose() => Client.Dispose();
}
