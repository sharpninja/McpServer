using QBrainAi.Cqrs;
using QBrainAi.Support.Mcp.Storage;
using Microsoft.EntityFrameworkCore;
using QBrainAi.Support.Mcp.Services;
using QBrainAi.Support.Mcp.Memory.Models;
using QBrainAi.Support.Mcp.Storage.Services;

namespace QBrainAi.Support.Mcp.Memory.Commands;

/// <summary>
/// FR-MCP-MEMORY-013 / TEST-MCP-MEMORY-013: Handles <see cref="ConsolidateMemoryCommand"/>.
/// </summary>
public sealed class ConsolidateMemoryCommandHandler(DbContextOptions<McpDbContext> options)
    : ICommandHandler<ConsolidateMemoryCommand, MemoryConsolidateResult>
{
    /// <inheritdoc />
    public async Task<Result<MemoryConsolidateResult>> HandleAsync(ConsolidateMemoryCommand command, CallContext context)
    {
        var db = new McpDbContext(options, new WorkspaceContext { WorkspacePath = command.WorkspacePath });
        await using var dbDisposal = db.ConfigureAwait(false);
        var operations = new MemoryConsolidateOperations(db, command.WorkspacePath);
        var result = await operations.ConsolidateAsync(command.Request, command.ReadOnlyCaller, context.CancellationToken)
            .ConfigureAwait(false);
        return Result<MemoryConsolidateResult>.Success(result);
    }
}
