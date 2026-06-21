using Microsoft.Extensions.Logging;

namespace McpServer.Support.Mcp.Services;

/// <summary>
/// FR-MCP-082: Decorator that wraps an <see cref="ITodoService"/> to merge local and
/// remote TODO data when federation is enabled. Read operations query both local and
/// remote in parallel and merge results (local wins on ID collision). Write operations
/// delegate exclusively to the inner (local) service. When federation is disabled or
/// no target resolves, all calls pass through to the inner service with zero overhead.
/// </summary>
public sealed class FederatedTodoService : ITodoService, ITodoCompensationService, ITodoCompensationCapability
{
    private readonly ITodoService _inner;
    private readonly FederationRegistry _registry;
    private readonly IFederationDataClient _client;
    private readonly ILogger<FederatedTodoService> _logger;

    /// <summary>Initializes a new instance of the <see cref="FederatedTodoService"/> class.</summary>
    /// <param name="inner">The local TODO service to delegate to.</param>
    /// <param name="registry">Federation registry for target resolution.</param>
    /// <param name="client">Federation data client for remote queries.</param>
    /// <param name="logger">Logger for diagnostic output.</param>
    public FederatedTodoService(
        ITodoService inner,
        FederationRegistry registry,
        IFederationDataClient client,
        ILogger<FederatedTodoService> logger)
    {
        _inner = inner;
        _registry = registry;
        _client = client;
        _logger = logger;
    }

    /// <inheritdoc />
    public bool SupportsRollbackCompensation
        => _inner is ITodoCompensationService &&
           (_inner is not ITodoCompensationCapability capability || capability.SupportsRollbackCompensation);

    /// <inheritdoc />
    public async Task<TodoQueryResult> QueryAsync(TodoQueryRequest request, CancellationToken cancellationToken = default)
    {
        var target = _registry.ResolveTarget(null);
        if (target is null)
            return await _inner.QueryAsync(request, cancellationToken).ConfigureAwait(false);

        var localTask = _inner.QueryAsync(request, cancellationToken);
        TodoQueryResult? remote = null;

        try
        {
            remote = await _client.QueryTodosAsync(target, request, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogWarning(ex, "Federation query to {Target} failed, using local-only results", target.Name);
        }

        var local = await localTask.ConfigureAwait(false);

        if (remote is null || remote.Items.Count == 0)
            return local;

        return MergeResults(local, remote);
    }

    /// <inheritdoc />
    public async Task<TodoFlatItem?> GetByIdAsync(string id, CancellationToken cancellationToken = default)
    {
        var localResult = await _inner.GetByIdAsync(id, cancellationToken).ConfigureAwait(false);
        if (localResult is not null)
            return localResult;

        var target = _registry.ResolveTarget(null);
        if (target is null)
            return null;

        try
        {
            return await _client.GetTodoByIdAsync(target, id, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogWarning(ex, "Federation GetById to {Target} failed for id {Id}", target.Name, id);
            return null;
        }
    }

    /// <inheritdoc />
    public Task<TodoMutationResult> CreateAsync(TodoCreateRequest request, CancellationToken cancellationToken = default)
        => _inner.CreateAsync(request, cancellationToken);

    /// <inheritdoc />
    public Task<TodoMutationResult> UpdateAsync(string id, TodoUpdateRequest request, CancellationToken cancellationToken = default)
        => _inner.UpdateAsync(id, request, cancellationToken);

    /// <inheritdoc />
    public Task<TodoMutationResult> DeleteAsync(string id, CancellationToken cancellationToken = default)
        => _inner.DeleteAsync(id, cancellationToken);

    /// <inheritdoc />
    public Task<TodoAuditQueryResult> GetAuditAsync(string id, int limit = 50, int offset = 0, CancellationToken cancellationToken = default)
        => _inner.GetAuditAsync(id, limit, offset, cancellationToken);

    /// <inheritdoc />
    public Task<TodoProjectionStatusResult> GetProjectionStatusAsync(CancellationToken cancellationToken = default)
        => _inner.GetProjectionStatusAsync(cancellationToken);

    /// <inheritdoc />
    public Task<TodoProjectionRepairResult> RepairProjectionAsync(CancellationToken cancellationToken = default)
        => _inner.RepairProjectionAsync(cancellationToken);

    /// <inheritdoc />
    public Task<TodoCompensationSnapshot?> CaptureForRestoreAsync(string id, CancellationToken cancellationToken = default)
        => _inner is ITodoCompensationService compensation
            ? compensation.CaptureForRestoreAsync(id, cancellationToken)
            : Task.FromResult<TodoCompensationSnapshot?>(null);

    /// <inheritdoc />
    public Task<TodoCompensatedMutationResult> UpdateWithRestorePointAsync(string id, TodoUpdateRequest request, CancellationToken cancellationToken = default)
        => _inner is ITodoCompensationService compensation
            ? compensation.UpdateWithRestorePointAsync(id, request, cancellationToken)
            : Task.FromResult(new TodoCompensatedMutationResult
            {
                Result = new TodoMutationResult(false, "The active TODO provider does not support transaction rollback compensation.", FailureKind: TodoMutationFailureKind.Conflict),
            });

    /// <inheritdoc />
    public Task<TodoCompensatedMutationResult> DeleteWithRestorePointAsync(string id, CancellationToken cancellationToken = default)
        => _inner is ITodoCompensationService compensation
            ? compensation.DeleteWithRestorePointAsync(id, cancellationToken)
            : Task.FromResult(new TodoCompensatedMutationResult
            {
                Result = new TodoMutationResult(false, "The active TODO provider does not support transaction rollback compensation.", FailureKind: TodoMutationFailureKind.Conflict),
            });

    /// <inheritdoc />
    public Task<TodoMutationResult> DeleteCreatedAsync(string id, CancellationToken cancellationToken = default)
        => _inner is ITodoCompensationService compensation
            ? compensation.DeleteCreatedAsync(id, cancellationToken)
            : Task.FromResult(new TodoMutationResult(false, "The active TODO provider does not support transaction rollback compensation.", FailureKind: TodoMutationFailureKind.Conflict));

    /// <inheritdoc />
    public Task<TodoMutationResult> RestoreAsync(TodoCompensationSnapshot snapshot, CancellationToken cancellationToken = default)
        => _inner is ITodoCompensationService compensation
            ? compensation.RestoreAsync(snapshot, cancellationToken)
            : Task.FromResult(new TodoMutationResult(false, "The active TODO provider does not support transaction rollback compensation.", FailureKind: TodoMutationFailureKind.Conflict));

    private static TodoQueryResult MergeResults(TodoQueryResult local, TodoQueryResult remote)
    {
        var localIds = new HashSet<string>(local.Items.Select(i => i.Id), StringComparer.OrdinalIgnoreCase);
        var merged = new List<TodoFlatItem>(local.Items);

        foreach (var item in remote.Items)
        {
            if (!localIds.Contains(item.Id))
                merged.Add(item);
        }

        return new TodoQueryResult(merged, merged.Count);
    }
}
