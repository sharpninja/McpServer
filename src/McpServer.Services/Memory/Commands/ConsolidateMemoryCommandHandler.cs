using McpServer.Cqrs;
using McpServer.Support.Mcp.Storage;
using Microsoft.EntityFrameworkCore;

namespace McpServer.Support.Mcp.Services;

/// <summary>
/// FR-MCP-MEMORY-013 / TEST-MCP-MEMORY-013: Handles <see cref="ConsolidateMemoryCommand"/>.
/// </summary>
public sealed class ConsolidateMemoryCommandHandler(DbContextOptions<McpDbContext> options)
    : ICommandHandler<ConsolidateMemoryCommand, MemoryConsolidateResult>
{
    /// <inheritdoc />
    public async Task<Result<MemoryConsolidateResult>> HandleAsync(ConsolidateMemoryCommand command, CallContext context)
    {
        await using var db = new McpDbContext(options, new WorkspaceContext { WorkspacePath = command.WorkspacePath });
        var operations = new MemoryConsolidateOperations(db, command.WorkspacePath);
        var result = await operations.ConsolidateAsync(command.Request, command.ReadOnlyCaller, context.CancellationToken)
            .ConfigureAwait(false);
        return Result<MemoryConsolidateResult>.Success(result);
    }
}
