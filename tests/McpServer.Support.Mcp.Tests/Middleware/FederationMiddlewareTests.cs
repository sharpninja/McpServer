using McpServer.Support.Mcp.Middleware;
using McpServer.Support.Mcp.Options;
using McpServer.Support.Mcp.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NSubstitute;
#pragma warning disable CA2000 // Dispose objects before losing scope — test objects are short-lived

namespace McpServer.Support.Mcp.Tests.Middleware;

/// <summary>
/// Unit tests for <see cref="FederationMiddleware"/>. Validates pass-through and proxy
/// decision logic including the anti-loop check. FR-MCP-077.
/// </summary>
public sealed class FederationMiddlewareTests
{
    private static FederationRegistry CreateRegistry(bool enabled = false, string? defaultTarget = null)
    {
        var opts = new FederationOptions { Enabled = enabled, DefaultTarget = defaultTarget };
        if (defaultTarget is not null)
            opts.Targets.Add(new FederationTargetOptions { Name = defaultTarget, BaseUrl = "http://localhost:7148" });
        return new FederationRegistry(Microsoft.Extensions.Options.Options.Create(opts));
    }

    private static FederationProxyService CreateProxyService()
    {
        var factory = Substitute.For<IHttpClientFactory>();
        // Return an HttpClient with a fake base address that will cause HttpRequestException on connect.
        // This lets us verify the middleware routing logic without a live server.
        factory.CreateClient(FederationProxyService.HttpClientName)
               .Returns(new HttpClient { BaseAddress = new Uri("http://localhost:1/") });
        return new FederationProxyService(factory, NullLogger<FederationProxyService>.Instance);
    }

    private static FederationMiddleware CreateMiddleware(
        RequestDelegate next,
        FederationRegistry registry,
        FederationProxyService? proxy = null,
        int maxHops = 3)
    {
        proxy ??= CreateProxyService();
        var opts = Microsoft.Extensions.Options.Options.Create(new FederationOptions { MaxHops = maxHops });
        return new FederationMiddleware(next, registry, proxy, opts);
    }

    private static DefaultHttpContext CreateContext(string path, string? hopHeader = null)
    {
        var ctx = new DefaultHttpContext
        {
            Request = { Method = "GET", Path = path },
            Response = { Body = new MemoryStream() },
        };
        if (hopHeader is not null)
            ctx.Request.Headers[FederationProxyService.HopCountHeader] = hopHeader;
        return ctx;
    }

    // --- Pass-through cases ---

    /// <summary>Management API path always calls next regardless of federation state.</summary>
    [Fact]
    public async Task InvokeAsync_ManagementPath_CallsNext()
    {
        var registry = CreateRegistry(enabled: true, defaultTarget: "t1");
        var nextCalled = false;
        var mw = CreateMiddleware(_ => { nextCalled = true; return Task.CompletedTask; }, registry);

        await mw.InvokeAsync(CreateContext("/mcpserver/federation/status"), new WorkspaceContext());

        Assert.True(nextCalled);
    }

    /// <summary>Sub-paths of the management prefix also pass through.</summary>
    [Fact]
    public async Task InvokeAsync_ManagementSubPath_CallsNext()
    {
        var registry = CreateRegistry(enabled: true, defaultTarget: "t1");
        var nextCalled = false;
        var mw = CreateMiddleware(_ => { nextCalled = true; return Task.CompletedTask; }, registry);

        await mw.InvokeAsync(CreateContext("/mcpserver/federation/targets"), new WorkspaceContext());

        Assert.True(nextCalled);
    }

    /// <summary>When federation is disabled, all requests pass through.</summary>
    [Fact]
    public async Task InvokeAsync_WhenDisabled_CallsNext()
    {
        var registry = CreateRegistry(enabled: false);
        var nextCalled = false;
        var mw = CreateMiddleware(_ => { nextCalled = true; return Task.CompletedTask; }, registry);

        await mw.InvokeAsync(CreateContext("/mcpserver/todo/list"), new WorkspaceContext());

        Assert.True(nextCalled);
    }

    /// <summary>When enabled but no target resolves, the request passes through.</summary>
    [Fact]
    public async Task InvokeAsync_NoTargetResolved_CallsNext()
    {
        var registry = CreateRegistry(enabled: true);  // no default, no routes
        var nextCalled = false;
        var mw = CreateMiddleware(_ => { nextCalled = true; return Task.CompletedTask; }, registry);

        await mw.InvokeAsync(CreateContext("/mcpserver/todo/list"), new WorkspaceContext());

        Assert.True(nextCalled);
    }

    // --- Anti-loop ---

    /// <summary>Request with hop count at MaxHops returns 508 and does not call next.</summary>
    [Fact]
    public async Task InvokeAsync_HopAtMax_Returns508()
    {
        var registry = CreateRegistry(enabled: true, defaultTarget: "t1");
        var nextCalled = false;
        var mw = CreateMiddleware(_ => { nextCalled = true; return Task.CompletedTask; }, registry, maxHops: 3);

        var ctx = CreateContext("/mcpserver/todo/list", hopHeader: "3");
        await mw.InvokeAsync(ctx, new WorkspaceContext());

        Assert.False(nextCalled);
        Assert.Equal(508, ctx.Response.StatusCode);
    }

    /// <summary>Request with hop count below MaxHops is not treated as a loop (returns non-508).</summary>
    [Fact]
    public async Task InvokeAsync_HopBelowMax_NotALoop()
    {
        var registry = CreateRegistry(enabled: true, defaultTarget: "t1");
        // Use the real proxy service — it will fail to connect to the fake target but that is expected.
        // The important assertion is that the middleware did NOT short-circuit with a 508 Loop Detected.
        var proxy = CreateProxyService();
        var mw = CreateMiddleware(_ => Task.CompletedTask, registry, proxy, maxHops: 3);

        var ctx = CreateContext("/mcpserver/todo/list", hopHeader: "2");
        await mw.InvokeAsync(ctx, new WorkspaceContext());

        Assert.NotEqual(508, ctx.Response.StatusCode);
    }

    /// <summary>Malformed hop header returns 508.</summary>
    [Fact]
    public async Task InvokeAsync_MalformedHopHeader_Returns508()
    {
        var registry = CreateRegistry(enabled: true, defaultTarget: "t1");
        var nextCalled = false;
        var mw = CreateMiddleware(_ => { nextCalled = true; return Task.CompletedTask; }, registry);

        var ctx = CreateContext("/mcpserver/todo/list", hopHeader: "not-a-number");
        await mw.InvokeAsync(ctx, new WorkspaceContext());

        Assert.False(nextCalled);
        Assert.Equal(508, ctx.Response.StatusCode);
    }

    /// <summary>No hop header is treated as hop 0 and is not a loop (returns non-508).</summary>
    [Fact]
    public async Task InvokeAsync_NoHopHeader_NotALoop()
    {
        var registry = CreateRegistry(enabled: true, defaultTarget: "t1");
        // Use the real proxy service — connection to fake target will fail but that is expected.
        // The important assertion is that the middleware did NOT short-circuit with a 508 Loop Detected.
        var proxy = CreateProxyService();
        var mw = CreateMiddleware(_ => Task.CompletedTask, registry, proxy);

        var ctx = CreateContext("/mcpserver/todo/list");
        await mw.InvokeAsync(ctx, new WorkspaceContext());

        Assert.NotEqual(508, ctx.Response.StatusCode);
    }
}
