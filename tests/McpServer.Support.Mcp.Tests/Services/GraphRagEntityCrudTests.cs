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
/// FR-MCP-079, TR-GRAPHRAG-ADHOC-002: Validates entity CRUD operations
/// (Create, Get, Update, List, Delete) in <see cref="GraphRagService"/>
/// using an in-memory EF Core database.
/// </summary>
public sealed class GraphRagEntityCrudTests : IDisposable
{
    private const string WorkspacePath = @"E:\tests\graphrag-entity-crud";

    private readonly McpDbContext _db;
    private readonly string _tempWorkspacePath;

    /// <summary>Initializes in-memory DB for each test.</summary>
    public GraphRagEntityCrudTests()
    {
        var options = new DbContextOptionsBuilder<McpDbContext>()
            .UseInMemoryDatabase($"EntityCrudTests_{Guid.NewGuid():N}")
            .Options;
        _db = new McpDbContext(options);
        _db.Database.EnsureCreated();
        _db.OverrideWorkspaceId(WorkspacePath);

        _tempWorkspacePath = Path.Combine(Path.GetTempPath(), $"graphrag-entity-crud-{Guid.NewGuid():N}");
        Directory.CreateDirectory(_tempWorkspacePath);
    }

    /// <summary>Disposes DB and cleans up temp directory.</summary>
    public void Dispose()
    {
        _db.Dispose();
        try { if (Directory.Exists(_tempWorkspacePath)) Directory.Delete(_tempWorkspacePath, true); } catch { /* best-effort */ }
    }

    /// <summary>
    /// FR-MCP-079: Verifies that CreateEntityAsync generates an ID with "ge-" prefix.
    /// </summary>
    [Fact]
    public async Task CreateEntity_GeneratesIdWithPrefix()
    {
        var sut = CreateSut();
        var request = new GraphEntityRequest { Name = "Alice", EntityType = "person" };

        var result = await sut.CreateEntityAsync(request).ConfigureAwait(true);

        Assert.StartsWith("ge-", result.Id, StringComparison.Ordinal);
        Assert.Equal("Alice", result.Name);
        Assert.Equal("person", result.EntityType);
    }

    /// <summary>
    /// FR-MCP-079: Verifies that CreateEntityAsync sets both CreatedAtUtc and UpdatedAtUtc.
    /// </summary>
    [Fact]
    public async Task CreateEntity_SetsTimestamps()
    {
        var sut = CreateSut();
        var before = DateTime.UtcNow.AddSeconds(-1);
        var request = new GraphEntityRequest { Name = "Bob", EntityType = "person" };

        var result = await sut.CreateEntityAsync(request).ConfigureAwait(true);
        var after = DateTime.UtcNow.AddSeconds(1);

        Assert.InRange(result.CreatedAtUtc, before, after);
        Assert.InRange(result.UpdatedAtUtc, before, after);
        Assert.Equal(result.CreatedAtUtc, result.UpdatedAtUtc);
    }

    /// <summary>
    /// FR-MCP-079: Verifies that GetEntityAsync returns null for a nonexistent entity.
    /// </summary>
    [Fact]
    public async Task GetEntity_ReturnsNullForNonexistent()
    {
        var sut = CreateSut();

        var result = await sut.GetEntityAsync("ge-nonexistent").ConfigureAwait(true);

        Assert.Null(result);
    }

    /// <summary>
    /// FR-MCP-079: Verifies that UpdateEntityAsync modifies fields and bumps UpdatedAtUtc.
    /// </summary>
    [Fact]
    public async Task UpdateEntity_ModifiesFieldsAndBumpsTimestamp()
    {
        var sut = CreateSut();
        var created = await sut.CreateEntityAsync(new GraphEntityRequest { Name = "Original", EntityType = "concept" }).ConfigureAwait(true);

        // Small delay to ensure timestamp difference
        await Task.Delay(10).ConfigureAwait(true);

        var updated = await sut.UpdateEntityAsync(created.Id, new GraphEntityRequest
        {
            Name = "Updated",
            EntityType = "organization",
            Description = "Updated description",
            Metadata = """{"key":"value"}"""
        }).ConfigureAwait(true);

        Assert.NotNull(updated);
        Assert.Equal("Updated", updated!.Name);
        Assert.Equal("organization", updated.EntityType);
        Assert.Equal("Updated description", updated.Description);
        Assert.Equal("""{"key":"value"}""", updated.Metadata);
        Assert.True(updated.UpdatedAtUtc >= created.UpdatedAtUtc);
    }

    /// <summary>
    /// FR-MCP-079: Verifies that ListEntitiesAsync paginates correctly.
    /// </summary>
    [Fact]
    public async Task ListEntities_PaginatesCorrectly()
    {
        var sut = CreateSut();
        for (var i = 0; i < 5; i++)
        {
            await sut.CreateEntityAsync(new GraphEntityRequest { Name = $"Entity{i}", EntityType = "concept" }).ConfigureAwait(true);
        }

        var page1 = await sut.ListEntitiesAsync(skip: 0, take: 3).ConfigureAwait(true);
        var page2 = await sut.ListEntitiesAsync(skip: 3, take: 3).ConfigureAwait(true);

        Assert.Equal(3, page1.Entities.Count);
        Assert.Equal(5, page1.TotalCount);
        Assert.Equal(2, page2.Entities.Count);
        Assert.Equal(5, page2.TotalCount);
    }

    /// <summary>
    /// FR-MCP-079: Verifies that ListEntitiesAsync filters by entity type.
    /// </summary>
    [Fact]
    public async Task ListEntities_FiltersByEntityType()
    {
        var sut = CreateSut();
        await sut.CreateEntityAsync(new GraphEntityRequest { Name = "Person1", EntityType = "person" }).ConfigureAwait(true);
        await sut.CreateEntityAsync(new GraphEntityRequest { Name = "Org1", EntityType = "organization" }).ConfigureAwait(true);
        await sut.CreateEntityAsync(new GraphEntityRequest { Name = "Person2", EntityType = "person" }).ConfigureAwait(true);

        var result = await sut.ListEntitiesAsync(entityType: "person").ConfigureAwait(true);

        Assert.Equal(2, result.Entities.Count);
        Assert.Equal(2, result.TotalCount);
        Assert.All(result.Entities, e => Assert.Equal("person", e.EntityType));
    }

    /// <summary>
    /// FR-MCP-079: Verifies that DeleteEntityAsync returns true and removes the entity.
    /// </summary>
    [Fact]
    public async Task DeleteEntity_ReturnsTrueAndRemoves()
    {
        var sut = CreateSut();
        var created = await sut.CreateEntityAsync(new GraphEntityRequest { Name = "ToDelete", EntityType = "concept" }).ConfigureAwait(true);

        var deleted = await sut.DeleteEntityAsync(created.Id).ConfigureAwait(true);

        Assert.True(deleted);
        var check = await sut.GetEntityAsync(created.Id).ConfigureAwait(true);
        Assert.Null(check);
    }

    /// <summary>
    /// FR-MCP-079: Verifies that DeleteEntityAsync returns false for a nonexistent entity.
    /// </summary>
    [Fact]
    public async Task DeleteEntity_ReturnsFalseForNonexistent()
    {
        var sut = CreateSut();

        var deleted = await sut.DeleteEntityAsync("ge-nonexistent").ConfigureAwait(true);

        Assert.False(deleted);
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
