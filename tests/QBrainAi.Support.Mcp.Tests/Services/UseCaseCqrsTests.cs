using QBrainAi.Cqrs;
using QBrainAi.Support.Mcp.Services;
using QBrainAi.Support.Mcp.UseCases.Models;
using QBrainAi.Support.Mcp.Storage;
using QBrainAi.Support.Mcp.Storage.Entities;
using QBrainAi.Support.Mcp.UseCases;
using QBrainAi.Support.Mcp.UseCases.Commands;
using QBrainAi.Support.Mcp.UseCases.Queries;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace QBrainAi.Support.Mcp.Tests.Services;

/// <summary>
/// TEST-MCP-USECASE-002 / FR-MCP-USECASE-001..004 / TR-MCP-USECASE-002:
/// Unit tests for use case CQRS handlers: create, link FR string id, soft delete, and coverage.
/// </summary>
/// <remarks>2026-11-07, TEST-MCP-USECASE-002; FR-MCP-USECASE-001; TR-MCP-USECASE-002; FR-MCP-BUILD-001; TR-MCP-NUKE-019 AC-7: the unit half of this class: its 2 Category=Integration cases (DeleteUseCase_SoftDeletes_HidesFromGetAndList and GetUseCaseDiagram_ReturnsScopedMermaidUml) moved unchanged to the class of the same name in the integration-owned QBrainAi.Support.Mcp.EnvironmentIntegrationTests (slice S7, hostile validation finding R14-F4), so this unit-owned project holds only unit cases; the state and helpers both halves use (_connection, _options, _workspace, _ctx, .ctor, Dispose, CreateContext, CreateWorkspaceContext and SeedWorkspace) moved once into the abstract base class UseCaseCqrsTestsBase in Services/UseCaseCqrsTestsBase.cs beside this file, which both halves derive from and the integration project links (DRY); the unit cases and the class attributes are unchanged.</remarks>
public sealed class UseCaseCqrsTests : UseCaseCqrsTestsBase, IDisposable
{
    /// <summary>
    /// BUG-TRIAGE-139 AC1/AC4: create without a pre-seeded Workspaces row auto-creates the parent
    /// and returns a positive useCaseId that list can see.
    /// </summary>
    [Fact]
    public async Task CreateUseCase_WithoutPreSeededWorkspace_AutoCreatesParentAndPersists()
    {
        var isolated = Path.Combine(Path.GetTempPath(), "mcp-uc-noseed-" + Guid.NewGuid().ToString("N"));
        await using var db = new McpDbContext(_options, new WorkspaceContext
        {
            WorkspacePath = isolated,
            WorkspaceName = "uc-noseed",
            DataDirectory = isolated,
            TodoFilePath = Path.Combine(isolated, "docs", "todo.yaml"),
            SessionsPath = Path.Combine(isolated, "docs", "sessions"),
            ExternalDocsPath = Path.Combine(isolated, "docs", "external"),
        });

        Assert.False(await db.Workspaces.IgnoreQueryFilters().AnyAsync(
            w => w.WorkspaceId == isolated,
            TestContext.Current.CancellationToken).ConfigureAwait(true));

        var result = await new CreateUseCaseCommandHandler(db, new WorkspaceContext
        {
            WorkspacePath = isolated,
            WorkspaceName = "uc-noseed",
            DataDirectory = isolated,
        }).HandleAsync(
            new CreateUseCaseCommand(isolated, new CreateUseCaseRequest { Title = "No seed parent" }),
            _ctx).ConfigureAwait(true);

        Assert.True(result.IsSuccess, result.Error);
        Assert.NotNull(result.Value);
        Assert.True(result.Value!.UseCaseId > 0);
        Assert.Equal("No seed parent", result.Value.Title);
        Assert.True(await db.Workspaces.IgnoreQueryFilters().AnyAsync(
            w => w.WorkspaceId == isolated,
            TestContext.Current.CancellationToken).ConfigureAwait(true));
        Assert.Equal(1, await db.UseCases.CountAsync(TestContext.Current.CancellationToken).ConfigureAwait(true));
    }

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
