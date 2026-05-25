using McpServer.Support.Mcp.Storage;
using McpServer.Support.Mcp.Storage.Entities;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace McpServer.Support.Mcp.Tests.Services;

/// <summary>
/// FR-MCP-079, TR-GRAPHRAG-ADHOC-002: Validates GraphEntityEntity persistence,
/// workspace isolation, cascade delete behavior, and duplicate name tolerance
/// using an in-memory EF Core database.
/// </summary>
public sealed class GraphRagEntityStorageTests : IDisposable
{
    private const string WorkspaceA = @"E:\tests\graph-entity-ws-a";
    private const string WorkspaceB = @"E:\tests\graph-entity-ws-b";

    private readonly McpDbContext _db;

    /// <summary>Creates an in-memory database for each test instance.</summary>
    public GraphRagEntityStorageTests()
    {
        var options = new DbContextOptionsBuilder<McpDbContext>()
            .UseInMemoryDatabase($"GraphEntityTests_{Guid.NewGuid():N}")
            .Options;
        _db = new McpDbContext(options);
        _db.Database.EnsureCreated();
        _db.OverrideWorkspaceId(WorkspaceA);
    }

    /// <summary>Disposes the database context.</summary>
    public void Dispose() => _db.Dispose();

    /// <summary>
    /// FR-MCP-079: Verifies that a GraphEntityEntity with all fields populated
    /// is persisted and retrievable from the database.
    /// </summary>
    [Fact]
    public async Task PersistsEntityWithAllFields()
    {
        var now = DateTime.UtcNow;
        var entity = new GraphEntityEntity
        {
            Id = $"ge-{Guid.NewGuid():N}",
            Name = "Alice",
            EntityType = "person",
            Description = "Test person entity",
            Metadata = """{"role":"engineer"}""",
            CreatedAtUtc = now,
            UpdatedAtUtc = now,
        };

        _db.GraphEntities.Add(entity);
        await _db.SaveChangesAsync().ConfigureAwait(true);

        var stored = await _db.GraphEntities.FirstAsync(e => e.Id == entity.Id).ConfigureAwait(true);
        Assert.Equal("Alice", stored.Name);
        Assert.Equal("person", stored.EntityType);
        Assert.Equal("Test person entity", stored.Description);
        Assert.Equal("""{"role":"engineer"}""", stored.Metadata);
        Assert.Equal(now, stored.CreatedAtUtc);
        Assert.Equal(now, stored.UpdatedAtUtc);
    }

    /// <summary>
    /// TR-MCP-MT-003: Verifies that SaveChanges auto-stamps the WorkspaceId
    /// from the current workspace context when the entity has an empty WorkspaceId.
    /// </summary>
    [Fact]
    public async Task AutoStampsWorkspaceIdOnAdd()
    {
        var entity = new GraphEntityEntity
        {
            Id = $"ge-{Guid.NewGuid():N}",
            Name = "AutoStamped",
            EntityType = "concept",
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow,
        };

        _db.GraphEntities.Add(entity);
        await _db.SaveChangesAsync().ConfigureAwait(true);

        var stored = await _db.GraphEntities.FirstAsync(e => e.Id == entity.Id).ConfigureAwait(true);
        Assert.Equal(WorkspaceA, stored.WorkspaceId);
    }

    /// <summary>
    /// TR-MCP-MT-003: Verifies that entities in workspace A are not visible
    /// when the context is switched to workspace B.
    /// </summary>
    [Fact]
    public async Task WorkspaceIsolation_EntitiesNotVisibleAcrossWorkspaces()
    {
        var entity = new GraphEntityEntity
        {
            Id = $"ge-{Guid.NewGuid():N}",
            Name = "Workspace-A-Only",
            EntityType = "organization",
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow,
        };

        _db.GraphEntities.Add(entity);
        await _db.SaveChangesAsync().ConfigureAwait(true);

        // Switch to workspace B — the entity should not be visible.
        _db.OverrideWorkspaceId(WorkspaceB);

        var visible = await _db.GraphEntities
            .Where(e => e.Id == entity.Id)
            .ToListAsync()
            .ConfigureAwait(true);

        Assert.Empty(visible);
    }

    /// <summary>
    /// FR-MCP-079: Verifies that deleting a GraphEntityEntity cascades to
    /// its source and target relationships.
    /// </summary>
    [Fact]
    public async Task CascadeDelete_RemovesSourceAndTargetRelationships()
    {
        var entityA = new GraphEntityEntity
        {
            Id = $"ge-{Guid.NewGuid():N}",
            Name = "EntityA",
            EntityType = "concept",
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow,
        };
        var entityB = new GraphEntityEntity
        {
            Id = $"ge-{Guid.NewGuid():N}",
            Name = "EntityB",
            EntityType = "concept",
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow,
        };
        var relationship = new GraphRelationshipEntity
        {
            Id = $"gr-{Guid.NewGuid():N}",
            SourceEntityId = entityA.Id,
            TargetEntityId = entityB.Id,
            RelationshipType = "depends_on",
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow,
        };

        _db.GraphEntities.AddRange(entityA, entityB);
        _db.GraphRelationships.Add(relationship);
        await _db.SaveChangesAsync().ConfigureAwait(true);

        // Verify the relationship exists.
        Assert.Single(await _db.GraphRelationships.ToListAsync().ConfigureAwait(true));

        // Delete the source entity - DB-FK-001 soft-deletes related relationships
        // instead of physically cascading durable graph state.
        _db.GraphEntities.Remove(entityA);
        await _db.SaveChangesAsync().ConfigureAwait(true);

        var remaining = await _db.GraphRelationships.ToListAsync().ConfigureAwait(true);
        Assert.Empty(remaining);

        var retained = await _db.GraphRelationships
            .IgnoreQueryFilters()
            .SingleAsync(r => r.Id == relationship.Id)
            .ConfigureAwait(true);
        Assert.True((bool)_db.Entry(retained).Property("IsDeleted").CurrentValue!);
    }

    /// <summary>
    /// FR-MCP-079: Verifies that multiple entities with the same Name can coexist
    /// because the schema does not enforce unique names.
    /// </summary>
    [Fact]
    public async Task DuplicateNamesAreAllowed()
    {
        var entity1 = new GraphEntityEntity
        {
            Id = $"ge-{Guid.NewGuid():N}",
            Name = "DuplicateName",
            EntityType = "person",
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow,
        };
        var entity2 = new GraphEntityEntity
        {
            Id = $"ge-{Guid.NewGuid():N}",
            Name = "DuplicateName",
            EntityType = "organization",
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow,
        };

        _db.GraphEntities.AddRange(entity1, entity2);
        await _db.SaveChangesAsync().ConfigureAwait(true);

        var count = await _db.GraphEntities
            .Where(e => e.Name == "DuplicateName")
            .CountAsync()
            .ConfigureAwait(true);

        Assert.Equal(2, count);
    }
}
