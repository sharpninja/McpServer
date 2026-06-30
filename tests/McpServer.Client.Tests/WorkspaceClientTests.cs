using System;
using System.Net;
using System.Net.Http;
using McpServer.Client.Models;
using Xunit;

namespace McpServer.Client.Tests;

public sealed class WorkspaceClientTests
{
    private static readonly McpServerClientOptions DefaultOptions = new()
    {
        BaseUrl = new Uri("http://localhost:7147"),
        ApiKey = "test-key"
    };

    [Fact]
    public async System.Threading.Tasks.Task ListAsync_GetsWorkspaces()
    {
        var handler = new MockHttpHandler(HttpStatusCode.OK, """{"items":[],"totalCount":0}""");
        using var http = new HttpClient(handler);
        var client = new WorkspaceClient(http, DefaultOptions);

        var result = await client.ListAsync();

        Assert.Equal(HttpMethod.Get, handler.LastRequest!.Method);
        Assert.Equal(0, result.TotalCount);
    }

    [Fact]
    public async System.Threading.Tasks.Task StartAsync_PostsToCorrectUrl()
    {
        var handler = new MockHttpHandler(HttpStatusCode.OK, """{"isRunning":true,"port":7149}""");
        using var http = new HttpClient(handler);
        var client = new WorkspaceClient(http, DefaultOptions);

        var result = await client.StartAsync("abc123");

        Assert.True(result.IsRunning);
        Assert.Contains("/mcpserver/workspace/abc123/start", handler.LastRequest!.RequestUri!.AbsolutePath);
    }

    [Fact]
    public async System.Threading.Tasks.Task GetGlobalPromptAsync_GetsPrompt()
    {
        var handler = new MockHttpHandler(HttpStatusCode.OK, """{"template":"Hello {baseUrl}","isDefault":false}""");
        using var http = new HttpClient(handler);
        var client = new WorkspaceClient(http, DefaultOptions);

        var result = await client.GetGlobalPromptAsync();

        Assert.Equal(HttpMethod.Get, handler.LastRequest!.Method);
        Assert.Contains("/mcpserver/workspace/prompt", handler.LastRequest.RequestUri!.AbsolutePath);
        Assert.Equal("Hello {baseUrl}", result.Template);
        Assert.False(result.IsDefault);
    }

    [Fact]
    public async System.Threading.Tasks.Task UpdateGlobalPromptAsync_PutsPrompt()
    {
        var handler = new MockHttpHandler(HttpStatusCode.OK, """{"template":"Custom prompt","isDefault":false}""");
        using var http = new HttpClient(handler);
        var client = new WorkspaceClient(http, DefaultOptions);

        var result = await client.UpdateGlobalPromptAsync(new Models.GlobalPromptUpdateRequest { Template = "Custom prompt" });

        Assert.Equal(HttpMethod.Put, handler.LastRequest!.Method);
        Assert.Contains("/mcpserver/workspace/prompt", handler.LastRequest.RequestUri!.AbsolutePath);
        Assert.Equal("Custom prompt", result.Template);
    }

    /// <summary>TEST-MCP-MARKER-REFRESH-001: marker regeneration uses the typed workspace client endpoint.</summary>
    [Fact]
    public async System.Threading.Tasks.Task RegenerateMarkersAsync_PostsToMarkerRegenerationEndpoint()
    {
        var handler = new MockHttpHandler(HttpStatusCode.OK, """{"regenerated":true,"workspaceCount":2}""");
        using var http = new HttpClient(handler);
        var client = new WorkspaceClient(http, DefaultOptions);

        var result = await client.RegenerateMarkersAsync();

        Assert.Equal(HttpMethod.Post, handler.LastRequest!.Method);
        Assert.Contains("/mcpserver/workspace/markers/regenerate", handler.LastRequest.RequestUri!.AbsolutePath);
        Assert.True(result.Regenerated);
        Assert.Equal(2, result.WorkspaceCount);
    }

    [Fact]
    public async System.Threading.Tasks.Task GetAsync_GetsWorkspaceByKey()
    {
        var handler = new MockHttpHandler(HttpStatusCode.OK, """{"workspacePath":"/tmp","name":"test","todoPath":"t","workspacePort":7149,"isPrimary":false,"isEnabled":true,"statusPrompt":"s","implementPrompt":"i","planPrompt":"p"}""");
        using var http = new HttpClient(handler);
        var client = new WorkspaceClient(http, DefaultOptions);

        var result = await client.GetAsync("abc123");

        Assert.Equal(HttpMethod.Get, handler.LastRequest!.Method);
        Assert.Contains("/mcpserver/workspace/abc123", handler.LastRequest.RequestUri!.AbsolutePath);
        Assert.Equal("test", result.Name);
    }

    [Fact]
    public async System.Threading.Tasks.Task CreateAsync_PostsWorkspace()
    {
        var handler = new MockHttpHandler(HttpStatusCode.Created, """{"success":true}""");
        using var http = new HttpClient(handler);
        var client = new WorkspaceClient(http, DefaultOptions);

        var result = await client.CreateAsync(new Models.WorkspaceCreateRequest { WorkspacePath = "/tmp", Name = "new" });

        Assert.Equal(HttpMethod.Post, handler.LastRequest!.Method);
        Assert.Contains("/mcpserver/workspace", handler.LastRequest.RequestUri!.AbsolutePath);
        Assert.True(result.Success);
    }

    [Fact]
    public async System.Threading.Tasks.Task UpdateAsync_PutsWorkspace()
    {
        var handler = new MockHttpHandler(HttpStatusCode.OK, """{"success":true}""");
        using var http = new HttpClient(handler);
        var client = new WorkspaceClient(http, DefaultOptions);

        var result = await client.UpdateAsync("abc123", new Models.WorkspaceUpdateRequest { Name = "renamed" });

        Assert.Equal(HttpMethod.Put, handler.LastRequest!.Method);
        Assert.Contains("/mcpserver/workspace/abc123", handler.LastRequest.RequestUri!.AbsolutePath);
        Assert.True(result.Success);
    }

    [Fact]
    public async System.Threading.Tasks.Task ApplyPolicyAsync_PostsPolicyDirective()
    {
        var handler = new MockHttpHandler(HttpStatusCode.OK, """{"success":true,"workspaceResults":[]}""");
        using var http = new HttpClient(handler);
        var client = new WorkspaceClient(http, DefaultOptions);

        var result = await client.ApplyPolicyAsync(new Models.WorkspacePolicyApplyRequest
        {
            Directive = "Ban GPL-3.0 in this workspace"
        });

        Assert.Equal(HttpMethod.Post, handler.LastRequest!.Method);
        Assert.Contains("/mcpserver/workspace/policy", handler.LastRequest.RequestUri!.AbsolutePath);
        Assert.True(result.Success);
    }

    [Fact]
    public async System.Threading.Tasks.Task DeleteAsync_DeletesWorkspace()
    {
        var handler = new MockHttpHandler(HttpStatusCode.OK, """{"success":true}""");
        using var http = new HttpClient(handler);
        var client = new WorkspaceClient(http, DefaultOptions);

        var result = await client.DeleteAsync("abc123");

        Assert.Equal(HttpMethod.Delete, handler.LastRequest!.Method);
        Assert.Contains("/mcpserver/workspace/abc123", handler.LastRequest.RequestUri!.AbsolutePath);
        Assert.True(result.Success);
    }

    [Fact]
    public async System.Threading.Tasks.Task InitAsync_PostsInit()
    {
        var handler = new MockHttpHandler(HttpStatusCode.OK, """{"success":true,"filesCreated":["todo.yaml"]}""");
        using var http = new HttpClient(handler);
        var client = new WorkspaceClient(http, DefaultOptions);

        var result = await client.InitAsync("abc123");

        Assert.Equal(HttpMethod.Post, handler.LastRequest!.Method);
        Assert.Contains("/mcpserver/workspace/abc123/init", handler.LastRequest.RequestUri!.AbsolutePath);
        Assert.True(result.Success);
    }

    [Fact]
    public async System.Threading.Tasks.Task StopAsync_PostsStop()
    {
        var handler = new MockHttpHandler(HttpStatusCode.OK, """{"isRunning":false}""");
        using var http = new HttpClient(handler);
        var client = new WorkspaceClient(http, DefaultOptions);

        var result = await client.StopAsync("abc123");

        Assert.Equal(HttpMethod.Post, handler.LastRequest!.Method);
        Assert.Contains("/mcpserver/workspace/abc123/stop", handler.LastRequest.RequestUri!.AbsolutePath);
        Assert.False(result.IsRunning);
    }

    [Fact]
    public async System.Threading.Tasks.Task GetStatusAsync_GetsProcessStatus()
    {
        var handler = new MockHttpHandler(HttpStatusCode.OK, """{"isRunning":true,"pid":1234,"port":7149}""");
        using var http = new HttpClient(handler);
        var client = new WorkspaceClient(http, DefaultOptions);

        var result = await client.GetStatusAsync("abc123");

        Assert.Equal(HttpMethod.Get, handler.LastRequest!.Method);
        Assert.Contains("/mcpserver/workspace/abc123/status", handler.LastRequest.RequestUri!.AbsolutePath);
        Assert.True(result.IsRunning);
        Assert.Equal(1234, result.Pid);
    }

    /// <summary>
    /// TEST-MCP-WORKSPACE-LAYER-001 / TEST-MCP-REQSCOPE-005: workspace current
    /// requirement layer client methods use the workspace endpoints and typed DTOs.
    /// </summary>
    [Fact]
    public async System.Threading.Tasks.Task CurrentRequirementLayerAsync_UsesWorkspaceEndpoints()
    {
        var getHandler = new MockHttpHandler(
            HttpStatusCode.OK,
            """{"currentLayerKey":"layer-2","layer":{"key":"layer-2","order":2,"name":"Layer 2"}}""");
        using var getHttp = new HttpClient(getHandler);
        var getClient = new WorkspaceClient(getHttp, DefaultOptions);

        var current = await getClient.GetCurrentRequirementLayerAsync();

        Assert.Equal(HttpMethod.Get, getHandler.LastRequest!.Method);
        Assert.Contains("/mcpserver/workspace/current-requirement-layer", getHandler.LastRequest.RequestUri!.AbsolutePath);
        Assert.Equal("layer-2", current.CurrentLayerKey);
        Assert.Equal("Layer 2", current.Layer.Name);

        var setHandler = new MockHttpHandler(
            HttpStatusCode.OK,
            """{"currentLayerKey":"layer-3","layer":{"key":"layer-3","order":3,"name":"Layer 3"}}""");
        using var setHttp = new HttpClient(setHandler);
        var setClient = new WorkspaceClient(setHttp, DefaultOptions);

        var updated = await setClient.SetCurrentRequirementLayerAsync(new WorkspaceCurrentRequirementLayerUpdate
        {
            LayerKey = "layer-3"
        });

        Assert.Equal(HttpMethod.Put, setHandler.LastRequest!.Method);
        Assert.Contains("/mcpserver/workspace/current-requirement-layer", setHandler.LastRequest.RequestUri!.AbsolutePath);
        Assert.Contains("\"layerKey\":\"layer-3\"", setHandler.LastRequestBody!, StringComparison.Ordinal);
        Assert.Equal("layer-3", updated.CurrentLayerKey);
    }
}
