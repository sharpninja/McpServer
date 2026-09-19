using McpServer.Support.Mcp.Services;

namespace McpServer.Support.Mcp.Tests.Memory;

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
