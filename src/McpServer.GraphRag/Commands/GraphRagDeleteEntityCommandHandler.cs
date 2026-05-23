using McpServer.Cqrs;
using McpServer.Support.Mcp.Services;

namespace McpServer.GraphRag.Commands;

/// <summary>
/// FR-MCP-079, TR-GRAPHRAG-ADHOC-002: Handles <see cref="GraphRagDeleteEntityCommand"/> by delegating to <see cref="IGraphRagService"/>.
/// </summary>
public sealed class GraphRagDeleteEntityCommandHandler(IGraphRagService graphRagService)
    : ICommandHandler<GraphRagDeleteEntityCommand, bool>
{
    /// <inheritdoc />
    public async Task<Result<bool>> HandleAsync(GraphRagDeleteEntityCommand command, CallContext context)
    {
        try
        {
            var deleted = await graphRagService.DeleteEntityAsync(command.EntityId, context.CancellationToken).ConfigureAwait(false);
            return Result<bool>.Success(deleted);
        }
        catch (Exception ex)
        {
            return Result<bool>.Failure(ex.Message, ex);
        }
    }
}
