using McpServer.Cqrs;
using McpServer.Support.Mcp.Models;
using McpServer.Support.Mcp.Services;

namespace McpServer.GraphRag.Queries;

/// <summary>
/// FR-MCP-079, TR-GRAPHRAG-ADHOC-002: Handles <see cref="GraphRagGetEntityQuery"/> by delegating to <see cref="IGraphRagService"/>.
/// </summary>
public sealed class GraphRagGetEntityQueryHandler(IGraphRagService graphRagService)
    : IQueryHandler<GraphRagGetEntityQuery, GraphEntityResponse>
{
    /// <inheritdoc />
    public async Task<Result<GraphEntityResponse>> HandleAsync(GraphRagGetEntityQuery query, CallContext context)
    {
        try
        {
            var response = await graphRagService.GetEntityAsync(query.EntityId, context.CancellationToken).ConfigureAwait(false);
            if (response is null)
                return Result<GraphEntityResponse>.Failure($"Entity '{query.EntityId}' not found.");
            return Result<GraphEntityResponse>.Success(response);
        }
        catch (Exception ex)
        {
            return Result<GraphEntityResponse>.Failure(ex.Message, ex);
        }
    }
}
