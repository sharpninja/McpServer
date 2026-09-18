using McpServer.Support.Mcp.Storage.Entities;
using Microsoft.EntityFrameworkCore;

namespace McpServer.Support.Mcp.Tests.Memory;

/// <summary>
/// TEST-MCP-MEMORY-010 / TR-MCP-MEMORY-MODEL-002: Unique (From, To, EdgeType) at the DB layer.
/// </summary>
public sealed class MemoryEdgeTests : IDisposable
{
    private readonly MemoryS1Harness _harness = new();

    /// <inheritdoc />
    public void Dispose() => _harness.Dispose();

    /// <summary>AC-TR-MCP-MEMORY-MODEL-002-03: Unique (FromMemoryId, ToMemoryId, EdgeType) is enforced at the DB layer.</summary>
    [Fact]
    public async Task UniqueFromToType_Enforced()
    {
        Assert.True(_harness.HasUniqueEdgeIndex());
        var entity = _harness.FindMappedEntity(typeof(MemoryEdgeEntity));
        Assert.NotNull(entity);

        await using var db = _harness.CreateContext(_harness.WorkspaceA);
        db.Set<MemoryEdgeEntity>().Add(new MemoryEdgeEntity
        {
            FromMemoryId = "MEMORY-A-001",
            ToMemoryId = "MEMORY-B-001",
            EdgeType = "explicit",
            Weight = 1,
        });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken).ConfigureAwait(true);

        db.Set<MemoryEdgeEntity>().Add(new MemoryEdgeEntity
        {
            FromMemoryId = "MEMORY-A-001",
            ToMemoryId = "MEMORY-B-001",
            EdgeType = "explicit",
            Weight = 0.5,
        });
        await Assert.ThrowsAnyAsync<DbUpdateException>(() =>
            db.SaveChangesAsync(TestContext.Current.CancellationToken)).ConfigureAwait(true);
    }
}
