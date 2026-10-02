using McpServer.Support.Mcp.Services;

namespace McpServer.Support.Mcp.Tests.Memory;

/// <summary>
/// TEST-MCP-MEMORY-016 / TEST-MCP-MEMORY-010 / FR-MCP-MEMORY-010:
/// Compat CRUD remains green for pre-migration rows while mapping legacy text to Content.
/// </summary>
public sealed class MemoryCompatCrudTests : IDisposable
{
    private readonly MemoryS1Harness _harness = new();

    /// <inheritdoc />
    public void Dispose() => _harness.Dispose();

    /// <summary>AC-FR-MCP-MEMORY-010-04: Compat add/list/update/remove still works; legacy text maps to Content.</summary>
    [Fact]
    public async Task LegacyRows_ListUpdateRemove_StillWork()
    {
        var added = await _harness.AddCompatAsync(new MemoryAddRequest
        {
            Category = "legacy",
            Text = "legacy text body",
        }, cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);
        Assert.True(added.Success, added.Error);

        var listed = await _harness.ListCompatAsync(cancellationToken: TestContext.Current.CancellationToken)
            .ConfigureAwait(true);
        Assert.Contains(listed.Items, item => item.Id == added.Memory!.Id);
        Assert.Equal("legacy text body", listed.Items.Single(item => item.Id == added.Memory!.Id).Content);

        var updated = await _harness.UpdateCompatAsync(
            added.Memory!.Id,
            new MemoryUpdateRequest { Text = "legacy text updated" },
            cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);
        Assert.True(updated.Success, updated.Error);
        Assert.Equal("legacy text updated", updated.Memory?.Content ?? updated.Memory?.Text);

        var removed = await _harness.RemoveCompatAsync(
            added.Memory.Id,
            cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);
        Assert.True(removed.Success, removed.Error);
        var after = await _harness.GetCompatAsync(added.Memory.Id, cancellationToken: TestContext.Current.CancellationToken)
            .ConfigureAwait(true);
        Assert.Null(after);
    }

    /// <summary>AC-FR-MCP-MEMORY-010-40: Legacy Category still accepted and round-trips; maps into Type or a preserved field.</summary>
    [Fact]
    public async Task LegacyCategory_RoundTrips()
    {
        var added = await _harness.AddCompatAsync(new MemoryAddRequest
        {
            Category = "operator notes",
            Text = "category round trip",
        }, cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);
        Assert.True(added.Success, added.Error);

        var stored = await _harness.GetCompatAsync(added.Memory!.Id, cancellationToken: TestContext.Current.CancellationToken)
            .ConfigureAwait(true);
        Assert.Equal("OPERATOR-NOTES", stored?.Category);
        Assert.True(
            string.Equals(stored?.Type, "OPERATOR-NOTES", StringComparison.OrdinalIgnoreCase)
            || string.Equals(stored?.Type, "other", StringComparison.OrdinalIgnoreCase)
            || !string.IsNullOrWhiteSpace(stored?.Category));
    }

    /// <summary>AC-TR-MCP-MEMORY-API-002-03: Compat memory_add|list|update|remove remain registered.</summary>
    [Fact]
    public void ToolsStillRegistered()
    {
        var names = MemoryS5Catalog.DiscoverMcpToolNames();
        foreach (var verb in MemoryS5Catalog.CompatVerbs)
            Assert.Contains(verb, names);
    }
}
