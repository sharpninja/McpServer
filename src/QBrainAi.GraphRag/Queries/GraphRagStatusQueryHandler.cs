using QBrainAi.Cqrs;
using QBrainAi.Support.Mcp.Models;
using QBrainAi.Support.Mcp.Services;

namespace QBrainAi.GraphRag.Queries;

/// <summary>
/// Handles <see cref="GraphRagStatusQuery"/> by delegating to <see cref="IGraphRagService"/>.
/// </summary>
public sealed class GraphRagStatusQueryHandler(IGraphRagService graphRagService)
    : IQueryHandler<GraphRagStatusQuery, GraphRagStatusResponse>
{
    /// <inheritdoc />
    public async Task<Result<GraphRagStatusResponse>> HandleAsync(GraphRagStatusQuery query, CallContext context)
    {
        try
        {
            var response = await graphRagService
                .GetStatusAsync(GraphRagStorageScope.Workspace, context.CancellationToken)
                .ConfigureAwait(false);
            return Result<GraphRagStatusResponse>.Success(response);
        }
        catch (Exception ex)
        {
            return Result<GraphRagStatusResponse>.Failure(ex.Message, ex);
        }
    }
}
