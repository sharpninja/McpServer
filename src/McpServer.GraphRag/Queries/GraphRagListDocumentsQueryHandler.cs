using McpServer.Cqrs;
using McpServer.Support.Mcp.Models;
using McpServer.Support.Mcp.Services;

namespace McpServer.GraphRag.Queries;

/// <summary>
/// FR-MCP-080, TR-GRAPHRAG-ADHOC-003: Handles <see cref="GraphRagListDocumentsQuery"/> by delegating to <see cref="IGraphRagService"/>.
/// </summary>
public sealed class GraphRagListDocumentsQueryHandler(IGraphRagService graphRagService)
    : IQueryHandler<GraphRagListDocumentsQuery, GraphRagDocumentListResponse>
{
    /// <inheritdoc />
    public async Task<Result<GraphRagDocumentListResponse>> HandleAsync(GraphRagListDocumentsQuery query, CallContext context)
    {
        try
        {
            var response = await graphRagService.ListDocumentsAsync(query.Skip, query.Take, query.SourceType, context.CancellationToken).ConfigureAwait(false);
            return Result<GraphRagDocumentListResponse>.Success(response);
        }
        catch (Exception ex)
        {
            return Result<GraphRagDocumentListResponse>.Failure(ex.Message, ex);
        }
    }
}
