using McpServer.Support.Mcp.Services;
using McpServer.Support.Mcp.Storage;
using McpServer.Support.Mcp.Storage.Entities;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace McpServer.Support.Mcp.Tests.Storage;

/// <summary>
/// TEST-MCP-USECASE-001 / TR-MCP-USECASE-001: Use case 4NF storage, workspace isolation,
/// soft-delete, and string FR link FK behavior.
/// </summary>
public sealed class UseCaseStorageTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly DbContextOptions<McpDbContext> _options;
    private readonly string _workspaceA = Path.Combine(Path.GetTempPath(), "mcp-uc-a");
    private readonly string _workspaceB = Path.Combine(Path.GetTempPath(), "mcp-uc-b");

    /// <summary>Creates an isolated in-memory relational schema.</summary>
    public UseCaseStorageTests()
    {
        _connection = new SqliteConnection("Data Source=:memory:");
        _connection.Open();
        _options = new DbContextOptionsBuilder<McpDbContext>()
            .UseSqlite(_connection)
            .Options;

        using var ctx = CreateContext(_workspaceA);
        ctx.Database.EnsureCreated();
        SeedWorkspace(ctx, _workspaceA, "Workspace A");
        SeedWorkspace(ctx, _workspaceB, "Workspace B");
        ctx.SaveChanges();
    }

    /// <summary>Releases the shared in-memory SQLite connection.</summary>
    public void Dispose() => _connection.Dispose();

    /// <summary>
    /// Persist a use case with a basic flow and step; reload returns the graph.
    /// </summary>
    [Fact]
    public async Task PersistUseCase_WithFlowAndStep_RoundTrips()
    {
        await using (var db = CreateContext(_workspaceA))
        {
            var now = DateTimeOffset.UtcNow;
            var uc = new UseCaseEntity
            {
                WorkspaceId = _workspaceA,
                Title = "Authenticate user",
                BriefDescription = "User signs in",
                Priority = 1,
                CreatedAtUtc = now,
                UpdatedAtUtc = now,
            };
            db.UseCases.Add(uc);
            await db.SaveChangesAsync(TestContext.Current.CancellationToken).ConfigureAwait(true);

            db.UseCaseFlows.Add(new UseCaseFlowEntity
            {
                WorkspaceId = _workspaceA,
                UseCaseId = uc.UseCaseId,
                FlowType = "Basic",
                Name = "Main",
                SequenceNumber = 1,
            });
            await db.SaveChangesAsync(TestContext.Current.CancellationToken).ConfigureAwait(true);

            var flow = await db.UseCaseFlows.SingleAsync(cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);
            db.UseCaseSteps.Add(new UseCaseStepEntity
            {
                WorkspaceId = _workspaceA,
                FlowId = flow.FlowId,
                StepNumber = 1,
                Action = "Submit credentials",
                SystemResponse = "Issue token",
            });
            await db.SaveChangesAsync(TestContext.Current.CancellationToken).ConfigureAwait(true);
        }

        await using (var db = CreateContext(_workspaceA))
        {
            var loaded = await db.UseCases
                .Include(u => u.Flows)
                .ThenInclude(f => f.Steps)
                .SingleAsync(cancellationToken: TestContext.Current.CancellationToken)
                .ConfigureAwait(true);
            Assert.Equal("Authenticate user", loaded.Title);
            Assert.Single(loaded.Flows);
            Assert.Single(loaded.Flows.First().Steps);
            Assert.Equal("Submit credentials", loaded.Flows.First().Steps.First().Action);
        }
    }

    /// <summary>
    /// Workspace query filter prevents cross-workspace leak of use cases.
    /// </summary>
    [Fact]
    public async Task UseCases_AreIsolated_ByWorkspaceFilter()
    {
        await using (var db = CreateContext(_workspaceA))
        {
            var now = DateTimeOffset.UtcNow;
            db.UseCases.Add(new UseCaseEntity
            {
                WorkspaceId = _workspaceA,
                Title = "Only A",
                CreatedAtUtc = now,
                UpdatedAtUtc = now,
            });
            await db.SaveChangesAsync(TestContext.Current.CancellationToken).ConfigureAwait(true);
        }

        await using (var db = CreateContext(_workspaceB))
        {
            var count = await db.UseCases.CountAsync(cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);
            Assert.Equal(0, count);
        }
    }

    /// <summary>
    /// Soft-delete via Remove hides the use case from default queries (TR-MCP-DB-003).
    /// </summary>
    [Fact]
    public async Task SoftDelete_HidesUseCase_FromDefaultQuery()
    {
        long id;
        await using (var db = CreateContext(_workspaceA))
        {
            var now = DateTimeOffset.UtcNow;
            var uc = new UseCaseEntity
            {
                WorkspaceId = _workspaceA,
                Title = "To delete",
                CreatedAtUtc = now,
                UpdatedAtUtc = now,
            };
            db.UseCases.Add(uc);
            await db.SaveChangesAsync(TestContext.Current.CancellationToken).ConfigureAwait(true);
            id = uc.UseCaseId;

            db.UseCases.Remove(uc);
            await db.SaveChangesAsync(TestContext.Current.CancellationToken).ConfigureAwait(true);
        }

        await using (var db = CreateContext(_workspaceA))
        {
            var visible = await db.UseCases.AnyAsync(u => u.UseCaseId == id, TestContext.Current.CancellationToken).ConfigureAwait(true);
            Assert.False(visible);

            var hidden = await db.UseCases.IgnoreQueryFilters()
                .AnyAsync(u => u.UseCaseId == id, TestContext.Current.CancellationToken)
                .ConfigureAwait(true);
            Assert.True(hidden);
        }
    }

    /// <summary>
    /// UseCaseFrLink requires an existing FR string id on RequirementEntity (kind fr).
    /// </summary>
    [Fact]
    public async Task FrLink_RequiresExistingStringFrId()
    {
        await using var db = CreateContext(_workspaceA);
        var now = DateTimeOffset.UtcNow;
        var uc = new UseCaseEntity
        {
            WorkspaceId = _workspaceA,
            Title = "Linked UC",
            CreatedAtUtc = now,
            UpdatedAtUtc = now,
        };
        db.UseCases.Add(uc);
        db.Requirements.Add(new RequirementEntity
        {
            WorkspaceId = _workspaceA,
            Kind = "fr",
            Id = "FR-MCP-USECASE-001",
            Title = "CRUD use cases",
            Body = "body",
            Priority = "medium",
            Status = "pending",
            ScopeStartLayerKey = "layer-1",
            CreatedAtUtc = now.ToString("O"),
            UpdatedAtUtc = now.ToString("O"),
        });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken).ConfigureAwait(true);

        db.UseCaseFrLinks.Add(new UseCaseFrLinkEntity
        {
            WorkspaceId = _workspaceA,
            UseCaseId = uc.UseCaseId,
            FrId = "FR-MCP-USECASE-001",
            FrKind = "fr",
            LinkType = "Realizes",
            CreatedAtUtc = now,
        });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken).ConfigureAwait(true);

        var link = await db.UseCaseFrLinks
            .Include(l => l.FunctionalRequirement)
            .SingleAsync(cancellationToken: TestContext.Current.CancellationToken)
            .ConfigureAwait(true);
        Assert.Equal("FR-MCP-USECASE-001", link.FrId);
        Assert.Equal("Realizes", link.LinkType);
        Assert.NotNull(link.FunctionalRequirement);
        Assert.Equal("CRUD use cases", link.FunctionalRequirement!.Title);
    }

    /// <summary>
    /// Duplicate active (workspace, use case, FR) link violates unique index.
    /// </summary>
    [Fact]
    public async Task FrLink_Duplicate_Throws()
    {
        await using var db = CreateContext(_workspaceA);
        var now = DateTimeOffset.UtcNow;
        var uc = new UseCaseEntity
        {
            WorkspaceId = _workspaceA,
            Title = "Dup link UC",
            CreatedAtUtc = now,
            UpdatedAtUtc = now,
        };
        db.UseCases.Add(uc);
        db.Requirements.Add(new RequirementEntity
        {
            WorkspaceId = _workspaceA,
            Kind = "fr",
            Id = "FR-MCP-USECASE-003",
            Title = "Links",
            Body = "body",
            Priority = "medium",
            Status = "pending",
            ScopeStartLayerKey = "layer-1",
            CreatedAtUtc = now.ToString("O"),
            UpdatedAtUtc = now.ToString("O"),
        });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken).ConfigureAwait(true);

        db.UseCaseFrLinks.Add(new UseCaseFrLinkEntity
        {
            WorkspaceId = _workspaceA,
            UseCaseId = uc.UseCaseId,
            FrId = "FR-MCP-USECASE-003",
            FrKind = "fr",
            LinkType = "Realizes",
            CreatedAtUtc = now,
        });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken).ConfigureAwait(true);

        db.UseCaseFrLinks.Add(new UseCaseFrLinkEntity
        {
            WorkspaceId = _workspaceA,
            UseCaseId = uc.UseCaseId,
            FrId = "FR-MCP-USECASE-003",
            FrKind = "fr",
            LinkType = "Realizes",
            CreatedAtUtc = now,
        });
        await Assert.ThrowsAsync<DbUpdateException>(() =>
            db.SaveChangesAsync(TestContext.Current.CancellationToken)).ConfigureAwait(true);
    }

    private McpDbContext CreateContext(string workspacePath)
    {
        var ws = new WorkspaceContext
        {
            WorkspacePath = workspacePath,
            WorkspaceName = Path.GetFileName(workspacePath) ?? "ws",
            DataDirectory = workspacePath,
            TodoFilePath = Path.Combine(workspacePath, "docs", "todo.yaml"),
            SessionsPath = Path.Combine(workspacePath, "docs", "sessions"),
            ExternalDocsPath = Path.Combine(workspacePath, "docs", "external"),
        };
        return new McpDbContext(_options, ws);
    }

    private static void SeedWorkspace(McpDbContext ctx, string workspaceId, string name)
    {
        if (ctx.Workspaces.IgnoreQueryFilters().Any(w => w.WorkspaceId == workspaceId))
        {
            return;
        }

        ctx.Workspaces.Add(new WorkspaceEntity
        {
            WorkspaceId = workspaceId,
            WorkspacePath = workspaceId,
            Name = name,
        });
    }
}
