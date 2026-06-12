using McpServer.Support.Mcp.Models;
using Microsoft.Extensions.Logging;

namespace McpServer.Support.Mcp.Services;

/// <summary>
/// FR-MCP-083: Decorator that wraps an <see cref="ISessionLogService"/> to merge local
/// and remote session log data when federation is enabled. Read operations query both
/// local and remote in parallel and merge results (local wins on SourceType+SessionId
/// collision). Write operations delegate exclusively to the inner (local) service.
/// When federation is disabled or no target resolves, all calls pass through with zero overhead.
/// </summary>
public sealed class FederatedSessionLogService : ISessionLogService
{
    private readonly ISessionLogService _inner;
    private readonly FederationRegistry _registry;
    private readonly IFederationDataClient _client;
    private readonly ILogger<FederatedSessionLogService> _logger;

    /// <summary>Initializes a new instance of the <see cref="FederatedSessionLogService"/> class.</summary>
    /// <param name="inner">The local session log service to delegate to.</param>
    /// <param name="registry">Federation registry for target resolution.</param>
    /// <param name="client">Federation data client for remote queries.</param>
    /// <param name="logger">Logger for diagnostic output.</param>
    public FederatedSessionLogService(
        ISessionLogService inner,
        FederationRegistry registry,
        IFederationDataClient client,
        ILogger<FederatedSessionLogService> logger)
    {
        _inner = inner;
        _registry = registry;
        _client = client;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<SessionLogQueryResult> QueryAsync(SessionLogQueryRequest request, CancellationToken cancellationToken = default)
    {
        var target = _registry.ResolveTarget(null);
        if (target is null)
            return await _inner.QueryAsync(request, cancellationToken).ConfigureAwait(false);

        var localTask = _inner.QueryAsync(request, cancellationToken);
        SessionLogQueryResult? remote = null;

        try
        {
            remote = await _client.QuerySessionLogsAsync(target, request, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogWarning(ex, "Federation session log query to {Target} failed, using local-only results", target.Name);
        }

        var local = await localTask.ConfigureAwait(false);

        if (remote is null || remote.Items.Count == 0)
            return local;

        return MergeResults(local, remote);
    }

    /// <inheritdoc />
    public Task<long> SubmitAsync(UnifiedSessionLogDto dto, string? sourceFilePath = null, string? contentHash = null, CancellationToken cancellationToken = default)
        => _inner.SubmitAsync(dto, sourceFilePath, contentHash, cancellationToken);

    /// <inheritdoc />
    public Task<bool> IsUnchangedAsync(string sourceType, string sessionId, string contentHash, CancellationToken cancellationToken = default)
        => _inner.IsUnchangedAsync(sourceType, sessionId, contentHash, cancellationToken);

    /// <inheritdoc />
    public Task<int> AppendProcessingDialogAsync(string sourceType, string sessionId, string requestId, IReadOnlyList<ProcessingDialogItemDto> items, CancellationToken cancellationToken = default)
        => _inner.AppendProcessingDialogAsync(sourceType, sessionId, requestId, items, cancellationToken);

    /// <inheritdoc />
    public Task<UnifiedSessionLogDto?> GetAsync(string sourceType, string sessionId, CancellationToken cancellationToken = default)
        => _inner.GetAsync(sourceType, sessionId, cancellationToken);

    /// <inheritdoc />
    public Task<long> UpsertTurnAsync(string sourceType, string sessionId, UnifiedRequestEntryDto turn, CancellationToken cancellationToken = default)
        => _inner.UpsertTurnAsync(sourceType, sessionId, turn, cancellationToken);

    /// <inheritdoc />
    public Task<bool> OpenSessionAsync(string sourceType, string sessionId, string? title = null, string? model = null, CancellationToken cancellationToken = default)
        => _inner.OpenSessionAsync(sourceType, sessionId, title, model, cancellationToken);

    /// <inheritdoc />
    public Task<int> RepairWorkspaceStampsAsync(bool dryRun = false, CancellationToken cancellationToken = default)
        => _inner.RepairWorkspaceStampsAsync(dryRun, cancellationToken);

    private static SessionLogQueryResult MergeResults(SessionLogQueryResult local, SessionLogQueryResult remote)
    {
        var localKeys = new HashSet<string>(
            local.Items
                .Where(i => i.SourceType is not null && i.SessionId is not null)
                .Select(i => $"{i.SourceType}|{i.SessionId}"),
            StringComparer.OrdinalIgnoreCase);

        var merged = new List<UnifiedSessionLogDto>(local.Items);

        foreach (var item in remote.Items)
        {
            var key = $"{item.SourceType}|{item.SessionId}";
            if (!localKeys.Contains(key))
                merged.Add(item);
        }

        return new SessionLogQueryResult
        {
            TotalCount = merged.Count,
            Limit = local.Limit,
            Offset = local.Offset,
            Items = merged,
        };
    }
}
