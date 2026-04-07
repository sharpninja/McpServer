using McpServer.Cqrs;
using McpServer.Support.Mcp.Models;
using McpServer.Support.Mcp.Services;

namespace McpServer.GraphRag.Queries;

/// <summary>
/// FR-MCP-080, TR-GRAPHRAG-ADHOC-003: Handles <see cref="GraphRagGetDocumentChunksQuery"/> by delegating to <see cref="IGraphRagService"/>.
/// </summary>
public sealed class GraphRagGetDocumentChunksQueryHandler(IGraphRagService graphRagService)
    : IQueryHandler<GraphRagGetDocumentChunksQuery, GraphRagDocumentChunksResponse>
{
    /// <inheritdoc />
    public async Task<Result<GraphRagDocumentChunksResponse>> HandleAsync(GraphRagGetDocumentChunksQuery query, CallContext context)
    {
        try
        {
            var response = await graphRagService.GetDocumentChunksAsync(query.DocumentId, context.CancellationToken).ConfigureAwait(false);
            if (response is null)
                return Result<GraphRagDocumentChunksResponse>.Failure($"Document '{query.DocumentId}' not found.");
            return Result<GraphRagDocumentChunksResponse>.Success(response);
        }
        catch (Exception ex)
        {
            return Result<GraphRagDocumentChunksResponse>.Failure(ex.Message, ex);
        }
    }
}
