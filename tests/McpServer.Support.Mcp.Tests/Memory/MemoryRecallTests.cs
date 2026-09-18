using McpServer.Support.Mcp.Services;

namespace McpServer.Support.Mcp.Tests.Memory;

/// <summary>
/// TEST-MCP-MEMORY-011 / FR-MCP-MEMORY-014: S1 Red acceptance that revert refreshes recall/index.
/// </summary>
public sealed class MemoryRecallTests : IDisposable
{
    private readonly MemoryS1Harness _harness = new();

    /// <inheritdoc />
    public void Dispose() => _harness.Dispose();

    /// <summary>AC-FR-MCP-MEMORY-014-14: After revert, recall/index refresh reflects reverted Content.</summary>
    [Fact]
    public async Task AfterRevert_IndexReflectsContent()
    {
        var added = await _harness.AddCompatAsync(new MemoryAddRequest
        {
            Category = "recall",
            Text = "original-recall-token",
            Content = "original-recall-token",
        }, cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);
        Assert.True(added.Success, added.Error);

        await _harness.UpdateCompatAsync(
            added.Memory!.Id,
            new MemoryUpdateRequest { Text = "updated-recall-token", Content = "updated-recall-token" },
            cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);

        var reverted = await _harness.RevertAsync(added.Memory.Id, 1, cancellationToken: TestContext.Current.CancellationToken)
            .ConfigureAwait(true);
        Assert.True(reverted.StatusCode is 200 or 201, reverted.Error);

        var recall = await _harness.RecallAsync("original-recall-token", TestContext.Current.CancellationToken)
            .ConfigureAwait(true);
        Assert.Contains(recall.Items, item =>
            item.Id == added.Memory.Id
            && (item.Content ?? item.Text).Contains("original-recall-token", StringComparison.Ordinal));
        Assert.DoesNotContain(recall.Items, item =>
            item.Id == added.Memory.Id
            && (item.Content ?? item.Text).Contains("updated-recall-token", StringComparison.Ordinal));
    }
}
