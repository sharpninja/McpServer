using McpServer.Cqrs;
using McpServer.Support.Mcp.Storage;
using Microsoft.EntityFrameworkCore;

namespace McpServer.Support.Mcp.Services;

/// <summary>
/// TR-MCP-MEMORY-JOBS-002: Handles <see cref="RunConsolidateJobCommand"/>.
/// </summary>
public sealed class RunConsolidateJobCommandHandler(DbContextOptions<McpDbContext> options)
    : ICommandHandler<RunConsolidateJobCommand, MemoryConsolidateJobResult>
{
    /// <inheritdoc />
    public async Task<Result<MemoryConsolidateJobResult>> HandleAsync(RunConsolidateJobCommand command, CallContext context)
    {
        await using var db = new McpDbContext(options, new WorkspaceContext { WorkspacePath = command.WorkspacePath });
        var operations = new MemoryConsolidateOperations(db, command.WorkspacePath);
        var result = await operations.RunJobAsync(command.DryRun, command.ForceOverlap, context.CancellationToken)
            .ConfigureAwait(false);
        return Result<MemoryConsolidateJobResult>.Success(result);
    }
}
