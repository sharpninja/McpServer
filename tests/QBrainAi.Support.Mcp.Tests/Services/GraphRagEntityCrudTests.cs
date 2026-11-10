using QBrainAi.Support.Mcp.Indexing;
using QBrainAi.Support.Mcp.Ingestion;
using QBrainAi.Support.Mcp.Models;
using QBrainAi.Support.Mcp.Options;
using QBrainAi.Support.Mcp.Services;
using QBrainAi.Support.Mcp.Storage;
using QBrainAi.Support.Mcp.Tests.TestSupport;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Xunit;

namespace QBrainAi.Support.Mcp.Tests.Services;

/// <summary>
/// FR-MCP-079, TR-GRAPHRAG-ADHOC-002: Validates entity CRUD operations
/// (Create, Get, Update, List, Delete) in <see cref="GraphRagService"/>
/// using an in-memory EF Core database.
/// </summary>
/// <remarks>
/// <list type="table">
/// <listheader><term>Date</term><description>Requirements; AC; change and reason</description></listheader>
/// <item><term>2026-11-03</term><description>FR-MCP-079; TR-MCP-USECASE-018 AC-006 (appended "This covers every recorded time a unit test checks, including the creation and update times a service stamps on a record it stores"): the timestamp cases give the service a ScriptedTimeProvider and assert the exact scripted times through StampedTimeContractTests, instead of a range around the live clock and a sleep between create and update, because the sibling sweep before round 11 of hostile validation round 10 finding R10-F3 found both (operator 2026-11-03: "don't gate on me between rounds").</description></item>
/// </list>
/// </remarks>
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

        var result = await sut.CreateEntityAsync(request, ct: TestContext.Current.CancellationToken).ConfigureAwait(true);

        Assert.StartsWith("ge-", result.Id, StringComparison.Ordinal);
        Assert.Equal("Alice", result.Name);
        Assert.Equal("person", result.EntityType);
    }

    /// <summary>
    /// FR-MCP-079: Verifies that CreateEntityAsync sets both CreatedAtUtc and UpdatedAtUtc to one reading of the
    /// service's clock, and that each UpdateEntityAsync sets UpdatedAtUtc to the next reading and keeps CreatedAtUtc,
    /// for every row of StampedTimeContractTests (scripted times, no live clock, no sleep).
    /// </summary>
    /// <remarks>2026-11-03, FR-MCP-079; TR-MCP-USECASE-018 AC-006 (appended): asserts the exact scripted times through StampedTimeContractTests.AssertStampsAsync instead of a range around DateTime.UtcNow (sibling sweep before round 11 of hostile validation round 10 finding R10-F3).</remarks>
    /// <returns>The test task.</returns>
    [Fact]
    public Task CreateEntity_SetsTimestamps() =>
        StampedTimeContractTests.AssertStampsAsync(async (clock, updates) =>
        {
            var sut = CreateSut(clock);
            var entity = await sut.CreateEntityAsync(new GraphEntityRequest { Name = "Bob", EntityType = "person" }, ct: TestContext.Current.CancellationToken).ConfigureAwait(true);
            for (var update = 0; update < updates; update++)
            {
                var updated = await sut.UpdateEntityAsync(entity.Id, new GraphEntityRequest { Name = $"Bob {update}", EntityType = "person" }, ct: TestContext.Current.CancellationToken).ConfigureAwait(true);
                Assert.NotNull(updated);
                entity = updated;
            }

            return (StampedTimeContractTests.AsUtc(entity.CreatedAtUtc), StampedTimeContractTests.AsUtc(entity.UpdatedAtUtc));
        });

    /// <summary>
    /// FR-MCP-079: Verifies that GetEntityAsync returns null for a nonexistent entity.
    /// </summary>
    [Fact]
    public async Task GetEntity_ReturnsNullForNonexistent()
    {
        var sut = CreateSut();

        var result = await sut.GetEntityAsync("ge-nonexistent", ct: TestContext.Current.CancellationToken).ConfigureAwait(true);

        Assert.Null(result);
    }

    /// <summary>
    /// FR-MCP-079: Verifies that UpdateEntityAsync modifies fields and bumps UpdatedAtUtc.
    /// </summary>
    /// <remarks>2026-11-03, FR-MCP-079; TR-MCP-USECASE-018 AC-006 (appended): the service reads a ScriptedTimeProvider (creation time, then update time), and the case asserts both exact times instead of sleeping 10 ms and checking that the update time did not go back (sibling sweep before round 11 of hostile validation round 10 finding R10-F3).</remarks>
    [Fact]
    public async Task UpdateEntity_ModifiesFieldsAndBumpsTimestamp()
    {
        var sut = CreateSut(new ScriptedTimeProvider(StampedTimeContractTests.Created, StampedTimeContractTests.FirstUpdate));
        var created = await sut.CreateEntityAsync(new GraphEntityRequest { Name = "Original", EntityType = "concept" }, ct: TestContext.Current.CancellationToken).ConfigureAwait(true);

        var updated = await sut.UpdateEntityAsync(created.Id, new GraphEntityRequest
        {
            Name = "Updated",
            EntityType = "organization",
            Description = "Updated description",
            Metadata = """{"key":"value"}"""
        }, ct: TestContext.Current.CancellationToken).ConfigureAwait(true);

        Assert.NotNull(updated);
        Assert.Equal("Updated", updated!.Name);
        Assert.Equal("organization", updated.EntityType);
        Assert.Equal("Updated description", updated.Description);
        Assert.Equal("""{"key":"value"}""", updated.Metadata);
        Assert.Equal(StampedTimeContractTests.Created, StampedTimeContractTests.AsUtc(created.UpdatedAtUtc));
        Assert.Equal(StampedTimeContractTests.Created, StampedTimeContractTests.AsUtc(updated.CreatedAtUtc));
        Assert.Equal(StampedTimeContractTests.FirstUpdate, StampedTimeContractTests.AsUtc(updated.UpdatedAtUtc));
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
            await sut.CreateEntityAsync(new GraphEntityRequest { Name = $"Entity{i}", EntityType = "concept" }, ct: TestContext.Current.CancellationToken).ConfigureAwait(true);
        }

        var page1 = await sut.ListEntitiesAsync(skip: 0, take: 3, ct: TestContext.Current.CancellationToken).ConfigureAwait(true);
        var page2 = await sut.ListEntitiesAsync(skip: 3, take: 3, ct: TestContext.Current.CancellationToken).ConfigureAwait(true);

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
        await sut.CreateEntityAsync(new GraphEntityRequest { Name = "Person1", EntityType = "person" }, ct: TestContext.Current.CancellationToken).ConfigureAwait(true);
        await sut.CreateEntityAsync(new GraphEntityRequest { Name = "Org1", EntityType = "organization" }, ct: TestContext.Current.CancellationToken).ConfigureAwait(true);
        await sut.CreateEntityAsync(new GraphEntityRequest { Name = "Person2", EntityType = "person" }, ct: TestContext.Current.CancellationToken).ConfigureAwait(true);

        var result = await sut.ListEntitiesAsync(entityType: "person", ct: TestContext.Current.CancellationToken).ConfigureAwait(true);

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
        var created = await sut.CreateEntityAsync(new GraphEntityRequest { Name = "ToDelete", EntityType = "concept" }, ct: TestContext.Current.CancellationToken).ConfigureAwait(true);

        var deleted = await sut.DeleteEntityAsync(created.Id, ct: TestContext.Current.CancellationToken).ConfigureAwait(true);

        Assert.True(deleted);
        var check = await sut.GetEntityAsync(created.Id, ct: TestContext.Current.CancellationToken).ConfigureAwait(true);
        Assert.Null(check);
    }

    /// <summary>
    /// FR-MCP-079: Verifies that DeleteEntityAsync returns false for a nonexistent entity.
    /// </summary>
    [Fact]
    public async Task DeleteEntity_ReturnsFalseForNonexistent()
    {
        var sut = CreateSut();

        var deleted = await sut.DeleteEntityAsync("ge-nonexistent", ct: TestContext.Current.CancellationToken).ConfigureAwait(true);

        Assert.False(deleted);
    }

    /// <summary>FR-MCP-079: the service under test over the in-memory database, stamping times from <paramref name="clock"/>.</summary>
    /// <param name="clock">The clock the service stamps entity times from; null keeps the service's system clock.</param>
    /// <returns>The service.</returns>
    /// <remarks>2026-11-03, FR-MCP-079; TR-MCP-USECASE-018 AC-006 (appended): takes the clock the timestamp cases script (sibling sweep before round 11 of hostile validation round 10 finding R10-F3).</remarks>
    private GraphRagService CreateSut(TimeProvider? clock = null)
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

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["DataFolder"] = _tempWorkspacePath })
            .Build();

        return new GraphRagService(
            graphRagOptions,
            ingestionOptions,
            configuration,
            workspaceContext,
            contextSearch,
            adapters,
            NullLogger<GraphRagService>.Instance,
            _db,
            embeddingService,
            vectorIndexService,
            clock);
    }
}
