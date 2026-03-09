using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using Xunit;

namespace McpServer.Client.Tests;

public sealed class ConfigurationClientTests
{
    private static readonly McpServerClientOptions DefaultOptions = new()
    {
        BaseUrl = new Uri("http://localhost:7147"),
        BearerToken = "admin-token"
    };

    [Fact]
    public async System.Threading.Tasks.Task GetValuesAsync_GetsConfigurationEndpoint()
    {
        var handler = new MockHttpHandler(HttpStatusCode.OK, """{"VoiceConversation:CopilotModel":"gpt-5.4"}""");
        using var http = new HttpClient(handler);
        var client = new ConfigurationClient(http, DefaultOptions);

        var result = await client.GetValuesAsync();

        Assert.Equal(HttpMethod.Get, handler.LastRequest!.Method);
        Assert.Contains("/mcpserver/configuration", handler.LastRequest.RequestUri!.AbsolutePath);
        Assert.Equal("gpt-5.4", result["VoiceConversation:CopilotModel"]);
    }

    [Fact]
    public async System.Threading.Tasks.Task PatchValuesAsync_PreservesNullValuesInRequestBody()
    {
        var handler = new MockHttpHandler(
            HttpStatusCode.OK,
            """{"VoiceConversation:CopilotModel":"gpt-5.4","VoiceConversation:ModelApiKeyEnvironmentVariableName":"OPENAI_API_KEY"}""");
        using var http = new HttpClient(handler);
        var client = new ConfigurationClient(http, DefaultOptions);

        var result = await client.PatchValuesAsync(new Dictionary<string, string?>
        {
            ["VoiceConversation:CopilotModel"] = "gpt-5.4",
            ["VoiceConversation:ModelApiKey"] = null
        });

        Assert.Equal(HttpMethod.Patch, handler.LastRequest!.Method);
        Assert.Contains("/mcpserver/configuration", handler.LastRequest.RequestUri!.AbsolutePath);
        Assert.Contains("\"VoiceConversation:CopilotModel\":\"gpt-5.4\"", handler.LastRequestBody!);
        Assert.Contains("\"VoiceConversation:ModelApiKey\":null", handler.LastRequestBody!);
        Assert.Equal("gpt-5.4", result["VoiceConversation:CopilotModel"]);
    }
}
