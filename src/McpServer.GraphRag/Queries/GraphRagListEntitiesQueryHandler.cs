using McpServer.Cqrs;
using McpServer.Support.Mcp.Models;
using McpServer.Support.Mcp.Services;

namespace McpServer.GraphRag.Queries;

/// <summary>
/// FR-MCP-079, TR-GRAPHRAG-ADHOC-002: Handles <see cref="GraphRagListEntitiesQuery"/> by delegating to <see cref="IGraphRagService"/>.
/// </summary>
public sealed class GraphRagListEntitiesQueryHandler(IGraphRagService graphRagService)
    : IQueryHandler<GraphRagListEntitiesQuery, GraphEntityListResponse>
{
    /// <inheritdoc />
    public async Task<Result<GraphEntityListResponse>> HandleAsync(GraphRagListEntitiesQuery query, CallContext context)
    {
        try
        {
            var response = await graphRagService.ListEntitiesAsync(query.Skip, query.Take, query.EntityType, context.CancellationToken).ConfigureAwait(false);
            return Result<GraphEntityListResponse>.Success(response);
        }
        catch (Exception ex)
        {
            return Result<GraphEntityListResponse>.Failure(ex.Message, ex);
        }
    }
}
