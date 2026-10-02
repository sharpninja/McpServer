namespace McpServer.Support.Mcp.Storage;

/// <summary>
/// FR-MCP-TRIAGESTORE-002 / TR-MCP-TRIAGESTORE-002: connect+command budget for triage intake,
/// session-log SaveChanges, and session-graph materialization. The shared
/// <see cref="Default"/> is 5 seconds. Session-log Submit and graph loads pass a longer budget.
/// Expiry fails as storage-unavailable instead of hanging until the REPL timeout.
/// </summary>
public static class StorageCommandBudget
{
    /// <summary>Triage intake and non-submit session-log save budget. Session-log Submit and graph loads do not use this value.</summary>
    public static readonly TimeSpan Default = TimeSpan.FromSeconds(5);

    /// <summary>Runs <paramref name="action"/> under <see cref="Default"/> and maps budget expiry to <see cref="StorageCommandBudgetExceededException"/>.</summary>
    /// <param name="action">The storage work.</param>
    /// <param name="cancellationToken">Caller cancellation.</param>
    /// <param name="budget">
    /// Optional per-call budget. <see langword="null"/> uses <see cref="Default"/> (5 seconds).
    /// Session-log Submit and graph materialization pass the configured budget here.
    /// </param>
    /// <returns>A task that completes when the work finishes or the budget expires.</returns>
    public static async Task ExecuteAsync(
        Func<CancellationToken, Task> action,
        CancellationToken cancellationToken,
        TimeSpan? budget = null)
    {
        ArgumentNullException.ThrowIfNull(action);
        var limit = budget ?? Default;
        if (limit <= TimeSpan.Zero)
            throw new ArgumentOutOfRangeException(nameof(budget), limit, "Storage command budget must be positive.");

        using var linked = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        linked.CancelAfter(limit);
        try
        {
            await action(linked.Token).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            throw new StorageCommandBudgetExceededException(limit);
        }
    }

    /// <summary>Runs <paramref name="action"/> and returns its result under the given storage budget.</summary>
    /// <typeparam name="T">Result type.</typeparam>
    /// <param name="action">The storage work.</param>
    /// <param name="cancellationToken">Caller cancellation.</param>
    /// <param name="budget">
    /// Optional per-call budget. <see langword="null"/> uses <see cref="Default"/> (5 seconds).
    /// </param>
    /// <returns>The action result.</returns>
    public static async Task<T> ExecuteAsync<T>(
        Func<CancellationToken, Task<T>> action,
        CancellationToken cancellationToken,
        TimeSpan? budget = null)
    {
        ArgumentNullException.ThrowIfNull(action);
        T result = default!;
        await ExecuteAsync(
            async ct => { result = await action(ct).ConfigureAwait(false); },
            cancellationToken,
            budget).ConfigureAwait(false);
        return result;
    }
}

/// <summary>
/// FR-MCP-TRIAGESTORE-002: the storage engine did not respond within the 5 second intake budget.
/// Classified as <c>backend_unavailable</c>.
/// </summary>
public sealed class StorageCommandBudgetExceededException : TimeoutException
{
    /// <summary>Initializes the budget-exceeded error.</summary>
    public StorageCommandBudgetExceededException()
        : base("The storage backend did not respond within the 5 second intake budget.")
    {
    }

    /// <summary>Initializes a budget-exceeded error for an explicit per-call budget.</summary>
    /// <param name="budget">The budget that elapsed.</param>
    public StorageCommandBudgetExceededException(TimeSpan budget)
        : base(budget == StorageCommandBudget.Default
            ? "The storage backend did not respond within the 5 second intake budget."
            : $"The storage backend did not respond within the {budget.TotalSeconds:0.###} second storage command budget.")
    {
    }
}

/// <summary>
/// FR-MCP-TRIAGESTORE-002: session-graph materialization failed closed.
/// SQL deadlock 1205 is the expected cause. The session was not treated as missing
/// and the mutation was not persisted. Classified as retryable <c>backend_unavailable</c>.
/// </summary>
public sealed class StorageGraphMaterializationException : Exception
{
    /// <summary>Initializes the graph-load failure.</summary>
    /// <param name="message">Failure description that does not claim the session is missing.</param>
    /// <param name="innerException">The provider deadlock or load failure.</param>
    public StorageGraphMaterializationException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
