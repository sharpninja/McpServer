using McpServer.Cqrs;
using McpServer.Support.Mcp.Storage;
using Microsoft.EntityFrameworkCore;

namespace McpServer.Support.Mcp.Services;

/// <summary>
/// FR-MCP-MEMORY-011 / FR-MCP-MEMORY-014-14 / TR-MCP-MEMORY-SEARCH-002:
/// Handles <see cref="RecallMemoryQuery"/> with hybrid BM25+vector fusion.
/// Read-only callers may recall.
/// </summary>
public sealed class RecallMemoryQueryHandler(DbContextOptions<McpDbContext> options)
    : IQueryHandler<RecallMemoryQuery, MemoryRecallResult>
{
    /// <inheritdoc />
    public async Task<Result<MemoryRecallResult>> HandleAsync(RecallMemoryQuery query, CallContext context)
    {
        await using var db = new McpDbContext(options, new WorkspaceContext { WorkspacePath = query.WorkspacePath });
        var operations = new MemorySearchOperations(db);
        var result = await operations.RecallAsync(query, context.CancellationToken).ConfigureAwait(false);
        return Result<MemoryRecallResult>.Success(result);
    }
}
