using System.Text.Json;
using QBrainAi.Cqrs;
using QBrainAi.Support.Mcp.Services;
using QBrainAi.Support.Mcp.UseCases.Models;
using QBrainAi.Support.Mcp.Storage;

namespace QBrainAi.Support.Mcp.UseCases.Queries;

/// <summary>
/// TR-MCP-USECASE-004: Get a scoped UML use-case diagram for a use case aggregate.
/// </summary>
/// <param name="WorkspacePath">Workspace path override.</param>
/// <param name="UseCaseId">Use case id.</param>
/// <param name="Format">Diagram format: mermaid (primary) or plantuml.</param>
public sealed record GetUseCaseDiagramQuery(string WorkspacePath, long UseCaseId, string Format)
    : IQuery<UseCaseDiagramDto>;

/// <summary>
/// TR-MCP-USECASE-004: Loads the aggregate through canonical workspace resolution and exports
/// the saved UML graph, or an aggregate-derived graph when no canvas graph has been saved.
/// </summary>
public sealed class GetUseCaseDiagramQueryHandler(
    McpDbContext db,
    WorkspaceContext workspaceContext,
    IUseCaseUmlSerializationService umlSerializationService)
    : IQueryHandler<GetUseCaseDiagramQuery, UseCaseDiagramDto>
{
    /// <inheritdoc />
    public async Task<Result<UseCaseDiagramDto>> HandleAsync(GetUseCaseDiagramQuery query, CallContext context)
    {
        ArgumentNullException.ThrowIfNull(query);

        try
        {
            await UseCaseCqrsHelpers.ResolveWorkspaceAsync(
                    db,
                    workspaceContext,
                    query.WorkspacePath,
                    context.CancellationToken)
                .ConfigureAwait(false);

            var entity = await UseCaseCqrsHelpers.LoadAggregateAsync(db, query.UseCaseId, context.CancellationToken)
                .ConfigureAwait(false);
            if (entity is null)
                return Result<UseCaseDiagramDto>.Failure($"Use case '{query.UseCaseId}' was not found.");

            var detail = UseCaseCqrsHelpers.ToDetailDto(entity);
            var graph = DeserializeSavedGraph(entity.DiagramGraphJson);
            if (graph is null || graph.Nodes.Count == 0)
                graph = BuildAggregateGraph(detail, workspaceContext.WorkspaceName);

            var format = string.IsNullOrWhiteSpace(query.Format)
                ? "mermaid"
                : query.Format.Trim().ToLowerInvariant();
            string content;
            try
            {
                content = format switch
                {
                    "mermaid" => umlSerializationService.ToMermaid(graph),
                    "plantuml" => umlSerializationService.ToPlantUml(graph),
                    _ => throw new ArgumentException($"Unsupported diagram format '{query.Format}'."),
                };
            }
            catch (ArgumentException ex)
            {
                return Result<UseCaseDiagramDto>.Failure(ex.Message);
            }

            return Result<UseCaseDiagramDto>.Success(new UseCaseDiagramDto
            {
                UseCaseId = detail.UseCaseId,
                Format = format,
                Content = content,
            });
        }
        catch (Exception ex)
        {
            return Result<UseCaseDiagramDto>.Failure(ex.Message, ex);
        }
    }

    private static UseCaseDiagramGraphDto? DeserializeSavedGraph(string? graphJson)
        => string.IsNullOrWhiteSpace(graphJson)
            ? null
            : JsonSerializer.Deserialize<UseCaseDiagramGraphDto>(graphJson);

    private static UseCaseDiagramGraphDto BuildAggregateGraph(UseCaseDetailDto detail, string? workspaceName)
    {
        var mainUseCaseId = $"uc_{detail.UseCaseId}";
        var graph = new UseCaseDiagramGraphDto
        {
            SchemaVersion = 1,
            Kind = "uml-usecase",
            SystemBoundary = new UseCaseDiagramBoundaryDto
            {
                Id = $"system_{detail.UseCaseId}",
                Label = !string.IsNullOrWhiteSpace(detail.Scope)
                    ? detail.Scope
                    : !string.IsNullOrWhiteSpace(workspaceName)
                        ? workspaceName
                        : "System",
            },
            Nodes =
            [
                new UseCaseDiagramNodeDto
                {
                    Id = mainUseCaseId,
                    Type = "usecase",
                    Label = detail.Title,
                },
            ],
        };

        foreach (var actor in detail.Actors
                     .GroupBy(a => a.ActorId)
                     .Select(g => g.First())
                     .OrderBy(a => a.ActorId))
        {
            var actorId = $"actor_{actor.ActorId}";
            graph.Nodes.Add(new UseCaseDiagramNodeDto
            {
                Id = actorId,
                Type = "actor",
                Label = actor.Name,
            });
            graph.Edges.Add(new UseCaseDiagramEdgeDto
            {
                Id = $"association_{actor.ActorId}_{detail.UseCaseId}",
                Type = "association",
                Source = actorId,
                Target = mainUseCaseId,
            });
        }

        foreach (var flow in detail.Flows
                     .Where(f => !string.Equals(f.FlowType, "Basic", StringComparison.OrdinalIgnoreCase))
                     .OrderBy(f => f.SequenceNumber)
                     .ThenBy(f => f.FlowId))
        {
            var flowId = $"flow_{flow.FlowId}";
            graph.Nodes.Add(new UseCaseDiagramNodeDto
            {
                Id = flowId,
                Type = "usecase",
                Label = !string.IsNullOrWhiteSpace(flow.Name)
                    ? flow.Name
                    : $"{flow.FlowType} flow {flow.SequenceNumber}",
            });
            graph.Edges.Add(new UseCaseDiagramEdgeDto
            {
                Id = $"extend_{flow.FlowId}_{detail.UseCaseId}",
                Type = "extend",
                Source = flowId,
                Target = mainUseCaseId,
            });
        }

        return graph;
    }
}
