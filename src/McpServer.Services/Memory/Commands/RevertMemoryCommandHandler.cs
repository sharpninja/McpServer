using McpServer.Cqrs;
using McpServer.Support.Mcp.Storage;
using Microsoft.EntityFrameworkCore;

namespace McpServer.Support.Mcp.Services;

/// <summary>
/// FR-MCP-MEMORY-014 / TR-MCP-MEMORY-API-002: Handles <see cref="RevertMemoryCommand"/>.
/// </summary>
public sealed class RevertMemoryCommandHandler(DbContextOptions<McpDbContext> options)
    : ICommandHandler<RevertMemoryCommand, MemoryRevertResult>
{
    /// <inheritdoc />
    public async Task<Result<MemoryRevertResult>> HandleAsync(RevertMemoryCommand command, CallContext context)
    {
        await using var db = new McpDbContext(options, new WorkspaceContext { WorkspacePath = command.WorkspacePath });
        var operations = new MemoryCompetitiveOperations(db);
        var result = await operations.RevertAsync(
            command.MemoryId,
            command.VersionNumber,
            command.ReadOnlyCaller,
            context.CancellationToken).ConfigureAwait(false);
        return Result<MemoryRevertResult>.Success(result);
    }
}
