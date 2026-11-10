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
/// TEST-MCP-USECASE-021; FR-MCP-USECASE-018; TR-MCP-USECASE-020: the helpers that both halves of UseCaseComponentUpdateContractTests use (the unit half in QBrainAi.Support.Mcp.Tests, the integration half in QBrainAi.Support.Mcp.EnvironmentIntegrationTests), defined once and compiled into both projects.
/// </summary>
/// <remarks>2026-11-07, TEST-MCP-USECASE-021; FR-MCP-USECASE-018; TR-MCP-USECASE-020; FR-MCP-BUILD-001; TR-MCP-NUKE-019 AC-7: extracted unchanged from UseCaseComponentUpdateContractTests (ActorInput, FlowInput, StepInput, AssertActorContract, AssertFlowContract, AssertStepContract, IUseCaseComponentEditor, ActorUpdateInput, FlowUpdateInput, StepUpdateInput, ActorUpdateResult, FlowUpdateResult and StepUpdateResult) when that class split into its unit half and its integration half (slice S7, hostile validation finding R14-F4), so both halves use one definition instead of a copy (DRY); only the accessibility changed, from private to internal.</remarks>
internal static class UseCaseComponentUpdateContractTestsSupport
{
    internal static readonly ActorUpdateInput ActorInput = new(
        UseCaseId: 125,
        ActorId: 20,
        Name: "TEST-NODE-B ExampleProject Tentacle",
        Description: "Octopus Tentacle on TEST-NODE-B deploys commit-pinned ExampleProject services.",
        Type: "System",
        IsPrimary: true);

    internal static readonly FlowUpdateInput FlowInput = new(
        UseCaseId: 125,
        FlowId: 314,
        FlowType: "Basic",
        Name: "Octopus-owned ExampleProject deployment",
        SequenceNumber: 1);

    internal static readonly StepUpdateInput StepInput = new(
        UseCaseId: 125,
        FlowId: 314,
        StepId: 383,
        StepNumber: 1,
        ActorId: 20,
        Action: "Octopus dispatches the commit-pinned Nuke deployment.",
        SystemResponse: "The TEST-NODE-B Tentacle runs the requested Nuke target.",
        DataEntities: "Commit SHA, Octopus release, Nuke target");

    internal static void AssertActorContract(ActorUpdateInput expected, ActorUpdateResult actual)
    {
        Assert.Equal(expected.UseCaseId, actual.UseCaseId);
        Assert.Equal(expected.ActorId, actual.ActorId);
        Assert.Equal(expected.Name, actual.Name);
        Assert.Equal(expected.Description, actual.Description);
        Assert.Equal(expected.Type, actual.Type);
        Assert.Equal(expected.IsPrimary, actual.IsPrimary);
        Assert.Equal(1, actual.MutationCount);
    }

    internal static void AssertFlowContract(FlowUpdateInput expected, FlowUpdateResult actual)
    {
        Assert.Equal(expected.UseCaseId, actual.UseCaseId);
        Assert.Equal(expected.FlowId, actual.FlowId);
        Assert.Equal(expected.FlowType, actual.FlowType);
        Assert.Equal(expected.Name, actual.Name);
        Assert.Equal(expected.SequenceNumber, actual.SequenceNumber);
        Assert.Equal(1, actual.MutationCount);
    }

    internal static void AssertStepContract(StepUpdateInput expected, StepUpdateResult actual)
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

    internal interface IUseCaseComponentEditor
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

    internal sealed record ActorUpdateInput(
        long UseCaseId,
        long ActorId,
        string Name,
        string? Description,
        string Type,
        bool IsPrimary);

    internal sealed record FlowUpdateInput(
        long UseCaseId,
        long FlowId,
        string FlowType,
        string? Name,
        int SequenceNumber);

    internal sealed record StepUpdateInput(
        long UseCaseId,
        long FlowId,
        long StepId,
        int StepNumber,
        long? ActorId,
        string Action,
        string? SystemResponse,
        string? DataEntities);

    internal sealed record ActorUpdateResult(
        long UseCaseId,
        long ActorId,
        string Name,
        string? Description,
        string Type,
        bool IsPrimary,
        int MutationCount);

    internal sealed record FlowUpdateResult(
        long UseCaseId,
        long FlowId,
        string FlowType,
        string? Name,
        int SequenceNumber,
        int MutationCount);

    internal sealed record StepUpdateResult(
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
