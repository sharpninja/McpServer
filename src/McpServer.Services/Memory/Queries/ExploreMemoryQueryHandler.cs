using McpServer.Cqrs;
using McpServer.Support.Mcp.Storage;
using Microsoft.EntityFrameworkCore;

namespace McpServer.Support.Mcp.Services;

/// <summary>
/// FR-MCP-MEMORY-012 / TEST-MCP-MEMORY-012: Handles <see cref="ExploreMemoryQuery"/>.
/// Hebbian stays off unless the request enables it. Explore does not mutate Content.
/// </summary>
public sealed class ExploreMemoryQueryHandler(DbContextOptions<McpDbContext> options)
    : IQueryHandler<ExploreMemoryQuery, MemoryExploreResult>
{
    /// <inheritdoc />
    public async Task<Result<MemoryExploreResult>> HandleAsync(ExploreMemoryQuery query, CallContext context)
    {
        await using var db = new McpDbContext(options, new WorkspaceContext { WorkspacePath = query.WorkspacePath });
        var operations = new MemoryExploreOperations(db);
        var result = await operations.ExploreAsync(query, context.CancellationToken).ConfigureAwait(false);
        return Result<MemoryExploreResult>.Success(result);
    }
}
