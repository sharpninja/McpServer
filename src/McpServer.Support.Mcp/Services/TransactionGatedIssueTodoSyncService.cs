using McpServer.Support.Mcp.Models;
using McpServer.TransactionSecurity.Models;
using McpServer.TransactionSecurity.Options;
using McpServer.TransactionSecurity.Services;
using Microsoft.Extensions.Options;

namespace McpServer.Support.Mcp.Services;

/// <summary>
/// TR-MCP-TXN-001: Fails closed for bidirectional GitHub issue/TODO sync while
/// required turn transactions are active because cross-store compensation is not implemented.
/// </summary>
public sealed class TransactionGatedIssueTodoSyncService : IIssueTodoSyncService
{
    private const string DeferredIssueSyncMutationMessage =
        "GitHub issue/TODO sync mutations are not transaction compensated while required turn transactions are active.";

    private readonly IIssueTodoSyncService _inner;
    private readonly ITurnTransactionCoordinator? _coordinator;
    private readonly IOptions<TurnTransactionOptions>? _transactionOptions;

    /// <summary>Initializes a new instance of the <see cref="TransactionGatedIssueTodoSyncService"/> class.</summary>
    /// <param name="inner">Underlying issue/TODO sync service.</param>
    /// <param name="coordinator">Optional turn transaction coordinator.</param>
    /// <param name="transactionOptions">Optional transaction enforcement options.</param>
    public TransactionGatedIssueTodoSyncService(
        IIssueTodoSyncService inner,
        ITurnTransactionCoordinator? coordinator = null,
        IOptions<TurnTransactionOptions>? transactionOptions = null)
    {
        _inner = inner ?? throw new ArgumentNullException(nameof(inner));
        _coordinator = coordinator;
        _transactionOptions = transactionOptions;
    }

    /// <inheritdoc />
    public Task<TodoMutationResult> SyncIssueToTodoAsync(GitHubIssueDetail issue, CancellationToken ct = default)
    {
        if (ShouldDeferMutation(out var error))
        {
            return Task.FromResult(new TodoMutationResult(
                false,
                error,
                FailureKind: TodoMutationFailureKind.ExternalSyncFailed));
        }

        return _inner.SyncIssueToTodoAsync(issue, ct);
    }

    /// <inheritdoc />
    public Task<IssueSyncResult> SyncAllIssuesToTodosAsync(string? state, int limit, CancellationToken ct = default)
    {
        if (ShouldDeferMutation(out var error))
            return Task.FromResult(FailedSync(error));

        return _inner.SyncAllIssuesToTodosAsync(state, limit, ct);
    }

    /// <inheritdoc />
    public Task<GitHubMutationResult> SyncTodoToIssueAsync(string todoId, CancellationToken ct = default)
    {
        if (ShouldDeferMutation(out var error))
            return Task.FromResult(new GitHubMutationResult(false, null, error));

        return _inner.SyncTodoToIssueAsync(todoId, ct);
    }

    /// <inheritdoc />
    public Task<GitHubCommentResult> CommentOnTodoUpdateAsync(
        TodoFlatItem previousTodo,
        TodoFlatItem currentTodo,
        CancellationToken ct = default)
    {
        if (ShouldDeferMutation(out var error))
            return Task.FromResult(new GitHubCommentResult(false, error));

        return _inner.CommentOnTodoUpdateAsync(previousTodo, currentTodo, ct);
    }

    /// <inheritdoc />
    public Task<IssueSyncResult> SyncAllTodosToIssuesAsync(CancellationToken ct = default)
    {
        if (ShouldDeferMutation(out var error))
            return Task.FromResult(FailedSync(error));

        return _inner.SyncAllTodosToIssuesAsync(ct);
    }

    private static IssueSyncResult FailedSync(string error)
        => new()
        {
            Failed = 1,
            Errors = [error],
        };

    private bool ShouldDeferMutation(out string error)
    {
        error = string.Empty;
        if (_coordinator is null)
            return false;

        var status = _coordinator.GetStatus();
        if (status.Degraded)
        {
            error = string.IsNullOrWhiteSpace(status.Message)
                ? "Turn transaction coordinator is degraded."
                : status.Message;
            return true;
        }

        if (!RequiresMutationTransactions(status))
            return false;

        error = DeferredIssueSyncMutationMessage;
        return true;
    }

    private bool RequiresMutationTransactions(TurnTransactionStatusResponse status)
        => status.Enabled && (_transactionOptions?.Value.RequiredForMutations ?? true);
}
