using QBrainAi.Support.Mcp.Services;

namespace QBrainAi.Support.Mcp.Tests.Memory;

/// <summary>
/// No requirement id applies: this part of MemoryS1Harness holds the S4 dispatch helpers ConsolidateAsync, PromoteAsync
/// and RunConsolidateJobAsync; each sends its consolidate, promote or consolidate-job command through the production
/// CQRS dispatcher for workspace A unless another workspace is given, and returns a 501 result carrying the dispatch
/// error when no handler answers. The memory tests that build a MemoryS1Harness use them.
/// </summary>
/// <remarks>2026-11-07, no requirement id applies: documented in slice S9 of hostile validation finding R14-F4, because it had no XML documentation (CLAUDE.md: what is tested, the data or fixtures used, the requirement ids validated); the code is unchanged.</remarks>
public sealed partial class MemoryS1Harness
{
    /// <summary>Consolidates through the S4 CQRS command port when a handler is registered.</summary>
    public async Task<MemoryConsolidateResult> ConsolidateAsync(
        MemoryConsolidateRequest? request = null,
        string? workspacePath = null,
        bool readOnlyCaller = false,
        CancellationToken cancellationToken = default)
    {
        var dispatcher = CreateProductionDispatcher();
        var dispatched = await dispatcher.SendAsync(
            new ConsolidateMemoryCommand(workspacePath ?? WorkspaceA, request, readOnlyCaller),
            cancellationToken).ConfigureAwait(true);
        if (dispatched.IsSuccess && dispatched.Value is not null)
            return dispatched.Value;

        return new MemoryConsolidateResult(
            StatusCode: 501,
            FailureKind: MemoryMutationFailureKind.None,
            Error: dispatched.Error ?? dispatched.Exception?.Message ?? "memory_consolidate handler is not registered");
    }

    /// <summary>Promotes through the S4 CQRS command port when a handler is registered.</summary>
    public async Task<MemoryPromoteResult> PromoteAsync(
        MemoryPromoteRequest? request,
        string? workspacePath = null,
        bool readOnlyCaller = false,
        CancellationToken cancellationToken = default)
    {
        var dispatcher = CreateProductionDispatcher();
        var dispatched = await dispatcher.SendAsync(
            new PromoteMemoryCommand(workspacePath ?? WorkspaceA, request, readOnlyCaller),
            cancellationToken).ConfigureAwait(true);
        if (dispatched.IsSuccess && dispatched.Value is not null)
            return dispatched.Value;

        return new MemoryPromoteResult(
            StatusCode: 501,
            FailureKind: MemoryMutationFailureKind.None,
            Error: dispatched.Error ?? dispatched.Exception?.Message ?? "memory_promote handler is not registered");
    }

    /// <summary>Runs a consolidate job tick through the S4 CQRS command port when a handler is registered.</summary>
    public async Task<MemoryConsolidateJobResult> RunConsolidateJobAsync(
        bool? dryRun = null,
        string? workspacePath = null,
        bool forceOverlap = false,
        CancellationToken cancellationToken = default)
    {
        var dispatcher = CreateProductionDispatcher();
        var dispatched = await dispatcher.SendAsync(
            new RunConsolidateJobCommand(workspacePath ?? WorkspaceA, dryRun, forceOverlap),
            cancellationToken).ConfigureAwait(true);
        if (dispatched.IsSuccess && dispatched.Value is not null)
            return dispatched.Value;

        return new MemoryConsolidateJobResult(
            StatusCode: 501,
            FailureKind: MemoryMutationFailureKind.None,
            Error: dispatched.Error ?? dispatched.Exception?.Message ?? "memory consolidate job handler is not registered");
    }

}

/// <summary>
/// TEST-MCP-MEMORY-013 / TEST-MCP-MEMORY-015: Port used only for S4 mocks-first contract proof. Not a production facade.
/// </summary>
public interface IMemoryS4Port
{
    /// <summary>Plans or applies a consolidate merge.</summary>
    Task<MemoryConsolidateResult> ConsolidateAsync(MemoryConsolidateRequest? request, CancellationToken cancellationToken);

    /// <summary>Promotes a sessionlog or context source into memory.</summary>
    Task<MemoryPromoteResult> PromoteAsync(MemoryPromoteRequest? request, CancellationToken cancellationToken);

    /// <summary>Runs a scheduled consolidate tick.</summary>
    Task<MemoryConsolidateJobResult> RunJobAsync(bool? dryRun, bool forceOverlap, CancellationToken cancellationToken);
}
