using McpServer.Cqrs;
using McpServer.Support.Mcp.Services;
using McpServer.Support.Mcp.UseCases.Models;
using McpServer.Support.Mcp.Storage;
using Microsoft.EntityFrameworkCore;

namespace McpServer.Support.Mcp.UseCases.Queries;

/// <summary>
/// FR-MCP-USECASE-003 / TR-MCP-USECASE-006: List use cases linked to a functional requirement (FR projection).
/// </summary>
/// <param name="WorkspacePath">Workspace path override.</param>
/// <param name="FrId">Functional requirement id (string).</param>
public sealed record GetUseCasesForFrQuery(string WorkspacePath, string FrId)
    : IQuery<IReadOnlyList<LinkedUseCaseDto>>;

/// <summary>
/// FR-MCP-USECASE-003 / TR-MCP-USECASE-006: Handles <see cref="GetUseCasesForFrQuery"/>.
/// </summary>
public sealed class GetUseCasesForFrQueryHandler(
    McpDbContext db,
    WorkspaceContext workspaceContext)
    : IQueryHandler<GetUseCasesForFrQuery, IReadOnlyList<LinkedUseCaseDto>>
{
    /// <inheritdoc />
    public async Task<Result<IReadOnlyList<LinkedUseCaseDto>>> HandleAsync(GetUseCasesForFrQuery query, CallContext context)
    {
        ArgumentNullException.ThrowIfNull(query);

        try
        {
            UseCaseCqrsHelpers.ResolveWorkspaceId(db, workspaceContext, query.WorkspacePath);
            var frId = UseCaseCqrsHelpers.NormalizeOptional(query.FrId);
            if (frId is null)
                return Result<IReadOnlyList<LinkedUseCaseDto>>.Failure("FrId is required.");

            var rows = await db.UseCaseFrLinks
                .AsNoTracking()
                .Where(l => l.FrId == frId)
                .Join(
                    db.UseCases.AsNoTracking(),
                    l => l.UseCaseId,
                    u => u.UseCaseId,
                    (l, u) => new LinkedUseCaseDto
                    {
                        UseCaseId = u.UseCaseId,
                        Title = u.Title,
                        LinkType = l.LinkType,
                        LinkOrder = l.LinkOrder,
                    })
                .OrderBy(x => x.LinkOrder)
                .ThenBy(x => x.UseCaseId)
                .ToListAsync(context.CancellationToken)
                .ConfigureAwait(false);

            return Result<IReadOnlyList<LinkedUseCaseDto>>.Success(rows);
        }
        catch (Exception ex)
        {
            return Result<IReadOnlyList<LinkedUseCaseDto>>.Failure(ex.Message, ex);
        }
    }
}
