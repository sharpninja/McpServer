using QBrainAi.Client;
using NSubstitute;

namespace QBrainAi.Repl.Core.Tests;

public class QBrainAiClientIntegrationTests
{
    [Fact]
    public void QBrainAiClient_ApiKeyRotation_UpdatesAllSubClients()
    {
        var httpClient = new HttpClient();
        var options = new QBrainAiClientOptions
        {
            BaseUrl = new Uri("http://localhost:5177"),
            ApiKey = "initial-key"
        };

        var client = new QBrainAiClient(httpClient, options);

        Assert.Equal("initial-key", client.ApiKey);

        client.ApiKey = "rotated-key";

        Assert.Equal("rotated-key", client.ApiKey);
    }

    [Fact]
    public void QBrainAiClient_WorkspacePathChange_UpdatesAllSubClients()
    {
        var httpClient = new HttpClient();
        var options = new QBrainAiClientOptions
        {
            BaseUrl = new Uri("http://localhost:5177"),
            WorkspacePath = "/initial/workspace"
        };

        var client = new QBrainAiClient(httpClient, options);

        Assert.Equal("/initial/workspace", client.WorkspacePath);

        client.WorkspacePath = "/new/workspace";

        Assert.Equal("/new/workspace", client.WorkspacePath);
    }

    [Fact]
    public void QBrainAiClient_PortChange_UpdatesAllSubClients()
    {
        var httpClient = new HttpClient();
        var options = new QBrainAiClientOptions
        {
            BaseUrl = new Uri("http://localhost:5177")
        };

        var client = new QBrainAiClient(httpClient, options);

        Assert.Equal(5177, client.Port);

        client.Port = 5178;

        Assert.Equal(5178, client.Port);
    }

    [Fact]
    public void QBrainAiClient_Logout_ClearsApiKeyAndBearerToken()
    {
        var httpClient = new HttpClient();
        var options = new QBrainAiClientOptions
        {
            BaseUrl = new Uri("http://localhost:5177"),
            ApiKey = "test-key",
            BearerToken = "test-bearer"
        };

        var client = new QBrainAiClient(httpClient, options);

        Assert.Equal("test-key", client.ApiKey);
        Assert.Equal("test-bearer", client.BearerToken);

        client.Logout();

        Assert.Equal(string.Empty, client.ApiKey);
        Assert.Equal(string.Empty, client.BearerToken);
    }

    [Fact]
    public async Task QBrainAiClient_MultipleAuthRotations_MaintainsConsistency()
    {
        var httpClient = new HttpClient();
        var options = new QBrainAiClientOptions
        {
            BaseUrl = new Uri("http://localhost:5177"),
            ApiKey = "key-1"
        };

        var client = new QBrainAiClient(httpClient, options);

        client.ApiKey = "key-2";
        Assert.Equal("key-2", client.ApiKey);

        client.ApiKey = "key-3";
        Assert.Equal("key-3", client.ApiKey);

        client.ApiKey = "key-4";
        Assert.Equal("key-4", client.ApiKey);

        await Task.CompletedTask;
    }

    [Fact]
    public async Task QBrainAiClient_SimultaneousWorkspaceAndAuthUpdate_HandlesCorrectly()
    {
        var httpClient = new HttpClient();
        var options = new QBrainAiClientOptions
        {
            BaseUrl = new Uri("http://localhost:5177"),
            ApiKey = "initial-key",
            WorkspacePath = "/initial/workspace"
        };

        var client = new QBrainAiClient(httpClient, options);

        client.ApiKey = "new-key";
        client.WorkspacePath = "/new/workspace";

        Assert.Equal("new-key", client.ApiKey);
        Assert.Equal("/new/workspace", client.WorkspacePath);

        await Task.CompletedTask;
    }

    [Fact]
    public void QBrainAiClient_NullHttpClient_ThrowsArgumentNullException()
    {
        var options = new QBrainAiClientOptions
        {
            BaseUrl = new Uri("http://localhost:5177")
        };

        Assert.Throws<ArgumentNullException>(() => new QBrainAiClient(null!, options));
    }

    [Fact]
    public void QBrainAiClient_NullOptions_ThrowsArgumentNullException()
    {
        var httpClient = new HttpClient();

        Assert.Throws<ArgumentNullException>(() => new QBrainAiClient(httpClient, null!));
    }

    [Fact]
    public async Task QBrainAiClient_BearerTokenRotation_UpdatesAllSubClients()
    {
        var httpClient = new HttpClient();
        var options = new QBrainAiClientOptions
        {
            BaseUrl = new Uri("http://localhost:5177"),
            BearerToken = "initial-bearer"
        };

        var client = new QBrainAiClient(httpClient, options);

        Assert.Equal("initial-bearer", client.BearerToken);

        client.BearerToken = "rotated-bearer";

        Assert.Equal("rotated-bearer", client.BearerToken);

        await Task.CompletedTask;
    }
}
