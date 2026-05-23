using McpServer.Support.Mcp.Indexing;
using McpServer.Support.Mcp.Ingestion;
using McpServer.Support.Mcp.Models;
using McpServer.Support.Mcp.Options;
using McpServer.Support.Mcp.Services;
using McpServer.Support.Mcp.Storage;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Xunit;

namespace McpServer.Support.Mcp.Tests.Services;

/// <summary>
/// FR-MCP-079, TR-GRAPHRAG-ADHOC-002: Validates relationship CRUD operations
/// (Create, Get, Update, List, Delete) in <see cref="GraphRagService"/>
/// using an in-memory EF Core database.
/// </summary>
public sealed class GraphRagRelationshipCrudTests : IDisposable
{
    private const string WorkspacePath = @"E:\tests\graphrag-rel-crud";

    private readonly McpDbContext _db;
    private readonly string _tempWorkspacePath;

    /// <summary>Initializes in-memory DB for each test.</summary>
    public GraphRagRelationshipCrudTests()
    {
        var options = new DbContextOptionsBuilder<McpDbContext>()
            .UseInMemoryDatabase($"RelCrudTests_{Guid.NewGuid():N}")
            .Options;
        _db = new McpDbContext(options);
        _db.Database.EnsureCreated();
        _db.OverrideWorkspaceId(WorkspacePath);

        _tempWorkspacePath = Path.Combine(Path.GetTempPath(), $"graphrag-rel-crud-{Guid.NewGuid():N}");
        Directory.CreateDirectory(_tempWorkspacePath);
    }

    /// <summary>Disposes DB and cleans up temp directory.</summary>
    public void Dispose()
    {
        _db.Dispose();
        try { if (Directory.Exists(_tempWorkspacePath)) Directory.Delete(_tempWorkspacePath, true); } catch { /* best-effort */ }
    }

    /// <summary>
    /// FR-MCP-079: Verifies that CreateRelationshipAsync generates an ID with "gr-" prefix.
    /// </summary>
    [Fact]
    public async Task CreateRelationship_GeneratesIdWithPrefix()
    {
        var sut = CreateSut();
        var entityA = await sut.CreateEntityAsync(new GraphEntityRequest { Name = "A", EntityType = "concept" }).ConfigureAwait(true);
        var entityB = await sut.CreateEntityAsync(new GraphEntityRequest { Name = "B", EntityType = "concept" }).ConfigureAwait(true);

        var result = await sut.CreateRelationshipAsync(new GraphRelationshipRequest
        {
            SourceEntityId = entityA.Id,
            TargetEntityId = entityB.Id,
            RelationshipType = "depends_on"
        }).ConfigureAwait(true);

        Assert.StartsWith("gr-", result.Id, StringComparison.Ordinal);
        Assert.Equal(entityA.Id, result.SourceEntityId);
        Assert.Equal(entityB.Id, result.TargetEntityId);
        Assert.Equal("depends_on", result.RelationshipType);
    }

    /// <summary>
    /// FR-MCP-079: Verifies that CreateRelationshipAsync throws when source entity does not exist.
    /// </summary>
    [Fact]
    public async Task CreateRelationship_ValidatesSourceEntityExists()
    {
        var sut = CreateSut();
        var entityB = await sut.CreateEntityAsync(new GraphEntityRequest { Name = "B", EntityType = "concept" }).ConfigureAwait(true);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            sut.CreateRelationshipAsync(new GraphRelationshipRequest
            {
                SourceEntityId = "ge-nonexistent",
                TargetEntityId = entityB.Id,
                RelationshipType = "depends_on"
            })).ConfigureAwait(true);
    }

    /// <summary>
    /// FR-MCP-079: Verifies that CreateRelationshipAsync throws when target entity does not exist.
    /// </summary>
    [Fact]
    public async Task CreateRelationship_ValidatesTargetEntityExists()
    {
        var sut = CreateSut();
        var entityA = await sut.CreateEntityAsync(new GraphEntityRequest { Name = "A", EntityType = "concept" }).ConfigureAwait(true);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            sut.CreateRelationshipAsync(new GraphRelationshipRequest
            {
                SourceEntityId = entityA.Id,
                TargetEntityId = "ge-nonexistent",
                RelationshipType = "depends_on"
            })).ConfigureAwait(true);
    }

    /// <summary>
    /// FR-MCP-079: Verifies that ListRelationshipsAsync filters by entity ID (source or target).
    /// </summary>
    [Fact]
    public async Task ListRelationships_FiltersByEntityId()
    {
        var sut = CreateSut();
        var a = await sut.CreateEntityAsync(new GraphEntityRequest { Name = "A", EntityType = "concept" }).ConfigureAwait(true);
        var b = await sut.CreateEntityAsync(new GraphEntityRequest { Name = "B", EntityType = "concept" }).ConfigureAwait(true);
        var c = await sut.CreateEntityAsync(new GraphEntityRequest { Name = "C", EntityType = "concept" }).ConfigureAwait(true);

        await sut.CreateRelationshipAsync(new GraphRelationshipRequest { SourceEntityId = a.Id, TargetEntityId = b.Id, RelationshipType = "uses" }).ConfigureAwait(true);
        await sut.CreateRelationshipAsync(new GraphRelationshipRequest { SourceEntityId = b.Id, TargetEntityId = c.Id, RelationshipType = "uses" }).ConfigureAwait(true);
        await sut.CreateRelationshipAsync(new GraphRelationshipRequest { SourceEntityId = a.Id, TargetEntityId = c.Id, RelationshipType = "depends_on" }).ConfigureAwait(true);

        var result = await sut.ListRelationshipsAsync(entityId: a.Id).ConfigureAwait(true);

        Assert.Equal(2, result.Relationships.Count);
        Assert.Equal(2, result.TotalCount);
    }

    /// <summary>
    /// FR-MCP-079: Verifies that ListRelationshipsAsync filters by relationship type.
    /// </summary>
    [Fact]
    public async Task ListRelationships_FiltersByRelationshipType()
    {
        var sut = CreateSut();
        var a = await sut.CreateEntityAsync(new GraphEntityRequest { Name = "A", EntityType = "concept" }).ConfigureAwait(true);
        var b = await sut.CreateEntityAsync(new GraphEntityRequest { Name = "B", EntityType = "concept" }).ConfigureAwait(true);

        await sut.CreateRelationshipAsync(new GraphRelationshipRequest { SourceEntityId = a.Id, TargetEntityId = b.Id, RelationshipType = "uses" }).ConfigureAwait(true);
        await sut.CreateRelationshipAsync(new GraphRelationshipRequest { SourceEntityId = b.Id, TargetEntityId = a.Id, RelationshipType = "depends_on" }).ConfigureAwait(true);

        var result = await sut.ListRelationshipsAsync(relationshipType: "uses").ConfigureAwait(true);

        Assert.Single(result.Relationships);
        Assert.Equal("uses", result.Relationships[0].RelationshipType);
    }

    /// <summary>
    /// FR-MCP-079: Verifies that UpdateRelationshipAsync modifies fields.
    /// </summary>
    [Fact]
    public async Task UpdateRelationship_ModifiesFields()
    {
        var sut = CreateSut();
        var a = await sut.CreateEntityAsync(new GraphEntityRequest { Name = "A", EntityType = "concept" }).ConfigureAwait(true);
        var b = await sut.CreateEntityAsync(new GraphEntityRequest { Name = "B", EntityType = "concept" }).ConfigureAwait(true);

        var created = await sut.CreateRelationshipAsync(new GraphRelationshipRequest
        {
            SourceEntityId = a.Id,
            TargetEntityId = b.Id,
            RelationshipType = "uses",
            Weight = 0.5
        }).ConfigureAwait(true);

        var updated = await sut.UpdateRelationshipAsync(created.Id, new GraphRelationshipRequest
        {
            SourceEntityId = a.Id,
            TargetEntityId = b.Id,
            RelationshipType = "depends_on",
            Description = "Updated relationship",
            Weight = 0.9,
            Metadata = """{"updated":true}"""
        }).ConfigureAwait(true);

        Assert.NotNull(updated);
        Assert.Equal("depends_on", updated!.RelationshipType);
        Assert.Equal("Updated relationship", updated.Description);
        Assert.Equal(0.9, updated.Weight);
        Assert.Equal("""{"updated":true}""", updated.Metadata);
    }

    /// <summary>
    /// FR-MCP-079: Verifies that DeleteRelationshipAsync returns true when found, false when not found.
    /// </summary>
    [Fact]
    public async Task DeleteRelationship_ReturnsTrueOrFalse()
    {
        var sut = CreateSut();
        var a = await sut.CreateEntityAsync(new GraphEntityRequest { Name = "A", EntityType = "concept" }).ConfigureAwait(true);
        var b = await sut.CreateEntityAsync(new GraphEntityRequest { Name = "B", EntityType = "concept" }).ConfigureAwait(true);

        var created = await sut.CreateRelationshipAsync(new GraphRelationshipRequest
        {
            SourceEntityId = a.Id,
            TargetEntityId = b.Id,
            RelationshipType = "uses"
        }).ConfigureAwait(true);

        var deletedExisting = await sut.DeleteRelationshipAsync(created.Id).ConfigureAwait(true);
        var deletedNonexistent = await sut.DeleteRelationshipAsync("gr-nonexistent").ConfigureAwait(true);

        Assert.True(deletedExisting);
        Assert.False(deletedNonexistent);
    }

    private GraphRagService CreateSut()
    {
        var graphRagOptions = Microsoft.Extensions.Options.Options.Create(new GraphRagOptions
        {
            Enabled = true,
            RootPath = "mcp-data/graphrag",
            ArtifactVersion = "v1"
        });
        var ingestionOptions = Microsoft.Extensions.Options.Options.Create(new IngestionOptions { RepoRoot = _tempWorkspacePath });
        var workspaceContext = new WorkspaceContext { WorkspacePath = _tempWorkspacePath };
        var contextSearch = Substitute.For<IContextSearchService>();
        contextSearch
            .SearchAsync(Arg.Any<string>(), Arg.Any<int>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(new ContextSearchResult([], []));
        var embeddingService = Substitute.For<IEmbeddingService>();
        embeddingService.Dimensions.Returns(384);
        embeddingService.IsAvailable.Returns(true);
        var vectorIndexService = Substitute.For<IVectorIndexService>();
        var adapters = new IGraphRagBackendAdapter[]
        {
            new InternalFallbackGraphRagBackendAdapter()
        };

        return new GraphRagService(
            graphRagOptions,
            ingestionOptions,
            workspaceContext,
            contextSearch,
            adapters,
            NullLogger<GraphRagService>.Instance,
            _db,
            embeddingService,
            vectorIndexService);
    }
}
