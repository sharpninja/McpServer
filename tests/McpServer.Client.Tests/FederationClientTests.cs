using System;
using System.Net;
using System.Net.Http;
using McpServer.Client.Models;
using Xunit;

namespace McpServer.Client.Tests;

/// <summary>
/// Unit tests for <see cref="FederationClient"/>. Validates correct HTTP method,
/// URL construction, request/response serialization for all federation endpoints.
/// FR-MCP-077, FR-MCP-085.
/// </summary>
public sealed class FederationClientTests
{
    private static readonly McpServerClientOptions DefaultOptions = new()
    {
        BaseUrl = new Uri("http://localhost:7147"),
        ApiKey = "test-key"
    };

    [Fact]
    public async System.Threading.Tasks.Task GetStatusAsync_SendsCorrectRequest()
    {
        var handler = new MockHttpHandler(HttpStatusCode.OK, """{"enabled":true,"targets":[],"workspaceRoutes":[]}""");
        using var http = new HttpClient(handler);
        var client = new FederationClient(http, DefaultOptions);

        var result = await client.GetStatusAsync();

        Assert.NotNull(handler.LastRequest);
        Assert.Equal(HttpMethod.Get, handler.LastRequest.Method);
        Assert.Contains("/mcpserver/federation/status", handler.LastRequest.RequestUri!.AbsolutePath);
        Assert.True(result.Enabled);
    }

    [Fact]
    public async System.Threading.Tasks.Task EnableAsync_PostsCorrectUrl()
    {
        var handler = new MockHttpHandler(HttpStatusCode.OK, """{"enabled":true,"targets":[],"workspaceRoutes":[]}""");
        using var http = new HttpClient(handler);
        var client = new FederationClient(http, DefaultOptions);

        var result = await client.EnableAsync();

        Assert.Equal(HttpMethod.Post, handler.LastRequest!.Method);
        Assert.Contains("/mcpserver/federation/enable", handler.LastRequest.RequestUri!.AbsolutePath);
        Assert.True(result.Enabled);
    }

    [Fact]
    public async System.Threading.Tasks.Task DisableAsync_PostsCorrectUrl()
    {
        var handler = new MockHttpHandler(HttpStatusCode.OK, """{"enabled":false,"targets":[],"workspaceRoutes":[]}""");
        using var http = new HttpClient(handler);
        var client = new FederationClient(http, DefaultOptions);

        var result = await client.DisableAsync();

        Assert.Equal(HttpMethod.Post, handler.LastRequest!.Method);
        Assert.Contains("/mcpserver/federation/disable", handler.LastRequest.RequestUri!.AbsolutePath);
        Assert.False(result.Enabled);
    }

    [Fact]
    public async System.Threading.Tasks.Task ListTargetsAsync_SendsGet()
    {
        var handler = new MockHttpHandler(HttpStatusCode.OK, """[{"name":"remote1","baseUrl":"http://r:7148","hasApiKey":false,"isDefault":true}]""");
        using var http = new HttpClient(handler);
        var client = new FederationClient(http, DefaultOptions);

        var result = await client.ListTargetsAsync();

        Assert.Equal(HttpMethod.Get, handler.LastRequest!.Method);
        Assert.Contains("/mcpserver/federation/targets", handler.LastRequest.RequestUri!.AbsolutePath);
        Assert.Single(result);
        Assert.Equal("remote1", result[0].Name);
        Assert.True(result[0].IsDefault);
    }

    [Fact]
    public async System.Threading.Tasks.Task AddTargetAsync_PostsBodyAndDeserializes()
    {
        var handler = new MockHttpHandler(HttpStatusCode.Created, """{"name":"new-target","baseUrl":"http://r:7148","hasApiKey":true,"isDefault":false}""");
        using var http = new HttpClient(handler);
        var client = new FederationClient(http, DefaultOptions);

        var result = await client.AddTargetAsync(new FederationTargetAddRequest
        {
            Name = "new-target",
            BaseUrl = "http://r:7148",
            ApiKey = "secret"
        });

        Assert.Equal(HttpMethod.Post, handler.LastRequest!.Method);
        Assert.Contains("/mcpserver/federation/targets", handler.LastRequest.RequestUri!.AbsolutePath);
        Assert.Contains("new-target", handler.LastRequestBody);
        Assert.Equal("new-target", result.Name);
        Assert.True(result.HasApiKey);
    }

    [Fact]
    public async System.Threading.Tasks.Task RemoveTargetAsync_SendsDelete()
    {
        var handler = new MockHttpHandler(HttpStatusCode.NoContent, "");
        using var http = new HttpClient(handler);
        var client = new FederationClient(http, DefaultOptions);

        var status = await client.RemoveTargetAsync("old-target");

        Assert.Equal(HttpMethod.Delete, handler.LastRequest!.Method);
        Assert.Contains("/mcpserver/federation/targets/old-target", handler.LastRequest.RequestUri!.AbsolutePath);
        Assert.Equal(HttpStatusCode.NoContent, status);
    }

    [Fact]
    public async System.Threading.Tasks.Task SetDefaultTargetAsync_PostsCorrectUrl()
    {
        var handler = new MockHttpHandler(HttpStatusCode.OK, """{"enabled":true,"targets":[],"workspaceRoutes":[]}""");
        using var http = new HttpClient(handler);
        var client = new FederationClient(http, DefaultOptions);

        var result = await client.SetDefaultTargetAsync("primary");

        Assert.Equal(HttpMethod.Post, handler.LastRequest!.Method);
        Assert.Contains("/mcpserver/federation/targets/primary/set-default", handler.LastRequest.RequestUri!.AbsolutePath);
        Assert.True(result.Enabled);
    }

    [Fact]
    public async System.Threading.Tasks.Task ClearDefaultTargetAsync_SendsDelete()
    {
        var handler = new MockHttpHandler(HttpStatusCode.OK, """{"enabled":true,"targets":[],"workspaceRoutes":[]}""");
        using var http = new HttpClient(handler);
        var client = new FederationClient(http, DefaultOptions);

        var result = await client.ClearDefaultTargetAsync();

        Assert.Equal(HttpMethod.Delete, handler.LastRequest!.Method);
        Assert.Contains("/mcpserver/federation/targets/default", handler.LastRequest.RequestUri!.AbsolutePath);
    }

    [Fact]
    public async System.Threading.Tasks.Task AddRouteAsync_PostsBodyAndDeserializes()
    {
        var handler = new MockHttpHandler(HttpStatusCode.OK, """[{"workspacePath":"C:\\proj","targetName":"remote1"}]""");
        using var http = new HttpClient(handler);
        var client = new FederationClient(http, DefaultOptions);

        var result = await client.AddRouteAsync(new WorkspaceRouteRequest
        {
            WorkspacePath = @"C:\proj",
            TargetName = "remote1"
        });

        Assert.Equal(HttpMethod.Post, handler.LastRequest!.Method);
        Assert.Contains("/mcpserver/federation/routes", handler.LastRequest.RequestUri!.AbsolutePath);
        Assert.Single(result);
        Assert.Equal("remote1", result[0].TargetName);
    }

    [Fact]
    public async System.Threading.Tasks.Task RemoveRouteAsync_SendsDeleteWithBody()
    {
        var handler = new MockHttpHandler(HttpStatusCode.NoContent, "");
        using var http = new HttpClient(handler);
        var client = new FederationClient(http, DefaultOptions);

        var status = await client.RemoveRouteAsync(new WorkspaceRouteRequest
        {
            WorkspacePath = @"C:\proj",
            TargetName = "remote1"
        });

        Assert.Equal(HttpMethod.Delete, handler.LastRequest!.Method);
        Assert.Contains("/mcpserver/federation/routes", handler.LastRequest.RequestUri!.AbsolutePath);
        Assert.Equal(HttpStatusCode.NoContent, status);
    }

    [Fact]
    public async System.Threading.Tasks.Task GetConnectionAsync_SendsWorkspaceName()
    {
        var handler = new MockHttpHandler(HttpStatusCode.OK, """{"baseUrl":"http://host:7147","port":7147,"apiKey":"ws-token"}""");
        using var http = new HttpClient(handler);
        var client = new FederationClient(http, DefaultOptions);

        var result = await client.GetConnectionAsync("MyProject");

        Assert.Equal(HttpMethod.Get, handler.LastRequest!.Method);
        Assert.Contains("workspaceName=MyProject", handler.LastRequest.RequestUri!.Query);
        Assert.Equal(7147, result.Port);
        Assert.Equal("ws-token", result.ApiKey);
    }

    [Fact]
    public async System.Threading.Tasks.Task DiscoverFromTunnelsAsync_PostsCorrectUrl()
    {
        var handler = new MockHttpHandler(HttpStatusCode.OK, """{"discovered":1,"targets":[{"name":"ngrok","baseUrl":"https://abc.ngrok.io","hasApiKey":false,"isDefault":false}]}""");
        using var http = new HttpClient(handler);
        var client = new FederationClient(http, DefaultOptions);

        var result = await client.DiscoverFromTunnelsAsync();

        Assert.Equal(HttpMethod.Post, handler.LastRequest!.Method);
        Assert.Contains("/mcpserver/federation/targets/discover-from-tunnels", handler.LastRequest.RequestUri!.AbsolutePath);
        Assert.Equal(1, result.Discovered);
        Assert.Single(result.Targets);
    }

    [Fact]
    public async System.Threading.Tasks.Task PushAsync_NoFilter_PostsEmptyTypes()
    {
        var handler = new MockHttpHandler(HttpStatusCode.OK, """{"succeeded":5,"failed":0,"errors":[]}""");
        using var http = new HttpClient(handler);
        var client = new FederationClient(http, DefaultOptions);

        var result = await client.PushAsync();

        Assert.Equal(HttpMethod.Post, handler.LastRequest!.Method);
        Assert.Contains("/mcpserver/federation/push", handler.LastRequest.RequestUri!.AbsolutePath);
        Assert.Equal(5, result.Succeeded);
        Assert.Equal(0, result.Failed);
    }

    [Fact]
    public async System.Threading.Tasks.Task PushAsync_WithTypeFilter_PostsTypes()
    {
        var handler = new MockHttpHandler(HttpStatusCode.OK, """{"succeeded":3,"failed":1,"errors":["oops"]}""");
        using var http = new HttpClient(handler);
        var client = new FederationClient(http, DefaultOptions);

        var result = await client.PushAsync(["todos"]);

        Assert.Contains("todos", handler.LastRequestBody);
        Assert.Equal(3, result.Succeeded);
        Assert.Equal(1, result.Failed);
        Assert.Single(result.Errors);
    }

    [Fact]
    public async System.Threading.Tasks.Task FederationClient_ExposedOnFacade()
    {
        var handler = new MockHttpHandler(HttpStatusCode.OK, """{"enabled":false,"targets":[],"workspaceRoutes":[]}""");
        using var http = new HttpClient(handler);
        var facade = new McpServerClient(http, DefaultOptions);

        Assert.NotNull(facade.Federation);

        var result = await facade.Federation.GetStatusAsync();
        Assert.False(result.Enabled);
    }
}
