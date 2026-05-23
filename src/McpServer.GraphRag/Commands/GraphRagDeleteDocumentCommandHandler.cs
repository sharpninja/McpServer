using McpServer.Cqrs;
using McpServer.Support.Mcp.Models;
using McpServer.Support.Mcp.Services;

namespace McpServer.GraphRag.Commands;

/// <summary>
/// FR-MCP-080, TR-GRAPHRAG-ADHOC-003: Handles <see cref="GraphRagDeleteDocumentCommand"/> by delegating to <see cref="IGraphRagService"/>.
/// </summary>
public sealed class GraphRagDeleteDocumentCommandHandler(IGraphRagService graphRagService)
    : ICommandHandler<GraphRagDeleteDocumentCommand, GraphRagDocumentDeleteResponse>
{
    /// <inheritdoc />
    public async Task<Result<GraphRagDocumentDeleteResponse>> HandleAsync(GraphRagDeleteDocumentCommand command, CallContext context)
    {
        try
        {
            var response = await graphRagService.DeleteDocumentAsync(command.DocumentId, context.CancellationToken).ConfigureAwait(false);
            return Result<GraphRagDocumentDeleteResponse>.Success(response);
        }
        catch (Exception ex)
        {
            return Result<GraphRagDocumentDeleteResponse>.Failure(ex.Message, ex);
        }
    }
}
