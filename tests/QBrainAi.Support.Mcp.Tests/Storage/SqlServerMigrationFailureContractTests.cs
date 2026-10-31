using Xunit;

namespace QBrainAi.Support.Mcp.Tests.Storage;

/// <summary>
/// FR-MCP-TESTRESOURCE-001 ac-5: scripted migration failures prove first-failure reporting,
/// later-error retention and operation isolation before the real reporting adapter is implemented.
/// No connection, process environment or filesystem is used.
/// </summary>
public sealed class SqlServerMigrationFailureContractTests
{
    /// <summary>Explicit operations supplied by either the independent mock or the real reporter.</summary>
    internal sealed record Probe(Action<Exception> Record, Action<Action> Run, Func<Func<Task>, Task> RunAsync);

    /// <summary>Runs identical command/connection, ordering, duplication and fallback assertions.</summary>
    internal static void AssertFailureSequences(Func<Probe> create)
    {
        foreach (var firstMessage in new[] { "first command failure", "first connection failure" })
        {
            var probe = create();
            var first = new InvalidOperationException(firstMessage);
            var later = new IOException("later command failure");
            var release = new InvalidOperationException("migration lock release failure");
            var result = Assert.Throws<AggregateException>(() => probe.Run(() =>
            {
                probe.Record(first);
                probe.Record(first);
                probe.Record(later);
                throw release;
            }));
            Assert.Collection(result.InnerExceptions,
                e => Assert.Same(first, e), e => Assert.Same(later, e), e => Assert.Same(release, e));
            Assert.Same(first, result.InnerException);
        }

        var noEvents = create();
        var escaped = new IOException("no diagnostic event");
        var fallback = Assert.Throws<AggregateException>(() => noEvents.Run(() => throw escaped));
        Assert.Same(escaped, Assert.Single(fallback.InnerExceptions));
    }

    /// <summary>Success stays successful and previous-operation errors never enter a later failure.</summary>
    internal static void AssertOperationIsolation(Func<Probe> create)
    {
        var probe = create();
        probe.Run(() => probe.Record(new IOException("recovered transient")));
        var first = new IOException("failed old operation");
        Assert.Throws<AggregateException>(() => probe.Run(() => throw first));
        var current = new IOException("current operation only");
        var result = Assert.Throws<AggregateException>(() => probe.Run(() => throw current));
        Assert.Same(current, Assert.Single(result.InnerExceptions));
        var ran = false;
        probe.Run(() => ran = true);
        Assert.True(ran);
    }

    /// <summary>Async failures preserve the same sequence; cancellation retains its token and diagnostics.</summary>
    internal static async Task AssertAsyncSequences(Func<Probe> create)
    {
        var probe = create();
        var first = new IOException("async first");
        var release = new InvalidOperationException("async release");
        var result = await Assert.ThrowsAsync<AggregateException>(() => probe.RunAsync(async () =>
        {
            probe.Record(first);
            await Task.Yield();
            throw release;
        }));
        Assert.Collection(result.InnerExceptions, e => Assert.Same(first, e), e => Assert.Same(release, e));
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        var cancelled = new OperationCanceledException(cancellation.Token);
        var observed = await Assert.ThrowsAsync<OperationCanceledException>(() => probe.RunAsync(() =>
        {
            probe.Record(first);
            return Task.FromException(cancelled);
        }));
        Assert.Same(cancelled, observed);
        Assert.Equal(cancellation.Token, observed.CancellationToken);
        var diagnostics = Assert.IsType<Exception[]>(observed.Data["MigrationFailures"]);
        Assert.Collection(diagnostics, e => Assert.Same(first, e), e => Assert.Same(cancelled, e));
        await probe.RunAsync(() => Task.CompletedTask);
    }

    /// <summary>Independent literal sequence collector; wrong modes are assertion rejection controls.</summary>
    private static Probe Reference(string mode = "correct")
    {
        var errors = new List<Exception>();
        void Record(Exception error)
        {
            if (!errors.Any(existing => ReferenceEquals(existing, error)))
                errors.Add(error);
        }
        Exception Report(Exception escaped)
        {
            Record(escaped);
            var selected = mode switch
            {
                "last only" => new[] { errors[^1] },
                "first only" => new[] { errors[0] },
                _ => errors.ToArray(),
            };
            if (escaped is OperationCanceledException)
            {
                escaped.Data["MigrationFailures"] = selected;
                return escaped;
            }
            return new AggregateException("Migration first failure and later failures", selected);
        }
        void Begin()
        {
            if (mode != "leak") errors.Clear();
        }
        void Run(Action operation)
        {
            Begin();
            try { operation(); }
            catch (Exception error) { throw Report(error); }
        }
        async Task RunAsync(Func<Task> operation)
        {
            Begin();
            try { await operation(); }
            catch (Exception error) { throw Report(error); }
        }
        return new(Record, Run, RunAsync);
    }

    /// <summary>Correct scripted command and connection failures satisfy the ordered contract.</summary>
    [Fact]
    public void ScriptedFailures_PreserveFirstAndLaterErrors() => AssertFailureSequences(() => Reference());

    /// <summary>Correct scripted operations cannot leak recovered or failed earlier errors.</summary>
    [Fact]
    public void ScriptedOperations_AreIsolated() => AssertOperationIsolation(() => Reference());

    /// <summary>Correct asynchronous sequences and cancellation satisfy the same contract.</summary>
    [Fact]
    public Task ScriptedAsyncFailures_PreserveErrorsAndCancellation() => AssertAsyncSequences(() => Reference());

    /// <summary>Reporters that omit either first or later errors must fail the sequence assertions.</summary>
    [Theory]
    [InlineData("last only")]
    [InlineData("first only")]
    public void IncompleteReporters_AreRejected(string mode) =>
        Assert.ThrowsAny<Xunit.Sdk.XunitException>(() => AssertFailureSequences(() => Reference(mode)));

    /// <summary>A reporter that leaks earlier operations must fail the isolation assertions.</summary>
    [Fact]
    public void LeakingReporter_IsRejected() =>
        Assert.ThrowsAny<Xunit.Sdk.XunitException>(() => AssertOperationIsolation(() => Reference("leak")));
}
