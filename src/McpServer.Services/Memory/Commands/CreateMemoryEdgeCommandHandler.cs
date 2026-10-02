using McpServer.Cqrs;
using McpServer.Support.Mcp.Storage;
using Microsoft.EntityFrameworkCore;

namespace McpServer.Support.Mcp.Services;

/// <summary>
/// FR-MCP-MEMORY-012 / TEST-MCP-MEMORY-012: Handles <see cref="CreateMemoryEdgeCommand"/>.
/// Rejects self-loops, invalid types/weights, duplicates, and foreign targets.
/// </summary>
public sealed class CreateMemoryEdgeCommandHandler(DbContextOptions<McpDbContext> options)
    : ICommandHandler<CreateMemoryEdgeCommand, MemoryCreateEdgeResult>
{
    /// <inheritdoc />
    public async Task<Result<MemoryCreateEdgeResult>> HandleAsync(CreateMemoryEdgeCommand command, CallContext context)
    {
        await using var db = new McpDbContext(options, new WorkspaceContext { WorkspacePath = command.WorkspacePath });
        var operations = new MemoryExploreOperations(db);
        var result = await operations.CreateEdgeAsync(command.Request, command.ReadOnlyCaller, context.CancellationToken)
            .ConfigureAwait(false);
        return Result<MemoryCreateEdgeResult>.Success(result);
    }
}

/// <summary>
/// FR-MCP-MEMORY-012 / TEST-MCP-MEMORY-012: Handles <see cref="RecordHebbianCoRetrievalCommand"/>.
/// Strengthens co-retrieved edges only when Hebbian is enabled and does not change recall ranking.
/// </summary>
public sealed class RecordHebbianCoRetrievalCommandHandler(DbContextOptions<McpDbContext> options)
    : ICommandHandler<RecordHebbianCoRetrievalCommand, MemoryHebbianResult>
{
    /// <inheritdoc />
    public async Task<Result<MemoryHebbianResult>> HandleAsync(RecordHebbianCoRetrievalCommand command, CallContext context)
    {
        await using var db = new McpDbContext(options, new WorkspaceContext { WorkspacePath = command.WorkspacePath });
        var operations = new MemoryExploreOperations(db);
        var result = await operations.RecordHebbianAsync(
            command.MemoryIds,
            command.HebbianEnabled,
            context.CancellationToken).ConfigureAwait(false);
        return Result<MemoryHebbianResult>.Success(result);
    }
}
