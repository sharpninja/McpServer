using McpServer.Cqrs;
using McpServer.Support.Mcp.Services;
using McpServer.Support.Mcp.Storage;
using McpServer.Support.Mcp.Storage.Entities;
using McpServer.Support.Mcp.UseCases;
using McpServer.Support.Mcp.UseCases.Commands;
using McpServer.Support.Mcp.UseCases.Models;
using McpServer.Support.Mcp.UseCases.Queries;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Xunit;

namespace McpServer.Support.Mcp.Tests.Services;

/// <summary>
/// Expanded-scope Use Case tests: PlantUML diagrams, approval versioning, product key hooks,
/// and shared Realizes coverage evaluation for traceability.
/// </summary>
public sealed class UseCaseExpandedScopeTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly ServiceProvider _provider;
    private readonly string _workspace = Path.Combine(Path.GetTempPath(), "mcp-uc-expanded-" + Guid.NewGuid().ToString("N"));

    /// <summary>Builds an in-memory EF store with Use Case CQRS registered.</summary>
    public UseCaseExpandedScopeTests()
    {
        _connection = new SqliteConnection("Data Source=:memory:");
        _connection.Open();
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddDbContext<McpDbContext>(o => o.UseSqlite(_connection));
        services.AddScoped<WorkspaceContext>();
        services.AddCqrsDispatcher();
        services.AddUseCaseCqrs();
        _provider = services.BuildServiceProvider();

        using var scope = _provider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<McpDbContext>();
        db.Database.EnsureCreated();
        db.Workspaces.Add(new WorkspaceEntity { WorkspaceId = _workspace, WorkspacePath = _workspace, Name = "expanded" });
        db.SaveChanges();
    }

    /// <inheritdoc />
    public void Dispose()
    {
        _provider.Dispose();
        _connection.Dispose();
    }

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

    /// <summary>FR-MCP-USECASE-008: Approving increments version number.</summary>
    [Fact]
    public async Task Approval_Approve_IncrementsVersion()
    {
        using var scope = _provider.CreateScope();
        var ws = scope.ServiceProvider.GetRequiredService<WorkspaceContext>();
        ws.WorkspacePath = _workspace;
        var dispatcher = scope.ServiceProvider.GetRequiredService<IDispatcher>();

        var created = await dispatcher.SendAsync(
            new CreateUseCaseCommand(_workspace, new CreateUseCaseRequest { Title = "Approve me", CreateBasicFlow = true }),
            CancellationToken.None).ConfigureAwait(true);
        Assert.True(created.IsSuccess, created.Error);
        Assert.Equal(1, created.Value!.VersionNumber);
        Assert.Equal("Draft", created.Value.ApprovalStatus);

        var submitted = await dispatcher.SendAsync(
            new SetUseCaseApprovalStatusCommand(_workspace, created.Value.UseCaseId, "Submitted"),
            CancellationToken.None).ConfigureAwait(true);
        Assert.True(submitted.IsSuccess, submitted.Error);

        var approved = await dispatcher.SendAsync(
            new SetUseCaseApprovalStatusCommand(_workspace, created.Value.UseCaseId, "Approved"),
            CancellationToken.None).ConfigureAwait(true);
        Assert.True(approved.IsSuccess, approved.Error);
        Assert.Equal("Approved", approved.Value!.ApprovalStatus);
        Assert.Equal(2, approved.Value.VersionNumber);
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
