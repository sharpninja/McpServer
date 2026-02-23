using System;
using System.Net;
using System.Net.Http;
using Xunit;

namespace McpServer.Client.Tests;

public sealed class McpServerClientTests
{
    [Fact]
    public void AllSubClients_AreInitialized()
    {
        var options = new McpServerClientOptions { BaseUrl = new Uri("http://localhost:7148") };
        using var http = new HttpClient();
        var client = new McpServerClient(http, options);

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
        var options = new McpServerClientOptions { BaseUrl = new Uri("http://localhost:7148") };
        var client = McpServerClientFactory.Create(options);

        Assert.NotNull(client);
        Assert.NotNull(client.Todo);
    }

    [Fact]
    public void Factory_CreateWithHttpClient_Works()
    {
        var options = new McpServerClientOptions { BaseUrl = new Uri("http://localhost:7148") };
        using var http = new HttpClient();
        var client = McpServerClientFactory.Create(http, options);

        Assert.NotNull(client);
        Assert.NotNull(client.Todo);
    }

    [Fact]
    public void Factory_NullOptions_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => McpServerClientFactory.Create(null!));
    }
}
