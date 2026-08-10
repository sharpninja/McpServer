using McpServer.Cqrs;
using McpServer.Support.Mcp.Services;
using McpServer.Support.Mcp.UseCases.Models;
using McpServer.Support.Mcp.Storage;

namespace McpServer.Support.Mcp.UseCases.Queries;

/// <summary>
/// FR-MCP-USECASE-003 / TR-MCP-USECASE-006: Coverage gaps for Realizes UC-FR links.
/// </summary>
/// <param name="WorkspacePath">Workspace path override.</param>
public sealed record GetUseCaseFrCoverageQuery(string WorkspacePath) : IQuery<UseCaseFrCoverageDto>;

/// <summary>
/// FR-MCP-USECASE-003 / TR-MCP-USECASE-006: Handles <see cref="GetUseCaseFrCoverageQuery"/>
/// via shared <see cref="UseCaseFrCoverageEvaluator"/>.
/// </summary>
public sealed class GetUseCaseFrCoverageQueryHandler(
    McpDbContext db,
    WorkspaceContext workspaceContext)
    : IQueryHandler<GetUseCaseFrCoverageQuery, UseCaseFrCoverageDto>
{
    /// <inheritdoc />
    public async Task<Result<UseCaseFrCoverageDto>> HandleAsync(GetUseCaseFrCoverageQuery query, CallContext context)
    {
        ArgumentNullException.ThrowIfNull(query);

        try
        {
            UseCaseCqrsHelpers.ResolveWorkspaceId(db, workspaceContext, query.WorkspacePath);
            var snap = await UseCaseFrCoverageEvaluator.EvaluateAsync(db, context.CancellationToken).ConfigureAwait(false);
            var dto = new UseCaseFrCoverageDto
            {
                TotalUseCases = snap.TotalUseCases,
                TotalFunctionalRequirements = snap.TotalFunctionalRequirements,
                LinkedUseCases = snap.LinkedUseCases,
                LinkedFunctionalRequirements = snap.LinkedFunctionalRequirements,
                UseCasesWithoutRealizesLink = snap.UseCasesWithoutRealizesLink
                    .Select(u => new UseCaseSummaryDto { UseCaseId = u.UseCaseId, Title = u.Title })
                    .ToArray(),
                FunctionalRequirementsWithoutRealizesUseCase = snap.FunctionalRequirementsWithoutRealizesUseCase,
            };
            return Result<UseCaseFrCoverageDto>.Success(dto);
        }
        catch (Exception ex)
        {
            return Result<UseCaseFrCoverageDto>.Failure(ex.Message, ex);
        }
    }
}
