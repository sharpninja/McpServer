using McpServer.Support.Mcp.Models;
using McpServer.Support.Mcp.Options;

namespace McpServer.Support.Mcp.Services;

/// <summary>
/// Internal default GraphRAG adapter used when no external backend command is configured.
/// </summary>
internal sealed class InternalFallbackGraphRagBackendAdapter : IGraphRagBackendAdapter
{
    /// <inheritdoc />
    public string AdapterName => "internal-fallback";

    /// <inheritdoc />
    public bool CanHandle(GraphRagOptions options) => string.IsNullOrWhiteSpace(options.BackendCommand);

    /// <inheritdoc />
    public Task<GraphRagBackendIndexResult> IndexAsync(GraphRagBackendExecutionContext context, GraphRagIndexRequest? request, CancellationToken cancellationToken = default)
    {
        var inputPath = Path.Combine(context.GraphRoot, "input");
        var docCount = Directory.Exists(inputPath)
            ? Directory.EnumerateFiles(inputPath, "*", SearchOption.AllDirectories).Count()
            : 0;
        return Task.FromResult(new GraphRagBackendIndexResult(true, docCount));
    }

    /// <inheritdoc />
    public Task<GraphRagQueryResponse?> QueryAsync(
        GraphRagBackendExecutionContext context,
        GraphRagQueryRequest request,
        string query,
        string mode,
        int maxChunks,
        CancellationToken cancellationToken = default)
    {
        // Internal adapter delegates query payload construction to orchestrator fallback path.
        return Task.FromResult<GraphRagQueryResponse?>(null);
    }
}
