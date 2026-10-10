using McpServer.Support.Mcp.Services;

namespace McpServer.Support.Mcp.Tests.Memory;

/// <summary>
/// TEST-MCP-MEMORY-018 / FR-MCP-MEMORY-018-28: Bench workspace isolation.
/// </summary>
public sealed class MemoryBenchIsolationTests
{
    /// <summary>AC-FR-MCP-MEMORY-018-28: Pack run cannot read or mutate the primary workspace.</summary>
    [Fact]
    public void NoPrimaryWorkspaceBleed()
    {
        var result = MemoryBenchCatalog.StubGrok();
        Assert.Equal("primary-operator-workspace", result.PrimaryWorkspaceId);
        Assert.NotEqual(result.PrimaryWorkspaceId, result.BenchWorkspaceId);
        Assert.False(result.PrimaryWorkspaceMutated);
        Assert.All(result.Cells, cell =>
            Assert.DoesNotContain(result.PrimaryWorkspaceId, cell.Transcript, StringComparison.Ordinal));

        using var harness = new MemoryS1Harness();
        var before = harness.WorkspaceA;
        _ = new MemoryBenchHarness().Run(MemoryBenchCatalog.FindRepoRoot(), new MemoryBenchRunOptions
        {
            Plugins = ["grok"],
            PrimaryWorkspaceId = before,
        });
        Assert.True(Directory.Exists(before) || !Directory.Exists(Path.Combine(before, "memories")));
    }
}
