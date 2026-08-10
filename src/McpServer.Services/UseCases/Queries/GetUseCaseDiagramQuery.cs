using McpServer.Cqrs;
using McpServer.Support.Mcp.Services;
using McpServer.Support.Mcp.UseCases.Models;
using McpServer.Support.Mcp.Storage;

namespace McpServer.Support.Mcp.UseCases.Queries;

/// <summary>
/// TR-MCP-USECASE-004: Get Mermaid diagram for a use case aggregate.
/// </summary>
/// <param name="WorkspacePath">Workspace path override.</param>
/// <param name="UseCaseId">Use case id.</param>
/// <param name="Format">Diagram format: mermaid (primary) or plantuml.</param>
public sealed record GetUseCaseDiagramQuery(string WorkspacePath, long UseCaseId, string Format)
    : IQuery<UseCaseDiagramDto>;

/// <summary>
/// TR-MCP-USECASE-004: Handles <see cref="GetUseCaseDiagramQuery"/> by loading the aggregate
/// and calling <see cref="IUseCaseDiagramService"/>.
/// </summary>
public sealed class GetUseCaseDiagramQueryHandler(
    McpDbContext db,
    WorkspaceContext workspaceContext,
    IUseCaseDiagramService diagramService)
    : IQueryHandler<GetUseCaseDiagramQuery, UseCaseDiagramDto>
{
    /// <inheritdoc />
    public async Task<Result<UseCaseDiagramDto>> HandleAsync(GetUseCaseDiagramQuery query, CallContext context)
    {
        ArgumentNullException.ThrowIfNull(query);

        try
        {
            UseCaseCqrsHelpers.ResolveWorkspaceId(db, workspaceContext, query.WorkspacePath);

            var entity = await UseCaseCqrsHelpers.LoadAggregateAsync(db, query.UseCaseId, context.CancellationToken)
                .ConfigureAwait(false);
            if (entity is null)
                return Result<UseCaseDiagramDto>.Failure($"Use case '{query.UseCaseId}' was not found.");

            var detail = UseCaseCqrsHelpers.ToDetailDto(entity);
            var format = string.IsNullOrWhiteSpace(query.Format) ? "mermaid" : query.Format.Trim();
            string content;
            try
            {
                content = diagramService.Generate(detail, format);
            }
            catch (ArgumentException ex)
            {
                return Result<UseCaseDiagramDto>.Failure(ex.Message);
            }

            return Result<UseCaseDiagramDto>.Success(new UseCaseDiagramDto
            {
                UseCaseId = detail.UseCaseId,
                Format = format.ToLowerInvariant(),
                Content = content,
            });
        }
        catch (Exception ex)
        {
            return Result<UseCaseDiagramDto>.Failure(ex.Message, ex);
        }
    }
}
