using McpServer.Support.Mcp.Models;

namespace McpServer.Support.Mcp.Services;

#pragma warning disable CS1591

/// <summary>
/// Workspace-scoped GraphRAG orchestration service.
/// </summary>
public interface IGraphRagService
{
    Task<GraphRagStatusResponse> GetStatusAsync(CancellationToken cancellationToken = default);
    Task<GraphRagStatusResponse> InitializeAsync(CancellationToken cancellationToken = default);
    Task<GraphRagStatusResponse> IndexAsync(GraphRagIndexRequest? request = null, CancellationToken cancellationToken = default);
    Task<GraphRagQueryResponse> QueryAsync(GraphRagQueryRequest request, CancellationToken cancellationToken = default);
}

#pragma warning restore CS1591
