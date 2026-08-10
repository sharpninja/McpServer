using System.Text.Json;
using McpServer.Cqrs;
using McpServer.Support.Mcp.Services;
using McpServer.Support.Mcp.UseCases.Models;
using McpServer.Support.Mcp.Storage;
using Microsoft.EntityFrameworkCore;

namespace McpServer.Support.Mcp.UseCases.Queries;

/// <summary>
/// FR-MCP-USECASE-012 / TR-MCP-USECASE-012: Load UML diagram graph for a use case.
/// </summary>
/// <param name="WorkspacePath">Workspace path override.</param>
/// <param name="UseCaseId">Target use case id.</param>
public sealed record GetUseCaseDiagramGraphQuery(string WorkspacePath, long UseCaseId)
    : IQuery<UseCaseDiagramGraphDto>;

/// <summary>
/// FR-MCP-USECASE-012 / AC-012-1: Returns empty schema-v1 graph when none saved.
/// </summary>
public sealed class GetUseCaseDiagramGraphQueryHandler(
    McpDbContext db,
    WorkspaceContext workspaceContext)
    : IQueryHandler<GetUseCaseDiagramGraphQuery, UseCaseDiagramGraphDto>
{
    /// <inheritdoc />
    public async Task<Result<UseCaseDiagramGraphDto>> HandleAsync(
        GetUseCaseDiagramGraphQuery query,
        CallContext context)
    {
        ArgumentNullException.ThrowIfNull(query);

        try
        {
            UseCaseCqrsHelpers.ResolveWorkspaceId(db, workspaceContext, query.WorkspacePath);

            var entity = await db.UseCases
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.UseCaseId == query.UseCaseId, context.CancellationToken)
                .ConfigureAwait(false);
            if (entity is null)
                return Result<UseCaseDiagramGraphDto>.Failure($"Use case '{query.UseCaseId}' was not found.");

            if (string.IsNullOrWhiteSpace(entity.DiagramGraphJson))
            {
                return Result<UseCaseDiagramGraphDto>.Success(new UseCaseDiagramGraphDto
                {
                    SchemaVersion = 1,
                    Kind = "uml-usecase",
                });
            }

            var graph = JsonSerializer.Deserialize<UseCaseDiagramGraphDto>(entity.DiagramGraphJson);
            if (graph is null)
            {
                return Result<UseCaseDiagramGraphDto>.Success(new UseCaseDiagramGraphDto
                {
                    SchemaVersion = 1,
                    Kind = "uml-usecase",
                });
            }

            if (graph.SchemaVersion == 0)
                graph.SchemaVersion = 1;
            if (string.IsNullOrWhiteSpace(graph.Kind))
                graph.Kind = "uml-usecase";

            return Result<UseCaseDiagramGraphDto>.Success(graph);
        }
        catch (Exception ex)
        {
            return Result<UseCaseDiagramGraphDto>.Failure(ex.Message, ex);
        }
    }
}
