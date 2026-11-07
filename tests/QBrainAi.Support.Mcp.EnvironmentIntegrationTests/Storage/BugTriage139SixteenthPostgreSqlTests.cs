using System.Reflection;
using QBrainAi.Support.Mcp.Services;
using Npgsql;

namespace QBrainAi.Support.Mcp.EnvironmentIntegrationTests.Storage;

/// <summary>
/// TEST-MCP-USECASE-020: Fresh-PostgreSQL causal cancellation boundary evidence.
/// </summary>
/// <remarks>2026-11-07, TEST-MCP-USECASE-020; FR-MCP-BUILD-001; TR-MCP-NUKE-019 AC-7: moved whole with its 3 Category=Integration cases from QBrainAi.Support.Mcp.Tests (Storage/BugTriage139SixteenthPostgreSqlTests.cs), whose unit classes stay there, into the integration-owned QBrainAi.Support.Mcp.EnvironmentIntegrationTests, so the unit-owned project holds only unit cases (slice S7, hostile validation finding R14-F4; operator 2026-11-06: "...Environment.IntegrationTests (Recommended)"); only the namespace changed.</remarks>
[Trait("Category", "Integration")]
public sealed class BugTriage139SixteenthPostgreSqlBoundaryTests
    : IClassFixture<EphemeralPostgresFixture>
{
    private readonly EphemeralPostgresFixture _fixture;

    /// <summary>Initializes the fresh PostgreSQL boundary tests.</summary>
    /// <param name="fixture">Fresh PostgreSQL server fixture.</param>
    public BugTriage139SixteenthPostgreSqlBoundaryTests(
        EphemeralPostgresFixture fixture) =>
        _fixture = fixture;

    /// <summary>
    /// A real server statement timeout that has already occurred remains the original
    /// <see cref="PostgresException"/> when caller cancellation races with propagation.
    /// </summary>
    [Fact]
    public async Task FreshPostgreSql_StatementTimeoutConcurrentWithCallerCancellation_IsPreserved()
    {
        var testToken = TestContext.Current.CancellationToken;
        await using var connection = new NpgsqlConnection(_fixture.ServerConnectionString);
        await connection.OpenAsync(testToken).ConfigureAwait(true);
        await using (var setup = connection.CreateCommand())
        {
            setup.CommandText = "SET statement_timeout = 75";
            await setup.ExecuteNonQueryAsync(testToken).ConfigureAwait(true);
        }

        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT pg_sleep(5)";
        using var caller = new CancellationTokenSource();
        var providerFailure = new TaskCompletionSource<PostgresException>(
            TaskCreationOptions.RunContinuationsAsynchronously);
        var releaseFailure = new TaskCompletionSource(
            TaskCreationOptions.RunContinuationsAsynchronously);
        var operation = InvokeProviderCancelableAsync(
            async () =>
            {
                try
                {
                    await command.ExecuteScalarAsync(CancellationToken.None)
                        .ConfigureAwait(false);
                    return 0;
                }
                catch (PostgresException exception)
                    when (exception.SqlState == PostgresErrorCodes.QueryCanceled)
                {
                    providerFailure.TrySetResult(exception);
                    await releaseFailure.Task.ConfigureAwait(false);
                    throw;
                }
            },
            caller.Token);

        var original = await providerFailure.Task
            .WaitAsync(TimeSpan.FromSeconds(20), testToken)
            .ConfigureAwait(true);
        caller.Cancel();
        releaseFailure.TrySetResult();

        var observed = await Assert.ThrowsAsync<PostgresException>(
                async () => await operation.ConfigureAwait(false))
            .ConfigureAwait(true);
        Assert.Same(original, observed);
        Assert.Contains(
            "statement timeout",
            observed.MessageText,
            StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// A real Npgsql operation cancelled through the exact caller token remains observable as
    /// caller cancellation.
    /// </summary>
    [Fact]
    public async Task FreshPostgreSql_ExactCallerTokenCancellation_RemainsCancellation()
    {
        var testToken = TestContext.Current.CancellationToken;
        await using var connection = new NpgsqlConnection(_fixture.ServerConnectionString);
        await connection.OpenAsync(testToken).ConfigureAwait(true);
        await using (var setup = connection.CreateCommand())
        {
            setup.CommandText = "SET statement_timeout = 0";
            await setup.ExecuteNonQueryAsync(testToken).ConfigureAwait(true);
        }

        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT pg_sleep(5)";
        using var caller = CancellationTokenSource.CreateLinkedTokenSource(testToken);
        var operation = InvokeProviderCancelableAsync(
            async () =>
            {
                await command.ExecuteScalarAsync(caller.Token).ConfigureAwait(false);
                return 0;
            },
            caller.Token);

        caller.CancelAfter(TimeSpan.FromMilliseconds(100));
        var observed = await Assert.ThrowsAnyAsync<OperationCanceledException>(
                async () => await operation.ConfigureAwait(false))
            .ConfigureAwait(true);
        Assert.Equal(caller.Token, observed.CancellationToken);
    }

    /// <summary>
    /// A real administrative <c>pg_cancel_backend</c> produces SQLSTATE 57014 and remains the
    /// original <see cref="PostgresException"/> when caller cancellation races with propagation.
    /// </summary>
    [Fact]
    public async Task FreshPostgreSql_AdministrativeCancellationConcurrentWithCallerCancellation_IsPreserved()
    {
        var testToken = TestContext.Current.CancellationToken;
        await using var connection = new NpgsqlConnection(_fixture.ServerConnectionString);
        await using var administrator = new NpgsqlConnection(_fixture.ServerConnectionString);
        await connection.OpenAsync(testToken).ConfigureAwait(true);
        await administrator.OpenAsync(testToken).ConfigureAwait(true);

        int backendProcessId;
        await using (var processIdCommand = connection.CreateCommand())
        {
            processIdCommand.CommandText = "SELECT pg_backend_pid();";
            backendProcessId = Convert.ToInt32(
                await processIdCommand.ExecuteScalarAsync(testToken).ConfigureAwait(true),
                System.Globalization.CultureInfo.InvariantCulture);
        }

        var lockKey = Random.Shared.Next(1, int.MaxValue);
        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT pg_advisory_lock(@key), pg_sleep(30);";
        command.Parameters.AddWithValue("key", lockKey);
        using var caller = new CancellationTokenSource();
        var providerFailure = new TaskCompletionSource<PostgresException>(
            TaskCreationOptions.RunContinuationsAsynchronously);
        var releaseFailure = new TaskCompletionSource(
            TaskCreationOptions.RunContinuationsAsynchronously);
        var operation = InvokeProviderCancelableAsync(
            async () =>
            {
                try
                {
                    await command.ExecuteScalarAsync(CancellationToken.None).ConfigureAwait(false);
                    return 0;
                }
                catch (PostgresException exception)
                    when (exception.SqlState == PostgresErrorCodes.QueryCanceled)
                {
                    providerFailure.TrySetResult(exception);
                    await releaseFailure.Task.ConfigureAwait(false);
                    throw;
                }
            },
            caller.Token);

        await WaitForAdvisoryLockAsync(
                administrator,
                backendProcessId,
                lockKey,
                testToken)
            .ConfigureAwait(true);
        await using (var cancelCommand = administrator.CreateCommand())
        {
            cancelCommand.CommandText = "SELECT pg_cancel_backend(@pid);";
            cancelCommand.Parameters.AddWithValue("pid", backendProcessId);
            var cancelled = await cancelCommand.ExecuteScalarAsync(testToken).ConfigureAwait(true);
            Assert.True(Assert.IsType<bool>(cancelled));
        }

        var original = await providerFailure.Task
            .WaitAsync(TimeSpan.FromSeconds(20), testToken)
            .ConfigureAwait(true);
        caller.Cancel();
        releaseFailure.TrySetResult();

        var observed = await Assert.ThrowsAsync<PostgresException>(
                async () => await operation.ConfigureAwait(false))
            .ConfigureAwait(true);
        Assert.Same(original, observed);
        Assert.Equal(PostgresErrorCodes.QueryCanceled, observed.SqlState);
        Assert.Contains(
            "user request",
            observed.MessageText,
            StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain(
            "statement timeout",
            observed.MessageText,
            StringComparison.OrdinalIgnoreCase);
    }

    private static async Task WaitForAdvisoryLockAsync(
        NpgsqlConnection administrator,
        int backendProcessId,
        int lockKey,
        CancellationToken cancellationToken)
    {
        var deadline = DateTimeOffset.UtcNow.AddSeconds(20);
        while (DateTimeOffset.UtcNow < deadline)
        {
            await using var probe = administrator.CreateCommand();
            probe.CommandText =
                """
                SELECT EXISTS (
                    SELECT 1
                    FROM pg_locks
                    WHERE pid = @pid
                      AND locktype = 'advisory'
                      AND granted
                      AND classid = 0
                      AND objid = @key);
                """;
            probe.Parameters.AddWithValue("pid", backendProcessId);
            probe.Parameters.AddWithValue("key", lockKey);
            var acquired = await probe.ExecuteScalarAsync(cancellationToken).ConfigureAwait(true);
            if (Assert.IsType<bool>(acquired))
                return;

            await Task.Delay(TimeSpan.FromMilliseconds(25), cancellationToken).ConfigureAwait(true);
        }

        throw new TimeoutException("PostgreSQL backend did not acquire the administrative-cancel gate lock.");
    }

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
