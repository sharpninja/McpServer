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
/// TEST-MCP-PRODUCT-002 / FR-MCP-PRODUCT-003 / FR-MCP-PRODUCT-004:
/// Shared effective-requirements acceptance. Phase 2 red until the query is implemented.
/// </summary>
public sealed class GetProductEffectiveRequirementsQueryHandlerTests : IDisposable
{
    private const string Owner = @"F:\GitHub\product-owner";
    private const string Sibling = @"F:\GitHub\product-sibling";
    private const string Outsider = @"F:\GitHub\product-outsider";
    private readonly SqliteConnection _connection;
    private readonly DbContextOptions<McpDbContext> _options;
    private readonly CallContext _ctx = new();

    /// <summary>Opens an isolated SQLite database for share tests.</summary>
    public GetProductEffectiveRequirementsQueryHandlerTests()
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
        SeedLayer(db, Owner, "layer-2", 2);
        SeedLayer(db, Sibling, "layer-1", 1);
        SeedLayer(db, Outsider, "layer-1", 1);
        SeedRequirement(db, Owner, "fr", "FR-OWNER-001", "Owner FR", "owner body", "layer-1");
        SeedRequirement(db, Owner, "tr", "TR-OWNER-001", "Owner TR", "owner tr", "layer-1");
        SeedRequirement(db, Owner, "test", "TEST-OWNER-001", "Owner TEST", "owner test", "layer-1");
        SeedRequirement(db, Sibling, "fr", "FR-SIB-001", "Sibling FR", "sibling body", "layer-1");
        SeedRequirement(db, Sibling, "tr", "TR-SIB-001", "Sibling TR", "sibling tr", "layer-1");
        SeedRequirement(db, Sibling, "test", "TEST-SIB-001", "Sibling TEST", "sibling test", "layer-1");
        SeedRequirement(db, Sibling, "fr", "FR-SHARE-001", "Collision sibling", "sib collision", "layer-1");
        SeedRequirement(db, Owner, "fr", "FR-SHARE-001", "Collision owner", "owner collision", "layer-1");
        SeedRequirement(db, Sibling, "fr", "FR-LAYER-MISS", "Needs layer 2", "miss", "layer-2");
        SeedRequirement(db, Outsider, "fr", "FR-OUT-001", "Outsider FR", "secret", "layer-1");
        SeedLink(db, Sibling, "FR-SIB-001", "tr", "TR-SIB-001");
        SeedLink(db, Sibling, "FR-SIB-001", "test", "TEST-SIB-001");
        SeedCriterion(db, Owner, "fr", "FR-OWNER-001", "ac-owner-1", "Owner must keep local AC");
        SeedCriterion(db, Sibling, "fr", "FR-SIB-001", "ac-sib-1", "Sibling AC must travel with the union");
        SeedCriterion(db, Outsider, "fr", "FR-OUT-001", "ac-out-1", "Zero-product local AC must survive");
        db.SaveChanges();
    }

    /// <inheritdoc />
    public void Dispose() => _connection.Dispose();

    /// <summary>Default product scope unions sibling in-scope FR/TR/TEST/mappings with origin ids.</summary>
    [Fact]
    public async Task HandleAsync_ProductScope_UnionsSiblingRows()
    {
        await using var db = new McpDbContext(_options);
        await CreateProductAsync(db);
        var result = await new GetProductEffectiveRequirementsQueryHandler(db)
            .HandleAsync(new GetProductEffectiveRequirementsQuery(Owner, "layer-1", "product"), _ctx);

        Assert.True(result.IsSuccess, result.Error);
        Assert.Contains(result.Value!.Functional, f => f.Id == "FR-SIB-001" && f.WorkspaceId == Sibling);
        Assert.Contains(
            result.Value.Functional,
            f => f.Id == "FR-OWNER-001"
                 && f.AcceptanceCriteria is not null
                 && f.AcceptanceCriteria.Any(c => c.Id == "ac-owner-1" && c.Text == "Owner must keep local AC"));
        Assert.Contains(
            result.Value.Functional,
            f => f.Id == "FR-SIB-001"
                 && f.AcceptanceCriteria is not null
                 && f.AcceptanceCriteria.Any(c => c.Id == "ac-sib-1" && c.Text == "Sibling AC must travel with the union"));
        Assert.Contains(result.Value.Technical, t => t.Id == "TR-SIB-001" && t.WorkspaceId == Sibling);
        Assert.Contains(result.Value.Testing, t => t.Id == "TEST-SIB-001" && t.WorkspaceId == Sibling);
        Assert.Contains(result.Value.Mappings, m => m.FrId == "FR-SIB-001" && m.WorkspaceId == Sibling);
        Assert.Contains(result.Value.ProductKeys ?? [], k => k == "PROD-MCPSERVER");
    }

    /// <summary>productScope=local hides siblings.</summary>
    [Fact]
    public async Task HandleAsync_LocalScope_HidesSiblings()
    {
        await using var db = new McpDbContext(_options);
        await CreateProductAsync(db);
        var result = await new GetProductEffectiveRequirementsQueryHandler(db)
            .HandleAsync(new GetProductEffectiveRequirementsQuery(Owner, "layer-1", "local"), _ctx);

        Assert.True(result.IsSuccess, result.Error);
        Assert.DoesNotContain(result.Value!.Functional, f => f.WorkspaceId == Sibling);
        Assert.Contains(result.Value.Functional, f => f.Id == "FR-OWNER-001");
        Assert.Contains(
            result.Value.Functional,
            f => f.Id == "FR-OWNER-001"
                 && f.AcceptanceCriteria is not null
                 && f.AcceptanceCriteria.Any(c => c.Id == "ac-owner-1"));
    }

    /// <summary>Zero-product productScope=product still attaches local acceptance criteria (AC4).</summary>
    [Fact]
    public async Task HandleAsync_ZeroProductWorkspace_KeepsLocalAcceptanceCriteria()
    {
        await using var db = new McpDbContext(_options);
        var result = await new GetProductEffectiveRequirementsQueryHandler(db)
            .HandleAsync(new GetProductEffectiveRequirementsQuery(Outsider, "layer-1", "product"), _ctx);

        Assert.True(result.IsSuccess, result.Error);
        Assert.Contains(
            result.Value!.Functional,
            f => f.Id == "FR-OUT-001"
                 && f.AcceptanceCriteria is not null
                 && f.AcceptanceCriteria.Any(c => c.Id == "ac-out-1" && c.Text == "Zero-product local AC must survive"));
    }

    /// <summary>Same id in two workspaces stays two rows with different originWorkspaceId.</summary>
    [Fact]
    public async Task HandleAsync_Collision_ReturnsTwoOrigins()
    {
        await using var db = new McpDbContext(_options);
        await CreateProductAsync(db);
        var result = await new GetProductEffectiveRequirementsQueryHandler(db)
            .HandleAsync(new GetProductEffectiveRequirementsQuery(Owner, "layer-1", "product"), _ctx);

        Assert.True(result.IsSuccess, result.Error);
        var collisions = result.Value!.Functional.Where(f => f.Id == "FR-SHARE-001").ToArray();
        Assert.Equal(2, collisions.Length);
        Assert.Contains(collisions, f => f.WorkspaceId == Owner);
        Assert.Contains(collisions, f => f.WorkspaceId == Sibling);
    }

    /// <summary>Missing origin layer key excludes the sibling row.</summary>
    [Fact]
    public async Task HandleAsync_OriginLayerMiss_ExcludesSiblingRow()
    {
        await using var db = new McpDbContext(_options);
        await CreateProductAsync(db);
        var result = await new GetProductEffectiveRequirementsQueryHandler(db)
            .HandleAsync(new GetProductEffectiveRequirementsQuery(Owner, "layer-2", "product"), _ctx);

        Assert.True(result.IsSuccess, result.Error);
        Assert.DoesNotContain(result.Value!.Functional, f => f.Id == "FR-LAYER-MISS");
    }

    /// <summary>After leave, the leaver no longer sees siblings.</summary>
    [Fact]
    public async Task HandleAsync_AfterLeave_DropsSibling()
    {
        await using var db = new McpDbContext(_options);
        await CreateProductAsync(db);
        var left = await new RemoveProductMemberCommandHandler(db).HandleAsync(
            new RemoveProductMemberCommand(Sibling, "PROD-MCPSERVER", Sibling),
            _ctx);
        Assert.True(left.IsSuccess, left.Error);

        var result = await new GetProductEffectiveRequirementsQueryHandler(db)
            .HandleAsync(new GetProductEffectiveRequirementsQuery(Sibling, "layer-1", "product"), _ctx);

        Assert.True(result.IsSuccess, result.Error);
        Assert.DoesNotContain(result.Value!.Functional, f => f.WorkspaceId == Owner);
        Assert.Contains(result.Value.Functional, f => f.Id == "FR-SIB-001");
    }

    /// <summary>Outsider effective is local-only.</summary>
    [Fact]
    public async Task HandleAsync_Outsider_IsLocalOnly()
    {
        await using var db = new McpDbContext(_options);
        await CreateProductAsync(db);
        var result = await new GetProductEffectiveRequirementsQueryHandler(db)
            .HandleAsync(new GetProductEffectiveRequirementsQuery(Outsider, "layer-1", "product"), _ctx);

        Assert.True(result.IsSuccess, result.Error);
        Assert.DoesNotContain(result.Value!.Functional, f => f.WorkspaceId == Owner);
        Assert.DoesNotContain(result.Value.Functional, f => f.WorkspaceId == Sibling);
        Assert.Contains(result.Value.Functional, f => f.Id == "FR-OUT-001");
    }

    /// <summary>Local delete of a sibling-only id does not change the sibling row.</summary>
    [Fact]
    public async Task HandleAsync_LocalDeleteSiblingOnlyId_LeavesSiblingRow()
    {
        await using var db = new McpDbContext(_options);
        await CreateProductAsync(db);
        db.OverrideWorkspaceId(Owner);
        var local = await db.Requirements.FirstOrDefaultAsync(
            r => r.Id == "FR-SIB-001",
            TestContext.Current.CancellationToken);
        Assert.Null(local);

        var result = await new GetProductEffectiveRequirementsQueryHandler(db)
            .HandleAsync(new GetProductEffectiveRequirementsQuery(Owner, "layer-1", "product"), _ctx);

        Assert.True(result.IsSuccess, result.Error);
        Assert.Contains(result.Value!.Functional, f => f.Id == "FR-SIB-001" && f.WorkspaceId == Sibling);
    }

    /// <summary>Zero-product workspace stays local-only even with productScope=product.</summary>
    [Fact]
    public async Task HandleAsync_ZeroProductWorkspace_StaysLocal()
    {
        await using var db = new McpDbContext(_options);
        var result = await new GetProductEffectiveRequirementsQueryHandler(db)
            .HandleAsync(new GetProductEffectiveRequirementsQuery(Outsider, "layer-1", "product"), _ctx);

        Assert.True(result.IsSuccess, result.Error);
        Assert.DoesNotContain(result.Value!.Functional, f => f.WorkspaceId == Owner);
        Assert.Contains(result.Value.Functional, f => f.Id == "FR-OUT-001");
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

    private static void SeedRequirement(
        McpDbContext db,
        string workspaceId,
        string kind,
        string id,
        string title,
        string body,
        string startLayer)
    {
        var now = DateTimeOffset.UtcNow.ToString("O");
        db.Requirements.Add(new RequirementEntity
        {
            WorkspaceId = workspaceId,
            Kind = kind,
            Id = id,
            Title = title,
            Body = body,
            Priority = "medium",
            Status = "pending",
            ScopeStartLayerKey = startLayer,
            CreatedAtUtc = now,
            UpdatedAtUtc = now,
        });
    }

    private static void SeedCriterion(
        McpDbContext db,
        string workspaceId,
        string kind,
        string requirementId,
        string criterionId,
        string text)
    {
        db.RequirementAcceptanceCriteria.Add(new RequirementAcceptanceCriterionEntity
        {
            WorkspaceId = workspaceId,
            RequirementKind = kind,
            RequirementId = requirementId,
            Ordinal = 0,
            CriterionId = criterionId,
            Text = text,
            IsSatisfied = false,
        });
    }

    private static void SeedLink(McpDbContext db, string workspaceId, string frId, string targetKind, string targetId)
    {
        db.RequirementTraceabilityLinks.Add(new RequirementTraceabilityLinkEntity
        {
            WorkspaceId = workspaceId,
            SourceKind = "fr",
            FrId = frId,
            TargetKind = targetKind,
            TargetId = targetId,
            CreatedAtUtc = DateTimeOffset.UtcNow.ToString("O"),
        });
    }
}
