using QBrainAi.Cqrs;
using QBrainAi.Support.Mcp.Storage;
using Microsoft.EntityFrameworkCore;

namespace QBrainAi.Support.Mcp.Services;

/// <summary>
/// TR-MCP-MEMORY-JOBS-002: Handles <see cref="RunConsolidateJobCommand"/>.
/// </summary>
/// <param name="options">The database options.</param>
/// <param name="time">The clock the job's workspace lock is read from; null uses <see cref="TimeProvider.System"/>.</param>
/// <remarks>2026-11-05, TR-MCP-MEMORY-JOBS-002; TR-MCP-USECASE-018 AC-006 (appended "The same holds for every age, expiry or staleness decision a unit test checks"): passes the optional clock to the operations, so a unit test supplies the time its lock decision reads (hostile validation round 12 finding R12-F2, sibling S37). TR-MCP-MEMORY-JOBS-002 in this file is a historical id of the memory plan, not a record in the requirement store, so it makes no coverage claim (labeled 2026-11-05 before hostile validation round 13).</remarks>
public sealed class RunConsolidateJobCommandHandler(DbContextOptions<McpDbContext> options, TimeProvider? time = null)
    : ICommandHandler<RunConsolidateJobCommand, MemoryConsolidateJobResult>
{
    /// <inheritdoc />
    public async Task<Result<MemoryConsolidateJobResult>> HandleAsync(RunConsolidateJobCommand command, CallContext context)
    {
        await using var db = new McpDbContext(options, new WorkspaceContext { WorkspacePath = command.WorkspacePath });
        var operations = new MemoryConsolidateOperations(db, command.WorkspacePath, time);
        var result = await operations.RunJobAsync(command.DryRun, command.ForceOverlap, context.CancellationToken)
            .ConfigureAwait(false);
        return Result<MemoryConsolidateJobResult>.Success(result);
    }
}
