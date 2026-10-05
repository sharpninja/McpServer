using QBrainAi.Cqrs;
using QBrainAi.Support.Mcp.Models;
using QBrainAi.Support.Mcp.Services;

namespace QBrainAi.GraphRag.Commands;

/// <summary>
/// FR-MCP-078, TR-GRAPHRAG-ADHOC-001: Handles <see cref="GraphRagIngestTextCommand"/> by delegating to <see cref="IGraphRagService"/>.
/// </summary>
public sealed class GraphRagIngestTextCommandHandler(IGraphRagService graphRagService)
    : ICommandHandler<GraphRagIngestTextCommand, GraphRagIngestTextResponse>
{
    /// <inheritdoc />
    public async Task<Result<GraphRagIngestTextResponse>> HandleAsync(GraphRagIngestTextCommand command, CallContext context)
    {
        try
        {
            var response = await graphRagService.IngestTextAsync(command.Request, context.CancellationToken).ConfigureAwait(false);
            return Result<GraphRagIngestTextResponse>.Success(response);
        }
        catch (Exception ex)
        {
            return Result<GraphRagIngestTextResponse>.Failure(ex.Message, ex);
        }
    }
}
