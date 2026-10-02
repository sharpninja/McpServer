using McpServer.Support.Mcp.Services;

namespace McpServer.Support.Mcp.Tests.Memory;

/// <summary>
/// TEST-MCP-MEMORY-016 / TR-MCP-MEMORY-API-002:
/// Idempotency-Key is documented as unsupported; duplicate posts create duplicates.
/// </summary>
public sealed class MemoryApiIdempotencyTests : IDisposable
{
    private readonly MemoryS1Harness _harness = new();

    /// <inheritdoc />
    public void Dispose() => _harness.Dispose();

    /// <summary>AC-TR-MCP-MEMORY-API-002-10: Idempotency-Key is not supported; duplicates are predictable.</summary>
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
