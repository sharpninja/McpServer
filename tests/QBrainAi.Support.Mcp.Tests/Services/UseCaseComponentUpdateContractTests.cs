using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using QBrainAi.Cqrs;
using QBrainAi.Support.Mcp.Services;
using QBrainAi.Support.Mcp.Storage;
using QBrainAi.Support.Mcp.Storage.Entities;
using QBrainAi.Support.Mcp.UseCases.Commands;
using QBrainAi.Support.Mcp.UseCases.Models;
using Xunit;

namespace QBrainAi.Support.Mcp.Tests.Services;

/// <summary>
/// TEST-MCP-USECASE-021 / FR-MCP-USECASE-018 / TR-MCP-USECASE-020:
/// verifies the identity-preserving component-update contract against an independent scripted fake
/// before production update commands exist. Fixtures use fixed unrelated identifiers so assertions
/// reject accidental replacement rows, parent drift, field loss, and extra mutations.
/// </summary>
public sealed class UseCaseComponentUpdateContractTests
{
    private static readonly ActorUpdateInput ActorInput = new(
        UseCaseId: 125,
        ActorId: 20,
        Name: "LAB-OMARCHY TruckMate Tentacle",
        Description: "Octopus Tentacle on LAB-OMARCHY deploys commit-pinned TruckMate services.",
        Type: "System",
        IsPrimary: true);

    private static readonly FlowUpdateInput FlowInput = new(
        UseCaseId: 125,
        FlowId: 314,
        FlowType: "Basic",
        Name: "Octopus-owned TruckMate deployment",
        SequenceNumber: 1);

    private static readonly StepUpdateInput StepInput = new(
        UseCaseId: 125,
        FlowId: 314,
        StepId: 383,
        StepNumber: 1,
        ActorId: 20,
        Action: "Octopus dispatches the commit-pinned Nuke deployment.",
        SystemResponse: "The LAB-OMARCHY Tentacle runs the requested Nuke target.",
        DataEntities: "Commit SHA, Octopus release, Nuke target");

    /// <summary>
    /// TEST-MCP-USECASE-021 AC1: correct actor, flow, and step responses from an independent fake
    /// preserve all fixed identities and fields, proving the positive contract before production edits.
    /// </summary>
    [Fact]
    public async Task CorrectFakeResponses_SatisfyIdentityPreservingContract()
    {
        var fake = ScriptedEditor.CreateCorrect();

        var actor = await fake.UpdateActorAsync(ActorInput, TestContext.Current.CancellationToken).ConfigureAwait(true);
        var flow = await fake.UpdateFlowAsync(FlowInput, TestContext.Current.CancellationToken).ConfigureAwait(true);
        var step = await fake.UpdateStepAsync(StepInput, TestContext.Current.CancellationToken).ConfigureAwait(true);

        AssertActorContract(ActorInput, actor);
        AssertFlowContract(FlowInput, flow);
        AssertStepContract(StepInput, step);
        Assert.Equal(3, fake.TotalMutations);
        Assert.Equal(["actor:125:20", "flow:125:314", "step:125:314:383"], fake.Calls);
    }

    /// <summary>
    /// TEST-MCP-USECASE-021 AC2: the production CQRS handlers satisfy the same identity and field
    /// assertions used by the independent fake while preserving aggregate state and row counts.
    /// </summary>
    [Fact]
    public async Task CorrectRealHandlers_SatisfyIdentityPreservingContract()
    {
        await using var editor = await RealHandlerEditor.CreateAsync().ConfigureAwait(true);

        var actor = await editor.UpdateActorAsync(ActorInput, TestContext.Current.CancellationToken).ConfigureAwait(true);
        var flow = await editor.UpdateFlowAsync(FlowInput, TestContext.Current.CancellationToken).ConfigureAwait(true);
        var step = await editor.UpdateStepAsync(StepInput, TestContext.Current.CancellationToken).ConfigureAwait(true);

        AssertActorContract(ActorInput, actor);
        AssertFlowContract(FlowInput, flow);
        AssertStepContract(StepInput, step);
        await editor.AssertAggregateInvariantsAsync(TestContext.Current.CancellationToken).ConfigureAwait(true);
    }

    /// <summary>
    /// TEST-MCP-USECASE-021 AC3-AC5: invalid values, missing associations, parent mismatches,
    /// soft-deleted records, and cross-workspace lookups fail without mutating durable rows.
    /// </summary>
    [Fact]
    public async Task RealHandlers_RejectInvalidAndOutOfScopeTargetsWithoutMutation()
    {
        await using var editor = await RealHandlerEditor.CreateAsync().ConfigureAwait(true);

        await editor.AssertInvalidAndOutOfScopeUpdatesFailAsync(
            TestContext.Current.CancellationToken).ConfigureAwait(true);
    }


    /// <summary>
    /// TEST-MCP-USECASE-021 AC1: deliberately wrong actor identity, fields, and mutation count are
    /// rejected by the exact positive assertions, proving those assertions discriminate replacement
    /// rows and incomplete writes while the test harness itself remains green.
    /// </summary>
    [Fact]
    public async Task ActorNegativeControls_AreRejectedInsidePassingHarness()
    {
        var wrongIdentity = ScriptedEditor.CreateCorrect() with
        {
            ActorResultFactory = input => new ActorUpdateResult(
                input.UseCaseId,
                input.ActorId + 9000,
                input.Name,
                input.Description,
                input.Type,
                input.IsPrimary,
                1),
        };
        var wrongFields = ScriptedEditor.CreateCorrect() with
        {
            ActorResultFactory = input => new ActorUpdateResult(
                input.UseCaseId,
                input.ActorId,
                "stale-name",
                input.Description,
                input.Type,
                input.IsPrimary,
                1),
        };
        var wrongCount = ScriptedEditor.CreateCorrect() with
        {
            ActorResultFactory = input => new ActorUpdateResult(
                input.UseCaseId,
                input.ActorId,
                input.Name,
                input.Description,
                input.Type,
                input.IsPrimary,
                2),
        };

        Assert.NotNull(Record.Exception(() => AssertActorContract(
            ActorInput,
            wrongIdentity.UpdateActorAsync(ActorInput, CancellationToken.None).GetAwaiter().GetResult())));
        Assert.NotNull(Record.Exception(() => AssertActorContract(
            ActorInput,
            wrongFields.UpdateActorAsync(ActorInput, CancellationToken.None).GetAwaiter().GetResult())));
        Assert.NotNull(Record.Exception(() => AssertActorContract(
            ActorInput,
            wrongCount.UpdateActorAsync(ActorInput, CancellationToken.None).GetAwaiter().GetResult())));

        await Task.CompletedTask.ConfigureAwait(true);
    }

    /// <summary>
    /// TEST-MCP-USECASE-021 AC1: deliberately wrong flow and step parent identities, field values,
    /// and mutation counts are rejected by the shared assertions, proving parent and no-duplicate
    /// checks before the real CQRS handlers are introduced.
    /// </summary>
    [Fact]
    public void FlowAndStepNegativeControls_AreRejectedInsidePassingHarness()
    {
        var wrongFlow = new FlowUpdateResult(
            FlowInput.UseCaseId + 1,
            FlowInput.FlowId,
            FlowInput.FlowType,
            FlowInput.Name,
            FlowInput.SequenceNumber,
            1);
        var wrongStepParent = new StepUpdateResult(
            StepInput.UseCaseId,
            StepInput.FlowId + 1,
            StepInput.StepId,
            StepInput.StepNumber,
            StepInput.ActorId,
            StepInput.Action,
            StepInput.SystemResponse,
            StepInput.DataEntities,
            1);
        var wrongStepFields = new StepUpdateResult(
            StepInput.UseCaseId,
            StepInput.FlowId,
            StepInput.StepId,
            StepInput.StepNumber,
            StepInput.ActorId,
            "stale-action",
            StepInput.SystemResponse,
            StepInput.DataEntities,
            1);
        var duplicateStepMutation = wrongStepFields with
        {
            Action = StepInput.Action,
            MutationCount = 2,
        };

        Assert.NotNull(Record.Exception(() => AssertFlowContract(FlowInput, wrongFlow)));
        Assert.NotNull(Record.Exception(() => AssertStepContract(StepInput, wrongStepParent)));
        Assert.NotNull(Record.Exception(() => AssertStepContract(StepInput, wrongStepFields)));
        Assert.NotNull(Record.Exception(() => AssertStepContract(StepInput, duplicateStepMutation)));
    }

    /// <summary>
    /// TEST-MCP-USECASE-021 AC1: the fake propagates a scripted dependency failure and observes
    /// cancellation, proving the contract tests do not replace failure behavior with successful data.
    /// </summary>
    [Fact]
    public async Task FailureAndCancellationControls_AreObservable()
    {
        var expected = new InvalidOperationException("scripted editor failure");
        var failing = ScriptedEditor.CreateCorrect() with
        {
            Failure = expected,
        };

        var actual = await Assert.ThrowsAsync<InvalidOperationException>(
            () => failing.UpdateFlowAsync(FlowInput, TestContext.Current.CancellationToken)).ConfigureAwait(true);
        Assert.Same(expected, actual);

        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => ScriptedEditor.CreateCorrect().UpdateStepAsync(StepInput, cancellation.Token)).ConfigureAwait(true);
    }

    private static void AssertActorContract(ActorUpdateInput expected, ActorUpdateResult actual)
    {
        Assert.Equal(expected.UseCaseId, actual.UseCaseId);
        Assert.Equal(expected.ActorId, actual.ActorId);
        Assert.Equal(expected.Name, actual.Name);
        Assert.Equal(expected.Description, actual.Description);
        Assert.Equal(expected.Type, actual.Type);
        Assert.Equal(expected.IsPrimary, actual.IsPrimary);
        Assert.Equal(1, actual.MutationCount);
    }

    private static void AssertFlowContract(FlowUpdateInput expected, FlowUpdateResult actual)
    {
        Assert.Equal(expected.UseCaseId, actual.UseCaseId);
        Assert.Equal(expected.FlowId, actual.FlowId);
        Assert.Equal(expected.FlowType, actual.FlowType);
        Assert.Equal(expected.Name, actual.Name);
        Assert.Equal(expected.SequenceNumber, actual.SequenceNumber);
        Assert.Equal(1, actual.MutationCount);
    }

    private static void AssertStepContract(StepUpdateInput expected, StepUpdateResult actual)
    {
        Assert.Equal(expected.UseCaseId, actual.UseCaseId);
        Assert.Equal(expected.FlowId, actual.FlowId);
        Assert.Equal(expected.StepId, actual.StepId);
        Assert.Equal(expected.StepNumber, actual.StepNumber);
        Assert.Equal(expected.ActorId, actual.ActorId);
        Assert.Equal(expected.Action, actual.Action);
        Assert.Equal(expected.SystemResponse, actual.SystemResponse);
        Assert.Equal(expected.DataEntities, actual.DataEntities);
        Assert.Equal(1, actual.MutationCount);
    }

    private interface IUseCaseComponentEditor
    {
        Task<ActorUpdateResult> UpdateActorAsync(
            ActorUpdateInput input,
            CancellationToken cancellationToken);

        Task<FlowUpdateResult> UpdateFlowAsync(
            FlowUpdateInput input,
            CancellationToken cancellationToken);

        Task<StepUpdateResult> UpdateStepAsync(
            StepUpdateInput input,
            CancellationToken cancellationToken);
    }

    private sealed class RealHandlerEditor : IUseCaseComponentEditor, IAsyncDisposable
    {
        private readonly SqliteConnection _connection;
        private readonly McpDbContext _db;
        private readonly WorkspaceContext _workspaceContext;
        private readonly DateTimeOffset _initialUpdatedAt;
        private readonly IReadOnlyDictionary<string, int> _initialAuditUpdateCounts;

        private RealHandlerEditor(
            SqliteConnection connection,
            McpDbContext db,
            WorkspaceContext workspaceContext,
            DateTimeOffset initialUpdatedAt,
            IReadOnlyDictionary<string, int> initialAuditUpdateCounts)
        {
            _connection = connection;
            _db = db;
            _workspaceContext = workspaceContext;
            _initialUpdatedAt = initialUpdatedAt;
            _initialAuditUpdateCounts = initialAuditUpdateCounts;
        }

        public static async Task<RealHandlerEditor> CreateAsync()
        {
            var workspace = Path.Combine(
                Path.GetTempPath(),
                "mcp-uc-component-update-" + Guid.NewGuid().ToString("N"));
            var workspaceContext = new WorkspaceContext
            {
                WorkspacePath = workspace,
                WorkspaceName = "uc-component-update",
                DataDirectory = workspace,
                TodoFilePath = Path.Combine(workspace, "docs", "todo.yaml"),
                SessionsPath = Path.Combine(workspace, "docs", "sessions"),
                ExternalDocsPath = Path.Combine(workspace, "docs", "external"),
            };
            var connection = new SqliteConnection("Data Source=:memory:");
            await connection.OpenAsync(TestContext.Current.CancellationToken).ConfigureAwait(true);
            var options = new DbContextOptionsBuilder<McpDbContext>()
                .UseSqlite(connection)
                .Options;
            var db = new McpDbContext(options, workspaceContext);
            await db.Database.EnsureCreatedAsync(TestContext.Current.CancellationToken).ConfigureAwait(true);

            var initialUpdatedAt = DateTimeOffset.UtcNow.AddHours(-1);
            db.Workspaces.Add(new WorkspaceEntity
            {
                WorkspaceId = workspace,
                WorkspacePath = workspace,
                Name = "Use-case component update",
                DataDirectory = workspace,
                DateTimeCreated = initialUpdatedAt,
                DateTimeModified = initialUpdatedAt,
            });
            db.UseCases.Add(new UseCaseEntity
            {
                UseCaseId = ActorInput.UseCaseId,
                WorkspaceId = workspace,
                Title = "Deploy TruckMate services",
                ApprovalStatus = "Draft",
                VersionNumber = 1,
                CreatedAtUtc = initialUpdatedAt,
                UpdatedAtUtc = initialUpdatedAt,
            });
            db.UseCases.Add(new UseCaseEntity
            {
                UseCaseId = 126,
                WorkspaceId = workspace,
                Title = "Parent mismatch target",
                ApprovalStatus = "Draft",
                VersionNumber = 1,
                CreatedAtUtc = initialUpdatedAt,
                UpdatedAtUtc = initialUpdatedAt,
            });
            db.Actors.Add(new ActorEntity
            {
                ActorId = ActorInput.ActorId,
                WorkspaceId = workspace,
                Name = "stale actor",
                Description = "stale description",
                Type = "External",
            });
            db.UseCaseActors.Add(new UseCaseActorEntity
            {
                WorkspaceId = workspace,
                UseCaseId = ActorInput.UseCaseId,
                ActorId = ActorInput.ActorId,
                IsPrimary = false,
            });
            db.UseCaseFlows.AddRange(
                new UseCaseFlowEntity
                {
                    FlowId = FlowInput.FlowId,
                    WorkspaceId = workspace,
                    UseCaseId = FlowInput.UseCaseId,
                    FlowType = "Alternative",
                    Name = "stale flow",
                    SequenceNumber = 9,
                },
                new UseCaseFlowEntity
                {
                    FlowId = 315,
                    WorkspaceId = workspace,
                    UseCaseId = FlowInput.UseCaseId,
                    FlowType = "Alternative",
                    Name = "unrelated flow",
                    SequenceNumber = 2,
                });
            db.UseCaseSteps.AddRange(
                new UseCaseStepEntity
                {
                    StepId = StepInput.StepId,
                    WorkspaceId = workspace,
                    FlowId = StepInput.FlowId,
                    StepNumber = 9,
                    Action = "stale action",
                    SystemResponse = "stale response",
                    DataEntities = "stale entities",
                },
                new UseCaseStepEntity
                {
                    StepId = 384,
                    WorkspaceId = workspace,
                    FlowId = 315,
                    StepNumber = 1,
                    Action = "unrelated action",
                    SystemResponse = "unrelated response",
                    DataEntities = "unrelated entities",
                });
            await db.SaveChangesAsync(TestContext.Current.CancellationToken).ConfigureAwait(true);

            var deletedActor = new ActorEntity
            {
                ActorId = 91,
                WorkspaceId = workspace,
                Name = "deleted actor",
                Type = "External",
            };
            var deletedAssociation = new UseCaseActorEntity
            {
                WorkspaceId = workspace,
                UseCaseId = ActorInput.UseCaseId,
                ActorId = deletedActor.ActorId,
                IsPrimary = false,
            };
            var deletedFlow = new UseCaseFlowEntity
            {
                FlowId = 316,
                WorkspaceId = workspace,
                UseCaseId = FlowInput.UseCaseId,
                FlowType = "Exception",
                Name = "deleted flow",
                SequenceNumber = 3,
            };
            var deletedStep = new UseCaseStepEntity
            {
                StepId = 385,
                WorkspaceId = workspace,
                FlowId = 315,
                StepNumber = 2,
                Action = "deleted step",
            };
            db.Actors.Add(deletedActor);
            db.UseCaseActors.Add(deletedAssociation);
            db.UseCaseFlows.Add(deletedFlow);
            db.UseCaseSteps.Add(deletedStep);
            await db.SaveChangesAsync(TestContext.Current.CancellationToken).ConfigureAwait(true);
            foreach (var entity in new object[] { deletedActor, deletedAssociation, deletedFlow, deletedStep })
            {
                db.Entry(entity).Property("IsDeleted").CurrentValue = true;
                db.Entry(entity).Property("DeletedAtUtc").CurrentValue = DateTimeOffset.UtcNow;
                db.Entry(entity).Property("DeletedBy").CurrentValue = "test";
                db.Entry(entity).Property("DeleteReason").CurrentValue = "soft-delete contract";
            }

            await db.SaveChangesAsync(TestContext.Current.CancellationToken).ConfigureAwait(true);
            var initialAuditUpdateCounts = await db.DataAuditLogs
                .AsNoTracking()
                .Where(row => row.Action == "update")
                .GroupBy(row => row.EntityKind)
                .ToDictionaryAsync(
                    group => group.Key,
                    group => group.Count(),
                    TestContext.Current.CancellationToken)
                .ConfigureAwait(true);
            return new RealHandlerEditor(
                connection,
                db,
                workspaceContext,
                initialUpdatedAt,
                initialAuditUpdateCounts);
        }

        public async Task<ActorUpdateResult> UpdateActorAsync(
            ActorUpdateInput input,
            CancellationToken cancellationToken)
        {
            var result = await new UpdateUseCaseActorCommandHandler(_db, _workspaceContext)
                .HandleAsync(
                    new UpdateUseCaseActorCommand(
                        _workspaceContext.WorkspacePath!,
                        input.UseCaseId,
                        input.ActorId,
                        new UpdateUseCaseActorRequest
                        {
                            Name = input.Name,
                            Description = input.Description,
                            Type = input.Type,
                            IsPrimary = input.IsPrimary,
                        }),
                    new CallContext { CancellationToken = cancellationToken })
                .ConfigureAwait(true);
            Assert.True(result.IsSuccess, result.Error);
            var value = Assert.IsType<UseCaseActorDto>(result.Value);
            return new ActorUpdateResult(
                value.UseCaseId,
                value.ActorId,
                value.Name,
                value.Description,
                value.Type,
                value.IsPrimary,
                await _db.UseCaseActors.CountAsync(
                    a => a.UseCaseId == input.UseCaseId && a.ActorId == input.ActorId,
                    cancellationToken).ConfigureAwait(true));
        }

        public async Task<FlowUpdateResult> UpdateFlowAsync(
            FlowUpdateInput input,
            CancellationToken cancellationToken)
        {
            var result = await new UpdateUseCaseFlowCommandHandler(_db, _workspaceContext)
                .HandleAsync(
                    new UpdateUseCaseFlowCommand(
                        _workspaceContext.WorkspacePath!,
                        input.UseCaseId,
                        input.FlowId,
                        new UpdateUseCaseFlowRequest
                        {
                            FlowType = input.FlowType,
                            Name = input.Name,
                            SequenceNumber = input.SequenceNumber,
                        }),
                    new CallContext { CancellationToken = cancellationToken })
                .ConfigureAwait(true);
            Assert.True(result.IsSuccess, result.Error);
            var value = Assert.IsType<UseCaseFlowDto>(result.Value);
            return new FlowUpdateResult(
                value.UseCaseId,
                value.FlowId,
                value.FlowType,
                value.Name,
                value.SequenceNumber,
                await _db.UseCaseFlows.CountAsync(
                    f => f.UseCaseId == input.UseCaseId && f.FlowId == input.FlowId,
                    cancellationToken).ConfigureAwait(true));
        }

        public async Task<StepUpdateResult> UpdateStepAsync(
            StepUpdateInput input,
            CancellationToken cancellationToken)
        {
            var result = await new UpdateUseCaseStepCommandHandler(_db, _workspaceContext)
                .HandleAsync(
                    new UpdateUseCaseStepCommand(
                        _workspaceContext.WorkspacePath!,
                        input.UseCaseId,
                        input.FlowId,
                        input.StepId,
                        new UpdateUseCaseStepRequest
                        {
                            StepNumber = input.StepNumber,
                            ActorId = input.ActorId,
                            Action = input.Action,
                            SystemResponse = input.SystemResponse,
                            DataEntities = input.DataEntities,
                        }),
                    new CallContext { CancellationToken = cancellationToken })
                .ConfigureAwait(true);
            Assert.True(result.IsSuccess, result.Error);
            var value = Assert.IsType<UseCaseStepDto>(result.Value);
            return new StepUpdateResult(
                input.UseCaseId,
                value.FlowId,
                value.StepId,
                value.StepNumber,
                value.ActorId,
                value.Action,
                value.SystemResponse,
                value.DataEntities,
                await _db.UseCaseSteps.CountAsync(
                    s => s.FlowId == input.FlowId && s.StepId == input.StepId,
                    cancellationToken).ConfigureAwait(true));
        }

        public async Task AssertInvalidAndOutOfScopeUpdatesFailAsync(CancellationToken cancellationToken)
        {
            var auditUpdatesBefore = await _db.DataAuditLogs
                .CountAsync(row => row.Action == "update", cancellationToken)
                .ConfigureAwait(true);
            var actorBefore = await _db.Actors.AsNoTracking()
                .SingleAsync(actor => actor.ActorId == ActorInput.ActorId, cancellationToken)
                .ConfigureAwait(true);
            var flowBefore = await _db.UseCaseFlows.AsNoTracking()
                .SingleAsync(flow => flow.FlowId == FlowInput.FlowId, cancellationToken)
                .ConfigureAwait(true);
            var stepBefore = await _db.UseCaseSteps.AsNoTracking()
                .SingleAsync(step => step.StepId == StepInput.StepId, cancellationToken)
                .ConfigureAwait(true);
            var callContext = new CallContext { CancellationToken = cancellationToken };
            var actorHandler = new UpdateUseCaseActorCommandHandler(_db, _workspaceContext);
            var flowHandler = new UpdateUseCaseFlowCommandHandler(_db, _workspaceContext);
            var stepHandler = new UpdateUseCaseStepCommandHandler(_db, _workspaceContext);

            var blankActor = await actorHandler.HandleAsync(
                new UpdateUseCaseActorCommand(
                    _workspaceContext.WorkspacePath!,
                    ActorInput.UseCaseId,
                    ActorInput.ActorId,
                    new UpdateUseCaseActorRequest { Name = " ", Type = "System" }),
                callContext).ConfigureAwait(true);
            Assert.False(blankActor.IsSuccess);
            Assert.Contains("Name is required", blankActor.Error, StringComparison.Ordinal);

            var invalidActorType = await actorHandler.HandleAsync(
                new UpdateUseCaseActorCommand(
                    _workspaceContext.WorkspacePath!,
                    ActorInput.UseCaseId,
                    ActorInput.ActorId,
                    new UpdateUseCaseActorRequest { Name = "valid", Type = "Unknown" }),
                callContext).ConfigureAwait(true);
            Assert.False(invalidActorType.IsSuccess);
            Assert.Contains("Type is invalid", invalidActorType.Error, StringComparison.Ordinal);

            var missingActorAssociation = await actorHandler.HandleAsync(
                new UpdateUseCaseActorCommand(
                    _workspaceContext.WorkspacePath!,
                    ActorInput.UseCaseId,
                    999,
                    new UpdateUseCaseActorRequest { Name = "valid", Type = "System" }),
                callContext).ConfigureAwait(true);
            Assert.False(missingActorAssociation.IsSuccess);
            Assert.Contains("was not found on use case", missingActorAssociation.Error, StringComparison.Ordinal);

            var deletedActor = await actorHandler.HandleAsync(
                new UpdateUseCaseActorCommand(
                    _workspaceContext.WorkspacePath!,
                    ActorInput.UseCaseId,
                    91,
                    new UpdateUseCaseActorRequest { Name = "valid", Type = "System" }),
                callContext).ConfigureAwait(true);
            Assert.False(deletedActor.IsSuccess);
            Assert.Contains("was not found on use case", deletedActor.Error, StringComparison.Ordinal);

            var crossWorkspaceActor = await actorHandler.HandleAsync(
                new UpdateUseCaseActorCommand(
                    _workspaceContext.WorkspacePath! + "-other",
                    ActorInput.UseCaseId,
                    ActorInput.ActorId,
                    new UpdateUseCaseActorRequest { Name = "valid", Type = "System" }),
                callContext).ConfigureAwait(true);
            Assert.False(crossWorkspaceActor.IsSuccess);
            Assert.Contains("Use case", crossWorkspaceActor.Error, StringComparison.Ordinal);

            var invalidFlowType = await flowHandler.HandleAsync(
                new UpdateUseCaseFlowCommand(
                    _workspaceContext.WorkspacePath!,
                    FlowInput.UseCaseId,
                    FlowInput.FlowId,
                    new UpdateUseCaseFlowRequest { FlowType = "Unknown", SequenceNumber = 1 }),
                callContext).ConfigureAwait(true);
            Assert.False(invalidFlowType.IsSuccess);
            Assert.Contains("FlowType is invalid", invalidFlowType.Error, StringComparison.Ordinal);

            var invalidFlowSequence = await flowHandler.HandleAsync(
                new UpdateUseCaseFlowCommand(
                    _workspaceContext.WorkspacePath!,
                    FlowInput.UseCaseId,
                    FlowInput.FlowId,
                    new UpdateUseCaseFlowRequest { FlowType = "Basic", SequenceNumber = 0 }),
                callContext).ConfigureAwait(true);
            Assert.False(invalidFlowSequence.IsSuccess);
            Assert.Contains("must be positive", invalidFlowSequence.Error, StringComparison.Ordinal);

            var mismatchedFlowParent = await flowHandler.HandleAsync(
                new UpdateUseCaseFlowCommand(
                    _workspaceContext.WorkspacePath!,
                    126,
                    FlowInput.FlowId,
                    new UpdateUseCaseFlowRequest { FlowType = "Basic", SequenceNumber = 1 }),
                callContext).ConfigureAwait(true);
            Assert.False(mismatchedFlowParent.IsSuccess);
            Assert.Contains("was not found on use case", mismatchedFlowParent.Error, StringComparison.Ordinal);

            var deletedFlow = await flowHandler.HandleAsync(
                new UpdateUseCaseFlowCommand(
                    _workspaceContext.WorkspacePath!,
                    FlowInput.UseCaseId,
                    316,
                    new UpdateUseCaseFlowRequest { FlowType = "Basic", SequenceNumber = 1 }),
                callContext).ConfigureAwait(true);
            Assert.False(deletedFlow.IsSuccess);
            Assert.Contains("was not found on use case", deletedFlow.Error, StringComparison.Ordinal);

            var invalidStepNumber = await stepHandler.HandleAsync(
                new UpdateUseCaseStepCommand(
                    _workspaceContext.WorkspacePath!,
                    StepInput.UseCaseId,
                    StepInput.FlowId,
                    StepInput.StepId,
                    new UpdateUseCaseStepRequest { StepNumber = 0, Action = "valid" }),
                callContext).ConfigureAwait(true);
            Assert.False(invalidStepNumber.IsSuccess);
            Assert.Contains("must be positive", invalidStepNumber.Error, StringComparison.Ordinal);

            var blankStepAction = await stepHandler.HandleAsync(
                new UpdateUseCaseStepCommand(
                    _workspaceContext.WorkspacePath!,
                    StepInput.UseCaseId,
                    StepInput.FlowId,
                    StepInput.StepId,
                    new UpdateUseCaseStepRequest { StepNumber = 1, Action = " " }),
                callContext).ConfigureAwait(true);
            Assert.False(blankStepAction.IsSuccess);
            Assert.Contains("Action is required", blankStepAction.Error, StringComparison.Ordinal);

            var mismatchedStepParent = await stepHandler.HandleAsync(
                new UpdateUseCaseStepCommand(
                    _workspaceContext.WorkspacePath!,
                    StepInput.UseCaseId,
                    315,
                    StepInput.StepId,
                    new UpdateUseCaseStepRequest { StepNumber = 1, Action = "valid" }),
                callContext).ConfigureAwait(true);
            Assert.False(mismatchedStepParent.IsSuccess);
            Assert.Contains("was not found on flow", mismatchedStepParent.Error, StringComparison.Ordinal);

            var unassociatedActor = await stepHandler.HandleAsync(
                new UpdateUseCaseStepCommand(
                    _workspaceContext.WorkspacePath!,
                    StepInput.UseCaseId,
                    StepInput.FlowId,
                    StepInput.StepId,
                    new UpdateUseCaseStepRequest { StepNumber = 1, ActorId = 999, Action = "valid" }),
                callContext).ConfigureAwait(true);
            Assert.False(unassociatedActor.IsSuccess);
            Assert.Contains("was not found on use case", unassociatedActor.Error, StringComparison.Ordinal);

            var deletedStep = await stepHandler.HandleAsync(
                new UpdateUseCaseStepCommand(
                    _workspaceContext.WorkspacePath!,
                    StepInput.UseCaseId,
                    315,
                    385,
                    new UpdateUseCaseStepRequest { StepNumber = 1, Action = "valid" }),
                callContext).ConfigureAwait(true);
            Assert.False(deletedStep.IsSuccess);
            Assert.Contains("was not found on flow", deletedStep.Error, StringComparison.Ordinal);

            _db.OverrideWorkspaceId(_workspaceContext.WorkspacePath!);
            _db.ChangeTracker.Clear();

            var actorAfter = await _db.Actors.AsNoTracking()
                .SingleAsync(actor => actor.ActorId == ActorInput.ActorId, cancellationToken)
                .ConfigureAwait(true);
            var flowAfter = await _db.UseCaseFlows.AsNoTracking()
                .SingleAsync(flow => flow.FlowId == FlowInput.FlowId, cancellationToken)
                .ConfigureAwait(true);
            var stepAfter = await _db.UseCaseSteps.AsNoTracking()
                .SingleAsync(step => step.StepId == StepInput.StepId, cancellationToken)
                .ConfigureAwait(true);
            Assert.Equal(actorBefore.Name, actorAfter.Name);
            Assert.Equal(actorBefore.Description, actorAfter.Description);
            Assert.Equal(actorBefore.Type, actorAfter.Type);
            Assert.Equal(flowBefore.FlowType, flowAfter.FlowType);
            Assert.Equal(flowBefore.Name, flowAfter.Name);
            Assert.Equal(flowBefore.SequenceNumber, flowAfter.SequenceNumber);
            Assert.Equal(stepBefore.StepNumber, stepAfter.StepNumber);
            Assert.Equal(stepBefore.Action, stepAfter.Action);
            Assert.Equal(stepBefore.SystemResponse, stepAfter.SystemResponse);
            Assert.Equal(stepBefore.DataEntities, stepAfter.DataEntities);
            Assert.Equal(
                auditUpdatesBefore,
                await _db.DataAuditLogs.CountAsync(
                    row => row.Action == "update",
                    cancellationToken).ConfigureAwait(true));
        }

        public async Task AssertAggregateInvariantsAsync(CancellationToken cancellationToken)
        {
            var useCase = await _db.UseCases
                .SingleAsync(u => u.UseCaseId == ActorInput.UseCaseId, cancellationToken)
                .ConfigureAwait(true);
            Assert.Equal("Draft", useCase.ApprovalStatus);
            Assert.Equal(1, useCase.VersionNumber);
            Assert.True(useCase.UpdatedAtUtc > _initialUpdatedAt);
            Assert.Equal(2, await _db.UseCaseFlows.CountAsync(cancellationToken).ConfigureAwait(true));
            Assert.Equal(2, await _db.UseCaseSteps.CountAsync(cancellationToken).ConfigureAwait(true));

            var unrelatedFlow = await _db.UseCaseFlows
                .SingleAsync(f => f.FlowId == 315, cancellationToken)
                .ConfigureAwait(true);
            Assert.Equal("unrelated flow", unrelatedFlow.Name);
            Assert.Equal("Alternative", unrelatedFlow.FlowType);
            Assert.Equal(2, unrelatedFlow.SequenceNumber);

            var unrelatedStep = await _db.UseCaseSteps
                .SingleAsync(s => s.StepId == 384, cancellationToken)
                .ConfigureAwait(true);
            Assert.Equal("unrelated action", unrelatedStep.Action);
            Assert.Equal("unrelated response", unrelatedStep.SystemResponse);
            Assert.Equal("unrelated entities", unrelatedStep.DataEntities);

            var auditUpdateCounts = await _db.DataAuditLogs
                .AsNoTracking()
                .Where(row => row.Action == "update")
                .GroupBy(row => row.EntityKind)
                .ToDictionaryAsync(
                    group => group.Key,
                    group => group.Count(),
                    cancellationToken)
                .ConfigureAwait(true);
            foreach (var entityKind in new[]
                     {
                         nameof(ActorEntity),
                         nameof(UseCaseActorEntity),
                         nameof(UseCaseFlowEntity),
                         nameof(UseCaseStepEntity),
                         nameof(UseCaseEntity),
                     })
            {
                _initialAuditUpdateCounts.TryGetValue(entityKind, out var initialCount);
                Assert.True(
                    auditUpdateCounts.TryGetValue(entityKind, out var currentCount) && currentCount > initialCount,
                    $"Expected a new update audit row for {entityKind}.");
            }
        }

        public async ValueTask DisposeAsync()
        {
            await _db.DisposeAsync().ConfigureAwait(true);
            await _connection.DisposeAsync().ConfigureAwait(true);
        }
    }

    private sealed record ScriptedEditor : IUseCaseComponentEditor
    {
        public Func<ActorUpdateInput, ActorUpdateResult> ActorResultFactory { get; init; } =
            input => new ActorUpdateResult(
                input.UseCaseId,
                input.ActorId,
                input.Name,
                input.Description,
                input.Type,
                input.IsPrimary,
                1);

        public Func<FlowUpdateInput, FlowUpdateResult> FlowResultFactory { get; init; } =
            input => new FlowUpdateResult(
                input.UseCaseId,
                input.FlowId,
                input.FlowType,
                input.Name,
                input.SequenceNumber,
                1);

        public Func<StepUpdateInput, StepUpdateResult> StepResultFactory { get; init; } =
            input => new StepUpdateResult(
                input.UseCaseId,
                input.FlowId,
                input.StepId,
                input.StepNumber,
                input.ActorId,
                input.Action,
                input.SystemResponse,
                input.DataEntities,
                1);

        public Exception? Failure { get; init; }

        public int TotalMutations { get; private set; }

        public List<string> Calls { get; } = [];

        public static ScriptedEditor CreateCorrect() => new();

        public Task<ActorUpdateResult> UpdateActorAsync(
            ActorUpdateInput input,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            ThrowIfFailed();
            Calls.Add($"actor:{input.UseCaseId}:{input.ActorId}");
            TotalMutations++;
            return Task.FromResult(ActorResultFactory(input));
        }

        public Task<FlowUpdateResult> UpdateFlowAsync(
            FlowUpdateInput input,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            ThrowIfFailed();
            Calls.Add($"flow:{input.UseCaseId}:{input.FlowId}");
            TotalMutations++;
            return Task.FromResult(FlowResultFactory(input));
        }

        public Task<StepUpdateResult> UpdateStepAsync(
            StepUpdateInput input,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            ThrowIfFailed();
            Calls.Add($"step:{input.UseCaseId}:{input.FlowId}:{input.StepId}");
            TotalMutations++;
            return Task.FromResult(StepResultFactory(input));
        }

        private void ThrowIfFailed()
        {
            if (Failure is not null)
                throw Failure;
        }
    }

    private sealed record ActorUpdateInput(
        long UseCaseId,
        long ActorId,
        string Name,
        string? Description,
        string Type,
        bool IsPrimary);

    private sealed record FlowUpdateInput(
        long UseCaseId,
        long FlowId,
        string FlowType,
        string? Name,
        int SequenceNumber);

    private sealed record StepUpdateInput(
        long UseCaseId,
        long FlowId,
        long StepId,
        int StepNumber,
        long? ActorId,
        string Action,
        string? SystemResponse,
        string? DataEntities);

    private sealed record ActorUpdateResult(
        long UseCaseId,
        long ActorId,
        string Name,
        string? Description,
        string Type,
        bool IsPrimary,
        int MutationCount);

    private sealed record FlowUpdateResult(
        long UseCaseId,
        long FlowId,
        string FlowType,
        string? Name,
        int SequenceNumber,
        int MutationCount);

    private sealed record StepUpdateResult(
        long UseCaseId,
        long FlowId,
        long StepId,
        int StepNumber,
        long? ActorId,
        string Action,
        string? SystemResponse,
        string? DataEntities,
        int MutationCount);
}
