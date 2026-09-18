using McpServer.Cqrs;
using McpServer.Support.Mcp.Storage;
using Microsoft.EntityFrameworkCore;

namespace McpServer.Support.Mcp.Services;

/// <summary>
/// FR-MCP-MEMORY-014-14: Handles <see cref="RecallMemoryQuery"/> for S1 post-revert index reflection.
/// </summary>
public sealed class RecallMemoryQueryHandler(DbContextOptions<McpDbContext> options)
    : IQueryHandler<RecallMemoryQuery, MemoryQueryResult>
{
    /// <inheritdoc />
    public async Task<Result<MemoryQueryResult>> HandleAsync(RecallMemoryQuery query, CallContext context)
    {
        await using var db = new McpDbContext(options, new WorkspaceContext { WorkspacePath = query.WorkspacePath });
        var operations = new MemoryCompetitiveOperations(db);
        var result = await operations.RecallAsync(query.Query, context.CancellationToken).ConfigureAwait(false);
        return Result<MemoryQueryResult>.Success(result);
    }
}
