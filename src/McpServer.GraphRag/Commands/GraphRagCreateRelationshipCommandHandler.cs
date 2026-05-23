using McpServer.Cqrs;
using McpServer.Support.Mcp.Models;
using McpServer.Support.Mcp.Services;

namespace McpServer.GraphRag.Commands;

/// <summary>
/// FR-MCP-079, TR-GRAPHRAG-ADHOC-002: Handles <see cref="GraphRagCreateRelationshipCommand"/> by delegating to <see cref="IGraphRagService"/>.
/// </summary>
public sealed class GraphRagCreateRelationshipCommandHandler(IGraphRagService graphRagService)
    : ICommandHandler<GraphRagCreateRelationshipCommand, GraphRelationshipResponse>
{
    /// <inheritdoc />
    public async Task<Result<GraphRelationshipResponse>> HandleAsync(GraphRagCreateRelationshipCommand command, CallContext context)
    {
        try
        {
            var response = await graphRagService.CreateRelationshipAsync(command.Request, context.CancellationToken).ConfigureAwait(false);
            return Result<GraphRelationshipResponse>.Success(response);
        }
        catch (Exception ex)
        {
            return Result<GraphRelationshipResponse>.Failure(ex.Message, ex);
        }
    }
}
