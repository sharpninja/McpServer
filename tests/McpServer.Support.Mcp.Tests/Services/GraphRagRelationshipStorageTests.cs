using McpServer.Support.Mcp.Storage;
using McpServer.Support.Mcp.Storage.Entities;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace McpServer.Support.Mcp.Tests.Services;

/// <summary>
/// FR-MCP-079, TR-GRAPHRAG-ADHOC-002: Validates GraphRelationshipEntity persistence,
/// foreign key behavior, cascade delete from parent entities, and default weight value
/// using an in-memory EF Core database.
/// </summary>
public sealed class GraphRagRelationshipStorageTests : IDisposable
{
    private const string WorkspacePath = @"E:\tests\graph-rel-ws";

    private readonly McpDbContext _db;

    /// <summary>Creates an in-memory database for each test instance.</summary>
    public GraphRagRelationshipStorageTests()
    {
        var options = new DbContextOptionsBuilder<McpDbContext>()
            .UseInMemoryDatabase($"GraphRelTests_{Guid.NewGuid():N}")
            .Options;
        _db = new McpDbContext(options);
        _db.Database.EnsureCreated();
        _db.OverrideWorkspaceId(WorkspacePath);
    }

    /// <summary>Disposes the database context.</summary>
    public void Dispose() => _db.Dispose();

    /// <summary>
    /// FR-MCP-079: Verifies that a relationship persists correctly with foreign key
    /// references to source and target entities.
    /// </summary>
    [Fact]
    public async Task PersistsWithForeignKeysToSourceAndTarget()
    {
        var source = CreateEntity("Source");
        var target = CreateEntity("Target");
        _db.GraphEntities.AddRange(source, target);

        var now = DateTime.UtcNow;
        var rel = new GraphRelationshipEntity
        {
            Id = $"gr-{Guid.NewGuid():N}",
            SourceEntityId = source.Id,
            TargetEntityId = target.Id,
            RelationshipType = "authored_by",
            Description = "Source authored by Target",
            Weight = 0.85,
            Metadata = """{"confidence":0.95}""",
            CreatedAtUtc = now,
            UpdatedAtUtc = now,
        };
        _db.GraphRelationships.Add(rel);
        await _db.SaveChangesAsync().ConfigureAwait(true);

        var stored = await _db.GraphRelationships
            .Include(r => r.SourceEntity)
            .Include(r => r.TargetEntity)
            .FirstAsync(r => r.Id == rel.Id)
            .ConfigureAwait(true);

        Assert.Equal(source.Id, stored.SourceEntityId);
        Assert.Equal(target.Id, stored.TargetEntityId);
        Assert.Equal("authored_by", stored.RelationshipType);
        Assert.Equal("Source authored by Target", stored.Description);
        Assert.Equal(0.85, stored.Weight);
        Assert.Equal("""{"confidence":0.95}""", stored.Metadata);
        Assert.NotNull(stored.SourceEntity);
        Assert.NotNull(stored.TargetEntity);
        Assert.Equal("Source", stored.SourceEntity!.Name);
        Assert.Equal("Target", stored.TargetEntity!.Name);
    }

    /// <summary>
    /// FR-MCP-079: Verifies that the model correctly configures the FK relationship
    /// from GraphRelationshipEntity to GraphEntityEntity. When both source and target
    /// entities exist and are loaded via Include, the navigation properties are populated.
    /// </summary>
    [Fact]
    public async Task ForeignKeyNavigation_LoadsSourceAndTargetEntities()
    {
        var source = CreateEntity("FKSource");
        var target = CreateEntity("FKTarget");
        _db.GraphEntities.AddRange(source, target);

        var rel = new GraphRelationshipEntity
        {
            Id = $"gr-{Guid.NewGuid():N}",
            SourceEntityId = source.Id,
            TargetEntityId = target.Id,
            RelationshipType = "depends_on",
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow,
        };
        _db.GraphRelationships.Add(rel);
        await _db.SaveChangesAsync().ConfigureAwait(true);

        var stored = await _db.GraphRelationships
            .Include(r => r.SourceEntity)
            .Include(r => r.TargetEntity)
            .FirstAsync(r => r.Id == rel.Id)
            .ConfigureAwait(true);

        Assert.NotNull(stored.SourceEntity);
        Assert.NotNull(stored.TargetEntity);
        Assert.Equal("FKSource", stored.SourceEntity!.Name);
        Assert.Equal("FKTarget", stored.TargetEntity!.Name);
    }

    /// <summary>
    /// FR-MCP-079: Verifies cascade delete — deleting a source entity removes
    /// all relationships where that entity is the source.
    /// </summary>
    [Fact]
    public async Task CascadeFromEntityDelete_RemovesRelationship()
    {
        var source = CreateEntity("CascadeSource");
        var target = CreateEntity("CascadeTarget");
        _db.GraphEntities.AddRange(source, target);

        var rel = new GraphRelationshipEntity
        {
            Id = $"gr-{Guid.NewGuid():N}",
            SourceEntityId = source.Id,
            TargetEntityId = target.Id,
            RelationshipType = "references",
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow,
        };
        _db.GraphRelationships.Add(rel);
        await _db.SaveChangesAsync().ConfigureAwait(true);

        _db.GraphEntities.Remove(source);
        await _db.SaveChangesAsync().ConfigureAwait(true);

        var remaining = await _db.GraphRelationships.ToListAsync().ConfigureAwait(true);
        Assert.Empty(remaining);
    }

    /// <summary>
    /// FR-MCP-079: Verifies that the Weight property defaults to 1.0 when
    /// not explicitly set.
    /// </summary>
    [Fact]
    public async Task WeightDefaultsToOne()
    {
        var source = CreateEntity("WeightSource");
        var target = CreateEntity("WeightTarget");
        _db.GraphEntities.AddRange(source, target);

        var rel = new GraphRelationshipEntity
        {
            Id = $"gr-{Guid.NewGuid():N}",
            SourceEntityId = source.Id,
            TargetEntityId = target.Id,
            RelationshipType = "links_to",
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow,
            // Weight not set — should default to 1.0
        };
        _db.GraphRelationships.Add(rel);
        await _db.SaveChangesAsync().ConfigureAwait(true);

        var stored = await _db.GraphRelationships
            .FirstAsync(r => r.Id == rel.Id)
            .ConfigureAwait(true);

        Assert.Equal(1.0, stored.Weight);
    }

    private static GraphEntityEntity CreateEntity(string name) => new()
    {
        Id = $"ge-{Guid.NewGuid():N}",
        Name = name,
        EntityType = "concept",
        CreatedAtUtc = DateTime.UtcNow,
        UpdatedAtUtc = DateTime.UtcNow,
    };
}
