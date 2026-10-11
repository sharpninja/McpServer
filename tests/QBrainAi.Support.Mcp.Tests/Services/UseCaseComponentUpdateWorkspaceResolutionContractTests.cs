using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using QBrainAi.Cqrs;
using QBrainAi.Support.Mcp.Services;
using QBrainAi.Support.Mcp.Storage;
using QBrainAi.Support.Mcp.Storage.Entities;
using QBrainAi.Support.Mcp.UseCases.Commands;
using QBrainAi.Support.Mcp.UseCases.Models;
using Xunit;
using Xunit.Sdk;

namespace QBrainAi.Support.Mcp.Tests.Services;

/// <summary>
/// TEST-MCP-USECASE-240 / FR-MCP-USECASE-018 / TR-MCP-USECASE-020:
/// proves component updates resolve equivalent workspace paths through the canonical workspace identity policy.
/// </summary>
public sealed class UseCaseComponentUpdateWorkspaceResolutionContractTests
{
    private const string CanonicalWorkspaceId = @"F:\GitHub\TruckMate";

    [Fact]
    public async Task MockGate_CanonicalComponentUpdates_PassContract()
    {
        await AssertComponentUpdateContractAsync(
            () => Task.FromResult(SuccessActor()),
            () => Task.FromResult(SuccessFlow()),
            () => Task.FromResult(SuccessStep())).ConfigureAwait(true);
    }

    [Fact]
    public async Task MockGate_RawWorkspaceActorFailure_IsRejected()
    {
        var rejection = await Assert.ThrowsAsync<TrueException>(
            () => AssertComponentUpdateContractAsync(
                () => Task.FromResult(Result<UseCaseActorDto>.Failure("raw workspace actor lookup failed")),
                () => Task.FromResult(SuccessFlow()),
                () => Task.FromResult(SuccessStep()))).ConfigureAwait(true);

        Assert.Contains("raw workspace actor lookup failed", rejection.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task MockGate_RawWorkspaceFlowFailure_IsRejected()
    {
        var rejection = await Assert.ThrowsAsync<TrueException>(
            () => AssertComponentUpdateContractAsync(
                () => Task.FromResult(SuccessActor()),
                () => Task.FromResult(Result<UseCaseFlowDto>.Failure("raw workspace flow lookup failed")),
                () => Task.FromResult(SuccessStep()))).ConfigureAwait(true);

        Assert.Contains("raw workspace flow lookup failed", rejection.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task MockGate_RawWorkspaceStepFailure_IsRejected()
    {
        var rejection = await Assert.ThrowsAsync<TrueException>(
            () => AssertComponentUpdateContractAsync(
                () => Task.FromResult(SuccessActor()),
                () => Task.FromResult(SuccessFlow()),
                () => Task.FromResult(Result<UseCaseStepDto>.Failure("raw workspace step lookup failed"))))
            .ConfigureAwait(true);

        Assert.Contains("raw workspace step lookup failed", rejection.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task ProductionGate_EquivalentRawPath_UpdatesExistingComponents()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync(TestContext.Current.CancellationToken).ConfigureAwait(true);
        var options = new DbContextOptionsBuilder<McpDbContext>()
            .UseSqlite(connection)
            .Options;
        var workspaceContext = new WorkspaceContext
        {
            WorkspacePath = CanonicalWorkspaceId,
            WorkspaceName = "TruckMate",
            DataDirectory = CanonicalWorkspaceId,
        };

        await using var db = new McpDbContext(options, workspaceContext);
        await db.Database.EnsureCreatedAsync(TestContext.Current.CancellationToken).ConfigureAwait(true);
        db.OverrideWorkspaceId(CanonicalWorkspaceId);
        var now = DateTimeOffset.UtcNow;
        db.Workspaces.Add(new WorkspaceEntity
        {
            WorkspaceId = CanonicalWorkspaceId,
            WorkspacePath = CanonicalWorkspaceId,
            Name = "TruckMate",
            DateTimeCreated = now,
            DateTimeModified = now,
        });
        db.UseCases.Add(new UseCaseEntity
        {
            UseCaseId = 125,
            WorkspaceId = CanonicalWorkspaceId,
            Title = "Start and certify LAB-OMARCHY TruckMate services",
            ApprovalStatus = "Draft",
            VersionNumber = 1,
            CreatedAtUtc = now,
            UpdatedAtUtc = now,
        });
        db.Actors.Add(new ActorEntity
        {
            ActorId = 17,
            WorkspaceId = CanonicalWorkspaceId,
            Name = "stale operator",
            Type = "External",
        });
        db.UseCaseActors.Add(new UseCaseActorEntity
        {
            WorkspaceId = CanonicalWorkspaceId,
            UseCaseId = 125,
            ActorId = 17,
            IsPrimary = false,
        });
        db.UseCaseFlows.Add(new UseCaseFlowEntity
        {
            FlowId = 260,
            WorkspaceId = CanonicalWorkspaceId,
            UseCaseId = 125,
            FlowType = "Alternative",
            Name = "Admin mutation lacks authenticated grant",
            SequenceNumber = 3,
        });
        db.UseCaseSteps.Add(new UseCaseStepEntity
        {
            StepId = 364,
            WorkspaceId = CanonicalWorkspaceId,
            FlowId = 260,
            StepNumber = 1,
            Action = "Request a deployed Admin mutation.",
            SystemResponse = "Reject unauthorized mutation.",
        });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken).ConfigureAwait(true);

        var equivalentRawPath = CanonicalWorkspaceId + Path.DirectorySeparatorChar + ".";
        var callContext = new CallContext { CancellationToken = TestContext.Current.CancellationToken };
        var actorHandler = new UpdateUseCaseActorCommandHandler(db, workspaceContext);
        var flowHandler = new UpdateUseCaseFlowCommandHandler(db, workspaceContext);
        var stepHandler = new UpdateUseCaseStepCommandHandler(db, workspaceContext);

        await AssertComponentUpdateContractAsync(
            () => actorHandler.HandleAsync(
                new UpdateUseCaseActorCommand(
                    equivalentRawPath,
                    125,
                    17,
                    new UpdateUseCaseActorRequest
                    {
                        Name = "Release operator",
                        Description = "Requests release and validates evidence",
                        Type = "Primary",
                        IsPrimary = true,
                    }),
                callContext),
            () => flowHandler.HandleAsync(
                new UpdateUseCaseFlowCommand(
                    equivalentRawPath,
                    125,
                    260,
                    new UpdateUseCaseFlowRequest
                    {
                        FlowType = "Alternative",
                        Name = "Admin mutation lacks authenticated grant",
                        SequenceNumber = 4,
                    }),
                callContext),
            () => stepHandler.HandleAsync(
                new UpdateUseCaseStepCommand(
                    equivalentRawPath,
                    125,
                    260,
                    364,
                    new UpdateUseCaseStepRequest
                    {
                        StepNumber = 1,
                        ActorId = 17,
                        Action = "Request a deployed Admin mutation.",
                        SystemResponse = "Reject unauthorized mutation.",
                    }),
                callContext)).ConfigureAwait(true);
    }

    private static async Task AssertComponentUpdateContractAsync(
        Func<Task<Result<UseCaseActorDto>>> updateActor,
        Func<Task<Result<UseCaseFlowDto>>> updateFlow,
        Func<Task<Result<UseCaseStepDto>>> updateStep)
    {
        var actor = await updateActor().ConfigureAwait(true);
        Assert.True(actor.IsSuccess, actor.Error);
        Assert.NotNull(actor.Value);
        Assert.Equal(125, actor.Value!.UseCaseId);
        Assert.Equal(17, actor.Value.ActorId);
        Assert.Equal("Release operator", actor.Value.Name);
        Assert.Equal("Primary", actor.Value.Type);
        Assert.True(actor.Value.IsPrimary);

        var flow = await updateFlow().ConfigureAwait(true);
        Assert.True(flow.IsSuccess, flow.Error);
        Assert.NotNull(flow.Value);
        Assert.Equal(125, flow.Value!.UseCaseId);
        Assert.Equal(260, flow.Value.FlowId);
        Assert.Equal("Alternative", flow.Value.FlowType);
        Assert.Equal("Admin mutation lacks authenticated grant", flow.Value.Name);
        Assert.Equal(4, flow.Value.SequenceNumber);

        var step = await updateStep().ConfigureAwait(true);
        Assert.True(step.IsSuccess, step.Error);
        Assert.NotNull(step.Value);
        Assert.Equal(260, step.Value!.FlowId);
        Assert.Equal(364, step.Value.StepId);
        Assert.Equal(1, step.Value.StepNumber);
        Assert.Equal(17, step.Value.ActorId);
        Assert.Equal("Request a deployed Admin mutation.", step.Value.Action);
        Assert.Equal("Reject unauthorized mutation.", step.Value.SystemResponse);
    }

    private static Result<UseCaseActorDto> SuccessActor()
        => Result<UseCaseActorDto>.Success(new UseCaseActorDto
        {
            UseCaseId = 125,
            ActorId = 17,
            Name = "Release operator",
            Description = "Requests release and validates evidence",
            Type = "Primary",
            IsPrimary = true,
        });

    private static Result<UseCaseFlowDto> SuccessFlow()
        => Result<UseCaseFlowDto>.Success(new UseCaseFlowDto
        {
            UseCaseId = 125,
            FlowId = 260,
            FlowType = "Alternative",
            Name = "Admin mutation lacks authenticated grant",
            SequenceNumber = 4,
        });

    private static Result<UseCaseStepDto> SuccessStep()
        => Result<UseCaseStepDto>.Success(new UseCaseStepDto
        {
            FlowId = 260,
            StepId = 364,
            StepNumber = 1,
            ActorId = 17,
            Action = "Request a deployed Admin mutation.",
            SystemResponse = "Reject unauthorized mutation.",
        });
}
