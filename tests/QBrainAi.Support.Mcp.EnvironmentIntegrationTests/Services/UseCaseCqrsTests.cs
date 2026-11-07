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

namespace QBrainAi.Support.Mcp.EnvironmentIntegrationTests.Services;

/// <summary>
/// TEST-MCP-USECASE-002 / FR-MCP-USECASE-001..004 / TR-MCP-USECASE-002:
/// Unit tests for use case CQRS handlers: create, link FR string id, soft delete, and coverage.
/// </summary>
/// <remarks>2026-11-07, TEST-MCP-USECASE-002; FR-MCP-USECASE-001; TR-MCP-USECASE-002; FR-MCP-BUILD-001; TR-MCP-NUKE-019 AC-7: the integration half of this class, split from QBrainAi.Support.Mcp.Tests (Services/UseCaseCqrsTests.cs), where its unit cases stay in the class of the same name, into the integration-owned QBrainAi.Support.Mcp.EnvironmentIntegrationTests, so the unit-owned project holds only unit cases (slice S7, hostile validation finding R14-F4; operator 2026-11-06: "...Environment.IntegrationTests (Recommended)"); its 2 Category=Integration cases moved unchanged with the class attributes; the state and helpers both halves use come from the abstract base class UseCaseCqrsTestsBase in Services/UseCaseCqrsTestsBase.cs of QBrainAi.Support.Mcp.Tests, linked into this project (DRY); only the namespace changed.</remarks>
public sealed class UseCaseCqrsTests : UseCaseCqrsTestsBase, IDisposable
{
    /// <summary>FR-MCP-USECASE-001: Soft-delete hides use case from get/list queries.</summary>
    /// <remarks>Integration: binds real CQRS handlers that resolve physical workspace identity (TR-MCP-USECASE-018 AC-006).</remarks>
    [Trait("Category", "Integration")]
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

    /// <summary>TR-MCP-USECASE-004: Diagram query returns scoped Mermaid UML use-case content.</summary>
    /// <remarks>Integration: binds real CQRS handlers that resolve physical workspace identity (TR-MCP-USECASE-018 AC-006).</remarks>
    [Trait("Category", "Integration")]
    [Fact]
    public async Task GetUseCaseDiagram_ReturnsScopedMermaidUml()
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
                new UseCaseUmlSerializationService())
            .HandleAsync(new GetUseCaseDiagramQuery(_workspace, create.Value!.UseCaseId, "mermaid"), _ctx)
            .ConfigureAwait(true);

        Assert.True(diagram.IsSuccess, diagram.Error);
        Assert.Equal("mermaid", diagram.Value!.Format);
        Assert.Contains("flowchart", diagram.Value.Content, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Diagram UC", diagram.Value.Content, StringComparison.Ordinal);
    }
}
