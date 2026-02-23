using System;
using System.Net;
using System.Net.Http;
using Xunit;

namespace McpServer.Client.Tests;

public sealed class McpServerClientTests
{
    private static readonly McpServerClientOptions TestOptions = new()
    {
        BaseUrl = new Uri("http://localhost:7148"),
        ApiKey = "test-key"
    };

    [Fact]
    public void AllSubClients_AreInitialized()
    {
        using var http = new HttpClient();
        var client = new McpServerClient(http, TestOptions);

        Assert.NotNull(client.Todo);
        Assert.NotNull(client.Context);
        Assert.NotNull(client.SessionLog);
        Assert.NotNull(client.GitHub);
        Assert.NotNull(client.Repo);
        Assert.NotNull(client.Sync);
        Assert.NotNull(client.Workspace);
        Assert.NotNull(client.Tools);
    }

    [Fact]
    public void Factory_CreateWithOptions_Works()
    {
        var client = McpServerClientFactory.Create(TestOptions);

        Assert.NotNull(client);
        Assert.NotNull(client.Todo);
    }

    [Fact]
    public void Factory_CreateWithHttpClient_Works()
    {
        using var http = new HttpClient();
        var client = McpServerClientFactory.Create(http, TestOptions);

        Assert.NotNull(client);
        Assert.NotNull(client.Todo);
    }

    [Fact]
    public void Factory_NullOptions_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => McpServerClientFactory.Create(null!));
    }

    [Fact]
    public void Constructor_MissingApiKey_Throws()
    {
        var options = new McpServerClientOptions { BaseUrl = new Uri("http://localhost:7148") };
        using var http = new HttpClient();
        Assert.Throws<ArgumentException>(() => new McpServerClient(http, options));
    }

    [Fact]
    public void Factory_MissingApiKey_Throws()
    {
        var options = new McpServerClientOptions { BaseUrl = new Uri("http://localhost:7148") };
        Assert.Throws<ArgumentException>(() => McpServerClientFactory.Create(options));
    }
}
