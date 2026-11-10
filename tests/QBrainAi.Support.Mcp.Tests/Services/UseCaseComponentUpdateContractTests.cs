using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using QBrainAi.Cqrs;
using QBrainAi.Support.Mcp.Services;
using QBrainAi.Support.Mcp.Storage;
using QBrainAi.Support.Mcp.Storage.Entities;
using QBrainAi.Support.Mcp.UseCases.Commands;
using QBrainAi.Support.Mcp.UseCases.Models;
using Xunit;
using static QBrainAi.Support.Mcp.Tests.Services.UseCaseComponentUpdateContractTestsSupport;

namespace QBrainAi.Support.Mcp.Tests.Services;

/// <summary>
/// TEST-MCP-USECASE-021 / FR-MCP-USECASE-018 / TR-MCP-USECASE-020:
/// verifies the identity-preserving component-update contract against an independent scripted fake
/// before production update commands exist. Fixtures use fixed unrelated identifiers so assertions
/// reject accidental replacement rows, parent drift, field loss, and extra mutations.
/// </summary>
/// <remarks>2026-11-07, TEST-MCP-USECASE-021; FR-MCP-USECASE-018; TR-MCP-USECASE-020; FR-MCP-BUILD-001; TR-MCP-NUKE-019 AC-7: the unit half of this class: its 2 Category=Integration cases (CorrectRealHandlers_SatisfyIdentityPreservingContract and RealHandlers_RejectInvalidAndOutOfScopeTargetsWithoutMutation) and the helpers only they use (RealHandlerEditor) moved unchanged to the class of the same name in the integration-owned QBrainAi.Support.Mcp.EnvironmentIntegrationTests (slice S7, hostile validation finding R14-F4), so this unit-owned project holds only unit cases; the helpers both halves use (ActorInput, FlowInput, StepInput, AssertActorContract, AssertFlowContract, AssertStepContract, IUseCaseComponentEditor, ActorUpdateInput, FlowUpdateInput, StepUpdateInput, ActorUpdateResult, FlowUpdateResult and StepUpdateResult) moved once into Services/UseCaseComponentUpdateContractTestsSupport.cs beside this file, which the integration project links (DRY); the unit cases and the class attributes are unchanged.</remarks>
public sealed class UseCaseComponentUpdateContractTests
{
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
}
