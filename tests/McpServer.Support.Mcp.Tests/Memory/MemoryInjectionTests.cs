using McpServer.Support.Mcp.Services;

namespace McpServer.Support.Mcp.Tests.Memory;

/// <summary>
/// TEST-MCP-MEMORY-016 / FR-MCP-MEMORY-010: REQUIRED MEMORIES injection uses raw Content only.
/// </summary>
public sealed class MemoryInjectionTests : IDisposable
{
    private readonly MemoryS1Harness _harness = new();

    /// <inheritdoc />
    public void Dispose() => _harness.Dispose();

    /// <summary>AC-FR-MCP-MEMORY-010-44: REQUIRED MEMORIES uses Content (or legacy text) raw; Summary/confidence/tags never appear.</summary>
    [Fact]
    public async Task RequiredBlock_RawContentOnly()
    {
        var added = await _harness.AddCompatAsync(new MemoryAddRequest
        {
            Category = "inj",
            Text = "RAW-CONTENT-ONLY",
            Title = "Inject title",
            Summary = "SUMMARY-MUST-NOT-APPEAR",
            Content = "RAW-CONTENT-ONLY",
            Type = "fact",
            Tags = ["tag-must-not-appear"],
            Confidence = 0.42,
        }, cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);
        Assert.True(added.Success, added.Error);

        var list = await _harness.ListCompatAsync(cancellationToken: TestContext.Current.CancellationToken)
            .ConfigureAwait(true);
        var block = _harness.RenderRequiredMemories(list.Items);

        Assert.Contains("REQUIRED MEMORIES", block, StringComparison.Ordinal);
        Assert.Contains("RAW-CONTENT-ONLY", block, StringComparison.Ordinal);
        Assert.DoesNotContain("SUMMARY-MUST-NOT-APPEAR", block, StringComparison.Ordinal);
        Assert.DoesNotContain("tag-must-not-appear", block, StringComparison.Ordinal);
        Assert.DoesNotContain("0.42", block, StringComparison.Ordinal);
        Assert.DoesNotContain("Inject title", block, StringComparison.Ordinal);
    }

    /// <summary>AC-FR-MCP-MEMORY-010-45: Empty Effective set still renders REQUIRED MEMORIES / - None.</summary>
    [Fact]
    public void EmptySet_RendersNone()
    {
        var block = _harness.RenderRequiredMemories([]);

        Assert.Contains("REQUIRED MEMORIES", block, StringComparison.Ordinal);
        Assert.Contains("- None", block, StringComparison.Ordinal);
    }
}
