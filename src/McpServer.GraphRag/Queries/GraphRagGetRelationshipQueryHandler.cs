using McpServer.Cqrs;
using McpServer.Support.Mcp.Models;
using McpServer.Support.Mcp.Services;

namespace McpServer.GraphRag.Queries;

/// <summary>
/// FR-MCP-079, TR-GRAPHRAG-ADHOC-002: Handles <see cref="GraphRagGetRelationshipQuery"/> by delegating to <see cref="IGraphRagService"/>.
/// </summary>
public sealed class GraphRagGetRelationshipQueryHandler(IGraphRagService graphRagService)
    : IQueryHandler<GraphRagGetRelationshipQuery, GraphRelationshipResponse>
{
    /// <inheritdoc />
    public async Task<Result<GraphRelationshipResponse>> HandleAsync(GraphRagGetRelationshipQuery query, CallContext context)
    {
        try
        {
            var response = await graphRagService.GetRelationshipAsync(query.RelationshipId, context.CancellationToken).ConfigureAwait(false);
            if (response is null)
                return Result<GraphRelationshipResponse>.Failure($"Relationship '{query.RelationshipId}' not found.");
            return Result<GraphRelationshipResponse>.Success(response);
        }
        catch (Exception ex)
        {
            return Result<GraphRelationshipResponse>.Failure(ex.Message, ex);
        }
    }
}
