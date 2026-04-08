using McpServer.Cqrs;
using McpServer.Support.Mcp.Models;
using McpServer.Support.Mcp.Services;

namespace McpServer.GraphRag.Queries;

/// <summary>
/// FR-MCP-079, TR-GRAPHRAG-ADHOC-002: Handles <see cref="GraphRagListRelationshipsQuery"/> by delegating to <see cref="IGraphRagService"/>.
/// </summary>
public sealed class GraphRagListRelationshipsQueryHandler(IGraphRagService graphRagService)
    : IQueryHandler<GraphRagListRelationshipsQuery, GraphRelationshipListResponse>
{
    /// <inheritdoc />
    public async Task<Result<GraphRelationshipListResponse>> HandleAsync(GraphRagListRelationshipsQuery query, CallContext context)
    {
        try
        {
            var response = await graphRagService.ListRelationshipsAsync(query.Skip, query.Take, query.EntityId, query.RelationshipType, context.CancellationToken).ConfigureAwait(false);
            return Result<GraphRelationshipListResponse>.Success(response);
        }
        catch (Exception ex)
        {
            return Result<GraphRelationshipListResponse>.Failure(ex.Message, ex);
        }
    }
}
