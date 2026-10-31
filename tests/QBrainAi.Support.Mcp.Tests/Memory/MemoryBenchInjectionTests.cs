namespace QBrainAi.Support.Mcp.Tests.Memory;

/// <summary>
/// TEST-MCP-MEMORY-018 (historical) / FR-MCP-MEMORY-018-14 (historical): REQUIRED MEMORIES appears only with_memory.
/// </summary>
public sealed class MemoryBenchInjectionTests
{
    /// <summary>AC-FR-MCP-MEMORY-018-14: Injection header is present only when memory is on.</summary>
    /// <remarks>Integration: loads the benchmark catalog, recorded pack or benchmark artifacts from the physical checkout (TR-MCP-USECASE-018 AC-006).</remarks>
    [Trait("Category", "Integration")]
    [Fact]
    public void InjectionPresentOnlyWithMemory()
    {
        var result = MemoryBenchCatalog.StubGrok();
        Assert.All(result.Cells.Where(cell => cell.Condition == "with_memory"), cell =>
        {
            Assert.True(cell.InjectionPresent);
            Assert.Contains("REQUIRED MEMORIES", cell.Transcript, StringComparison.Ordinal);
        });
        Assert.All(result.Cells.Where(cell => cell.Condition == "without_memory"), cell =>
        {
            Assert.False(cell.InjectionPresent);
            Assert.DoesNotContain("BENCH-PREF-EDITOR=neovim", cell.Transcript, StringComparison.Ordinal);
        });
    }
}
