using McpServer.Cqrs;
using McpServer.Support.Mcp.Storage;
using Microsoft.EntityFrameworkCore;

namespace McpServer.Support.Mcp.Services;

/// <summary>
/// FR-MCP-MEMORY-014-14: Handles <see cref="RecallMemoryQuery"/> for S1 post-revert keyword reflection.
/// S2 Green (validation, scores, fusion, filters, rerank) is intentionally not implemented here.
/// </summary>
public sealed class RecallMemoryQueryHandler(DbContextOptions<McpDbContext> options)
    : IQueryHandler<RecallMemoryQuery, MemoryRecallResult>
{
    /// <inheritdoc />
    public async Task<Result<MemoryRecallResult>> HandleAsync(RecallMemoryQuery query, CallContext context)
    {
        await using var db = new McpDbContext(options, new WorkspaceContext { WorkspacePath = query.WorkspacePath });
        var operations = new MemoryCompetitiveOperations(db);
        var keyword = query.Query ?? string.Empty;
        var result = await operations.RecallAsync(keyword, context.CancellationToken).ConfigureAwait(false);
        var hits = result.Items.Select(item => new MemoryRecallHit
        {
            Id = item.Id,
            Title = item.Title,
            Content = item.Content ?? item.Text,
            Text = item.Text,
            Type = item.Type,
            Tags = item.Tags,
            Scope = item.Scope,
        }).ToList();
        return Result<MemoryRecallResult>.Success(new MemoryRecallResult(200, hits));
    }
}
