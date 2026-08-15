using McpServer.Cqrs;
using McpServer.Support.Mcp.Services;
using McpServer.Support.Mcp.UseCases.Models;
using McpServer.Support.Mcp.Storage;
using McpServer.Support.Mcp.Storage.Entities;
using McpServer.Support.Mcp.UseCases;
using McpServer.Support.Mcp.UseCases.Commands;
using McpServer.Support.Mcp.UseCases.Queries;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace McpServer.Support.Mcp.Tests.Services;

/// <summary>
/// TEST-MCP-USECASE-002 / FR-MCP-USECASE-001..004 / TR-MCP-USECASE-002:
/// Unit tests for use case CQRS handlers: create, link FR string id, soft delete, and coverage.
/// </summary>
public sealed class UseCaseCqrsTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly DbContextOptions<McpDbContext> _options;
    private readonly string _workspace = Path.Combine(Path.GetTempPath(), "mcp-uc-cqrs-" + Guid.NewGuid().ToString("N"));
    private readonly CallContext _ctx = new();

    /// <summary>Creates an isolated SQLite schema shared across handler tests.</summary>
    public UseCaseCqrsTests()
    {
        _connection = new SqliteConnection("Data Source=:memory:");
        _connection.Open();
        _options = new DbContextOptionsBuilder<McpDbContext>()
            .UseSqlite(_connection)
            .Options;

        using var ctx = CreateContext();
        ctx.Database.EnsureCreated();
        SeedWorkspace(ctx, _workspace, "UseCase CQRS");
        ctx.SaveChanges();
    }

    /// <summary>Releases the shared SQLite connection.</summary>
    public void Dispose() => _connection.Dispose();

    /// <summary>FR-MCP-USECASE-001: CreateUseCaseCommand persists header and returns detail DTO.</summary>
    [Fact]
    public async Task CreateUseCase_PersistsHeader_AndReturnsDetail()
    {
        await using var db = CreateContext();
        var handler = new CreateUseCaseCommandHandler(db, CreateWorkspaceContext());
        var result = await handler.HandleAsync(
            new CreateUseCaseCommand(_workspace, new CreateUseCaseRequest
            {
                Title = "Sign in",
                BriefDescription = "User authenticates",
                Priority = 2,
                CreateBasicFlow = true,
                InitialSteps =
                [
                    new CreateUseCaseStepRequest
                    {
                        Action = "Submit credentials",
                        SystemResponse = "Issue token",
                    },
                ],
            }),
            _ctx).ConfigureAwait(true);

        Assert.True(result.IsSuccess, result.Error);
        Assert.NotNull(result.Value);
        Assert.True(result.Value!.UseCaseId > 0);
        Assert.Equal("Sign in", result.Value.Title);
        Assert.Equal(2, result.Value.Priority);
        Assert.Single(result.Value.Flows);
        Assert.Single(result.Value.Flows[0].Steps);
        Assert.Equal("Submit credentials", result.Value.Flows[0].Steps[0].Action);

        var count = await db.UseCases.CountAsync(TestContext.Current.CancellationToken).ConfigureAwait(true);
        Assert.Equal(1, count);
    }

    /// <summary>FR-MCP-USECASE-003: LinkUseCaseToFrCommand validates Kind=fr string id and defaults Realizes.</summary>
    [Fact]
    public async Task LinkUseCaseToFr_WithStringFrId_DefaultsRealizes()
    {
        await using var db = CreateContext();
        await SeedFrAsync(db, "FR-MCP-USECASE-001", "CRUD use cases").ConfigureAwait(true);

        var create = await new CreateUseCaseCommandHandler(db, CreateWorkspaceContext())
            .HandleAsync(new CreateUseCaseCommand(_workspace, new CreateUseCaseRequest { Title = "Manage UC" }), _ctx)
            .ConfigureAwait(true);
        Assert.True(create.IsSuccess, create.Error);

        var linkHandler = new LinkUseCaseToFrCommandHandler(db, CreateWorkspaceContext());
        var link = await linkHandler.HandleAsync(
            new LinkUseCaseToFrCommand(
                _workspace,
                create.Value!.UseCaseId,
                "FR-MCP-USECASE-001",
                LinkType: null,
                LinkOrder: 1,
                Notes: "auto"),
            _ctx).ConfigureAwait(true);

        Assert.True(link.IsSuccess, link.Error);
        Assert.Equal("FR-MCP-USECASE-001", link.Value!.FrId);
        Assert.Equal(UseCaseConstants.DefaultLinkType, link.Value.LinkType);
        Assert.Equal(1, link.Value.LinkOrder);

        var forFr = await new GetUseCasesForFrQueryHandler(db, CreateWorkspaceContext())
            .HandleAsync(new GetUseCasesForFrQuery(_workspace, "FR-MCP-USECASE-001"), _ctx)
            .ConfigureAwait(true);
        Assert.True(forFr.IsSuccess, forFr.Error);
        Assert.Single(forFr.Value!);
        Assert.Equal(create.Value.UseCaseId, forFr.Value![0].UseCaseId);
    }

    /// <summary>FR-MCP-USECASE-003: Linking a missing FR fails validation.</summary>
    [Fact]
    public async Task LinkUseCaseToFr_MissingFr_Fails()
    {
        await using var db = CreateContext();
        var create = await new CreateUseCaseCommandHandler(db, CreateWorkspaceContext())
            .HandleAsync(new CreateUseCaseCommand(_workspace, new CreateUseCaseRequest { Title = "Orphan" }), _ctx)
            .ConfigureAwait(true);
        Assert.True(create.IsSuccess, create.Error);

        var link = await new LinkUseCaseToFrCommandHandler(db, CreateWorkspaceContext())
            .HandleAsync(
                new LinkUseCaseToFrCommand(_workspace, create.Value!.UseCaseId, "FR-MISSING", null, 0, null),
                _ctx)
            .ConfigureAwait(true);

        Assert.True(link.IsFailure);
        Assert.Contains("not found", link.Error, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>FR-MCP-USECASE-003: Duplicate active UC-FR link returns conflict failure.</summary>
    [Fact]
    public async Task LinkUseCaseToFr_Duplicate_FailsWithConflict()
    {
        await using var db = CreateContext();
        await SeedFrAsync(db, "FR-MCP-USECASE-003", "Links").ConfigureAwait(true);
        var create = await new CreateUseCaseCommandHandler(db, CreateWorkspaceContext())
            .HandleAsync(new CreateUseCaseCommand(_workspace, new CreateUseCaseRequest { Title = "Dup" }), _ctx)
            .ConfigureAwait(true);

        var handler = new LinkUseCaseToFrCommandHandler(db, CreateWorkspaceContext());
        var first = await handler.HandleAsync(
            new LinkUseCaseToFrCommand(_workspace, create.Value!.UseCaseId, "FR-MCP-USECASE-003", null, 0, null),
            _ctx).ConfigureAwait(true);
        Assert.True(first.IsSuccess, first.Error);

        var second = await handler.HandleAsync(
            new LinkUseCaseToFrCommand(_workspace, create.Value.UseCaseId, "FR-MCP-USECASE-003", null, 0, null),
            _ctx).ConfigureAwait(true);
        Assert.True(second.IsFailure);
        Assert.Contains("Conflict", second.Error, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>FR-MCP-USECASE-001: Soft-delete hides use case from get/list queries.</summary>
    [Fact]
    public async Task DeleteUseCase_SoftDeletes_HidesFromGetAndList()
    {
        await using var db = CreateContext();
        var create = await new CreateUseCaseCommandHandler(db, CreateWorkspaceContext())
            .HandleAsync(new CreateUseCaseCommand(_workspace, new CreateUseCaseRequest { Title = "Doomed" }), _ctx)
            .ConfigureAwait(true);
        Assert.True(create.IsSuccess, create.Error);
        var id = create.Value!.UseCaseId;

        var deleted = await new DeleteUseCaseCommandHandler(db, CreateWorkspaceContext())
            .HandleAsync(new DeleteUseCaseCommand(_workspace, id), _ctx)
            .ConfigureAwait(true);
        Assert.True(deleted.IsSuccess, deleted.Error);
        Assert.True(deleted.Value);

        var get = await new GetUseCaseQueryHandler(db, CreateWorkspaceContext())
            .HandleAsync(new GetUseCaseQuery(_workspace, id), _ctx)
            .ConfigureAwait(true);
        Assert.True(get.IsFailure);
        Assert.Contains("not found", get.Error, StringComparison.OrdinalIgnoreCase);

        var list = await new ListUseCasesQueryHandler(db, CreateWorkspaceContext())
            .HandleAsync(new ListUseCasesQuery(_workspace), _ctx)
            .ConfigureAwait(true);
        Assert.True(list.IsSuccess, list.Error);
        Assert.Empty(list.Value!);

        var stillThere = await db.UseCases.IgnoreQueryFilters()
            .AnyAsync(u => u.UseCaseId == id, TestContext.Current.CancellationToken)
            .ConfigureAwait(true);
        Assert.True(stillThere);
    }

    /// <summary>FR-MCP-USECASE-003 / TR-MCP-USECASE-006: Coverage reports Realizes gaps for UC and FR.</summary>
    [Fact]
    public async Task Coverage_ReportsUseCasesAndFrsWithoutRealizes()
    {
        await using var db = CreateContext();
        await SeedFrAsync(db, "FR-A", "A").ConfigureAwait(true);
        await SeedFrAsync(db, "FR-B", "B").ConfigureAwait(true);

        var createHandler = new CreateUseCaseCommandHandler(db, CreateWorkspaceContext());
        var linked = await createHandler.HandleAsync(
            new CreateUseCaseCommand(_workspace, new CreateUseCaseRequest { Title = "Linked UC" }), _ctx)
            .ConfigureAwait(true);
        var unlinked = await createHandler.HandleAsync(
            new CreateUseCaseCommand(_workspace, new CreateUseCaseRequest { Title = "Unlinked UC" }), _ctx)
            .ConfigureAwait(true);
        Assert.True(linked.IsSuccess, linked.Error);
        Assert.True(unlinked.IsSuccess, unlinked.Error);

        var link = await new LinkUseCaseToFrCommandHandler(db, CreateWorkspaceContext())
            .HandleAsync(
                new LinkUseCaseToFrCommand(_workspace, linked.Value!.UseCaseId, "FR-A", "Realizes", 0, null),
                _ctx)
            .ConfigureAwait(true);
        Assert.True(link.IsSuccess, link.Error);

        var coverage = await new GetUseCaseFrCoverageQueryHandler(db, CreateWorkspaceContext())
            .HandleAsync(new GetUseCaseFrCoverageQuery(_workspace), _ctx)
            .ConfigureAwait(true);

        Assert.True(coverage.IsSuccess, coverage.Error);
        Assert.Equal(2, coverage.Value!.TotalUseCases);
        Assert.Equal(2, coverage.Value.TotalFunctionalRequirements);
        Assert.Equal(1, coverage.Value.LinkedUseCases);
        Assert.Equal(1, coverage.Value.LinkedFunctionalRequirements);
        Assert.Single(coverage.Value.UseCasesWithoutRealizesLink);
        Assert.Equal(unlinked.Value!.UseCaseId, coverage.Value.UseCasesWithoutRealizesLink[0].UseCaseId);
        Assert.Contains("FR-B", coverage.Value.FunctionalRequirementsWithoutRealizesUseCase);
        Assert.DoesNotContain("FR-A", coverage.Value.FunctionalRequirementsWithoutRealizesUseCase);
    }

    /// <summary>FR-MCP-USECASE-004: CreateUseCaseFromFrCommand builds shell UC with Realizes link.</summary>
    [Fact]
    public async Task CreateUseCaseFromFr_CreatesShellWithRealizesLink()
    {
        await using var db = CreateContext();
        await SeedFrAsync(db, "FR-MCP-USECASE-004", "From FR title").ConfigureAwait(true);

        var result = await new CreateUseCaseFromFrCommandHandler(db, CreateWorkspaceContext())
            .HandleAsync(new CreateUseCaseFromFrCommand(_workspace, "FR-MCP-USECASE-004"), _ctx)
            .ConfigureAwait(true);

        Assert.True(result.IsSuccess, result.Error);
        Assert.Equal("From FR title", result.Value!.Title);
        Assert.Single(result.Value.FrLinks);
        Assert.Equal("FR-MCP-USECASE-004", result.Value.FrLinks[0].FrId);
        Assert.Equal(UseCaseConstants.DefaultLinkType, result.Value.FrLinks[0].LinkType);
    }

    /// <summary>TR-MCP-USECASE-004: Diagram query returns mermaid sequenceDiagram content.</summary>
    [Fact]
    public async Task GetUseCaseDiagram_ReturnsMermaidSequence()
    {
        await using var db = CreateContext();
        var create = await new CreateUseCaseCommandHandler(db, CreateWorkspaceContext())
            .HandleAsync(new CreateUseCaseCommand(_workspace, new CreateUseCaseRequest
            {
                Title = "Diagram UC",
                CreateBasicFlow = true,
                InitialSteps = [new CreateUseCaseStepRequest { Action = "Do work" }],
            }), _ctx)
            .ConfigureAwait(true);
        Assert.True(create.IsSuccess, create.Error);

        var diagram = await new GetUseCaseDiagramQueryHandler(
                db,
                CreateWorkspaceContext(),
                new MermaidUseCaseDiagramService())
            .HandleAsync(new GetUseCaseDiagramQuery(_workspace, create.Value!.UseCaseId, "mermaid"), _ctx)
            .ConfigureAwait(true);

        Assert.True(diagram.IsSuccess, diagram.Error);
        Assert.Equal("mermaid", diagram.Value!.Format);
        Assert.Contains("sequenceDiagram", diagram.Value.Content, StringComparison.Ordinal);
        Assert.Contains("Do work", diagram.Value.Content, StringComparison.Ordinal);
    }

    private McpDbContext CreateContext()
    {
        return new McpDbContext(_options, CreateWorkspaceContext());
    }

    private WorkspaceContext CreateWorkspaceContext()
        => new()
        {
            WorkspacePath = _workspace,
            WorkspaceName = "uc-cqrs",
            DataDirectory = _workspace,
            TodoFilePath = Path.Combine(_workspace, "docs", "todo.yaml"),
            SessionsPath = Path.Combine(_workspace, "docs", "sessions"),
            ExternalDocsPath = Path.Combine(_workspace, "docs", "external"),
        };

    private static void SeedWorkspace(McpDbContext ctx, string workspaceId, string name)
    {
        if (ctx.Workspaces.IgnoreQueryFilters().Any(w => w.WorkspaceId == workspaceId))
            return;

        ctx.Workspaces.Add(new WorkspaceEntity
        {
            WorkspaceId = workspaceId,
            WorkspacePath = workspaceId,
            Name = name,
        });
    }

    private async Task SeedFrAsync(McpDbContext db, string id, string title)
    {
        var now = DateTimeOffset.UtcNow.ToString("O");
        db.Requirements.Add(new RequirementEntity
        {
            WorkspaceId = _workspace,
            Kind = UseCaseConstants.FrKind,
            Id = id,
            Title = title,
            Body = "body for " + title,
            Priority = "medium",
            Status = "pending",
            ScopeStartLayerKey = "layer-1",
            CreatedAtUtc = now,
            UpdatedAtUtc = now,
        });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken).ConfigureAwait(true);
    }
}
