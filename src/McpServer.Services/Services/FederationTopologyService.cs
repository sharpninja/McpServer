using McpServer.Support.Mcp.Options;
using McpServer.Support.Mcp.Storage;
using McpServer.Support.Mcp.Storage.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace McpServer.Support.Mcp.Services;

/// <summary>
/// FR-MCP-103: Default durable implementation of <see cref="IFederationTopologyService"/>.
/// The service is singleton-safe and creates scoped database contexts per operation.
/// </summary>
public sealed class FederationTopologyService : IFederationTopologyService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IOptionsMonitor<FederationOptions> _options;
    private readonly IReadOnlyDictionary<string, IFederationStateAdapter> _adapters;
    private readonly object _snapshotLock = new();
    private FederationTopologySnapshot _snapshot = new();

    /// <summary>Initializes a new instance of the <see cref="FederationTopologyService"/> class.</summary>
    /// <param name="scopeFactory">Scope factory used to resolve <see cref="McpDbContext"/>.</param>
    /// <param name="options">Federation options monitor.</param>
    /// <param name="adapters">Mutable state adapters used for optimistic conflict detection.</param>
    public FederationTopologyService(
        IServiceScopeFactory scopeFactory,
        IOptionsMonitor<FederationOptions> options,
        IEnumerable<IFederationStateAdapter> adapters)
    {
        _scopeFactory = scopeFactory;
        _options = options;
        _adapters = adapters.ToDictionary(a => a.Domain, StringComparer.OrdinalIgnoreCase);
    }

    /// <inheritdoc />
    public FederationTopologySnapshot GetSnapshot()
    {
        lock (_snapshotLock)
        {
            return new FederationTopologySnapshot
            {
                ProxyCount = _snapshot.ProxyCount,
                WorkspaceCount = _snapshot.WorkspaceCount,
                QueueDepth = _snapshot.QueueDepth,
                ConflictCount = _snapshot.ConflictCount,
            };
        }
    }

    /// <inheritdoc />
    public async Task<FederationEnrollmentResponse> EnrollAsync(FederationEnrollmentRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var now = DateTimeOffset.UtcNow;
        var proxyId = NormalizeProxyId(request.ProxyId, request.DisplayName);

        await using var scope = _scopeFactory.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<McpDbContext>();
        var proxy = await db.FederationProxies.FindAsync([proxyId], cancellationToken).ConfigureAwait(false);

        if (proxy is null)
        {
            proxy = new FederationProxyEntity
            {
                ProxyId = proxyId,
                CreatedAtUtc = now,
            };
            db.FederationProxies.Add(proxy);
        }

        proxy.DisplayName = request.DisplayName;
        proxy.BaseUrl = TrimTrailingSlash(request.BaseUrl);
        proxy.MetadataJson = request.MetadataJson;
        proxy.Role = FederationRole.LocalProxy.ToString();
        proxy.Status = "enrolled";
        proxy.UpdatedAtUtc = now;

        foreach (var workspace in request.Workspaces)
            UpsertWorkspace(db, proxyId, workspace, now);

        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        await RefreshSnapshotAsync(db, cancellationToken).ConfigureAwait(false);

        return new FederationEnrollmentResponse
        {
            ProxyId = proxyId,
            Accepted = true,
            ServerTimeUtc = now,
            HeartbeatSeconds = _options.CurrentValue.Sync.HeartbeatSeconds,
        };
    }

    /// <inheritdoc />
    public async Task<FederationHeartbeatResponse> HeartbeatAsync(string proxyId, FederationHeartbeatRequest request, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(proxyId);
        ArgumentNullException.ThrowIfNull(request);
        var now = DateTimeOffset.UtcNow;

        await using var scope = _scopeFactory.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<McpDbContext>();
        var proxy = await db.FederationProxies.FindAsync([proxyId], cancellationToken).ConfigureAwait(false);

        if (proxy is null)
        {
            proxy = new FederationProxyEntity
            {
                ProxyId = proxyId,
                DisplayName = proxyId,
                Role = FederationRole.LocalProxy.ToString(),
                CreatedAtUtc = now,
            };
            db.FederationProxies.Add(proxy);
        }

        proxy.Status = string.IsNullOrWhiteSpace(request.Status) ? "online" : request.Status.Trim();
        proxy.LastHeartbeatUtc = now;
        proxy.UpdatedAtUtc = now;
        proxy.MetadataJson = request.MetadataJson ?? proxy.MetadataJson;

        foreach (var workspace in request.Workspaces)
            UpsertWorkspace(db, proxyId, workspace, now);

        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        await RefreshSnapshotAsync(db, cancellationToken).ConfigureAwait(false);

        var queueStatus = await GetQueueStatusAsync(proxyId, cancellationToken).ConfigureAwait(false);
        return new FederationHeartbeatResponse
        {
            ProxyId = proxyId,
            RecordedAtUtc = now,
            QueueDepth = queueStatus.QueueDepth,
            ConflictCount = queueStatus.ConflictCount,
        };
    }

    /// <inheritdoc />
    public async Task<FederationWorkspaceInfo> RegisterWorkspaceAsync(string proxyId, FederationWorkspaceRegistrationRequest request, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(proxyId);
        ArgumentNullException.ThrowIfNull(request);
        var now = DateTimeOffset.UtcNow;

        await using var scope = _scopeFactory.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<McpDbContext>();
        var entity = UpsertWorkspace(db, proxyId, request, now);
        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        await RefreshSnapshotAsync(db, cancellationToken).ConfigureAwait(false);

        return ToWorkspaceInfo(entity);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<FederationProxyInfo>> ListProxiesAsync(CancellationToken cancellationToken)
    {
        await using var scope = _scopeFactory.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<McpDbContext>();
        var workspaceCounts = await db.FederationWorkspaces
            .GroupBy(w => w.ProxyId)
            .Select(g => new { ProxyId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.ProxyId, x => x.Count, StringComparer.OrdinalIgnoreCase, cancellationToken)
            .ConfigureAwait(false);

        return await db.FederationProxies
            .OrderBy(p => p.ProxyId)
            .Select(p => new FederationProxyInfo
            {
                ProxyId = p.ProxyId,
                DisplayName = p.DisplayName,
                Role = p.Role,
                BaseUrl = p.BaseUrl,
                Status = p.Status,
                LastHeartbeatUtc = p.LastHeartbeatUtc,
                WorkspaceCount = workspaceCounts.GetValueOrDefault(p.ProxyId),
            })
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<FederationWorkspaceInfo>> ListWorkspacesAsync(string? proxyId, CancellationToken cancellationToken)
    {
        await using var scope = _scopeFactory.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<McpDbContext>();
        var query = db.FederationWorkspaces.AsQueryable();
        if (!string.IsNullOrWhiteSpace(proxyId))
            query = query.Where(w => w.ProxyId == proxyId);

        return await query
            .OrderBy(w => w.ProxyId)
            .ThenBy(w => w.WorkspacePath)
            .Select(w => new FederationWorkspaceInfo
            {
                GlobalWorkspaceId = w.GlobalWorkspaceId,
                ProxyId = w.ProxyId,
                WorkspaceName = w.WorkspaceName,
                WorkspacePath = w.WorkspacePath,
                IsEnabled = w.IsEnabled,
                Version = w.Version,
                LastSeenUtc = w.LastSeenUtc,
            })
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<FederationOperationResponse> RecordOperationAsync(FederationOperationRequest request, CancellationToken cancellationToken)
        => await UpsertOperationAsync(request, "accepted", createOutbox: true, detectConflict: true, cancellationToken)
            .ConfigureAwait(false);

    /// <inheritdoc />
    public async Task<FederationOperationResponse> QueueLocalOperationAsync(FederationOperationRequest request, CancellationToken cancellationToken)
        => await UpsertOperationAsync(request, "queued", createOutbox: false, detectConflict: false, cancellationToken)
            .ConfigureAwait(false);

    /// <inheritdoc />
    public async Task<IReadOnlyList<FederationOperationReplayItem>> ListPendingOperationsAsync(
        string proxyId,
        int limit,
        int maxAttempts,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(proxyId);
        limit = limit <= 0 ? 25 : limit;
        maxAttempts = maxAttempts <= 0 ? int.MaxValue : maxAttempts;

        await using var scope = _scopeFactory.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<McpDbContext>();
        return await db.FederationOperations
            .Where(o =>
                o.ProxyId == proxyId &&
                (o.Status == "queued" || o.Status == "replay_failed") &&
                o.AttemptCount < maxAttempts)
            .OrderBy(o => o.CreatedAtUtc)
            .Take(limit)
            .Select(o => new FederationOperationReplayItem
            {
                OperationId = o.OperationId,
                ProxyId = o.ProxyId,
                SourceOperationId = o.SourceOperationId,
                GlobalWorkspaceId = o.GlobalWorkspaceId,
                Domain = o.Domain,
                ResourceId = o.ResourceId,
                HttpMethod = o.HttpMethod,
                Path = o.Path,
                Method = o.Method,
                HeadersJson = o.HeadersJson,
                BodyBase64 = o.BodyBase64,
                BaseVersion = o.BaseVersion,
                Status = o.Status,
                AttemptCount = o.AttemptCount,
            })
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<FederationOperationResponse> MarkReplayFailureAsync(
        string operationId,
        string error,
        int maxAttempts,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(operationId);
        var now = DateTimeOffset.UtcNow;

        await using var scope = _scopeFactory.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<McpDbContext>();
        var entity = await db.FederationOperations.FindAsync([operationId], cancellationToken).ConfigureAwait(false);
        if (entity is null)
        {
            return new FederationOperationResponse
            {
                OperationId = operationId,
                Status = "not_found",
                Created = false,
            };
        }

        entity.AttemptCount++;
        entity.LastError = error;
        entity.UpdatedAtUtc = now;
        entity.Status = entity.AttemptCount >= maxAttempts ? "replay_blocked" : "replay_failed";
        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        await RefreshSnapshotAsync(db, cancellationToken).ConfigureAwait(false);

        return new FederationOperationResponse
        {
            OperationId = operationId,
            Status = entity.Status,
            Created = false,
        };
    }

    private async Task<FederationOperationResponse> UpsertOperationAsync(
        FederationOperationRequest request,
        string createdStatus,
        bool createOutbox,
        bool detectConflict,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.ProxyId);
        var now = DateTimeOffset.UtcNow;
        var operationId = string.IsNullOrWhiteSpace(request.OperationId)
            ? $"fedop-{Guid.NewGuid():N}"
            : request.OperationId.Trim();

        await using var scope = _scopeFactory.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<McpDbContext>();
        await EnsureProxyAsync(db, request.ProxyId, now, cancellationToken).ConfigureAwait(false);

        var entity = await db.FederationOperations.FindAsync([operationId], cancellationToken).ConfigureAwait(false);
        var created = entity is null;
        if (entity is null)
        {
            entity = new FederationOperationEntity
            {
                OperationId = operationId,
                ProxyId = request.ProxyId,
                CreatedAtUtc = now,
            };
            db.FederationOperations.Add(entity);
        }

        entity.SourceOperationId = request.SourceOperationId;
        entity.GlobalWorkspaceId = request.GlobalWorkspaceId;
        entity.Domain = string.IsNullOrWhiteSpace(request.Domain) ? "unknown" : request.Domain.Trim();
        entity.ResourceId = request.ResourceId;
        entity.HttpMethod = request.HttpMethod;
        entity.Path = request.Path;
        entity.Method = request.Method;
        entity.HeadersJson = request.HeadersJson;
        entity.BodyBase64 = request.BodyBase64;
        entity.BaseVersion = request.BaseVersion;
        var hubVersion = detectConflict
            ? await ResolveHubVersionAsync(request.Domain, request.ResourceId, cancellationToken).ConfigureAwait(false)
            : null;
        entity.HubVersion = hubVersion ?? entity.HubVersion;
        var conflict = created && IsStaleVersion(request.BaseVersion, hubVersion);
        entity.Status = created ? conflict ? "conflict" : createdStatus : entity.Status;
        entity.UpdatedAtUtc = now;

        if (created && conflict)
        {
            db.FederationConflicts.Add(new FederationConflictEntity
            {
                ConflictId = $"fedconf-{Guid.NewGuid():N}",
                OperationId = operationId,
                ProxyId = request.ProxyId,
                Domain = entity.Domain,
                ResourceId = entity.ResourceId,
                ProxyVersion = request.BaseVersion,
                HubVersion = hubVersion,
                ResolutionStatus = "open",
                DetailsJson = "{\"resolution\":\"hub_wins_by_default\"}",
                CreatedAtUtc = now,
            });
        }

        if (created && createOutbox && !conflict)
        {
            db.FederationOutbox.Add(new FederationOutboxEntity
            {
                ProxyId = request.ProxyId,
                OperationId = operationId,
                CreatedAtUtc = now,
            });
        }

        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        await RefreshSnapshotAsync(db, cancellationToken).ConfigureAwait(false);

        return new FederationOperationResponse
        {
            OperationId = operationId,
            Status = entity.Status,
            Created = created,
        };
    }

    /// <inheritdoc />
    public async Task<FederationOperationResponse> AcknowledgeOperationAsync(string operationId, FederationOperationAckRequest request, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(operationId);
        ArgumentNullException.ThrowIfNull(request);
        var now = DateTimeOffset.UtcNow;

        await using var scope = _scopeFactory.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<McpDbContext>();
        var entity = await db.FederationOperations.FindAsync([operationId], cancellationToken).ConfigureAwait(false);
        if (entity is null)
        {
            return new FederationOperationResponse
            {
                OperationId = operationId,
                Status = "not_found",
                Created = false,
            };
        }

        entity.Status = string.IsNullOrWhiteSpace(request.Status) ? "acknowledged" : request.Status.Trim();
        entity.HubVersion = request.HubVersion ?? entity.HubVersion;
        entity.LastError = request.Error;
        entity.AcknowledgedAtUtc = now;
        entity.UpdatedAtUtc = now;

        var outboxRows = await db.FederationOutbox
            .Where(o => o.OperationId == operationId && o.AcknowledgedAtUtc == null)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
        foreach (var row in outboxRows)
            row.AcknowledgedAtUtc = now;

        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        await RefreshSnapshotAsync(db, cancellationToken).ConfigureAwait(false);

        return new FederationOperationResponse
        {
            OperationId = operationId,
            Status = entity.Status,
            Created = false,
        };
    }

    /// <inheritdoc />
    public async Task<FederationQueueStatusResponse> GetQueueStatusAsync(string? proxyId, CancellationToken cancellationToken)
    {
        await using var scope = _scopeFactory.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<McpDbContext>();
        var operations = db.FederationOperations.AsQueryable();
        var conflicts = db.FederationConflicts.AsQueryable();
        var outbox = db.FederationOutbox.AsQueryable();

        if (!string.IsNullOrWhiteSpace(proxyId))
        {
            operations = operations.Where(o => o.ProxyId == proxyId);
            conflicts = conflicts.Where(c => c.ProxyId == proxyId);
            outbox = outbox.Where(o => o.ProxyId == proxyId);
        }

        return new FederationQueueStatusResponse
        {
            ProxyId = proxyId,
            QueueDepth = await operations
                .CountAsync(o => o.Status == "queued" || o.Status == "accepted" || o.Status == "replay_failed", cancellationToken)
                .ConfigureAwait(false),
            ConflictCount = await conflicts.CountAsync(c => c.ResolutionStatus == "open", cancellationToken).ConfigureAwait(false),
            FanoutDepth = await outbox.CountAsync(o => o.AcknowledgedAtUtc == null, cancellationToken).ConfigureAwait(false),
        };
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<FederationConflictInfo>> ListConflictsAsync(string? proxyId, bool openOnly, CancellationToken cancellationToken)
    {
        await using var scope = _scopeFactory.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<McpDbContext>();
        var query = db.FederationConflicts.AsQueryable();
        if (!string.IsNullOrWhiteSpace(proxyId))
            query = query.Where(c => c.ProxyId == proxyId);
        if (openOnly)
            query = query.Where(c => c.ResolutionStatus == "open");

        return await query
            .OrderByDescending(c => c.CreatedAtUtc)
            .Select(c => new FederationConflictInfo
            {
                ConflictId = c.ConflictId,
                OperationId = c.OperationId,
                ProxyId = c.ProxyId,
                Domain = c.Domain,
                ResourceId = c.ResourceId,
                ProxyVersion = c.ProxyVersion,
                HubVersion = c.HubVersion,
                ResolutionStatus = c.ResolutionStatus,
                CreatedAtUtc = c.CreatedAtUtc,
            })
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<FederationConflictInfo?> ResolveConflictAsync(string conflictId, FederationConflictResolutionRequest request, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(conflictId);
        ArgumentNullException.ThrowIfNull(request);

        await using var scope = _scopeFactory.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<McpDbContext>();
        var entity = await db.FederationConflicts.FindAsync([conflictId], cancellationToken).ConfigureAwait(false);
        if (entity is null)
            return null;

        entity.ResolutionStatus = string.IsNullOrWhiteSpace(request.ResolutionStatus) ? "hub_wins" : request.ResolutionStatus.Trim();
        entity.ResolvedAtUtc = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        await RefreshSnapshotAsync(db, cancellationToken).ConfigureAwait(false);

        return new FederationConflictInfo
        {
            ConflictId = entity.ConflictId,
            OperationId = entity.OperationId,
            ProxyId = entity.ProxyId,
            Domain = entity.Domain,
            ResourceId = entity.ResourceId,
            ProxyVersion = entity.ProxyVersion,
            HubVersion = entity.HubVersion,
            ResolutionStatus = entity.ResolutionStatus,
            CreatedAtUtc = entity.CreatedAtUtc,
        };
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<FederationSyncItem>> GetSyncItemsAsync(string proxyId, long afterSequence, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(proxyId);

        await using var scope = _scopeFactory.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<McpDbContext>();
        return await db.FederationOutbox
            .Where(o => o.ProxyId == proxyId && o.Sequence > afterSequence && o.AcknowledgedAtUtc == null)
            .OrderBy(o => o.Sequence)
            .Join(
                db.FederationOperations,
                outbox => outbox.OperationId,
                operation => operation.OperationId,
                (outbox, operation) => new FederationSyncItem
                {
                    Sequence = outbox.Sequence,
                    OperationId = operation.OperationId,
                    Domain = operation.Domain,
                    ResourceId = operation.ResourceId,
                    HubVersion = operation.HubVersion,
                })
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    private static FederationWorkspaceEntity UpsertWorkspace(
        McpDbContext db,
        string proxyId,
        FederationWorkspaceRegistrationRequest request,
        DateTimeOffset now)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(request.WorkspacePath);
        var globalWorkspaceId = string.IsNullOrWhiteSpace(request.GlobalWorkspaceId)
            ? CreateGlobalWorkspaceId(proxyId, request.WorkspacePath)
            : request.GlobalWorkspaceId.Trim();

        var entity = db.FederationWorkspaces.Local.FirstOrDefault(w =>
                string.Equals(w.ProxyId, proxyId, StringComparison.OrdinalIgnoreCase) &&
                string.Equals(w.WorkspacePath, request.WorkspacePath, StringComparison.OrdinalIgnoreCase))
            ?? db.FederationWorkspaces.FirstOrDefault(w => w.ProxyId == proxyId && w.WorkspacePath == request.WorkspacePath);

        if (entity is null)
        {
            entity = new FederationWorkspaceEntity
            {
                ProxyId = proxyId,
                WorkspacePath = request.WorkspacePath,
                GlobalWorkspaceId = globalWorkspaceId,
                CreatedAtUtc = now,
            };
            db.FederationWorkspaces.Add(entity);
        }

        entity.GlobalWorkspaceId = globalWorkspaceId;
        entity.WorkspaceName = request.WorkspaceName;
        entity.IsEnabled = request.IsEnabled;
        entity.Version = request.Version;
        entity.MetadataJson = request.MetadataJson;
        entity.LastSeenUtc = now;
        return entity;
    }

    private async Task EnsureProxyAsync(McpDbContext db, string proxyId, DateTimeOffset now, CancellationToken cancellationToken)
    {
        var proxy = await db.FederationProxies.FindAsync([proxyId], cancellationToken).ConfigureAwait(false);
        if (proxy is not null)
            return;

        db.FederationProxies.Add(new FederationProxyEntity
        {
            ProxyId = proxyId,
            DisplayName = proxyId,
            Role = FederationRole.LocalProxy.ToString(),
            Status = "enrolled",
            CreatedAtUtc = now,
            UpdatedAtUtc = now,
        });
    }

    private async Task RefreshSnapshotAsync(McpDbContext db, CancellationToken cancellationToken)
    {
        var snapshot = new FederationTopologySnapshot
        {
            ProxyCount = await db.FederationProxies.CountAsync(cancellationToken).ConfigureAwait(false),
            WorkspaceCount = await db.FederationWorkspaces.CountAsync(cancellationToken).ConfigureAwait(false),
            QueueDepth = await db.FederationOperations
                .CountAsync(o => o.Status == "queued" || o.Status == "accepted" || o.Status == "replay_failed", cancellationToken)
                .ConfigureAwait(false),
            ConflictCount = await db.FederationConflicts.CountAsync(c => c.ResolutionStatus == "open", cancellationToken).ConfigureAwait(false),
        };

        lock (_snapshotLock)
            _snapshot = snapshot;
    }

    private static FederationWorkspaceInfo ToWorkspaceInfo(FederationWorkspaceEntity entity)
        => new()
        {
            GlobalWorkspaceId = entity.GlobalWorkspaceId,
            ProxyId = entity.ProxyId,
            WorkspaceName = entity.WorkspaceName,
            WorkspacePath = entity.WorkspacePath,
            IsEnabled = entity.IsEnabled,
            Version = entity.Version,
            LastSeenUtc = entity.LastSeenUtc,
        };

    private static string NormalizeProxyId(string? proxyId, string? displayName)
    {
        var value = !string.IsNullOrWhiteSpace(proxyId)
            ? proxyId
            : !string.IsNullOrWhiteSpace(displayName)
                ? displayName
                : Environment.MachineName;

        return value.Trim();
    }

    private static string? TrimTrailingSlash(string? value)
        => string.IsNullOrWhiteSpace(value) ? null : value.Trim().TrimEnd('/');

    private async Task<string?> ResolveHubVersionAsync(string? domain, string? resourceId, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(domain) || string.IsNullOrWhiteSpace(resourceId))
            return null;

        if (!_adapters.TryGetValue(domain, out var adapter) || adapter.IsLocalOnly)
            return null;

        return await adapter.GetVersionAsync(resourceId, cancellationToken).ConfigureAwait(false);
    }

    private static bool IsStaleVersion(string? proxyVersion, string? hubVersion)
        => !string.IsNullOrWhiteSpace(proxyVersion) &&
           hubVersion is not null &&
           !string.Equals(proxyVersion, hubVersion, StringComparison.Ordinal);

    private static string CreateGlobalWorkspaceId(string proxyId, string workspacePath)
    {
        var raw = $"{proxyId}:{workspacePath}".ToLowerInvariant();
        var bytes = System.Text.Encoding.UTF8.GetBytes(raw);
        return Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(bytes));
    }
}
