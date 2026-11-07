using QBrainAi.Support.Mcp.Services;

namespace QBrainAi.Support.Mcp.EnvironmentIntegrationTests.Memory;

/// <summary>
/// TEST-MCP-MEMORY-016 (historical) / TR-MCP-MEMORY-API-002 (historical):
/// Idempotency-Key is documented as unsupported; duplicate posts create duplicates.
/// </summary>
/// <remarks>2026-11-07, TEST-MCP-MEMORY-016; TR-MCP-MEMORY-API-002; TR-MCP-USECASE-018 AC-006; FR-MCP-BUILD-001; TR-MCP-NUKE-019 AC-7: moved whole from QBrainAi.Support.Mcp.Tests (Memory/MemoryApiIdempotencyTests.cs) into the integration-owned QBrainAi.Support.Mcp.EnvironmentIntegrationTests, so the unit-owned project holds only unit cases (slice S6, hostile validation finding R14-F4; operator 2026-11-06: "...Environment.IntegrationTests (Recommended)"); MemoryS1Harness and MemoryS5Catalog, which unit cases share, are linked into this assembly; only the namespace changed.</remarks>
public sealed class MemoryApiIdempotencyTests : IDisposable
{
    private readonly MemoryS1Harness _harness = new();

    /// <inheritdoc />
    public void Dispose() => _harness.Dispose();

    /// <summary>AC-TR-MCP-MEMORY-API-002-10: Idempotency-Key is not supported; duplicates are predictable.</summary>
    /// <remarks>Integration: exercises native filesystem reads or writes for this case instead of only scripted in-memory data (TR-MCP-USECASE-018 AC-006).</remarks>
    [Trait("Category", "Integration")]
    [Fact]
    public async Task DocumentedBehavior()
    {
        var docs = File.ReadAllText(Path.Combine(MemoryS5Catalog.FindRepoRoot(), "docs", "context", "memory.md"));
        Assert.Contains("Idempotency-Key", docs, StringComparison.Ordinal);
        Assert.Contains("not supported", docs, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("duplicate", docs, StringComparison.OrdinalIgnoreCase);

        var first = await _harness.RememberAsync(
            new MemoryRememberRequest { Content = "s5 idempotency duplicate", Type = "fact" },
            cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);
        var second = await _harness.RememberAsync(
            new MemoryRememberRequest { Content = "s5 idempotency duplicate", Type = "fact" },
            cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);

        Assert.Equal(201, first.StatusCode);
        Assert.Equal(201, second.StatusCode);
        Assert.False(string.IsNullOrWhiteSpace(first.MemoryId));
        Assert.False(string.IsNullOrWhiteSpace(second.MemoryId));
        Assert.NotEqual(first.MemoryId, second.MemoryId);
    }
}
