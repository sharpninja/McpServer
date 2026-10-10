using McpServer.Cqrs;
using McpServer.Support.Mcp.Storage;
using Microsoft.EntityFrameworkCore;

namespace McpServer.Support.Mcp.Services;

/// <summary>
/// FR-MCP-MEMORY-010 / TR-MCP-MEMORY-API-002: Handles <see cref="RememberMemoryCommand"/>.
/// </summary>
public sealed class RememberMemoryCommandHandler(DbContextOptions<McpDbContext> options)
    : ICommandHandler<RememberMemoryCommand, MemoryRememberResult>
{
    /// <inheritdoc />
    public async Task<Result<MemoryRememberResult>> HandleAsync(RememberMemoryCommand command, CallContext context)
    {
        await using var db = new McpDbContext(options, new WorkspaceContext { WorkspacePath = command.WorkspacePath });
        var operations = new MemoryCompetitiveOperations(db);
        var result = await operations.RememberAsync(command.Request, command.ReadOnlyCaller, context.CancellationToken)
            .ConfigureAwait(false);
        return Result<MemoryRememberResult>.Success(result);
    }
}
