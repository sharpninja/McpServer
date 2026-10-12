using QBrainAi.Cqrs;
using QBrainAi.Support.Mcp.Storage;
using Microsoft.EntityFrameworkCore;
using QBrainAi.Support.Mcp.Services;
using QBrainAi.Support.Mcp.Memory.Models;
using QBrainAi.Support.Mcp.Storage.Services;

namespace QBrainAi.Support.Mcp.Memory.Commands;

/// <summary>
/// FR-MCP-MEMORY-015 / TEST-MCP-MEMORY-015: Handles <see cref="PromoteMemoryCommand"/>.
/// </summary>
public sealed class PromoteMemoryCommandHandler(DbContextOptions<McpDbContext> options)
    : ICommandHandler<PromoteMemoryCommand, MemoryPromoteResult>
{
    /// <inheritdoc />
    public async Task<Result<MemoryPromoteResult>> HandleAsync(PromoteMemoryCommand command, CallContext context)
    {
        await using var db = new McpDbContext(options, new WorkspaceContext { WorkspacePath = command.WorkspacePath });
        var operations = new MemoryPromoteOperations(db, command.WorkspacePath);
        var result = await operations.PromoteAsync(command.Request, command.ReadOnlyCaller, context.CancellationToken)
            .ConfigureAwait(false);
        return Result<MemoryPromoteResult>.Success(result);
    }
}
