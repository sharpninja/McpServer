using System.Reflection;
using QBrainAi.Support.Mcp.Services;
using Npgsql;

namespace QBrainAi.Support.Mcp.Tests.Storage;

/// <summary>
/// TEST-MCP-USECASE-020: Consumer contracts for causal PostgreSQL cancellation classification.
/// </summary>
public sealed class BugTriage139SixteenthPostgreSqlContractTests
{
    /// <summary>
    /// A server statement timeout remains a provider failure even when the caller token is
    /// concurrently cancelled.
    /// </summary>
    [Fact]
    public async Task ProviderCancellation_ServerStatementTimeout_IsNotCallerCancellation()
    {
        using var caller = new CancellationTokenSource();
        caller.Cancel();
        var providerException = CreateQueryCancelled(
            "canceling statement due to statement timeout");

        var observed = await Assert.ThrowsAsync<PostgresException>(
                () => InvokeProviderCancelableAsync(
                    () => Task.FromException<int>(providerException),
                    caller.Token))
            .ConfigureAwait(true);

        Assert.Same(providerException, observed);
    }

    /// <summary>
    /// Administrative cancellation cannot be attributed to the caller merely because the caller
    /// token is concurrently cancelled.
    /// </summary>
    [Fact]
    public async Task ProviderCancellation_AdministrativeCancel_IsNotCallerCancellation()
    {
        using var caller = new CancellationTokenSource();
        caller.Cancel();
        var providerException = CreateQueryCancelled(
            "canceling statement due to administrator command");

        var observed = await Assert.ThrowsAsync<PostgresException>(
                () => InvokeProviderCancelableAsync(
                    () => Task.FromException<int>(providerException),
                    caller.Token))
            .ConfigureAwait(true);

        Assert.Same(providerException, observed);
    }

    /// <summary>
    /// An operation-cancel exception carrying the exact caller token remains caller cancellation.
    /// </summary>
    [Fact]
    public async Task ProviderCancellation_ExactCallerToken_RemainsCancellation()
    {
        using var caller = new CancellationTokenSource();
        caller.Cancel();
        var providerCancellation = new OperationCanceledException(caller.Token);

        var observed = await Assert.ThrowsAnyAsync<OperationCanceledException>(
                () => InvokeProviderCancelableAsync(
                    () => Task.FromException<int>(providerCancellation),
                    caller.Token))
            .ConfigureAwait(true);

        Assert.Equal(caller.Token, observed.CancellationToken);
    }

    private static PostgresException CreateQueryCancelled(string message) =>
        new(
            message,
            "ERROR",
            "ERROR",
            PostgresErrorCodes.QueryCanceled);


    private static Task<T> InvokeProviderCancelableAsync<T>(
        Func<Task<T>> operation,
        CancellationToken cancellationToken)
    {
        var method = typeof(FederationTopologyService)
            .GetMethods(BindingFlags.NonPublic | BindingFlags.Static)
            .Single(candidate =>
                candidate.Name == "ExecuteProviderCancelableAsync" &&
                candidate.IsGenericMethodDefinition)
            .MakeGenericMethod(typeof(T));
        return Assert.IsAssignableFrom<Task<T>>(
            method.Invoke(null, [operation, cancellationToken]));
    }
}
