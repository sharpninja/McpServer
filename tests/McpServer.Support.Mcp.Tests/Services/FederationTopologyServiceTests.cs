using McpServer.Support.Mcp.Options;
using McpServer.Support.Mcp.Services;
using McpServer.Support.Mcp.Storage;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.DependencyInjection;

namespace McpServer.Support.Mcp.Tests.Services;

/// <summary>Unit tests for hub-and-spoke federation topology persistence.</summary>
public sealed class FederationTopologyServiceTests
{
    /// <summary>Enrollment persists the proxy and hosted workspaces for hub inventory.</summary>
    [Fact]
    public async Task EnrollAsync_PersistsProxyAndWorkspaceInventory()
    {
        using var provider = CreateProvider();
        var sut = provider.GetRequiredService<IFederationTopologyService>();

        var response = await sut.EnrollAsync(new FederationEnrollmentRequest
        {
            ProxyId = "PAYTON-LEGION2",
            DisplayName = "PAYTON-LEGION2",
            BaseUrl = "http://payton-legion2:7147/",
            Workspaces =
            [
                new FederationWorkspaceRegistrationRequest
                {
                    WorkspaceName = "McpServer",
                    WorkspacePath = @"F:\GitHub\McpServer",
                },
            ],
        }, CancellationToken.None).ConfigureAwait(true);

        Assert.True(response.Accepted);
        Assert.Equal("PAYTON-LEGION2", response.ProxyId);
        Assert.Equal(5, response.HeartbeatSeconds);

        var proxies = await sut.ListProxiesAsync(CancellationToken.None).ConfigureAwait(true);
        var proxy = Assert.Single(proxies);
        Assert.Equal("PAYTON-LEGION2", proxy.ProxyId);
        Assert.Equal(1, proxy.WorkspaceCount);

        var workspaces = await sut.ListWorkspacesAsync("PAYTON-LEGION2", CancellationToken.None).ConfigureAwait(true);
        var workspace = Assert.Single(workspaces);
        Assert.Equal(@"F:\GitHub\McpServer", workspace.WorkspacePath);
        Assert.NotEmpty(workspace.GlobalWorkspaceId);
    }

    /// <summary>Operation intake is idempotent and acknowledgement drains queue counts.</summary>
    [Fact]
    public async Task RecordOperationAsync_IsIdempotentAndAcknowledgementDrainsQueue()
    {
        using var provider = CreateProvider();
        var sut = provider.GetRequiredService<IFederationTopologyService>();

        var first = await sut.RecordOperationAsync(new FederationOperationRequest
        {
            OperationId = "op-1",
            ProxyId = "PAYTON-LEGION2",
            Domain = "todo",
            ResourceId = "PLAN-FEDERATION-001",
            HttpMethod = "POST",
            Path = "/mcpserver/todo",
        }, CancellationToken.None).ConfigureAwait(true);

        var second = await sut.RecordOperationAsync(new FederationOperationRequest
        {
            OperationId = "op-1",
            ProxyId = "PAYTON-LEGION2",
            Domain = "todo",
            ResourceId = "PLAN-FEDERATION-001",
        }, CancellationToken.None).ConfigureAwait(true);

        Assert.True(first.Created);
        Assert.False(second.Created);

        var queued = await sut.GetQueueStatusAsync("PAYTON-LEGION2", CancellationToken.None).ConfigureAwait(true);
        Assert.Equal(1, queued.QueueDepth);
        Assert.Equal(1, queued.FanoutDepth);

        var ack = await sut.AcknowledgeOperationAsync("op-1", new FederationOperationAckRequest(), CancellationToken.None).ConfigureAwait(true);
        Assert.Equal("acknowledged", ack.Status);

        var drained = await sut.GetQueueStatusAsync("PAYTON-LEGION2", CancellationToken.None).ConfigureAwait(true);
        Assert.Equal(0, drained.QueueDepth);
        Assert.Equal(0, drained.FanoutDepth);
    }

    /// <summary>Local proxy queue rows are durable replay candidates and track failed attempts.</summary>
    [Fact]
    public async Task QueueLocalOperationAsync_PersistsReplayCandidateAndFailureState()
    {
        using var provider = CreateProvider();
        var sut = provider.GetRequiredService<IFederationTopologyService>();

        var queued = await sut.QueueLocalOperationAsync(new FederationOperationRequest
        {
            OperationId = "local-op-1",
            ProxyId = "PAYTON-LEGION2",
            Domain = "todo",
            ResourceId = "PLAN-FEDERATION-001",
            HttpMethod = "POST",
            Path = "/mcpserver/todo",
            BodyBase64 = Convert.ToBase64String("{}"u8.ToArray()),
        }, CancellationToken.None).ConfigureAwait(true);

        Assert.True(queued.Created);
        Assert.Equal("queued", queued.Status);

        var pending = await sut.ListPendingOperationsAsync("PAYTON-LEGION2", 10, 3, CancellationToken.None).ConfigureAwait(true);
        var item = Assert.Single(pending);
        Assert.Equal("local-op-1", item.OperationId);
        Assert.Equal("/mcpserver/todo", item.Path);

        var failed = await sut.MarkReplayFailureAsync("local-op-1", "hub offline", 3, CancellationToken.None).ConfigureAwait(true);
        Assert.Equal("replay_failed", failed.Status);

        var afterFailure = await sut.ListPendingOperationsAsync("PAYTON-LEGION2", 10, 3, CancellationToken.None).ConfigureAwait(true);
        Assert.Single(afterFailure);
        Assert.Equal(1, afterFailure[0].AttemptCount);
    }

    /// <summary>Hub operation intake records a conflict when the proxy base version is stale.</summary>
    [Fact]
    public async Task RecordOperationAsync_StaleBaseVersionCreatesConflict()
    {
        using var provider = CreateProvider(new StaticVersionAdapter("todo", "hub-v2"));
        var sut = provider.GetRequiredService<IFederationTopologyService>();

        var response = await sut.RecordOperationAsync(new FederationOperationRequest
        {
            OperationId = "stale-op-1",
            ProxyId = "PAYTON-LEGION2",
            Domain = "todo",
            ResourceId = "PLAN-FEDERATION-001",
            BaseVersion = "proxy-v1",
        }, CancellationToken.None).ConfigureAwait(true);

        Assert.Equal("conflict", response.Status);

        var conflicts = await sut.ListConflictsAsync("PAYTON-LEGION2", openOnly: true, CancellationToken.None).ConfigureAwait(true);
        var conflict = Assert.Single(conflicts);
        Assert.Equal("stale-op-1", conflict.OperationId);
        Assert.Equal("proxy-v1", conflict.ProxyVersion);
        Assert.Equal("hub-v2", conflict.HubVersion);

        var queued = await sut.GetQueueStatusAsync("PAYTON-LEGION2", CancellationToken.None).ConfigureAwait(true);
        Assert.Equal(0, queued.FanoutDepth);
        Assert.Equal(1, queued.ConflictCount);
    }

    private static ServiceProvider CreateProvider(params IFederationStateAdapter[] adapters)
    {
        var services = new ServiceCollection();
        services.AddOptions();
        services.Configure<FederationOptions>(options => options.Sync.HeartbeatSeconds = 5);
        var databaseRoot = new InMemoryDatabaseRoot();
        var databaseName = $"fed-topology-{Guid.NewGuid():N}";
        services.AddDbContext<McpDbContext>(options => options.UseInMemoryDatabase(databaseName, databaseRoot));
        foreach (var adapter in adapters)
            services.AddSingleton(adapter);

        services.AddSingleton<IFederationTopologyService, FederationTopologyService>();
        return services.BuildServiceProvider();
    }

    private sealed class StaticVersionAdapter : IFederationStateAdapter
    {
        private readonly string _version;

        public StaticVersionAdapter(string domain, string version)
        {
            Domain = domain;
            _version = version;
        }

        public string Domain { get; }

        public bool IsLocalOnly => false;

        public ValueTask<FederationStateSnapshot> SnapshotAsync(string resourceId, CancellationToken cancellationToken)
            => new(new FederationStateSnapshot
            {
                Domain = Domain,
                ResourceId = resourceId,
                Version = _version,
            });

        public ValueTask<FederationApplyResult> ApplyAsync(FederationStateOperation operation, CancellationToken cancellationToken)
            => new(new FederationApplyResult { Applied = true, Version = _version });

        public ValueTask<string?> GetVersionAsync(string resourceId, CancellationToken cancellationToken)
            => new(_version);

        public string GetIdempotencyKey(FederationStateOperation operation)
            => operation.OperationId;

        public bool IsEcho(FederationStateOperation operation)
            => false;
    }
}
