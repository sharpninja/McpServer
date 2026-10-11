using QBrainAi.Support.Mcp.UseCases.Models;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using QBrainAi.Cqrs;
using QBrainAi.Support.Mcp.Services;
using QBrainAi.Support.Mcp.Storage;
using QBrainAi.Support.Mcp.Storage.Entities;
using QBrainAi.Support.Mcp.UseCases;
using QBrainAi.Support.Mcp.UseCases.Commands;
using QBrainAi.Support.Mcp.UseCases.Queries;
using Xunit;
using Xunit.Sdk;

namespace QBrainAi.Support.Mcp.Tests.Services;

/// <summary>
/// TEST-MCP-USECASE-015 / FR-MCP-USECASE-005:
/// proves the MCP diagram contract requires a scoped UML use-case diagram rather than a sequence rendering.
/// </summary>
public sealed class UseCaseDiagramUmlContractTests
{
    /// <summary>Correct scoped UML output satisfies every diagram assertion.</summary>
    [Fact]
    public void MockGate_ScopedUmlUseCaseDiagram_PassesContract()
    {
        AssertScopedUmlDiagram(new UseCaseDiagramDto
        {
            UseCaseId = 125,
            Format = "mermaid",
            Content = """
                %% mcp-usecase-diagram-schema:1
                flowchart LR
                  subgraph system["TruckMate"]
                    uc_125(["Start and certify LAB-OMARCHY TruckMate services"])
                  end
                  actor_17(["Operator"])
                  actor_17 --- uc_125
                """,
        });
    }

    /// <summary>A sequence rendering is the deliberate wrong control rejected by the same assertions.</summary>
    [Fact]
    public void MockGate_SequenceDiagram_IsRejected()
    {
        var rejection = Assert.ThrowsAny<XunitException>(
            () => AssertScopedUmlDiagram(new UseCaseDiagramDto
            {
                UseCaseId = 125,
                Format = "mermaid",
                Content = "sequenceDiagram\n  participant Operator",
            }));

        Assert.Contains("flowchart", rejection.Message, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>Production binding canonicalizes the workspace and returns scoped UML for UC-125.</summary>
    [Fact]
    public async Task ProductionGate_EquivalentRawPath_ReturnsScopedUmlAndSupportsGraphAndApproval()
    {
        const string canonicalWorkspace = @"F:\GitHub\TruckMate";
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync(TestContext.Current.CancellationToken).ConfigureAwait(true);
        var options = new DbContextOptionsBuilder<McpDbContext>()
            .UseSqlite(connection)
            .Options;
        var workspaceContext = new WorkspaceContext
        {
            WorkspacePath = canonicalWorkspace,
            WorkspaceName = "TruckMate",
            DataDirectory = canonicalWorkspace,
        };

        await using var db = new McpDbContext(options, workspaceContext);
        await db.Database.EnsureCreatedAsync(TestContext.Current.CancellationToken).ConfigureAwait(true);
        db.OverrideWorkspaceId(canonicalWorkspace);
        db.Workspaces.Add(new WorkspaceEntity
        {
            WorkspaceId = canonicalWorkspace,
            WorkspacePath = canonicalWorkspace,
            Name = "TruckMate",
        });
        var useCase = new UseCaseEntity
        {
            UseCaseId = 125,
            WorkspaceId = canonicalWorkspace,
            Title = "Start and certify LAB-OMARCHY TruckMate services",
            Scope = "TruckMate",
            CreatedAtUtc = DateTimeOffset.UtcNow,
            UpdatedAtUtc = DateTimeOffset.UtcNow,
        };
        var actor = new ActorEntity
        {
            ActorId = 17,
            WorkspaceId = canonicalWorkspace,
            Name = "Operator",
            Type = "Primary",
        };
        db.UseCases.Add(useCase);
        db.Actors.Add(actor);
        db.UseCaseActors.Add(new UseCaseActorEntity
        {
            WorkspaceId = canonicalWorkspace,
            UseCaseId = 125,
            ActorId = 17,
            IsPrimary = true,
        });
        db.UseCaseFlows.Add(new UseCaseFlowEntity
        {
            FlowId = 260,
            WorkspaceId = canonicalWorkspace,
            UseCaseId = 125,
            FlowType = "Alternative",
            Name = "Service health failure",
            SequenceNumber = 4,
        });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken).ConfigureAwait(true);

        var equivalentRawPath = canonicalWorkspace + Path.DirectorySeparatorChar + ".";
        var context = new CallContext { CancellationToken = TestContext.Current.CancellationToken };
        var diagram = await new GetUseCaseDiagramQueryHandler(
                db,
                workspaceContext,
                new UseCaseUmlSerializationService())
            .HandleAsync(new GetUseCaseDiagramQuery(equivalentRawPath, 125, "mermaid"), context)
            .ConfigureAwait(true);
        Assert.True(diagram.IsSuccess, diagram.Error);
        AssertScopedUmlDiagram(diagram.Value!);
        Assert.Contains("Service health failure", diagram.Value!.Content, StringComparison.Ordinal);

        var graph = new UseCaseDiagramGraphDto
        {
            SystemBoundary = new UseCaseDiagramBoundaryDto { Id = "system", Label = "TruckMate" },
            Nodes =
            [
                new UseCaseDiagramNodeDto { Id = "actor_17", Type = "actor", Label = "Operator" },
                new UseCaseDiagramNodeDto
                {
                    Id = "uc_125",
                    Type = "usecase",
                    Label = "Start and certify LAB-OMARCHY TruckMate services",
                },
            ],
            Edges =
            [
                new UseCaseDiagramEdgeDto
                {
                    Id = "association_17_125",
                    Type = "association",
                    Source = "actor_17",
                    Target = "uc_125",
                },
            ],
        };
        var put = await new PutUseCaseDiagramGraphCommandHandler(db, workspaceContext)
            .HandleAsync(new PutUseCaseDiagramGraphCommand(equivalentRawPath, 125, graph), context)
            .ConfigureAwait(true);
        Assert.True(put.IsSuccess, put.Error);

        var get = await new GetUseCaseDiagramGraphQueryHandler(db, workspaceContext)
            .HandleAsync(new GetUseCaseDiagramGraphQuery(equivalentRawPath, 125), context)
            .ConfigureAwait(true);
        Assert.True(get.IsSuccess, get.Error);
        Assert.Equal(2, get.Value!.Nodes.Count);

        var approval = await new SetUseCaseApprovalStatusCommandHandler(db, workspaceContext)
            .HandleAsync(new SetUseCaseApprovalStatusCommand(equivalentRawPath, 125, "Approved"), context)
            .ConfigureAwait(true);
        Assert.True(approval.IsSuccess, approval.Error);
        Assert.Equal("Approved", approval.Value!.ApprovalStatus);
    }

    private static void AssertScopedUmlDiagram(UseCaseDiagramDto diagram)
    {
        Assert.Equal(125, diagram.UseCaseId);
        Assert.Equal("mermaid", diagram.Format);
        Assert.Contains("flowchart", diagram.Content, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("subgraph", diagram.Content, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Operator", diagram.Content, StringComparison.Ordinal);
        Assert.Contains("Start and certify LAB-OMARCHY TruckMate services", diagram.Content, StringComparison.Ordinal);
        Assert.DoesNotContain("sequenceDiagram", diagram.Content, StringComparison.Ordinal);
    }
}
