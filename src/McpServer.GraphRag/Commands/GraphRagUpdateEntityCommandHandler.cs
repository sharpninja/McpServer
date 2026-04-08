using McpServer.Cqrs;
using McpServer.Support.Mcp.Models;
using McpServer.Support.Mcp.Services;

namespace McpServer.GraphRag.Commands;

/// <summary>
/// FR-MCP-079, TR-GRAPHRAG-ADHOC-002: Handles <see cref="GraphRagUpdateEntityCommand"/> by delegating to <see cref="IGraphRagService"/>.
/// </summary>
public sealed class GraphRagUpdateEntityCommandHandler(IGraphRagService graphRagService)
    : ICommandHandler<GraphRagUpdateEntityCommand, GraphEntityResponse>
{
    /// <inheritdoc />
    public async Task<Result<GraphEntityResponse>> HandleAsync(GraphRagUpdateEntityCommand command, CallContext context)
    {
        try
        {
            var response = await graphRagService.UpdateEntityAsync(command.EntityId, command.Request, context.CancellationToken).ConfigureAwait(false);
            if (response is null)
                return Result<GraphEntityResponse>.Failure($"Entity '{command.EntityId}' not found.");
            return Result<GraphEntityResponse>.Success(response);
        }
        catch (Exception ex)
        {
            return Result<GraphEntityResponse>.Failure(ex.Message, ex);
        }
    }
}
