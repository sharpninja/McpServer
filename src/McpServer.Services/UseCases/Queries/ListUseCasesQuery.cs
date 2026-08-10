using McpServer.Cqrs;
using McpServer.Support.Mcp.Services;
using McpServer.Support.Mcp.UseCases.Models;
using McpServer.Support.Mcp.Storage;
using Microsoft.EntityFrameworkCore;

namespace McpServer.Support.Mcp.UseCases.Queries;

/// <summary>
/// FR-MCP-USECASE-001 / TR-MCP-USECASE-002: List non-deleted use cases with optional title filter.
/// </summary>
/// <param name="WorkspacePath">Workspace path override.</param>
/// <param name="TitleFilter">Optional case-insensitive title contains filter.</param>
public sealed record ListUseCasesQuery(string WorkspacePath, string? TitleFilter = null)
    : IQuery<IReadOnlyList<UseCaseSummaryDto>>;

/// <summary>
/// FR-MCP-USECASE-001 / TR-MCP-USECASE-002: Handles <see cref="ListUseCasesQuery"/>.
/// </summary>
public sealed class ListUseCasesQueryHandler(
    McpDbContext db,
    WorkspaceContext workspaceContext)
    : IQueryHandler<ListUseCasesQuery, IReadOnlyList<UseCaseSummaryDto>>
{
    /// <inheritdoc />
    public async Task<Result<IReadOnlyList<UseCaseSummaryDto>>> HandleAsync(ListUseCasesQuery query, CallContext context)
    {
        ArgumentNullException.ThrowIfNull(query);

        try
        {
            UseCaseCqrsHelpers.ResolveWorkspaceId(db, workspaceContext, query.WorkspacePath);

            var q = db.UseCases.AsNoTracking().AsQueryable();
            var filter = UseCaseCqrsHelpers.NormalizeOptional(query.TitleFilter);
            if (filter is not null)
            {
                // EF translates ToLower comparisons for SQLite/providers used in tests.
                var lower = filter.ToLowerInvariant();
                q = q.Where(u => u.Title.ToLower().Contains(lower));
            }

            var rows = await q
                .OrderBy(u => u.Title)
                .ThenBy(u => u.UseCaseId)
                .Select(u => new
                {
                    Entity = u,
                    FrLinkCount = u.FrLinks.Count,
                })
                .ToListAsync(context.CancellationToken)
                .ConfigureAwait(false);

            IReadOnlyList<UseCaseSummaryDto> result = rows
                .Select(r => UseCaseCqrsHelpers.ToSummaryDto(r.Entity, r.FrLinkCount))
                .ToList();

            return Result<IReadOnlyList<UseCaseSummaryDto>>.Success(result);
        }
        catch (Exception ex)
        {
            return Result<IReadOnlyList<UseCaseSummaryDto>>.Failure(ex.Message, ex);
        }
    }
}
