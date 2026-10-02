using McpServer.Cqrs;
using McpServer.Support.Mcp.Storage;
using Microsoft.EntityFrameworkCore;

namespace McpServer.Support.Mcp.Services;

/// <summary>
/// FR-MCP-MEMORY-014 / TR-MCP-MEMORY-API-002: Handles <see cref="ListMemoryVersionsQuery"/>.
/// </summary>
public sealed class ListMemoryVersionsQueryHandler(DbContextOptions<McpDbContext> options)
    : IQueryHandler<ListMemoryVersionsQuery, MemoryVersionListResult>
{
    /// <inheritdoc />
    public async Task<Result<MemoryVersionListResult>> HandleAsync(ListMemoryVersionsQuery query, CallContext context)
    {
        await using var db = new McpDbContext(options, new WorkspaceContext { WorkspacePath = query.WorkspacePath });
        var operations = new MemoryCompetitiveOperations(db);
        var result = await operations.ListVersionsAsync(query.MemoryId, context.CancellationToken).ConfigureAwait(false);
        return Result<MemoryVersionListResult>.Success(result);
    }
}
