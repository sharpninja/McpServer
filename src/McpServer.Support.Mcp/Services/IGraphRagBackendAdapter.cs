using McpServer.Support.Mcp.Models;
using McpServer.Support.Mcp.Options;

namespace McpServer.Support.Mcp.Services;

/// <summary>
/// Backend adapter abstraction for GraphRAG runtime operations.
/// </summary>
public interface IGraphRagBackendAdapter
{
    /// <summary>Backend name used in status/response payloads.</summary>
    string AdapterName { get; }

    /// <summary>Returns true when this adapter should handle the current options.</summary>
    bool CanHandle(GraphRagOptions options);

    /// <summary>Execute GraphRAG indexing logic for a workspace.</summary>
    Task<GraphRagBackendIndexResult> IndexAsync(GraphRagBackendExecutionContext context, GraphRagIndexRequest? request, CancellationToken cancellationToken = default);

    /// <summary>Execute GraphRAG query logic. Return null to let orchestrator use fallback retrieval.</summary>
    Task<GraphRagQueryResponse?> QueryAsync(
        GraphRagBackendExecutionContext context,
        GraphRagQueryRequest request,
        string query,
        string mode,
        int maxChunks,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Runtime context passed into GraphRAG backend adapters.
/// </summary>
/// <param name="WorkspacePath">Resolved workspace path.</param>
/// <param name="GraphRoot">Workspace GraphRAG root path.</param>
/// <param name="Options">Resolved GraphRAG options.</param>
public sealed record GraphRagBackendExecutionContext(string WorkspacePath, string GraphRoot, GraphRagOptions Options);

/// <summary>
/// Result returned from GraphRAG backend index operations.
/// </summary>
/// <param name="Success">True when index operation succeeded.</param>
/// <param name="DocumentCount">Indexed document count reported by adapter.</param>
/// <param name="FailureCode">Machine-readable failure code.</param>
/// <param name="Error">Human-readable failure details.</param>
public sealed record GraphRagBackendIndexResult(bool Success, int DocumentCount = 0, string? FailureCode = null, string? Error = null);
