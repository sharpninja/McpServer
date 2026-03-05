using McpServer.Cqrs;
using McpServer.Support.Mcp.Models;
using McpServer.Support.Mcp.Services;

namespace McpServer.GraphRag.Commands;

/// <summary>
/// Handles <see cref="GraphRagIndexCommand"/> by delegating to <see cref="IGraphRagService"/>.
/// </summary>
public sealed class GraphRagIndexCommandHandler(IGraphRagService graphRagService)
    : ICommandHandler<GraphRagIndexCommand, GraphRagStatusResponse>
{
    /// <inheritdoc />
    public async Task<Result<GraphRagStatusResponse>> HandleAsync(GraphRagIndexCommand command, CallContext context)
    {
        try
        {
            var request = new GraphRagIndexRequest { Force = command.ForceReindex };
            var response = await graphRagService.IndexAsync(request, context.CancellationToken).ConfigureAwait(false);
            return Result<GraphRagStatusResponse>.Success(response);
        }
        catch (Exception ex)
        {
            return Result<GraphRagStatusResponse>.Failure(ex.Message, ex);
        }
    }
}
