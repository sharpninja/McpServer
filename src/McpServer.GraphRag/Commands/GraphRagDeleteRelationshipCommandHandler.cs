using McpServer.Cqrs;
using McpServer.Support.Mcp.Services;

namespace McpServer.GraphRag.Commands;

/// <summary>
/// FR-MCP-079, TR-GRAPHRAG-ADHOC-002: Handles <see cref="GraphRagDeleteRelationshipCommand"/> by delegating to <see cref="IGraphRagService"/>.
/// </summary>
public sealed class GraphRagDeleteRelationshipCommandHandler(IGraphRagService graphRagService)
    : ICommandHandler<GraphRagDeleteRelationshipCommand, bool>
{
    /// <inheritdoc />
    public async Task<Result<bool>> HandleAsync(GraphRagDeleteRelationshipCommand command, CallContext context)
    {
        try
        {
            var deleted = await graphRagService.DeleteRelationshipAsync(command.RelationshipId, context.CancellationToken).ConfigureAwait(false);
            return Result<bool>.Success(deleted);
        }
        catch (Exception ex)
        {
            return Result<bool>.Failure(ex.Message, ex);
        }
    }
}
