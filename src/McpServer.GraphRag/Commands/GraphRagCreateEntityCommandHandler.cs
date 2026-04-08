using McpServer.Cqrs;
using McpServer.Support.Mcp.Models;
using McpServer.Support.Mcp.Services;

namespace McpServer.GraphRag.Commands;

/// <summary>
/// FR-MCP-079, TR-GRAPHRAG-ADHOC-002: Handles <see cref="GraphRagCreateEntityCommand"/> by delegating to <see cref="IGraphRagService"/>.
/// </summary>
public sealed class GraphRagCreateEntityCommandHandler(IGraphRagService graphRagService)
    : ICommandHandler<GraphRagCreateEntityCommand, GraphEntityResponse>
{
    /// <inheritdoc />
    public async Task<Result<GraphEntityResponse>> HandleAsync(GraphRagCreateEntityCommand command, CallContext context)
    {
        try
        {
            var response = await graphRagService.CreateEntityAsync(command.Request, context.CancellationToken).ConfigureAwait(false);
            return Result<GraphEntityResponse>.Success(response);
        }
        catch (Exception ex)
        {
            return Result<GraphEntityResponse>.Failure(ex.Message, ex);
        }
    }
}
