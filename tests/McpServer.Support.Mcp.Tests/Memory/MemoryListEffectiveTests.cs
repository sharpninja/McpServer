using McpServer.Support.Mcp.Services;

namespace McpServer.Support.Mcp.Tests.Memory;

/// <summary>
/// TEST-MCP-MEMORY-010 / FR-MCP-MEMORY-010: S1 Red acceptance for effective list ordering and filters.
/// </summary>
public sealed class MemoryListEffectiveTests : IDisposable
{
    private readonly MemoryS1Harness _harness = new();

    /// <inheritdoc />
    public void Dispose() => _harness.Dispose();

    /// <summary>AC-FR-MCP-MEMORY-010-03: Effective list order remains Global then Workspace.</summary>
    [Fact]
    public async Task OrdersGlobalThenWorkspace()
    {
        await _harness.AddCompatAsync(new MemoryAddRequest
        {
            Id = "MEMORY-ZETA-010",
            Category = "zeta",
            Scope = MemoryScope.Global,
            Text = "later global",
        }, cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);
        await _harness.AddCompatAsync(new MemoryAddRequest
        {
            Id = "MEMORY-ALPHA-001",
            Category = "alpha",
            Scope = MemoryScope.Global,
            Text = "first global",
        }, cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);
        await _harness.AddCompatAsync(new MemoryAddRequest
        {
            Id = "MEMORY-ZETA-011",
            Category = "zeta",
            Scope = MemoryScope.Workspace,
            Text = "later workspace",
        }, cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);
        await _harness.AddCompatAsync(new MemoryAddRequest
        {
            Id = "MEMORY-ALPHA-002",
            Category = "alpha",
            Scope = MemoryScope.Workspace,
            Text = "first workspace",
        }, cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);

        var list = await _harness.ListCompatAsync(cancellationToken: TestContext.Current.CancellationToken)
            .ConfigureAwait(true);

        Assert.Equal(4, list.TotalCount);
        Assert.Equal(MemoryScope.Global, list.Items[0].Scope);
        Assert.Equal("MEMORY-ALPHA-001", list.Items[0].Id);
        Assert.Equal(MemoryScope.Global, list.Items[1].Scope);
        Assert.Equal(MemoryScope.Workspace, list.Items[2].Scope);
        Assert.Equal("MEMORY-ALPHA-002", list.Items[2].Id);
    }

    /// <summary>AC-FR-MCP-MEMORY-010-06: Soft-deleted memories are omitted from default list and recall.</summary>
    [Fact]
    public async Task OmitsSoftDeleted()
    {
        var created = await _harness.AddCompatAsync(new MemoryAddRequest
        {
            Category = "temp",
            Text = "temporary",
        }, cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);
        Assert.True(created.Success, created.Error);
        await _harness.RemoveCompatAsync(created.Memory!.Id, cancellationToken: TestContext.Current.CancellationToken)
            .ConfigureAwait(true);

        var list = await _harness.ListCompatAsync(cancellationToken: TestContext.Current.CancellationToken)
            .ConfigureAwait(true);
        var recall = await _harness.RecallAsync("temporary", TestContext.Current.CancellationToken)
            .ConfigureAwait(true);

        Assert.DoesNotContain(list.Items, item => item.Id == created.Memory.Id);
        Assert.DoesNotContain(recall.Items, item => item.Id == created.Memory.Id);
    }

    /// <summary>AC-FR-MCP-MEMORY-010-34: Within each scope group, list sorts by Id ascending (ordinal).</summary>
    [Fact]
    public async Task WithinScope_SortsByIdOrdinal()
    {
        await _harness.AddCompatAsync(new MemoryAddRequest
        {
            Id = "MEMORY-ZETA-010",
            Category = "zeta",
            Scope = MemoryScope.Global,
            Text = "z",
        }, cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);
        await _harness.AddCompatAsync(new MemoryAddRequest
        {
            Id = "MEMORY-ALPHA-001",
            Category = "alpha",
            Scope = MemoryScope.Global,
            Text = "a",
        }, cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);

        var list = await _harness.ListCompatAsync(
            new MemoryListRequest { Scope = MemoryScope.Global },
            cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);
        var ids = list.Items.Select(item => item.Id).ToArray();

        Assert.Equal(ids.OrderBy(id => id, StringComparer.Ordinal), ids);
    }

    /// <summary>AC-FR-MCP-MEMORY-010-41: Keyword list filter is case-insensitive substring over Content/Title.</summary>
    [Fact]
    public async Task KeywordFilter_CaseInsensitive()
    {
        await _harness.AddCompatAsync(new MemoryAddRequest
        {
            Category = "kw",
            Text = "Hello World",
            Title = "Alpha Title",
            Content = "Hello World",
        }, cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);

        var byContent = await _harness.ListCompatAsync(
            new MemoryListRequest { Keyword = "hello" },
            cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);
        var byTitle = await _harness.ListCompatAsync(
            new MemoryListRequest { Keyword = "ALPHA" },
            cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);

        Assert.Contains(byContent.Items, item => (item.Content ?? item.Text).Contains("Hello World", StringComparison.Ordinal));
        Assert.Contains(byTitle.Items, item => item.Title == "Alpha Title");
    }
}
