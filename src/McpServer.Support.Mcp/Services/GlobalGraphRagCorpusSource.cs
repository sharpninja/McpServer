using McpServer.Support.Mcp.Models;
using McpServer.Support.Mcp.Options;
using McpServer.Support.Mcp.Services.AgentHelp;
using Microsoft.Extensions.Options;

namespace McpServer.Support.Mcp.Services;

/// <summary>
/// TR-MCP-GRAPHRAG-GLOBAL-001: Agent Help adapter over the host-global GraphRAG query surface.
/// </summary>
public sealed class GlobalGraphRagCorpusSource : IGlobalGraphRagCorpusSource
{
    private readonly IGraphRagService _graphRagService;
    private readonly IOptionsMonitor<GraphRagOptions> _options;
    private readonly ILogger<GlobalGraphRagCorpusSource> _logger;

    /// <summary>Initializes a new instance of the <see cref="GlobalGraphRagCorpusSource"/> class.</summary>
    /// <param name="graphRagService">GraphRAG service.</param>
    /// <param name="options">GraphRAG options.</param>
    /// <param name="logger">Diagnostic logger.</param>
    public GlobalGraphRagCorpusSource(
        IGraphRagService graphRagService,
        IOptionsMonitor<GraphRagOptions> options,
        ILogger<GlobalGraphRagCorpusSource> logger)
    {
        _graphRagService = graphRagService ?? throw new ArgumentNullException(nameof(graphRagService));
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<GlobalGraphRagCorpusExcerpt>> QueryAsync(
        string query,
        int maxChunks,
        CancellationToken cancellationToken = default)
    {
        if (!_options.CurrentValue.Enabled || string.IsNullOrWhiteSpace(query))
            return [];

        try
        {
            var response = await _graphRagService.QueryAsync(
                new GraphRagQueryRequest
                {
                    Scope = GraphRagStorageScope.Global,
                    Query = query.Trim(),
                    MaxChunks = Math.Clamp(maxChunks, 1, 20),
                    IncludeContextChunks = true,
                },
                cancellationToken).ConfigureAwait(false);

            return response.Chunks
                .Zip(response.SourceKeys.DefaultIfEmpty(string.Empty), (chunk, sourceKey) =>
                {
                    var key = string.IsNullOrWhiteSpace(sourceKey) ? chunk.DocumentId : sourceKey;
                    var text = string.IsNullOrWhiteSpace(chunk.Content) ? string.Empty : chunk.Content.Trim();
                    return new GlobalGraphRagCorpusExcerpt(key, text);
                })
                .Where(excerpt => !string.IsNullOrWhiteSpace(excerpt.Text))
                .ToList();
        }
        catch (Exception ex) when (ex is InvalidOperationException or IOException)
        {
            _logger.LogDebug(ex, "Global GraphRAG corpus query unavailable during Agent Help bootstrap.");
            return [];
        }
    }
}