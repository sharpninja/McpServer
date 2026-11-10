using QBrainAi.Support.Mcp.Services;
using QBrainAi.Support.Mcp.Tests.TestSupport;

namespace QBrainAi.Support.Mcp.Tests.Memory;

/// <summary>
/// TEST-MCP-MEMORY-011; TR-MCP-MEMORY-SEARCH-002; FR-MCP-MEMORY-011; TR-MCP-USECASE-018 AC-006; TR-MCP-USECASE-019 AC-008: the per-test state and helpers that both halves of MemoryIndexerTests use (the unit half in QBrainAi.Support.Mcp.Tests, the integration half in QBrainAi.Support.Mcp.EnvironmentIntegrationTests), defined once in a base class both halves derive from and compiled into both projects.
/// </summary>
/// <remarks>2026-11-07, TEST-MCP-MEMORY-011; TR-MCP-MEMORY-SEARCH-002; FR-MCP-MEMORY-011; TR-MCP-USECASE-018 AC-006; TR-MCP-USECASE-019 AC-008; FR-MCP-BUILD-001; TR-MCP-NUKE-019 AC-7: extracted unchanged from MemoryIndexerTests (_harness, Dispose and SeedAsync) when that class split into its unit half and its integration half (slice S7, hostile validation finding R14-F4), so both halves use one definition instead of a copy (DRY); only the accessibility changed, from private to internal; xUnit still creates one instance of a half per case, so each case gets fresh state as before.</remarks>
public abstract class MemoryIndexerTestsBase : IDisposable
{
    internal readonly MemoryS1Harness _harness = new();

    /// <inheritdoc />
    public void Dispose() => _harness.Dispose();

    internal async Task<MemoryItem> SeedAsync(string content, string? workspacePath = null)
    {
        var added = await _harness.AddCompatAsync(new MemoryAddRequest
        {
            Category = "fact",
            Scope = MemoryScope.Workspace,
            Text = content,
            Content = content,
            Type = "fact",
        }, workspacePath: workspacePath, cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);
        Assert.True(added.Success, added.Error);
        Assert.NotNull(added.Memory);
        return added.Memory!;
    }
}
