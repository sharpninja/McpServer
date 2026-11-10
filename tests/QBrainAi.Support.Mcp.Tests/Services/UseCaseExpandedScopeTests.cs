using QBrainAi.Cqrs;
using QBrainAi.Support.Mcp.Services;
using QBrainAi.Support.Mcp.Storage;
using QBrainAi.Support.Mcp.Storage.Entities;
using QBrainAi.Support.Mcp.UseCases;
using QBrainAi.Support.Mcp.UseCases.Commands;
using QBrainAi.Support.Mcp.UseCases.Models;
using QBrainAi.Support.Mcp.UseCases.Queries;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Xunit;

namespace QBrainAi.Support.Mcp.Tests.Services;

/// <summary>
/// Expanded-scope Use Case tests: PlantUML diagrams, approval versioning, product key hooks,
/// and shared Realizes coverage evaluation for traceability.
/// </summary>
/// <remarks>2026-11-07, FR-MCP-USECASE-008; TR-MCP-USECASE-018 AC-006; FR-MCP-BUILD-001; TR-MCP-NUKE-019 AC-7: the unit half of this class: its 1 Category=Integration case (Approval_Approve_IncrementsVersion) moved unchanged to the class of the same name in the integration-owned QBrainAi.Support.Mcp.EnvironmentIntegrationTests (slice S7, hostile validation finding R14-F4), so this unit-owned project holds only unit cases; the state and helpers both halves use (_connection, _provider, _workspace, .ctor and Dispose) moved once into the abstract base class UseCaseExpandedScopeTestsBase in Services/UseCaseExpandedScopeTestsBase.cs beside this file, which both halves derive from and the integration project links (DRY); the unit cases and the class attributes are unchanged.</remarks>
public sealed class UseCaseExpandedScopeTests : UseCaseExpandedScopeTestsBase, IDisposable
{
    /// <summary>TR-MCP-USECASE-004: PlantUML format is supported via diagram service Generate.</summary>
    [Fact]
    public void DiagramService_Generate_PlantUml_ContainsStartUml()
    {
        var svc = new MermaidUseCaseDiagramService();
        var dto = new UseCaseDetailDto
        {
            UseCaseId = 1,
            Title = "Login",
            Actors = [new UseCaseActorDto { ActorId = 1, Name = "User", Type = "Primary", IsPrimary = true }],
            Flows =
            [
                new UseCaseFlowDto
                {
                    FlowId = 1,
                    FlowType = "Basic",
                    SequenceNumber = 1,
                    Steps =
                    [
                        new UseCaseStepDto { StepId = 1, StepNumber = 1, ActorId = 1, ActorName = "User", Action = "Submit credentials" },
                    ],
                },
            ],
        };

        var plant = svc.Generate(dto, "plantuml");
        Assert.Contains("@startuml", plant, StringComparison.Ordinal);
        Assert.Contains("Submit credentials", plant, StringComparison.Ordinal);
        Assert.Contains("@enduml", plant, StringComparison.Ordinal);
    }

    /// <summary>FR-MCP-USECASE-009: Product key assignment and list-by-product hook.</summary>
    [Fact]
    public async Task ProductKey_AssignAndListByProduct()
    {
        using var scope = _provider.CreateScope();
        var ws = scope.ServiceProvider.GetRequiredService<WorkspaceContext>();
        ws.WorkspacePath = _workspace;
        var dispatcher = scope.ServiceProvider.GetRequiredService<IDispatcher>();

        var created = await dispatcher.SendAsync(
            new CreateUseCaseCommand(_workspace, new CreateUseCaseRequest { Title = "Product UC" }),
            CancellationToken.None).ConfigureAwait(true);
        Assert.True(created.IsSuccess, created.Error);

        var keyed = await dispatcher.SendAsync(
            new SetUseCaseProductKeyCommand(_workspace, created.Value!.UseCaseId, "prod-mcp-core"),
            CancellationToken.None).ConfigureAwait(true);
        Assert.True(keyed.IsSuccess, keyed.Error);
        Assert.Equal("prod-mcp-core", keyed.Value!.ProductKey);

        var listed = await dispatcher.QueryAsync(
            new ListUseCasesByProductQuery(_workspace, "prod-mcp-core"),
            CancellationToken.None).ConfigureAwait(true);
        Assert.True(listed.IsSuccess, listed.Error);
        Assert.Contains(listed.Value!, u => u.UseCaseId == created.Value.UseCaseId);
    }

    /// <summary>
    /// FR-MCP-USECASE-010: Shared evaluator reports FR without Realizes UC link for traceability tooling.
    /// </summary>
    [Fact]
    public async Task CoverageEvaluator_ReportsFrWithoutUseCaseLink()
    {
        using var scope = _provider.CreateScope();
        var ws = scope.ServiceProvider.GetRequiredService<WorkspaceContext>();
        ws.WorkspacePath = _workspace;
        var db = scope.ServiceProvider.GetRequiredService<McpDbContext>();
        db.OverrideWorkspaceId(_workspace);

        var now = DateTimeOffset.UtcNow.ToString("O");
        db.Requirements.Add(new RequirementEntity
        {
            WorkspaceId = _workspace,
            Kind = "fr",
            Id = "FR-MCP-COVERAGE-001",
            Title = "Needs UC",
            Body = "body",
            CreatedAtUtc = now,
            UpdatedAtUtc = now,
        });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken).ConfigureAwait(true);

        var snap = await UseCaseFrCoverageEvaluator.EvaluateAsync(db, TestContext.Current.CancellationToken).ConfigureAwait(true);
        Assert.Contains("FR-MCP-COVERAGE-001", snap.FunctionalRequirementsWithoutRealizesUseCase);
    }

    /// <summary>
    /// FR-MCP-USECASE-010: Traceability gate reports FR/UC without Realizes link; clears UC finding after link.
    /// </summary>
    [Fact]
    public async Task TraceabilityGate_ValidateRealizesCoverage_ClearsAfterLink()
    {
        using var scope = _provider.CreateScope();
        var ws = scope.ServiceProvider.GetRequiredService<WorkspaceContext>();
        ws.WorkspacePath = _workspace;
        var db = scope.ServiceProvider.GetRequiredService<McpDbContext>();
        db.OverrideWorkspaceId(_workspace);
        var dispatcher = scope.ServiceProvider.GetRequiredService<IDispatcher>();

        const string frId = "FR-MCP-GATE-001";
        var now = DateTimeOffset.UtcNow.ToString("O");
        db.Requirements.Add(new RequirementEntity
        {
            WorkspaceId = _workspace,
            Kind = "fr",
            Id = frId,
            Title = "Gate FR",
            Body = "body",
            CreatedAtUtc = now,
            UpdatedAtUtc = now,
        });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken).ConfigureAwait(true);

        var created = await dispatcher.SendAsync(
            new CreateUseCaseCommand(_workspace, new CreateUseCaseRequest { Title = "Gate UC" }),
            CancellationToken.None).ConfigureAwait(true);
        Assert.True(created.IsSuccess, created.Error);
        var useCaseId = created.Value!.UseCaseId;

        var before = await UseCaseTraceabilityGate.ValidateRealizesCoverageAsync(db, TestContext.Current.CancellationToken)
            .ConfigureAwait(true);
        Assert.NotEmpty(before);
        Assert.Contains(before, f => f.Contains(frId, StringComparison.OrdinalIgnoreCase));
        Assert.Contains(before, f => f.Contains(useCaseId.ToString(), StringComparison.Ordinal));

        var linked = await dispatcher.SendAsync(
            new LinkUseCaseToFrCommand(_workspace, useCaseId, frId, UseCaseFrCoverageEvaluator.Realizes, 0, null),
            CancellationToken.None).ConfigureAwait(true);
        Assert.True(linked.IsSuccess, linked.Error);

        var after = await UseCaseTraceabilityGate.ValidateRealizesCoverageAsync(db, TestContext.Current.CancellationToken)
            .ConfigureAwait(true);
        Assert.DoesNotContain(after, f => f.Contains(frId, StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(after, f => f.Contains($"UseCase {useCaseId} ", StringComparison.Ordinal));
    }
}
