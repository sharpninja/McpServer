using McpServer.Support.Mcp.Services;

namespace McpServer.Support.Mcp.Tests.Memory;

/// <summary>
/// TEST-MCP-MEMORY-014 / FR-MCP-MEMORY-014: S1 Red acceptance for version snapshots and revert.
/// </summary>
public sealed class MemoryVersionTests : IDisposable
{
    private readonly MemoryS1Harness _harness = new();

    /// <inheritdoc />
    public void Dispose() => _harness.Dispose();

    /// <summary>AC-FR-MCP-MEMORY-014-01: Updating Content increments VersionNumber and stores a snapshot.</summary>
    [Fact]
    public async Task Update_AppendsVersion()
    {
        var id = await CreateAsync("v1").ConfigureAwait(true);
        await _harness.UpdateCompatAsync(
            id,
            new MemoryUpdateRequest { Text = "v2", Content = "v2" },
            cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);

        var versions = await _harness.ListVersionsAsync(id, cancellationToken: TestContext.Current.CancellationToken)
            .ConfigureAwait(true);
        Assert.Equal(200, versions.StatusCode);
        Assert.True((versions.Items?.Count ?? 0) >= 2);
        Assert.Contains(versions.Items!, item => item.Content == "v1");
        Assert.Contains(versions.Items!, item => item.Content == "v2");
    }

    /// <summary>AC-FR-MCP-MEMORY-014-02: GET versions returns versions ordered ascending by VersionNumber.</summary>
    [Fact]
    public async Task List_OrderedAscending()
    {
        var id = await CreateAsync("one").ConfigureAwait(true);
        await _harness.UpdateCompatAsync(id, new MemoryUpdateRequest { Text = "two", Content = "two" }, cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);
        await _harness.UpdateCompatAsync(id, new MemoryUpdateRequest { Text = "three", Content = "three" }, cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);

        var versions = await _harness.ListVersionsAsync(id, cancellationToken: TestContext.Current.CancellationToken)
            .ConfigureAwait(true);
        Assert.Equal(200, versions.StatusCode);
        var numbers = versions.Items!.Select(item => item.VersionNumber).ToArray();
        Assert.Equal(numbers.OrderBy(n => n), numbers);
    }

    /// <summary>AC-FR-MCP-MEMORY-014-03: Revert to N restores Content (and declared mutable fields) to snapshot N.</summary>
    [Fact]
    public async Task Revert_RestoresSnapshot()
    {
        var id = await CreateAsync("original").ConfigureAwait(true);
        await _harness.UpdateCompatAsync(id, new MemoryUpdateRequest { Text = "changed", Content = "changed" }, cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);

        var reverted = await _harness.RevertAsync(id, 1, cancellationToken: TestContext.Current.CancellationToken)
            .ConfigureAwait(true);
        Assert.True(reverted.StatusCode is 200 or 201, reverted.Error);
        Assert.Equal("original", reverted.Memory?.Content ?? reverted.Memory?.Text);
    }

    /// <summary>AC-FR-MCP-MEMORY-014-04: Revert itself appends a new version (history preserved).</summary>
    [Fact]
    public async Task Revert_AppendsNewVersion()
    {
        var id = await CreateAsync("original").ConfigureAwait(true);
        await _harness.UpdateCompatAsync(id, new MemoryUpdateRequest { Text = "changed", Content = "changed" }, cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);
        var before = await _harness.ListVersionsAsync(id, cancellationToken: TestContext.Current.CancellationToken)
            .ConfigureAwait(true);
        await _harness.RevertAsync(id, 1, cancellationToken: TestContext.Current.CancellationToken)
            .ConfigureAwait(true);
        var after = await _harness.ListVersionsAsync(id, cancellationToken: TestContext.Current.CancellationToken)
            .ConfigureAwait(true);

        Assert.True((after.Items?.Count ?? 0) > (before.Items?.Count ?? 0));
    }

    /// <summary>AC-FR-MCP-MEMORY-014-05: Revert to missing N returns 400/404.</summary>
    [Fact]
    public async Task RevertMissing_Returns400Or404()
    {
        var id = await CreateAsync("only-one").ConfigureAwait(true);
        var result = await _harness.RevertAsync(id, 99, cancellationToken: TestContext.Current.CancellationToken)
            .ConfigureAwait(true);

        Assert.True(result.StatusCode is 400 or 404, result.Error);
    }

    /// <summary>AC-FR-MCP-MEMORY-014-06: Initial create yields VersionNumber=1 (or documented initial).</summary>
    [Fact]
    public async Task Create_InitialVersion()
    {
        var id = await CreateAsync("initial").ConfigureAwait(true);
        var versions = await _harness.ListVersionsAsync(id, cancellationToken: TestContext.Current.CancellationToken)
            .ConfigureAwait(true);

        Assert.Equal(200, versions.StatusCode);
        Assert.Contains(versions.Items!, item => item.VersionNumber == 1);
    }

    /// <summary>AC-FR-MCP-MEMORY-014-07: Update that does not change Content does not append a duplicate version (or appends a documented no-op).</summary>
    [Fact]
    public async Task NoOpUpdate_VersionPolicy()
    {
        var id = await CreateAsync("same").ConfigureAwait(true);
        var before = await _harness.ListVersionsAsync(id, cancellationToken: TestContext.Current.CancellationToken)
            .ConfigureAwait(true);
        await _harness.UpdateCompatAsync(id, new MemoryUpdateRequest { UpdatedBy = "same-actor" }, cancellationToken: TestContext.Current.CancellationToken)
            .ConfigureAwait(true);
        var after = await _harness.ListVersionsAsync(id, cancellationToken: TestContext.Current.CancellationToken)
            .ConfigureAwait(true);

        Assert.Equal(200, after.StatusCode);
        Assert.True(
            after.Items!.Count == before.Items!.Count
            || after.Items.Count == before.Items.Count + 1);
        var contents = after.Items.Select(item => item.Content).ToArray();
        Assert.DoesNotContain(contents, content => content is null);
    }

    /// <summary>AC-FR-MCP-MEMORY-014-08: Versions for soft-deleted memory are not listed to normal callers (404 on parent).</summary>
    [Fact]
    public async Task SoftDeletedParent_VersionsHidden()
    {
        var id = await CreateAsync("doomed").ConfigureAwait(true);
        await _harness.RemoveCompatAsync(id, cancellationToken: TestContext.Current.CancellationToken)
            .ConfigureAwait(true);

        var versions = await _harness.ListVersionsAsync(id, cancellationToken: TestContext.Current.CancellationToken)
            .ConfigureAwait(true);
        Assert.Equal(404, versions.StatusCode);
    }

    /// <summary>AC-FR-MCP-MEMORY-014-09: Foreign-workspace cannot list versions of a Workspace memory.</summary>
    [Fact]
    public async Task ForeignWorkspace_CannotListVersions()
    {
        var id = await CreateAsync("workspace-only").ConfigureAwait(true);
        var versions = await _harness.ListVersionsAsync(
            id,
            _harness.WorkspaceB,
            TestContext.Current.CancellationToken).ConfigureAwait(true);

        Assert.True(versions.StatusCode is 403 or 404, versions.Error);
    }

    /// <summary>AC-FR-MCP-MEMORY-014-10: Revert to current N is idempotent content-wise and still appends or no-ops per docs.</summary>
    [Fact]
    public async Task RevertToCurrent_Stable()
    {
        var id = await CreateAsync("current").ConfigureAwait(true);
        var before = await _harness.GetCompatAsync(id, cancellationToken: TestContext.Current.CancellationToken)
            .ConfigureAwait(true);
        var reverted = await _harness.RevertAsync(id, 1, cancellationToken: TestContext.Current.CancellationToken)
            .ConfigureAwait(true);

        Assert.True(reverted.StatusCode is 200 or 201, reverted.Error);
        Assert.Equal(before?.Content ?? before?.Text, reverted.Memory?.Content ?? reverted.Memory?.Text);
    }

    /// <summary>AC-FR-MCP-MEMORY-014-11: Version snapshot includes Content and Title at minimum.</summary>
    [Fact]
    public async Task Snapshot_IncludesContentAndTitle()
    {
        var remembered = await _harness.RememberAsync(new MemoryRememberRequest
        {
            Title = "Snap title",
            Content = "snap content",
            Type = "fact",
        }, cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);
        var id = remembered.MemoryId;
        if (string.IsNullOrWhiteSpace(id))
        {
            var added = await _harness.AddCompatAsync(new MemoryAddRequest
            {
                Category = "snap",
                Text = "snap content",
                Title = "Snap title",
                Content = "snap content",
            }, cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);
            id = added.Memory!.Id;
        }

        var versions = await _harness.ListVersionsAsync(id!, cancellationToken: TestContext.Current.CancellationToken)
            .ConfigureAwait(true);
        Assert.Equal(200, versions.StatusCode);
        Assert.Contains(versions.Items!, item => item.Content == "snap content" && item.Title == "Snap title");
    }

    /// <summary>AC-FR-MCP-MEMORY-014-12: Version list pagination is stable; without pagination returns full history up to the cap.</summary>
    [Fact]
    public async Task List_PaginationOrFullCap()
    {
        var id = await CreateAsync("p0").ConfigureAwait(true);
        for (var i = 1; i <= 5; i++)
        {
            await _harness.UpdateCompatAsync(
                id,
                new MemoryUpdateRequest { Text = "p" + i, Content = "p" + i },
                cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);
        }

        var versions = await _harness.ListVersionsAsync(id, cancellationToken: TestContext.Current.CancellationToken)
            .ConfigureAwait(true);
        Assert.Equal(200, versions.StatusCode);
        Assert.True(versions.Items!.Count >= 6);
    }

    /// <summary>AC-FR-MCP-MEMORY-014-15: Version numbers are contiguous positive integers with no gaps after successful updates.</summary>
    [Fact]
    public async Task VersionNumbers_Contiguous()
    {
        var id = await CreateAsync("c0").ConfigureAwait(true);
        await _harness.UpdateCompatAsync(id, new MemoryUpdateRequest { Text = "c1", Content = "c1" }, cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);
        await _harness.UpdateCompatAsync(id, new MemoryUpdateRequest { Text = "c2", Content = "c2" }, cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);

        var versions = await _harness.ListVersionsAsync(id, cancellationToken: TestContext.Current.CancellationToken)
            .ConfigureAwait(true);
        Assert.Equal(200, versions.StatusCode);
        var numbers = versions.Items!.Select(item => item.VersionNumber).OrderBy(n => n).ToArray();
        Assert.All(numbers, n => Assert.True(n > 0));
        for (var i = 1; i < numbers.Length; i++)
            Assert.Equal(numbers[i - 1] + 1, numbers[i]);
    }

    private async Task<string> CreateAsync(string content)
    {
        var added = await _harness.AddCompatAsync(new MemoryAddRequest
        {
            Category = "ver",
            Text = content,
            Title = content,
            Content = content,
        }, cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);
        Assert.True(added.Success, added.Error);
        return added.Memory!.Id;
    }
}
