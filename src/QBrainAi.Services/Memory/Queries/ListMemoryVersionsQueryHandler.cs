using QBrainAi.Cqrs;
using QBrainAi.Support.Mcp.Storage;
using Microsoft.EntityFrameworkCore;
using QBrainAi.Support.Mcp.Services;
using QBrainAi.Support.Mcp.Memory.Models;
using QBrainAi.Support.Mcp.Storage.Services;

namespace QBrainAi.Support.Mcp.Memory.Queries;

/// <summary>
/// FR-MCP-MEMORY-014 / TR-MCP-MEMORY-API-002: Handles <see cref="ListMemoryVersionsQuery"/>.
/// </summary>
public sealed class ListMemoryVersionsQueryHandler(DbContextOptions<McpDbContext> options)
    : IQueryHandler<ListMemoryVersionsQuery, MemoryVersionListResult>
{
    /// <inheritdoc />
    public async Task<Result<MemoryVersionListResult>> HandleAsync(ListMemoryVersionsQuery query, CallContext context)
    {
        var db = new McpDbContext(options, new WorkspaceContext { WorkspacePath = query.WorkspacePath });
        await using var dbDisposal = db.ConfigureAwait(false);
        var operations = new MemoryCompetitiveOperations(db);
        var result = await operations.ListVersionsAsync(query.MemoryId, context.CancellationToken).ConfigureAwait(false);
        return Result<MemoryVersionListResult>.Success(result);
    }
}
