using McpServer.Cqrs;
using McpServer.Support.Mcp.Services;
using McpServer.Support.Mcp.UseCases.Models;
using McpServer.Support.Mcp.Storage;
using Microsoft.EntityFrameworkCore;

namespace McpServer.Support.Mcp.UseCases.Queries;

/// <summary>
/// FR-MCP-USECASE-009: List use cases in the active workspace that share a product key
/// (product multi-workspace sharing hook).
/// </summary>
/// <param name="WorkspacePath">Workspace path override.</param>
/// <param name="ProductKey">Product key to filter.</param>
public sealed record ListUseCasesByProductQuery(string WorkspacePath, string ProductKey)
    : IQuery<IReadOnlyList<UseCaseSummaryDto>>;

/// <summary>FR-MCP-USECASE-009: Handles <see cref="ListUseCasesByProductQuery"/>.</summary>
public sealed class ListUseCasesByProductQueryHandler(
    McpDbContext db,
    WorkspaceContext workspaceContext)
    : IQueryHandler<ListUseCasesByProductQuery, IReadOnlyList<UseCaseSummaryDto>>
{
    /// <inheritdoc />
    public async Task<Result<IReadOnlyList<UseCaseSummaryDto>>> HandleAsync(ListUseCasesByProductQuery query, CallContext context)
    {
        ArgumentNullException.ThrowIfNull(query);
        try
        {
            UseCaseCqrsHelpers.ResolveWorkspaceId(db, workspaceContext, query.WorkspacePath);
            var key = query.ProductKey?.Trim();
            if (string.IsNullOrEmpty(key))
                return Result<IReadOnlyList<UseCaseSummaryDto>>.Failure("ProductKey is required.");

            var rows = await db.UseCases.AsNoTracking()
                .Where(u => u.ProductKey == key)
                .OrderBy(u => u.Title)
                .ThenBy(u => u.UseCaseId)
                .ToListAsync(context.CancellationToken)
                .ConfigureAwait(false);

            var result = rows.Select(u => UseCaseCqrsHelpers.ToSummaryDto(u)).ToArray();
            return Result<IReadOnlyList<UseCaseSummaryDto>>.Success(result);
        }
        catch (Exception ex)
        {
            return Result<IReadOnlyList<UseCaseSummaryDto>>.Failure(ex.Message, ex);
        }
    }
}
