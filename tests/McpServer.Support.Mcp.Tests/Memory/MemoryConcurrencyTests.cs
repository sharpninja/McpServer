using McpServer.Support.Mcp.Services;

namespace McpServer.Support.Mcp.Tests.Memory;

/// <summary>
/// TEST-MCP-MEMORY-010 / FR-MCP-MEMORY-010: Concurrent update and cancel semantics.
/// </summary>
public sealed class MemoryConcurrencyTests : IDisposable
{
    private readonly MemoryS1Harness _harness = new();

    /// <inheritdoc />
    public void Dispose() => _harness.Dispose();

    /// <summary>AC-FR-MCP-MEMORY-010-42: Concurrent updates are last-write-wins or 409 on stale version.</summary>
    [Fact]
    public async Task ConcurrentUpdate_DocumentedSemantics()
    {
        var created = await _harness.AddCompatAsync(new MemoryAddRequest
        {
            Category = "cc",
            Text = "base",
        }, cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);
        Assert.True(created.Success, created.Error);

        var first = _harness.UpdateCompatAsync(
            created.Memory!.Id,
            new MemoryUpdateRequest { Text = "writer-a" },
            cancellationToken: TestContext.Current.CancellationToken);
        var second = _harness.UpdateCompatAsync(
            created.Memory.Id,
            new MemoryUpdateRequest { Text = "writer-b" },
            cancellationToken: TestContext.Current.CancellationToken);
        var results = await Task.WhenAll(first, second).ConfigureAwait(true);

        var successes = results.Count(result => result.Success);
        var conflicts = results.Count(result => result.FailureKind == MemoryMutationFailureKind.Conflict);
        Assert.True(successes >= 1);
        Assert.True(successes == 2 || conflicts >= 1);

        var stored = await _harness.GetCompatAsync(created.Memory.Id, cancellationToken: TestContext.Current.CancellationToken)
            .ConfigureAwait(true);
        Assert.True(stored?.Text is "writer-a" or "writer-b");
    }

    /// <summary>AC-FR-MCP-MEMORY-010-43: Cancel mid-write does not leave a half-written active row visible.</summary>
    [Fact]
    public async Task CancelMidWrite_NoPartialVisible()
    {
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync().ConfigureAwait(true);

        try
        {
            var result = await _harness.AddCompatAsync(new MemoryAddRequest
            {
                Category = "cancel",
                Text = "partial-should-not-be-visible",
                Title = "partial title",
                Content = "partial-should-not-be-visible",
            }, cancellationToken: cts.Token).ConfigureAwait(true);
            Assert.False(result.Success);
        }
        catch (OperationCanceledException)
        {
            // Cancelled before commit is acceptable; visibility is asserted below.
        }
        var list = await _harness.ListCompatAsync(cancellationToken: CancellationToken.None).ConfigureAwait(true);
        Assert.DoesNotContain(list.Items, item =>
            (item.Content ?? item.Text).Contains("partial-should-not-be-visible", StringComparison.Ordinal));
    }
}
