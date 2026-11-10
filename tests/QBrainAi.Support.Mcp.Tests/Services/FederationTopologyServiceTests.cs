using QBrainAi.Support.Mcp.Options;
using QBrainAi.Support.Mcp.Services;
using QBrainAi.Support.Mcp.Storage;
using QBrainAi.Support.Mcp.Storage.Entities;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.DependencyInjection;

namespace QBrainAi.Support.Mcp.Tests.Services;

/// <summary>Unit tests for hub-and-spoke federation topology persistence.</summary>
/// <remarks>2026-11-07, TR-MCP-USECASE-018 AC-006; FR-MCP-BUILD-001; TR-MCP-NUKE-019 AC-7: the unit half of this class: its unit cases and class attributes are unchanged; the helper AssertEnrollmentPersistsInventoryAsync, which only the Windows and Linux platform classes of this file call, moved with them to the class of the same name in the integration-owned QBrainAi.Support.Mcp.EnvironmentIntegrationTests (slice S7, hostile validation finding R14-F4); the state and helpers both halves need (_workspacePath, Dispose and CreateProvider) moved once into the abstract base class FederationTopologyServiceTestsBase in Services/FederationTopologyServiceTestsBase.cs beside this file, which both halves derive from and the integration project links (DRY).</remarks>
public sealed class FederationTopologyServiceTests : FederationTopologyServiceTestsBase, IDisposable
{
    /// <summary>Creates an isolated workspace for identity resolution.</summary>
    public FederationTopologyServiceTests()
    {
        Directory.CreateDirectory(_workspacePath);
    }

    /// <summary>Cached topology status is refreshed from durable storage when status is requested.</summary>
    [Fact]
    public async Task GetSnapshot_RefreshesFromDurableStorage()
    {
        using var provider = CreateProvider();
        var sut = provider.GetRequiredService<IFederationTopologyService>();
        var now = DateTimeOffset.UtcNow;

        await using (var scope = provider.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<McpDbContext>();
            db.FederationProxies.Add(new FederationProxyEntity
            {
                ProxyId = "TEST-NODE-A",
                DisplayName = "TEST-NODE-A",
                Role = "LocalProxy",
                Status = "online",
                CreatedAtUtc = now,
                UpdatedAtUtc = now,
            });
            db.FederationOperations.Add(new FederationOperationEntity
            {
                OperationId = "op-stale-status-1",
                ProxyId = "TEST-NODE-A",
                Domain = "todo",
                ResourceId = "PLAN-FEDERATION-001",
                Status = "queued",
                CreatedAtUtc = now,
                UpdatedAtUtc = now,
            });
            await db.SaveChangesAsync(CancellationToken.None).ConfigureAwait(true);
        }

        var snapshot = sut.GetSnapshot();

        Assert.Equal(1, snapshot.ProxyCount);
        Assert.Equal(1, snapshot.QueueDepth);
    }

    /// <summary>Operation intake is idempotent and only fans out to other proxies.</summary>
    [Fact]
    public async Task RecordOperationAsync_IsIdempotentAndFansOutToOtherProxies()
    {
        using var provider = CreateProvider();
        var sut = provider.GetRequiredService<IFederationTopologyService>();
        await sut.EnrollAsync(new FederationEnrollmentRequest { ProxyId = "TEST-NODE-A" }, CancellationToken.None)
            .ConfigureAwait(true);
        await sut.EnrollAsync(new FederationEnrollmentRequest { ProxyId = "TEST-NODE-B" }, CancellationToken.None)
            .ConfigureAwait(true);

        var first = await sut.RecordOperationAsync(new FederationOperationRequest
        {
            OperationId = "op-1",
            ProxyId = "TEST-NODE-A",
            Domain = "todo",
            ResourceId = "PLAN-FEDERATION-001",
            HttpMethod = "POST",
            Path = "/qbrainai/todo",
        }, CancellationToken.None).ConfigureAwait(true);

        var second = await sut.RecordOperationAsync(new FederationOperationRequest
        {
            OperationId = "op-1",
            ProxyId = "TEST-NODE-A",
            Domain = "todo",
            ResourceId = "PLAN-FEDERATION-001",
        }, CancellationToken.None).ConfigureAwait(true);

        Assert.True(first.Created);
        Assert.False(second.Created);

        var queued = await sut.GetQueueStatusAsync("TEST-NODE-A", CancellationToken.None).ConfigureAwait(true);
        Assert.Equal(1, queued.QueueDepth);
        Assert.Equal(0, queued.FanoutDepth);

        var syncItems = await sut.GetSyncItemsAsync("TEST-NODE-B", 0, CancellationToken.None).ConfigureAwait(true);
        var syncItem = Assert.Single(syncItems);
        Assert.Equal("op-1", syncItem.OperationId);
        Assert.Equal("TEST-NODE-A", syncItem.ProxyId);
        Assert.Equal("POST", syncItem.HttpMethod);
        Assert.Equal("/qbrainai/todo", syncItem.Path);

        var ack = await sut.AcknowledgeSyncItemAsync("TEST-NODE-B", syncItem.Sequence, new FederationSyncAckRequest(), CancellationToken.None)
            .ConfigureAwait(true);
        Assert.Equal("acknowledged", ack.Status);

        var drained = await sut.GetQueueStatusAsync("TEST-NODE-B", CancellationToken.None).ConfigureAwait(true);
        Assert.Equal(0, drained.FanoutDepth);
    }

    /// <summary>Operation-level acknowledgements do not clear recipient-specific fanout rows.</summary>
    [Fact]
    public async Task AcknowledgeOperationAsync_DoesNotDrainRecipientFanoutRows()
    {
        using var provider = CreateProvider();
        var sut = provider.GetRequiredService<IFederationTopologyService>();
        await sut.EnrollAsync(new FederationEnrollmentRequest { ProxyId = "TEST-NODE-A" }, CancellationToken.None)
            .ConfigureAwait(true);
        await sut.EnrollAsync(new FederationEnrollmentRequest { ProxyId = "TEST-NODE-B" }, CancellationToken.None)
            .ConfigureAwait(true);
        await sut.EnrollAsync(new FederationEnrollmentRequest { ProxyId = "TEST-NODE-C" }, CancellationToken.None)
            .ConfigureAwait(true);

        await sut.RecordOperationAsync(new FederationOperationRequest
        {
            OperationId = "op-fanout-ack-1",
            ProxyId = "TEST-NODE-A",
            Domain = "todo",
            ResourceId = "PLAN-FEDERATION-001",
            HttpMethod = "POST",
            Path = "/qbrainai/todo",
        }, CancellationToken.None).ConfigureAwait(true);

        var operationAck = await sut.AcknowledgeOperationAsync(
                "op-fanout-ack-1",
                new FederationOperationAckRequest { Status = "applied" },
                CancellationToken.None)
            .ConfigureAwait(true);

        Assert.Equal("applied", operationAck.Status);
        Assert.Single(await sut.GetSyncItemsAsync("TEST-NODE-B", 0, CancellationToken.None).ConfigureAwait(true));
        Assert.Single(await sut.GetSyncItemsAsync("TEST-NODE-C", 0, CancellationToken.None).ConfigureAwait(true));
    }

    /// <summary>Terminal failed apply acknowledgements clear undeliverable fanout rows for the operation.</summary>
    [Fact]
    public async Task AcknowledgeOperationAsync_ConflictDrainsFanoutRows()
    {
        using var provider = CreateProvider();
        var sut = provider.GetRequiredService<IFederationTopologyService>();
        await sut.EnrollAsync(new FederationEnrollmentRequest { ProxyId = "TEST-NODE-A" }, CancellationToken.None)
            .ConfigureAwait(true);
        await sut.EnrollAsync(new FederationEnrollmentRequest { ProxyId = "TEST-NODE-B" }, CancellationToken.None)
            .ConfigureAwait(true);

        await sut.RecordOperationAsync(new FederationOperationRequest
        {
            OperationId = "op-fanout-conflict-1",
            ProxyId = "TEST-NODE-A",
            Domain = "todo",
            ResourceId = "PLAN-FEDERATION-001",
        }, CancellationToken.None).ConfigureAwait(true);

        var operationAck = await sut.AcknowledgeOperationAsync(
                "op-fanout-conflict-1",
                new FederationOperationAckRequest { Status = "conflict", Error = "apply failed" },
                CancellationToken.None)
            .ConfigureAwait(true);

        Assert.Equal("conflict", operationAck.Status);
        Assert.Empty(await sut.GetSyncItemsAsync("TEST-NODE-B", 0, CancellationToken.None).ConfigureAwait(true));
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
            ProxyId = "TEST-NODE-A",
            Domain = "todo",
            ResourceId = "PLAN-FEDERATION-001",
            HttpMethod = "POST",
            Path = "/qbrainai/todo",
            BodyBase64 = Convert.ToBase64String("{}"u8.ToArray()),
        }, CancellationToken.None).ConfigureAwait(true);

        Assert.True(queued.Created);
        Assert.Equal("queued", queued.Status);

        var pending = await sut.ListPendingOperationsAsync("TEST-NODE-A", 10, 3, CancellationToken.None).ConfigureAwait(true);
        var item = Assert.Single(pending);
        Assert.Equal("local-op-1", item.OperationId);
        Assert.Equal("/qbrainai/todo", item.Path);

        var failed = await sut.MarkReplayFailureAsync("local-op-1", "hub offline", 3, CancellationToken.None).ConfigureAwait(true);
        Assert.Equal("replay_failed", failed.Status);

        var afterFailure = await sut.ListPendingOperationsAsync("TEST-NODE-A", 10, 3, CancellationToken.None).ConfigureAwait(true);
        Assert.Single(afterFailure);
        Assert.Equal(1, afterFailure[0].AttemptCount);
    }

    /// <summary>SQLite-backed pending operation listing avoids provider-side DateTimeOffset ordering translation.</summary>
    [Fact]
    public async Task ListPendingOperationsAsync_WithSqliteProvider_ReturnsQueuedRows()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync(cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);
        using var provider = CreateSqliteProvider(connection);
        await using (var scope = provider.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<McpDbContext>();
            await db.Database.EnsureCreatedAsync(CancellationToken.None).ConfigureAwait(true);
        }

        var sut = provider.GetRequiredService<IFederationTopologyService>();
        await sut.QueueLocalOperationAsync(new FederationOperationRequest
        {
            OperationId = "sqlite-op-1",
            ProxyId = "TEST-NODE-A",
            Domain = "todo",
            ResourceId = "PLAN-FEDERATION-001",
            HttpMethod = "POST",
            Path = "/qbrainai/todo",
            BodyBase64 = Convert.ToBase64String("{}"u8.ToArray()),
        }, CancellationToken.None).ConfigureAwait(true);

        var pending = await sut.ListPendingOperationsAsync("TEST-NODE-A", 10, 3, CancellationToken.None)
            .ConfigureAwait(true);

        var item = Assert.Single(pending);
        Assert.Equal("sqlite-op-1", item.OperationId);
        Assert.Equal("/qbrainai/todo", item.Path);
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
            ProxyId = "TEST-NODE-A",
            Domain = "todo",
            ResourceId = "PLAN-FEDERATION-001",
            BaseVersion = "proxy-v1",
        }, CancellationToken.None).ConfigureAwait(true);

        Assert.Equal("conflict", response.Status);

        var conflicts = await sut.ListConflictsAsync("TEST-NODE-A", openOnly: true, CancellationToken.None).ConfigureAwait(true);
        var conflict = Assert.Single(conflicts);
        Assert.Equal("stale-op-1", conflict.OperationId);
        Assert.Equal("proxy-v1", conflict.ProxyVersion);
        Assert.Equal("hub-v2", conflict.HubVersion);

        var queued = await sut.GetQueueStatusAsync("TEST-NODE-A", CancellationToken.None).ConfigureAwait(true);
        Assert.Equal(0, queued.FanoutDepth);
        Assert.Equal(1, queued.ConflictCount);
    }

    private static ServiceProvider CreateSqliteProvider(SqliteConnection connection, params IFederationStateAdapter[] adapters)
    {
        var services = new ServiceCollection();
        services.AddOptions();
        services.Configure<FederationOptions>(options => options.Sync.HeartbeatSeconds = 5);
        services.AddDbContext<McpDbContext>(options => options.UseSqlite(connection));
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
