using McpServer.Cqrs;
using McpServer.Support.Mcp.Services;
using McpServer.Support.Mcp.UseCases.Models;
using McpServer.Support.Mcp.Storage;

namespace McpServer.Support.Mcp.UseCases.Queries;

/// <summary>
/// FR-MCP-USECASE-001 / TR-MCP-USECASE-002: Get a non-deleted use case aggregate by id.
/// </summary>
/// <param name="WorkspacePath">Workspace path override.</param>
/// <param name="UseCaseId">Use case id.</param>
public sealed record GetUseCaseQuery(string WorkspacePath, long UseCaseId) : IQuery<UseCaseDetailDto>;

/// <summary>
/// FR-MCP-USECASE-001 / TR-MCP-USECASE-002: Handles <see cref="GetUseCaseQuery"/>.
/// </summary>
public sealed class GetUseCaseQueryHandler(
    McpDbContext db,
    WorkspaceContext workspaceContext)
    : IQueryHandler<GetUseCaseQuery, UseCaseDetailDto>
{
    /// <inheritdoc />
    public async Task<Result<UseCaseDetailDto>> HandleAsync(GetUseCaseQuery query, CallContext context)
    {
        ArgumentNullException.ThrowIfNull(query);

        try
        {
            UseCaseCqrsHelpers.ResolveWorkspaceId(db, workspaceContext, query.WorkspacePath);
            var entity = await UseCaseCqrsHelpers.LoadAggregateAsync(db, query.UseCaseId, context.CancellationToken)
                .ConfigureAwait(false);
            if (entity is null)
                return Result<UseCaseDetailDto>.Failure($"Use case '{query.UseCaseId}' was not found.");

            return Result<UseCaseDetailDto>.Success(UseCaseCqrsHelpers.ToDetailDto(entity));
        }
        catch (Exception ex)
        {
            return Result<UseCaseDetailDto>.Failure(ex.Message, ex);
        }
    }
}
