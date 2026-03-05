using McpServer.Cqrs;
using McpServer.Support.Mcp.Models;
using McpServer.Support.Mcp.Services;

namespace McpServer.GraphRag.Queries;

/// <summary>
/// Handles <see cref="GraphRagQueryQuery"/> by delegating to <see cref="IGraphRagService"/>.
/// </summary>
public sealed class GraphRagQueryQueryHandler(IGraphRagService graphRagService)
    : IQueryHandler<GraphRagQueryQuery, GraphRagQueryResponse>
{
    /// <inheritdoc />
    public async Task<Result<GraphRagQueryResponse>> HandleAsync(GraphRagQueryQuery query, CallContext context)
    {
        try
        {
            var request = new GraphRagQueryRequest
            {
                Query = query.QueryText,
                MaxChunks = query.TopK,
                IncludeContextChunks = true
            };
            var response = await graphRagService.QueryAsync(request, context.CancellationToken).ConfigureAwait(false);
            return Result<GraphRagQueryResponse>.Success(response);
        }
        catch (Exception ex)
        {
            return Result<GraphRagQueryResponse>.Failure(ex.Message, ex);
        }
    }
}
