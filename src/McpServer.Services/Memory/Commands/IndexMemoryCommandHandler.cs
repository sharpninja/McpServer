using McpServer.Cqrs;
using McpServer.Support.Mcp.Storage;
using Microsoft.EntityFrameworkCore;

namespace McpServer.Support.Mcp.Services;

/// <summary>
/// TR-MCP-MEMORY-SEARCH-002: Handles <see cref="IndexMemoryCommand"/>.
/// </summary>
public sealed class IndexMemoryCommandHandler(DbContextOptions<McpDbContext> options)
    : ICommandHandler<IndexMemoryCommand, MemoryIndexResult>
{
    /// <inheritdoc />
    public async Task<Result<MemoryIndexResult>> HandleAsync(IndexMemoryCommand command, CallContext context)
    {
        try
        {
            await using var db = new McpDbContext(options, new WorkspaceContext { WorkspacePath = command.WorkspacePath });
            var operations = new MemorySearchOperations(db);
            var result = await operations.IndexAsync(
                command.MemoryId,
                command.Provider,
                command.CloudEnabled,
                context.CancellationToken).ConfigureAwait(false);
            return Result<MemoryIndexResult>.Success(result);
        }
        catch (OperationCanceledException)
        {
            return Result<MemoryIndexResult>.Success(new MemoryIndexResult(
                200,
                command.MemoryId,
                "pending"));
        }
    }
}

/// <summary>
/// TR-MCP-MEMORY-SEARCH-002: Handles <see cref="ReconcileMemoryIndexCommand"/>.
/// </summary>
public sealed class ReconcileMemoryIndexCommandHandler(DbContextOptions<McpDbContext> options)
    : ICommandHandler<ReconcileMemoryIndexCommand, MemoryIndexResult>
{
    /// <inheritdoc />
    public async Task<Result<MemoryIndexResult>> HandleAsync(ReconcileMemoryIndexCommand command, CallContext context)
    {
        await using var db = new McpDbContext(options, new WorkspaceContext { WorkspacePath = command.WorkspacePath });
        var operations = new MemorySearchOperations(db);
        var result = await operations.ReconcileAsync(context.CancellationToken).ConfigureAwait(false);
        return Result<MemoryIndexResult>.Success(result);
    }
}

/// <summary>
/// TR-MCP-MEMORY-SEARCH-002: Handles <see cref="BatchIndexMemoriesCommand"/>.
/// </summary>
public sealed class BatchIndexMemoriesCommandHandler(DbContextOptions<McpDbContext> options)
    : ICommandHandler<BatchIndexMemoriesCommand, MemoryIndexResult>
{
    /// <inheritdoc />
    public async Task<Result<MemoryIndexResult>> HandleAsync(BatchIndexMemoriesCommand command, CallContext context)
    {
        await using var db = new McpDbContext(options, new WorkspaceContext { WorkspacePath = command.WorkspacePath });
        var operations = new MemorySearchOperations(db);
        var result = await operations.BatchIndexAsync(command.MemoryIds, context.CancellationToken).ConfigureAwait(false);
        return Result<MemoryIndexResult>.Success(result);
    }
}
