using McpServer.Cqrs;
using McpServer.Support.Mcp.Products.Commands;
using McpServer.Support.Mcp.Products.Models;
using McpServer.Support.Mcp.Products.Queries;
using McpServer.Support.Mcp.Storage;
using McpServer.Support.Mcp.Storage.Entities;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace McpServer.Support.Mcp.Tests.Products;

/// <summary>
/// TEST-MCP-PRODUCT-006 / FR-MCP-PRODUCT-005: Product requirement context does not leak sibling source files.
/// Phase 4: member sibling FR body + origin; no sibling source files.
/// </summary>
public sealed class ProductRequirementContextTests : IDisposable
{
    private const string Owner = @"F:\GitHub\ctx-owner";
    private const string Sibling = @"F:\GitHub\ctx-sibling";
    private const string Outsider = @"F:\GitHub\ctx-outsider";
    private readonly SqliteConnection _connection;
    private readonly DbContextOptions<McpDbContext> _options;
    private readonly CallContext _ctx = new();

    /// <summary>Opens an isolated SQLite database and seeds product + sibling FR + sibling .cs chunk.</summary>
    public ProductRequirementContextTests()
    {
        _connection = new SqliteConnection("Data Source=:memory:");
        _connection.Open();
        _options = new DbContextOptionsBuilder<McpDbContext>()
            .UseSqlite(_connection)
            .Options;

        using var db = new McpDbContext(_options);
        db.Database.EnsureCreated();
        SeedWorkspace(db, Owner, "owner");
        SeedWorkspace(db, Sibling, "sibling");
        SeedWorkspace(db, Outsider, "outsider");
        SeedLayer(db, Owner, "layer-1", 1);
        SeedLayer(db, Sibling, "layer-1", 1);
        SeedLayer(db, Outsider, "layer-1", 1);
        SeedRequirement(db, Sibling, "FR-CTX-001", "Sibling context FR", "SIBLING-FR-BODY-UNIQUE");
        SeedRequirement(db, Owner, "FR-CTX-OWNER", "Owner FR", "OWNER-FR-BODY");
        db.Documents.Add(new ContextDocumentEntity
        {
            Id = "sib-cs",
            WorkspaceId = Sibling,
            SourceKey = @"F:\GitHub\ctx-sibling\src\Secret.cs",
            SourceType = "repo",
            ContentHash = "hash-secret",
        });
        db.Chunks.Add(new ContextChunkEntity
        {
            Id = "sib-cs-0",
            DocumentId = "sib-cs",
            WorkspaceId = Sibling,
            Content = "class Secret { }",
            ChunkIndex = 0,
        });
        db.SaveChanges();
    }

    /// <inheritdoc />
    public void Dispose() => _connection.Dispose();

    /// <summary>Member pack includes sibling FR body plus origin workspace tag.</summary>
    [Fact]
    public async Task HandleAsync_Member_IncludesSiblingFrBodyAndOrigin()
    {
        await using var db = new McpDbContext(_options);
        await CreateProductAsync(db);
        var result = await new GetProductRequirementContextQueryHandler(db)
            .HandleAsync(new GetProductRequirementContextQuery(Owner, null, "product-requirements"), _ctx);

        Assert.True(result.IsSuccess, result.Error);
        Assert.Contains(result.Value!, c =>
            c.Content.Contains("SIBLING-FR-BODY-UNIQUE", StringComparison.Ordinal)
            && c.OriginWorkspaceId == Sibling
            && c.SourceType == "product-requirements");
    }

    /// <summary>Member pack does not include sibling .cs source chunks.</summary>
    [Fact]
    public async Task HandleAsync_Member_DoesNotIncludeSiblingSourceFiles()
    {
        await using var db = new McpDbContext(_options);
        await CreateProductAsync(db);
        var result = await new GetProductRequirementContextQueryHandler(db)
            .HandleAsync(new GetProductRequirementContextQuery(Owner, null, "product-requirements"), _ctx);

        Assert.True(result.IsSuccess, result.Error);
        Assert.DoesNotContain(result.Value!, c =>
            c.Content.Contains("class Secret", StringComparison.Ordinal)
            || c.Content.Contains("Secret.cs", StringComparison.Ordinal));
    }

    /// <summary>Non-member pack does not contain sibling FR.</summary>
    [Fact]
    public async Task HandleAsync_Outsider_DoesNotIncludeSiblingFr()
    {
        await using var db = new McpDbContext(_options);
        await CreateProductAsync(db);
        var result = await new GetProductRequirementContextQueryHandler(db)
            .HandleAsync(new GetProductRequirementContextQuery(Outsider, null, "product-requirements"), _ctx);

        Assert.True(result.IsSuccess, result.Error);
        Assert.DoesNotContain(result.Value!, c =>
            c.Content.Contains("SIBLING-FR-BODY-UNIQUE", StringComparison.Ordinal));
    }

    /// <summary>Source type product-requirements returns only requirement chunks.</summary>
    [Fact]
    public async Task HandleAsync_ProductRequirementsSource_ReturnsOnlyRequirementChunks()
    {
        await using var db = new McpDbContext(_options);
        await CreateProductAsync(db);
        var result = await new GetProductRequirementContextQueryHandler(db)
            .HandleAsync(new GetProductRequirementContextQuery(Owner, null, "product-requirements"), _ctx);

        Assert.True(result.IsSuccess, result.Error);
        Assert.NotEmpty(result.Value!);
        Assert.All(result.Value!, c => Assert.Equal("product-requirements", c.SourceType));
    }

    private async Task CreateProductAsync(McpDbContext db)
    {
        var created = await new CreateProductCommandHandler(db).HandleAsync(
            new CreateProductCommand(Owner, new CreateProductRequest { Key = "PROD-MCPSERVER", Name = "Shared" }),
            _ctx);
        Assert.True(created.IsSuccess, created.Error);
        var added = await new AddProductMemberCommandHandler(db).HandleAsync(
            new AddProductMemberCommand(Owner, "PROD-MCPSERVER", Sibling),
            _ctx);
        Assert.True(added.IsSuccess, added.Error);
    }

    private static void SeedWorkspace(McpDbContext db, string id, string name)
    {
        db.Workspaces.Add(new WorkspaceEntity
        {
            WorkspaceId = id,
            WorkspacePath = id,
            Name = name,
            IsEnabled = true,
            CurrentRequirementLayerKey = "layer-1",
        });
    }

    private static void SeedLayer(McpDbContext db, string workspaceId, string key, int order)
    {
        db.RequirementScopeLayers.Add(new RequirementScopeLayerEntity
        {
            WorkspaceId = workspaceId,
            Key = key,
            Order = order,
            Name = key,
            CreatedAtUtc = DateTimeOffset.UtcNow,
            UpdatedAtUtc = DateTimeOffset.UtcNow,
        });
    }

    private static void SeedRequirement(McpDbContext db, string workspaceId, string id, string title, string body)
    {
        var now = DateTimeOffset.UtcNow.ToString("O");
        db.Requirements.Add(new RequirementEntity
        {
            WorkspaceId = workspaceId,
            Kind = "fr",
            Id = id,
            Title = title,
            Body = body,
            Priority = "medium",
            Status = "pending",
            ScopeStartLayerKey = "layer-1",
            CreatedAtUtc = now,
            UpdatedAtUtc = now,
        });
    }
}
