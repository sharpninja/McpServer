namespace McpServer.Support.Mcp.Storage;

/// <summary>
/// FR-MCP-TRIAGESTORE-002 / TR-MCP-TRIAGESTORE-002: short connect+command budget for
/// triage intake and non-submit session-log SaveChanges so those mutating calls fail as
/// storage-unavailable instead of hanging until the 30s REPL timeout.
/// Session-log Submit passes an explicit longer budget (see <c>SessionLogSubmitOptions</c>).
/// </summary>
public static class StorageCommandBudget
{
    /// <summary>Triage intake and non-submit session-log save budget. Session-log Submit does not use this value.</summary>
    public static readonly TimeSpan Default = TimeSpan.FromSeconds(5);

    /// <summary>Runs <paramref name="action"/> under <see cref="Default"/> and maps budget expiry to <see cref="StorageCommandBudgetExceededException"/>.</summary>
    /// <param name="action">The storage work.</param>
    /// <param name="cancellationToken">Caller cancellation.</param>
    /// <param name="budget">
    /// Optional per-call budget. <see langword="null"/> uses <see cref="Default"/> (5 seconds).
    /// Session-log Submit passes its configured budget here.
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
            throw limit == Default
                ? new StorageCommandBudgetExceededException()
                : new StorageCommandBudgetExceededException(limit);
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
/// FR-MCP-TRIAGESTORE-002: the storage engine did not respond within the command budget.
/// The parameterless constructor is the 5 second intake budget. A longer Submit budget
/// uses <see cref="StorageCommandBudgetExceededException(TimeSpan)"/>.
/// Classified as <c>backend_unavailable</c>.
/// </summary>
public sealed class StorageCommandBudgetExceededException : TimeoutException
{
    /// <summary>Initializes the 5 second intake budget error.</summary>
    public StorageCommandBudgetExceededException()
        : base("The storage backend did not respond within the 5 second intake budget.")
    {
    }

    /// <summary>Initializes a budget-exceeded error for an explicit per-call budget.</summary>
    /// <param name="budget">The budget that elapsed.</param>
    public StorageCommandBudgetExceededException(TimeSpan budget)
        : base($"The storage backend did not respond within the {budget.TotalSeconds:0.###} second storage command budget.")
    {
    }
}
