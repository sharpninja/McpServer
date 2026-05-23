using System.Net;
using System.Globalization;
using System.Text.Json;
using McpServer.Support.Mcp.Options;
using McpServer.Support.Mcp.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NSubstitute;

namespace McpServer.Support.Mcp.Tests.Services;

/// <summary>Tests for automatic LocalProxy enrollment and heartbeat behavior.</summary>
public sealed class FederationLocalProxyEnrollmentServiceTests
{
    /// <summary>Disabled federation does not contact the hub, validating TR-MCP-FED-001 role gating.</summary>
    [Fact]
    public async Task EnrollOrHeartbeatOnceAsync_DisabledFederationSkipsHubCall()
    {
        var handler = new CapturingHandler(_ => JsonResponse(new { accepted = true }));
        var sut = CreateSut(new FederationOptions(), handler);

        await sut.EnrollOrHeartbeatOnceAsync(CancellationToken.None).ConfigureAwait(true);

        Assert.Empty(handler.Requests);
    }

    /// <summary>A LocalProxy without a configured hub URL does not attempt enrollment.</summary>
    [Fact]
    public async Task EnrollOrHeartbeatOnceAsync_MissingHubUrlSkipsHubCall()
    {
        var handler = new CapturingHandler(_ => JsonResponse(new { accepted = true }));
        var sut = CreateSut(new FederationOptions
        {
            Enabled = true,
            Role = FederationRole.LocalProxy,
            ProxyId = "PAYTON-LEGION2",
        }, handler);

        await sut.EnrollOrHeartbeatOnceAsync(CancellationToken.None).ConfigureAwait(true);

        Assert.Empty(handler.Requests);
    }

    /// <summary>Initial LocalProxy cycle enrolls with proxy id, token, callback URL, metadata, and workspace inventory.</summary>
    [Fact]
    public async Task EnrollOrHeartbeatOnceAsync_FirstCycleEnrollsWithWorkspaceInventory()
    {
        var handler = new CapturingHandler(_ => JsonResponse(new
        {
            proxyId = "PAYTON-LEGION2",
            accepted = true,
            serverTimeUtc = DateTimeOffset.UtcNow,
            heartbeatSeconds = 5,
        }));
        var sut = CreateSut(CreateLocalProxyOptions(), handler);

        await sut.EnrollOrHeartbeatOnceAsync(CancellationToken.None).ConfigureAwait(true);

        var request = Assert.Single(handler.Requests);
        Assert.Equal("/mcpserver/federation/proxies/enroll", request.RequestUri!.AbsolutePath);
        Assert.Equal("hub-secret", request.Headers.GetValues("X-Api-Key").Single());
        using var document = JsonDocument.Parse(Assert.Single(handler.Bodies));
        var root = document.RootElement;
        Assert.Equal("PAYTON-LEGION2", root.GetProperty("proxyId").GetString());
        Assert.Equal("test-secret", root.GetProperty("enrollmentToken").GetString());
        Assert.Contains(":7147", root.GetProperty("baseUrl").GetString(), StringComparison.Ordinal);
        Assert.Equal("McpServer", root.GetProperty("workspaces")[0].GetProperty("workspaceName").GetString());
        Assert.Equal(@"F:\GitHub\McpServer", root.GetProperty("workspaces")[0].GetProperty("workspacePath").GetString());
    }

    /// <summary>After successful enrollment, the next LocalProxy cycle sends a heartbeat with fresh inventory.</summary>
    [Fact]
    public async Task EnrollOrHeartbeatOnceAsync_SecondCycleSendsHeartbeat()
    {
        var handler = new CapturingHandler(request => request.RequestUri!.AbsolutePath.EndsWith("/enroll", StringComparison.Ordinal)
            ? JsonResponse(new
            {
                proxyId = "PAYTON-LEGION2",
                accepted = true,
                serverTimeUtc = DateTimeOffset.UtcNow,
                heartbeatSeconds = 5,
            })
            : JsonResponse(new
            {
                proxyId = "PAYTON-LEGION2",
                recordedAtUtc = DateTimeOffset.UtcNow,
                queueDepth = 0,
                conflictCount = 0,
            }));
        var sut = CreateSut(CreateLocalProxyOptions(), handler);

        await sut.EnrollOrHeartbeatOnceAsync(CancellationToken.None).ConfigureAwait(true);
        await sut.EnrollOrHeartbeatOnceAsync(CancellationToken.None).ConfigureAwait(true);

        Assert.Equal(2, handler.Requests.Count);
        Assert.Equal("/mcpserver/federation/proxies/enroll", handler.Requests[0].RequestUri!.AbsolutePath);
        Assert.Equal("/mcpserver/federation/proxies/PAYTON-LEGION2/heartbeat", handler.Requests[1].RequestUri!.AbsolutePath);
        Assert.Equal("hub-secret", handler.Requests[1].Headers.GetValues("X-Api-Key").Single());
        using var heartbeat = JsonDocument.Parse(handler.Bodies[1]);
        Assert.Equal("online", heartbeat.RootElement.GetProperty("status").GetString());
        Assert.Equal(@"F:\GitHub\McpServer", heartbeat.RootElement.GetProperty("workspaces")[0].GetProperty("workspacePath").GetString());
    }

    /// <summary>Hub outages are logged and leave the next cycle ready to retry enrollment.</summary>
    [Fact]
    public async Task EnrollOrHeartbeatOnceAsync_HubOutageRetriesEnrollmentNextCycle()
    {
        var attempts = 0;
        var handler = new CapturingHandler(_ =>
        {
            attempts++;
            if (attempts == 1)
                throw new HttpRequestException("hub offline");

            return JsonResponse(new
            {
                proxyId = "PAYTON-LEGION2",
                accepted = true,
                serverTimeUtc = DateTimeOffset.UtcNow,
                heartbeatSeconds = 5,
            });
        });
        var sut = CreateSut(CreateLocalProxyOptions(), handler);

        await sut.EnrollOrHeartbeatOnceAsync(CancellationToken.None).ConfigureAwait(true);
        await sut.EnrollOrHeartbeatOnceAsync(CancellationToken.None).ConfigureAwait(true);

        Assert.Equal(2, handler.Requests.Count);
        Assert.All(handler.Requests, request => Assert.Equal("/mcpserver/federation/proxies/enroll", request.RequestUri!.AbsolutePath));
    }

    private static FederationOptions CreateLocalProxyOptions()
        => new()
        {
            Enabled = true,
            Role = FederationRole.LocalProxy,
            HubBaseUrl = "http://hub.example:7147",
            HubAccessToken = "hub-secret",
            ProxyId = "PAYTON-LEGION2",
            EnrollmentToken = "test-secret",
            Sync = new FederationSyncOptions { HeartbeatSeconds = 5 },
        };

    private static FederationLocalProxyEnrollmentService CreateSut(
        FederationOptions options,
        HttpMessageHandler handler)
    {
        var monitor = Substitute.For<IOptionsMonitor<FederationOptions>>();
        monitor.CurrentValue.Returns(options);
        var registry = new FederationRegistry(Microsoft.Extensions.Options.Options.Create(options));
        var factory = Substitute.For<IHttpClientFactory>();
        factory.CreateClient(FederationProxyService.HttpClientName).Returns(new HttpClient(handler));
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Mcp:Workspaces:0:Name"] = "McpServer",
                ["Mcp:Workspaces:0:WorkspacePath"] = @"F:\GitHub\McpServer",
                ["Mcp:Workspaces:0:IsEnabled"] = "true",
                ["Mcp:Workspaces:0:IsPrimary"] = "true",
                ["Mcp:Workspaces:1:Name"] = "Disabled",
                ["Mcp:Workspaces:1:WorkspacePath"] = @"F:\GitHub\Disabled",
                ["Mcp:Workspaces:1:IsEnabled"] = "false",
            })
            .Build();

        return new FederationLocalProxyEnrollmentService(
            registry,
            factory,
            monitor,
            configuration,
            new ServerRuntimeInfo(DateTimeOffset.Parse("2026-05-22T00:00:00Z", CultureInfo.InvariantCulture), 7147),
            NullLogger<FederationLocalProxyEnrollmentService>.Instance);
    }

    private static StringContent JsonContent<T>(T value)
        => new(JsonSerializer.Serialize(value, new JsonSerializerOptions(JsonSerializerDefaults.Web)), System.Text.Encoding.UTF8, "application/json");

    private static HttpResponseMessage JsonResponse<T>(T value)
        => new(HttpStatusCode.OK) { Content = JsonContent(value) };

    private sealed class CapturingHandler : HttpMessageHandler
    {
        private readonly Func<HttpRequestMessage, HttpResponseMessage> _responseFactory;

        public CapturingHandler(Func<HttpRequestMessage, HttpResponseMessage> responseFactory)
        {
            _responseFactory = responseFactory;
        }

        public List<HttpRequestMessage> Requests { get; } = [];

        public List<string> Bodies { get; } = [];

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Requests.Add(request);
            Bodies.Add(request.Content is null ? string.Empty : await request.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false));
            return _responseFactory(request);
        }
    }
}
