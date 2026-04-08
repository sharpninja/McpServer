using McpServer.Cqrs;
using McpServer.Support.Mcp.Models;
using McpServer.Support.Mcp.Services;

namespace McpServer.GraphRag.Commands;

/// <summary>
/// FR-MCP-079, TR-GRAPHRAG-ADHOC-002: Handles <see cref="GraphRagUpdateRelationshipCommand"/> by delegating to <see cref="IGraphRagService"/>.
/// </summary>
public sealed class GraphRagUpdateRelationshipCommandHandler(IGraphRagService graphRagService)
    : ICommandHandler<GraphRagUpdateRelationshipCommand, GraphRelationshipResponse>
{
    /// <inheritdoc />
    public async Task<Result<GraphRelationshipResponse>> HandleAsync(GraphRagUpdateRelationshipCommand command, CallContext context)
    {
        try
        {
            var response = await graphRagService.UpdateRelationshipAsync(command.RelationshipId, command.Request, context.CancellationToken).ConfigureAwait(false);
            if (response is null)
                return Result<GraphRelationshipResponse>.Failure($"Relationship '{command.RelationshipId}' not found.");
            return Result<GraphRelationshipResponse>.Success(response);
        }
        catch (Exception ex)
        {
            return Result<GraphRelationshipResponse>.Failure(ex.Message, ex);
        }
    }
}
