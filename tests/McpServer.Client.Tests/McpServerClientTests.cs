using System;
using System.Net;
using System.Net.Http;
using Xunit;

namespace McpServer.Client.Tests;

public sealed class McpServerClientTests
{
    private static readonly McpServerClientOptions TestOptions = new()
    {
        BaseUrl = new Uri("http://localhost:7147"),
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
    public void Constructor_NoApiKey_DoesNotThrow()
    {
        var options = new McpServerClientOptions { BaseUrl = new Uri("http://localhost:7147") };
        using var http = new HttpClient();
        var client = new McpServerClient(http, options);
        Assert.NotNull(client);
    }

    [Fact]
    public async System.Threading.Tasks.Task RuntimeCall_WithoutApiKey_ThrowsInvalidOperation()
    {
        var handler = new MockHttpHandler(HttpStatusCode.OK, "{}");
        using var http = new HttpClient(handler);
        var options = new McpServerClientOptions { BaseUrl = new Uri("http://localhost:7147") };
        var client = new McpServerClient(http, options);

        await Assert.ThrowsAsync<InvalidOperationException>(() => client.Todo.QueryAsync());
    }

    [Fact]
    public void Port_PropagatedToAllSubClients()
    {
        using var http = new HttpClient();
        var client = new McpServerClient(http, TestOptions);

        client.Port = 9999;

        Assert.Equal(9999, client.Todo.Port);
        Assert.Equal(9999, client.Context.Port);
        Assert.Equal(9999, client.Sync.Port);
        Assert.Equal(9999, client.Workspace.Port);
        Assert.Equal(9999, client.Repo.Port);
        Assert.Equal(9999, client.GitHub.Port);
        Assert.Equal(9999, client.SessionLog.Port);
        Assert.Equal(9999, client.Tools.Port);
    }

    [Fact]
    public async System.Threading.Tasks.Task InitializeAsync_FetchesDefaultKeyAndSetsOnAllClients()
    {
        var handler = new MockHttpHandler(HttpStatusCode.OK, """{"apiKey":"default-anon-key"}""");
        using var http = new HttpClient(handler);
        var options = new McpServerClientOptions { BaseUrl = new Uri("http://localhost:7147") };
        var client = new McpServerClient(http, options);

        var key = await client.InitializeAsync();

        Assert.Equal("default-anon-key", key);
        Assert.Equal("default-anon-key", client.ApiKey);
        Assert.Equal("default-anon-key", client.Todo.ApiKey);
        Assert.Equal("default-anon-key", client.Context.ApiKey);
        Assert.Equal("default-anon-key", client.Repo.ApiKey);
        Assert.Contains("/api-key", handler.LastRequest!.RequestUri!.ToString());
    }

    [Fact]
    public async System.Threading.Tasks.Task InitializeAsync_SkipsIfApiKeyAlreadySet()
    {
        var handler = new MockHttpHandler(HttpStatusCode.OK, """{"apiKey":"should-not-use"}""");
        using var http = new HttpClient(handler);
        var client = new McpServerClient(http, TestOptions);

        var key = await client.InitializeAsync();

        Assert.Equal("test-key", key);
        Assert.Null(handler.LastRequest); // No HTTP call made
    }

    [Fact]
    public async System.Threading.Tasks.Task InitializeAsync_ServerError_ThrowsMcpServerException()
    {
        var handler = new MockHttpHandler(HttpStatusCode.ServiceUnavailable, """{"error":"not ready"}""");
        using var http = new HttpClient(handler);
        var options = new McpServerClientOptions { BaseUrl = new Uri("http://localhost:7147") };
        var client = new McpServerClient(http, options);

        await Assert.ThrowsAsync<McpServerException>(() => client.InitializeAsync());
    }

    [Fact]
    public async System.Threading.Tasks.Task InitializeAsync_MissingApiKeyInResponse_ThrowsInvalidOperation()
    {
        var handler = new MockHttpHandler(HttpStatusCode.OK, """{"other":"value"}""");
        using var http = new HttpClient(handler);
        var options = new McpServerClientOptions { BaseUrl = new Uri("http://localhost:7147") };
        var client = new McpServerClient(http, options);

        await Assert.ThrowsAsync<InvalidOperationException>(() => client.InitializeAsync());
    }
}
