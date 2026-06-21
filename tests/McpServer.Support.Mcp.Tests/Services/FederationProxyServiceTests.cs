using System.Text;
using McpServer.Support.Mcp.Options;
using McpServer.Support.Mcp.Services;
using McpServer.Support.Mcp.Services.FederationAdapters;
using McpServer.Support.Mcp.Storage;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NSubstitute;

namespace McpServer.Support.Mcp.Tests.Services;

/// <summary>Unit tests for LocalProxy request forwarding and queued-write fallback.</summary>
public sealed class FederationProxyServiceTests
{
    /// <summary>Failed LocalProxy mutating requests are durably queued and return operation metadata.</summary>
    [Fact]
    public async Task ProxyAsync_LocalProxyWriteQueuesWhenHubUnavailable()
    {
        using var provider = CreateProvider();
        var topology = provider.GetRequiredService<IFederationTopologyService>();
        var factory = Substitute.For<IHttpClientFactory>();
        factory.CreateClient(FederationProxyService.HttpClientName)
            .Returns(_ => new HttpClient(new ThrowingHandler()));
        var sut = new FederationProxyService(
            factory,
            NullLogger<FederationProxyService>.Instance,
            topology,
            Microsoft.Extensions.Options.Options.Create(new FederationOptions { Queue = new FederationQueueOptions { Enabled = true } }));

        var body = Encoding.UTF8.GetBytes("{\"id\":\"PLAN-FEDERATION-001\"}");
        var context = new DefaultHttpContext
        {
            Request =
            {
                Method = HttpMethods.Post,
                Path = "/mcpserver/todo",
                Body = new MemoryStream(body),
                ContentLength = body.Length,
                ContentType = "application/json",
            },
            Response = { Body = new MemoryStream() },
        };

        await sut.ProxyAsync(
                context,
                new FederationTarget("hub", "http://hub.example:7147", null),
                hopCount: 1,
                CancellationToken.None,
                proxyId: "PAYTON-LEGION2",
                globalWorkspaceId: @"F:\GitHub\McpServer",
                queueOnFailure: true)
            .ConfigureAwait(true);

        Assert.Equal(StatusCodes.Status202Accepted, context.Response.StatusCode);
        Assert.Equal("true", context.Response.Headers[FederationHeaders.Queued].Single());
        Assert.True(context.Response.Headers.ContainsKey(FederationHeaders.OperationId));

        var pending = await topology.ListPendingOperationsAsync("PAYTON-LEGION2", 10, 3, CancellationToken.None).ConfigureAwait(true);
        var operation = Assert.Single(pending);
        Assert.Equal("todo", operation.Domain);
        Assert.Equal("/mcpserver/todo", operation.Path);
        Assert.Equal(Convert.ToBase64String(body), operation.BodyBase64);
        Assert.Equal(@"F:\GitHub\McpServer", operation.GlobalWorkspaceId);
    }

    /// <summary>Hub 5xx responses are treated as outage signals and queued when possible.</summary>
    [Fact]
    public async Task ProxyAsync_LocalProxyWriteQueuesWhenHubReturnsServerError()
    {
        using var provider = CreateProvider();
        var topology = provider.GetRequiredService<IFederationTopologyService>();
        var factory = Substitute.For<IHttpClientFactory>();
        factory.CreateClient(FederationProxyService.HttpClientName)
            .Returns(new HttpClient(new StatusHandler(StatusCodes.Status503ServiceUnavailable)));
        var sut = new FederationProxyService(
            factory,
            NullLogger<FederationProxyService>.Instance,
            topology,
            Microsoft.Extensions.Options.Options.Create(new FederationOptions { Queue = new FederationQueueOptions { Enabled = true } }));

        var body = Encoding.UTF8.GetBytes("{\"id\":\"PLAN-FEDERATION-002\"}");
        var context = new DefaultHttpContext
        {
            Request =
            {
                Method = HttpMethods.Post,
                Path = "/mcpserver/todo",
                Body = new MemoryStream(body),
                ContentLength = body.Length,
                ContentType = "application/json",
            },
            Response = { Body = new MemoryStream() },
        };

        await sut.ProxyAsync(
                context,
                new FederationTarget("hub", "http://hub.example:7147", null),
                hopCount: 1,
                CancellationToken.None,
                proxyId: "PAYTON-LEGION2",
                globalWorkspaceId: @"F:\GitHub\McpServer",
                queueOnFailure: true)
            .ConfigureAwait(true);

        Assert.Equal(StatusCodes.Status202Accepted, context.Response.StatusCode);
        Assert.Equal("true", context.Response.Headers[FederationHeaders.Queued].Single());

        var pending = await topology.ListPendingOperationsAsync("PAYTON-LEGION2", 10, 3, CancellationToken.None).ConfigureAwait(true);
        Assert.Single(pending);
    }

    /// <summary>MCP transport posts are forwarded live but are not accepted into the offline replay queue.</summary>
    [Fact]
    public async Task ProxyAsync_McpTransportPostDoesNotQueueWhenHubUnavailable()
    {
        using var provider = CreateProvider();
        var topology = provider.GetRequiredService<IFederationTopologyService>();
        var factory = Substitute.For<IHttpClientFactory>();
        factory.CreateClient(FederationProxyService.HttpClientName)
            .Returns(new HttpClient(new ThrowingHandler()));
        var sut = new FederationProxyService(
            factory,
            NullLogger<FederationProxyService>.Instance,
            topology,
            Microsoft.Extensions.Options.Options.Create(new FederationOptions { Queue = new FederationQueueOptions { Enabled = true } }));

        var body = Encoding.UTF8.GetBytes("{\"jsonrpc\":\"2.0\",\"method\":\"tools/call\"}");
        var context = new DefaultHttpContext
        {
            Request =
            {
                Method = HttpMethods.Post,
                Path = "/mcp-transport",
                Body = new MemoryStream(body),
                ContentLength = body.Length,
                ContentType = "application/json",
            },
            Response = { Body = new MemoryStream() },
        };

        await sut.ProxyAsync(
                context,
                new FederationTarget("hub", "http://hub.example:7147", null),
                hopCount: 1,
                CancellationToken.None,
                proxyId: "PAYTON-LEGION2",
                globalWorkspaceId: @"F:\GitHub\McpServer",
                queueOnFailure: true)
            .ConfigureAwait(true);

        Assert.Equal(StatusCodes.Status502BadGateway, context.Response.StatusCode);
        Assert.False(context.Response.Headers.ContainsKey(FederationHeaders.Queued));

        var pending = await topology.ListPendingOperationsAsync("PAYTON-LEGION2", 10, 3, CancellationToken.None).ConfigureAwait(true);
        Assert.Empty(pending);
    }

    /// <summary>Unsupported TODO subroutes are not accepted into durable replay as fake TODO creates.</summary>
    [Fact]
    public async Task ProxyAsync_UnsupportedTodoSubrouteDoesNotQueueWhenHubUnavailable()
    {
        using var provider = CreateProvider();
        var topology = provider.GetRequiredService<IFederationTopologyService>();
        var factory = Substitute.For<IHttpClientFactory>();
        factory.CreateClient(FederationProxyService.HttpClientName)
            .Returns(new HttpClient(new ThrowingHandler()));
        var sut = new FederationProxyService(
            factory,
            NullLogger<FederationProxyService>.Instance,
            topology,
            Microsoft.Extensions.Options.Options.Create(new FederationOptions { Queue = new FederationQueueOptions { Enabled = true } }));

        var body = Encoding.UTF8.GetBytes("{}");
        var context = new DefaultHttpContext
        {
            Request =
            {
                Method = HttpMethods.Post,
                Path = "/mcpserver/todo/PLAN-FEDERATION-001/requirements",
                Body = new MemoryStream(body),
                ContentLength = body.Length,
                ContentType = "application/json",
            },
            Response = { Body = new MemoryStream() },
        };

        await sut.ProxyAsync(
                context,
                new FederationTarget("hub", "http://hub.example:7147", null),
                hopCount: 1,
                CancellationToken.None,
                proxyId: "PAYTON-LEGION2",
                globalWorkspaceId: @"F:\GitHub\McpServer",
                queueOnFailure: true)
            .ConfigureAwait(true);

        Assert.Equal(StatusCodes.Status502BadGateway, context.Response.StatusCode);
        Assert.False(context.Response.Headers.ContainsKey(FederationHeaders.Queued));

        var pending = await topology.ListPendingOperationsAsync("PAYTON-LEGION2", 10, 3, CancellationToken.None).ConfigureAwait(true);
        Assert.Empty(pending);
    }

    /// <summary>Local proxy writes are not queued when no adapter can apply the domain later.</summary>
    [Fact]
    public async Task ProxyAsync_DoesNotQueueDomainWithoutApplyAdapter()
    {
        using var provider = CreateProvider();
        var topology = provider.GetRequiredService<IFederationTopologyService>();
        var factory = Substitute.For<IHttpClientFactory>();
        factory.CreateClient(FederationProxyService.HttpClientName)
            .Returns(new HttpClient(new ThrowingHandler()));
        var sut = new FederationProxyService(
            factory,
            NullLogger<FederationProxyService>.Instance,
            topology,
            Microsoft.Extensions.Options.Options.Create(new FederationOptions { Queue = new FederationQueueOptions { Enabled = true } }),
            new FederationStateAdapterRegistry(Array.Empty<IFederationStateAdapter>()));

        var body = Encoding.UTF8.GetBytes("{\"id\":\"PLAN-FEDERATION-001\"}");
        var context = new DefaultHttpContext
        {
            Request =
            {
                Method = HttpMethods.Post,
                Path = "/mcpserver/todo",
                Body = new MemoryStream(body),
                ContentLength = body.Length,
                ContentType = "application/json",
            },
            Response = { Body = new MemoryStream() },
        };

        await sut.ProxyAsync(
                context,
                new FederationTarget("hub", "http://hub.example:7147", null),
                hopCount: 1,
                CancellationToken.None,
                proxyId: "PAYTON-LEGION2",
                globalWorkspaceId: @"F:\GitHub\McpServer",
                queueOnFailure: true)
            .ConfigureAwait(true);

        Assert.Equal(StatusCodes.Status502BadGateway, context.Response.StatusCode);
        Assert.False(context.Response.Headers.ContainsKey(FederationHeaders.Queued));

        var pending = await topology.ListPendingOperationsAsync("PAYTON-LEGION2", 10, 3, CancellationToken.None).ConfigureAwait(true);
        Assert.Empty(pending);
    }

    /// <summary>TEST-MCP-MEMORY-FED-001: memory creates with explicit ids are accepted into the offline replay queue.</summary>
    [Fact]
    public async Task ProxyAsync_MemoryCreateWithExplicitIdQueuesWhenHubUnavailable()
    {
        using var provider = CreateProvider();
        var topology = provider.GetRequiredService<IFederationTopologyService>();
        var factory = Substitute.For<IHttpClientFactory>();
        factory.CreateClient(FederationProxyService.HttpClientName)
            .Returns(new HttpClient(new ThrowingHandler()));
        var sut = new FederationProxyService(
            factory,
            NullLogger<FederationProxyService>.Instance,
            topology,
            Microsoft.Extensions.Options.Options.Create(new FederationOptions { Queue = new FederationQueueOptions { Enabled = true } }),
            CreateReplayRegistry());

        var body = Encoding.UTF8.GetBytes("{\"id\":\"MEMORY-OPERATOR-001\",\"category\":\"operator\",\"scope\":\"Workspace\",\"text\":\"Remember exact wording.\"}");
        var context = new DefaultHttpContext
        {
            Request =
            {
                Method = HttpMethods.Post,
                Path = "/mcpserver/memory",
                Body = new MemoryStream(body),
                ContentLength = body.Length,
                ContentType = "application/json",
            },
            Response = { Body = new MemoryStream() },
        };

        await sut.ProxyAsync(
                context,
                new FederationTarget("hub", "http://hub.example:7147", null),
                hopCount: 1,
                CancellationToken.None,
                proxyId: "PAYTON-LEGION2",
                globalWorkspaceId: @"F:\GitHub\McpServer",
                queueOnFailure: true)
            .ConfigureAwait(true);

        Assert.Equal(StatusCodes.Status202Accepted, context.Response.StatusCode);
        Assert.Equal("true", context.Response.Headers[FederationHeaders.Queued].Single());

        var pending = await topology.ListPendingOperationsAsync("PAYTON-LEGION2", 10, 3, CancellationToken.None).ConfigureAwait(true);
        var operation = Assert.Single(pending);
        Assert.Equal("memory", operation.Domain);
        Assert.Equal("MEMORY-OPERATOR-001", operation.ResourceId);
        Assert.Equal("/mcpserver/memory", operation.Path);
        Assert.Equal(Convert.ToBase64String(body), operation.BodyBase64);
    }

    /// <summary>TEST-MCP-MEMORY-FED-001: memory creates without explicit ids are not queued for offline replay.</summary>
    [Fact]
    public async Task ProxyAsync_MemoryCreateWithoutExplicitIdDoesNotQueueWhenHubUnavailable()
    {
        using var provider = CreateProvider();
        var topology = provider.GetRequiredService<IFederationTopologyService>();
        var factory = Substitute.For<IHttpClientFactory>();
        factory.CreateClient(FederationProxyService.HttpClientName)
            .Returns(new HttpClient(new ThrowingHandler()));
        var sut = new FederationProxyService(
            factory,
            NullLogger<FederationProxyService>.Instance,
            topology,
            Microsoft.Extensions.Options.Options.Create(new FederationOptions { Queue = new FederationQueueOptions { Enabled = true } }),
            CreateReplayRegistry());

        var body = Encoding.UTF8.GetBytes("{\"category\":\"operator\",\"scope\":\"Workspace\",\"text\":\"No explicit id.\"}");
        var context = new DefaultHttpContext
        {
            Request =
            {
                Method = HttpMethods.Post,
                Path = "/mcpserver/memory",
                Body = new MemoryStream(body),
                ContentLength = body.Length,
                ContentType = "application/json",
            },
            Response = { Body = new MemoryStream() },
        };

        await sut.ProxyAsync(
                context,
                new FederationTarget("hub", "http://hub.example:7147", null),
                hopCount: 1,
                CancellationToken.None,
                proxyId: "PAYTON-LEGION2",
                globalWorkspaceId: @"F:\GitHub\McpServer",
                queueOnFailure: true)
            .ConfigureAwait(true);

        Assert.Equal(StatusCodes.Status502BadGateway, context.Response.StatusCode);
        Assert.False(context.Response.Headers.ContainsKey(FederationHeaders.Queued));

        var pending = await topology.ListPendingOperationsAsync("PAYTON-LEGION2", 10, 3, CancellationToken.None).ConfigureAwait(true);
        Assert.Empty(pending);
    }

    /// <summary>TEST-MCP-MEMORY-FED-001: memory update and delete requests queue with the memory domain and path id.</summary>
    [Fact]
    public async Task ProxyAsync_MemoryUpdateAndDeleteQueueWithResourceIdWhenHubUnavailable()
    {
        using var provider = CreateProvider();
        var topology = provider.GetRequiredService<IFederationTopologyService>();
        var factory = Substitute.For<IHttpClientFactory>();
        factory.CreateClient(FederationProxyService.HttpClientName)
            .Returns(_ => new HttpClient(new ThrowingHandler()));
        var sut = new FederationProxyService(
            factory,
            NullLogger<FederationProxyService>.Instance,
            topology,
            Microsoft.Extensions.Options.Options.Create(new FederationOptions { Queue = new FederationQueueOptions { Enabled = true } }),
            CreateReplayRegistry());

        var updateBody = Encoding.UTF8.GetBytes("{\"text\":\"Updated memory.\"}");
        var updateContext = CreateMemoryContext(HttpMethods.Put, "/mcpserver/memory/MEMORY-OPERATOR-002", updateBody);
        await sut.ProxyAsync(
                updateContext,
                new FederationTarget("hub", "http://hub.example:7147", null),
                hopCount: 1,
                CancellationToken.None,
                proxyId: "PAYTON-LEGION2",
                globalWorkspaceId: @"F:\GitHub\McpServer",
                queueOnFailure: true)
            .ConfigureAwait(true);

        var deleteContext = CreateMemoryContext(HttpMethods.Delete, "/mcpserver/memory/MEMORY-OPERATOR-003", []);
        await sut.ProxyAsync(
                deleteContext,
                new FederationTarget("hub", "http://hub.example:7147", null),
                hopCount: 1,
                CancellationToken.None,
                proxyId: "PAYTON-LEGION2",
                globalWorkspaceId: @"F:\GitHub\McpServer",
                queueOnFailure: true)
            .ConfigureAwait(true);

        Assert.Equal(StatusCodes.Status202Accepted, updateContext.Response.StatusCode);
        Assert.Equal(StatusCodes.Status202Accepted, deleteContext.Response.StatusCode);

        var pending = await topology.ListPendingOperationsAsync("PAYTON-LEGION2", 10, 3, CancellationToken.None).ConfigureAwait(true);
        Assert.Contains(pending, operation =>
            operation.Domain == "memory" &&
            operation.ResourceId == "MEMORY-OPERATOR-002" &&
            operation.HttpMethod == HttpMethods.Put);
        Assert.Contains(pending, operation =>
            operation.Domain == "memory" &&
            operation.ResourceId == "MEMORY-OPERATOR-003" &&
            operation.HttpMethod == HttpMethods.Delete);
    }

    private static ServiceProvider CreateProvider()
    {
        var services = new ServiceCollection();
        services.AddOptions();
        services.Configure<FederationOptions>(_ => { });
        var databaseRoot = new InMemoryDatabaseRoot();
        var databaseName = $"fed-proxy-{Guid.NewGuid():N}";
        services.AddDbContext<McpDbContext>(options => options.UseInMemoryDatabase(databaseName, databaseRoot));
        services.AddSingleton<IFederationTopologyService, FederationTopologyService>();
        return services.BuildServiceProvider();
    }

    private static DefaultHttpContext CreateMemoryContext(string method, string path, byte[] body)
    {
        var context = new DefaultHttpContext
        {
            Request =
            {
                Method = method,
                Path = path,
                Body = new MemoryStream(body),
                ContentLength = body.Length,
                ContentType = "application/json",
            },
            Response = { Body = new MemoryStream() },
        };
        return context;
    }

    private static FederationStateAdapterRegistry CreateReplayRegistry()
        => new([new ReplayAdapter("memory"), new ReplayAdapter("todo")]);

    private sealed class ReplayAdapter : IFederationStateAdapter
    {
        public ReplayAdapter(string domain)
        {
            Domain = domain;
        }

        public string Domain { get; }

        public bool IsLocalOnly => false;

        public ValueTask<FederationStateSnapshot> SnapshotAsync(string resourceId, CancellationToken cancellationToken)
            => new(new FederationStateSnapshot { Domain = Domain, ResourceId = resourceId, Version = "v1", PayloadJson = "{}" });

        public ValueTask<FederationApplyResult> ApplyAsync(FederationStateOperation operation, CancellationToken cancellationToken)
            => new(new FederationApplyResult { Applied = true, Version = "v2" });

        public ValueTask<string?> GetVersionAsync(string resourceId, CancellationToken cancellationToken)
            => new("v1");

        public string GetIdempotencyKey(FederationStateOperation operation)
            => operation.SourceOperationId ?? operation.OperationId;

        public bool IsEcho(FederationStateOperation operation)
            => false;
    }

    private sealed class ThrowingHandler : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            => throw new HttpRequestException("hub offline");
    }

    private sealed class StatusHandler : HttpMessageHandler
    {
        private readonly int _statusCode;

        public StatusHandler(int statusCode)
        {
            _statusCode = statusCode;
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            => Task.FromResult(new HttpResponseMessage((System.Net.HttpStatusCode)_statusCode));
    }
}
