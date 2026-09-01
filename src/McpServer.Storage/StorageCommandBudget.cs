namespace McpServer.Support.Mcp.Storage;

/// <summary>
/// FR-MCP-TRIAGESTORE-002 / TR-MCP-TRIAGESTORE-001: short connect+command budget for
/// triage intake and session-log SaveChanges so mutating calls fail as storage-unavailable
/// instead of hanging until the 30s REPL timeout.
/// </summary>
public static class StorageCommandBudget
{
    /// <summary>Intake and session-log save budget.</summary>
    public static readonly TimeSpan Default = TimeSpan.FromSeconds(5);

    /// <summary>Runs <paramref name="action"/> and maps budget expiry to <see cref="StorageCommandBudgetExceededException"/>.</summary>
    /// <param name="action">The storage work.</param>
    /// <param name="cancellationToken">Caller cancellation.</param>
    /// <returns>A task that completes when the work finishes or the budget expires.</returns>
    public static async Task ExecuteAsync(Func<CancellationToken, Task> action, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(action);
        using var linked = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        linked.CancelAfter(Default);
        try
        {
            await action(linked.Token).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            throw new StorageCommandBudgetExceededException();
        }
    }

    /// <summary>Runs <paramref name="action"/> and returns its result under the intake budget.</summary>
    /// <typeparam name="T">Result type.</typeparam>
    /// <param name="action">The storage work.</param>
    /// <param name="cancellationToken">Caller cancellation.</param>
    /// <returns>The action result.</returns>
    public static async Task<T> ExecuteAsync<T>(Func<CancellationToken, Task<T>> action, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(action);
        T result = default!;
        await ExecuteAsync(
            async ct => { result = await action(ct).ConfigureAwait(false); },
            cancellationToken).ConfigureAwait(false);
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
}
