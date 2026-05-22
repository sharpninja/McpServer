using System.Net;
using System.Text.Json;
using McpServer.Support.Mcp.Options;
using McpServer.Support.Mcp.Services;
using McpServer.Support.Mcp.Storage;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NSubstitute;

namespace McpServer.Support.Mcp.Tests.Services;

/// <summary>Tests for LocalProxy queued operation replay.</summary>
public sealed class FederationQueuedOperationReplayServiceTests
{
    /// <summary>Replay uses signed envelopes but keeps the local queue when the hub returns a non-terminal status.</summary>
    [Fact]
    public async Task ReplayOnceAsync_SendsSignedEnvelopeAndKeepsQueueForAcceptedHubStatus()
    {
        var topology = Substitute.For<IFederationTopologyService>();
        topology.ListPendingOperationsAsync("PAYTON-LEGION2", 25, 3, Arg.Any<CancellationToken>())
            .Returns([new FederationOperationReplayItem
            {
                OperationId = "op-1",
                ProxyId = "PAYTON-LEGION2",
                Domain = "todo",
                ResourceId = "PLAN-FEDERATION-001",
                BodyBase64 = Convert.ToBase64String("{}"u8.ToArray()),
            }]);
        var handler = new CapturingHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = JsonContent(new FederationOperationResponse
            {
                OperationId = "op-1",
                Status = "accepted",
                Created = true,
            }),
        });
        var sut = CreateSut(topology, handler);

        await sut.ReplayOnceAsync(CancellationToken.None).ConfigureAwait(true);

        Assert.Equal("/mcpserver/federation/envelopes", handler.Requests.Single().RequestUri!.AbsolutePath);
        Assert.Equal("hub-secret", handler.Requests.Single().Headers.GetValues("X-Api-Key").Single());
        Assert.Contains("\"sourceProxyId\":\"PAYTON-LEGION2\"", handler.Bodies.Single(), StringComparison.Ordinal);
        _ = topology.Received(1).MarkReplayFailureAsync(
            "op-1",
            Arg.Is<string>(message => message != null && message.Contains("non-terminal status 'accepted'", StringComparison.Ordinal)),
            3,
            Arg.Any<CancellationToken>());
        _ = topology.DidNotReceiveWithAnyArgs().AcknowledgeOperationAsync(default!, default!, default);
    }

    /// <summary>Terminal applied hub responses acknowledge the local queued operation.</summary>
    [Fact]
    public async Task ReplayOnceAsync_AppliedHubStatusAcknowledgesQueuedOperation()
    {
        var topology = Substitute.For<IFederationTopologyService>();
        topology.ListPendingOperationsAsync("PAYTON-LEGION2", 25, 3, Arg.Any<CancellationToken>())
            .Returns([new FederationOperationReplayItem
            {
                OperationId = "op-1",
                ProxyId = "PAYTON-LEGION2",
                Domain = "todo",
                ResourceId = "PLAN-FEDERATION-001",
                BodyBase64 = Convert.ToBase64String("{}"u8.ToArray()),
            }]);
        var handler = new CapturingHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = JsonContent(new FederationOperationResponse
            {
                OperationId = "op-1",
                Status = "applied",
                Created = false,
            }),
        });
        var sut = CreateSut(topology, handler);

        await sut.ReplayOnceAsync(CancellationToken.None).ConfigureAwait(true);

        _ = topology.Received(1).AcknowledgeOperationAsync(
            "op-1",
            Arg.Is<FederationOperationAckRequest>(r => r != null && r.Status == "acknowledged"),
            Arg.Any<CancellationToken>());
    }

    /// <summary>Hub conflict responses mark the queued operation conflicted.</summary>
    [Fact]
    public async Task ReplayOnceAsync_HubConflictMarksQueuedOperationConflict()
    {
        var topology = Substitute.For<IFederationTopologyService>();
        topology.ListPendingOperationsAsync("PAYTON-LEGION2", 25, 3, Arg.Any<CancellationToken>())
            .Returns([new FederationOperationReplayItem
            {
                OperationId = "op-1",
                ProxyId = "PAYTON-LEGION2",
                Domain = "todo",
            }]);
        var handler = new CapturingHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = JsonContent(new FederationOperationResponse
            {
                OperationId = "op-1",
                Status = "conflict",
            }),
        });
        var sut = CreateSut(topology, handler);

        await sut.ReplayOnceAsync(CancellationToken.None).ConfigureAwait(true);

        _ = topology.Received(1).AcknowledgeOperationAsync(
            "op-1",
            Arg.Is<FederationOperationAckRequest>(r => r != null && r.Status == "conflict"),
            Arg.Any<CancellationToken>());
    }

    /// <summary>Hub failures increment replay failure state.</summary>
    [Fact]
    public async Task ReplayOnceAsync_HubUnavailableRecordsFailure()
    {
        var topology = Substitute.For<IFederationTopologyService>();
        topology.ListPendingOperationsAsync("PAYTON-LEGION2", 25, 3, Arg.Any<CancellationToken>())
            .Returns([new FederationOperationReplayItem
            {
                OperationId = "op-1",
                ProxyId = "PAYTON-LEGION2",
                Domain = "todo",
            }]);
        var sut = CreateSut(topology, new ThrowingHandler());

        await sut.ReplayOnceAsync(CancellationToken.None).ConfigureAwait(true);

        _ = topology.Received(1).MarkReplayFailureAsync(
            "op-1",
            Arg.Any<string>(),
            3,
            Arg.Any<CancellationToken>());
    }

    /// <summary>Malformed hub JSON is persisted as a replay failure instead of escaping the replay cycle.</summary>
    [Fact]
    public async Task ReplayOnceAsync_MalformedHubJsonRecordsFailure()
    {
        var topology = Substitute.For<IFederationTopologyService>();
        topology.ListPendingOperationsAsync("PAYTON-LEGION2", 25, 3, Arg.Any<CancellationToken>())
            .Returns([new FederationOperationReplayItem
            {
                OperationId = "op-1",
                ProxyId = "PAYTON-LEGION2",
                Domain = "todo",
            }]);
        var handler = new CapturingHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("{not-json}", System.Text.Encoding.UTF8, "application/json"),
        });
        var sut = CreateSut(topology, handler);

        await sut.ReplayOnceAsync(CancellationToken.None).ConfigureAwait(true);

        _ = topology.Received(1).MarkReplayFailureAsync(
            "op-1",
            Arg.Is<string>(message => message != null && message.Contains("could not be parsed", StringComparison.OrdinalIgnoreCase)),
            3,
            Arg.Any<CancellationToken>());
    }

    /// <summary>Queued writes survive a topology service restart and replay to acknowledgement on reconnect.</summary>
    [Fact]
    public async Task ReplayOnceAsync_DurableQueuedOperationSurvivesRestartAndAcknowledges()
    {
        var databaseRoot = new InMemoryDatabaseRoot();
        var databaseName = $"fed-replay-{Guid.NewGuid():N}";
        using (var initialProvider = CreateTopologyProvider(databaseRoot, databaseName))
        {
            var topology = initialProvider.GetRequiredService<IFederationTopologyService>();
            await topology.QueueLocalOperationAsync(new FederationOperationRequest
            {
                OperationId = "op-durable-1",
                ProxyId = "PAYTON-LEGION2",
                Domain = "todo",
                ResourceId = "PLAN-FEDERATION-001",
                HttpMethod = "POST",
                Path = "/mcpserver/todo",
                BodyBase64 = Convert.ToBase64String("{}"u8.ToArray()),
            }, CancellationToken.None).ConfigureAwait(true);
        }

        using var replayProvider = CreateTopologyProvider(databaseRoot, databaseName);
        var replayTopology = replayProvider.GetRequiredService<IFederationTopologyService>();
        var handler = new CapturingHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = JsonContent(new FederationOperationResponse
            {
                OperationId = "op-durable-1",
                Status = "applied",
            }),
        });
        var sut = CreateSut(replayTopology, handler);

        await sut.ReplayOnceAsync(CancellationToken.None).ConfigureAwait(true);

        Assert.Equal("/mcpserver/federation/envelopes", handler.Requests.Single().RequestUri!.AbsolutePath);
        var pending = await replayTopology.ListPendingOperationsAsync("PAYTON-LEGION2", 10, 3, CancellationToken.None)
            .ConfigureAwait(true);
        Assert.Empty(pending);
        var status = await replayTopology.GetQueueStatusAsync("PAYTON-LEGION2", CancellationToken.None).ConfigureAwait(true);
        Assert.Equal(0, status.QueueDepth);
    }

    private static FederationQueuedOperationReplayService CreateSut(
        IFederationTopologyService topology,
        HttpMessageHandler handler)
    {
        var options = CreateOptions(new FederationOptions
        {
            Enabled = true,
            Role = FederationRole.LocalProxy,
            HubBaseUrl = "http://hub.example:7147",
            HubAccessToken = "hub-secret",
            ProxyId = "PAYTON-LEGION2",
            EnrollmentToken = "test-secret",
            Queue = new FederationQueueOptions { Enabled = true, MaxReplayAttempts = 3 },
        });
        var registry = new FederationRegistry(Microsoft.Extensions.Options.Options.Create(options.CurrentValue));
        var factory = Substitute.For<IHttpClientFactory>();
        factory.CreateClient(FederationProxyService.HttpClientName).Returns(new HttpClient(handler));
        return new FederationQueuedOperationReplayService(
            registry,
            topology,
            factory,
            options,
            new FederationEnvelopeSigner(options),
            NullLogger<FederationQueuedOperationReplayService>.Instance);
    }

    private static IOptionsMonitor<FederationOptions> CreateOptions(FederationOptions options)
    {
        var monitor = Substitute.For<IOptionsMonitor<FederationOptions>>();
        monitor.CurrentValue.Returns(options);
        return monitor;
    }

    private static ServiceProvider CreateTopologyProvider(InMemoryDatabaseRoot databaseRoot, string databaseName)
    {
        var services = new ServiceCollection();
        services.AddOptions();
        services.Configure<FederationOptions>(options => options.Sync.HeartbeatSeconds = 5);
        services.AddDbContext<McpDbContext>(options => options.UseInMemoryDatabase(databaseName, databaseRoot));
        services.AddSingleton<IFederationTopologyService, FederationTopologyService>();
        return services.BuildServiceProvider();
    }

    private static StringContent JsonContent<T>(T value)
        => new(JsonSerializer.Serialize(value, new JsonSerializerOptions(JsonSerializerDefaults.Web)), System.Text.Encoding.UTF8, "application/json");

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

    private sealed class ThrowingHandler : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            => throw new HttpRequestException("hub offline");
    }
}
