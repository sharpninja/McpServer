using System.Text.Json;
using McpServer.Support.Mcp.Models;
using McpServer.Support.Mcp.Options;

namespace McpServer.Support.Mcp.Services;

/// <summary>
/// GraphRAG backend adapter that executes an external command.
/// </summary>
internal sealed class ExternalCommandGraphRagBackendAdapter(
    IProcessRunner processRunner,
    ILogger<ExternalCommandGraphRagBackendAdapter> logger) : IGraphRagBackendAdapter
{
    /// <inheritdoc />
    public string AdapterName => "external-command";

    /// <inheritdoc />
    public bool CanHandle(GraphRagOptions options) => !string.IsNullOrWhiteSpace(options.BackendCommand);

    /// <inheritdoc />
    public async Task<GraphRagBackendIndexResult> IndexAsync(GraphRagBackendExecutionContext context, GraphRagIndexRequest? request, CancellationToken cancellationToken = default)
    {
        var command = context.Options.BackendCommand!;
        var args = RenderBackendArgs(context, "index", request?.Force == true, query: null, mode: null, maxChunks: null);
        var run = await processRunner.RunAsync(command, args, cancellationToken).ConfigureAwait(false);
        if (run.ExitCode != 0)
        {
            return new GraphRagBackendIndexResult(
                Success: false,
                FailureCode: "index_failed",
                Error: $"GraphRAG backend index failed (exit={run.ExitCode}): {run.Stderr ?? run.Stdout ?? "unknown error"}");
        }

        return new GraphRagBackendIndexResult(true);
    }

    /// <inheritdoc />
    public async Task<GraphRagQueryResponse?> QueryAsync(
        GraphRagBackendExecutionContext context,
        GraphRagQueryRequest request,
        string query,
        string mode,
        int maxChunks,
        CancellationToken cancellationToken = default)
    {
        _ = request;
        try
        {
            var command = context.Options.BackendCommand!;
            var args = RenderBackendArgs(context, "query", force: false, query, mode, maxChunks);
            var run = await processRunner.RunAsync(command, args, cancellationToken).ConfigureAwait(false);
            if (run.ExitCode != 0 || string.IsNullOrWhiteSpace(run.Stdout))
                return null;

            using var doc = JsonDocument.Parse(run.Stdout);
            var root = doc.RootElement;
            var answer = root.TryGetProperty("answer", out var answerNode)
                ? answerNode.GetString() ?? string.Empty
                : run.Stdout.Trim();
            var sourceKeys = root.TryGetProperty("sourceKeys", out var sourceNode)
                ? sourceNode.EnumerateArray().Select(static s => s.GetString() ?? string.Empty).Where(static s => s.Length > 0).ToList()
                : [];
            var citations = root.TryGetProperty("citations", out var citationNode)
                ? citationNode.EnumerateArray()
                    .Select(static c => new GraphRagCitation
                    {
                        SourceKey = c.TryGetProperty("sourceKey", out var sk) ? sk.GetString() ?? string.Empty : string.Empty,
                        ChunkId = c.TryGetProperty("chunkId", out var cid) ? cid.GetString() : null,
                        Snippet = c.TryGetProperty("snippet", out var sn) ? sn.GetString() : null
                    })
                    .ToList()
                : [];

            return new GraphRagQueryResponse
            {
                Query = query,
                Mode = mode,
                Answer = answer,
                Citations = citations,
                SourceKeys = sourceKeys,
                Chunks = [],
                Entities = root.TryGetProperty("entities", out var entitiesNode) ? entitiesNode.EnumerateArray().Select(static e => e.ToString()).ToList() : [],
                Relationships = root.TryGetProperty("relationships", out var relNode) ? relNode.EnumerateArray().Select(static e => e.ToString()).ToList() : [],
                Communities = root.TryGetProperty("communities", out var commNode) ? commNode.EnumerateArray().Select(static e => e.ToString()).ToList() : [],
                FallbackUsed = false,
                FallbackReason = null,
                FailureCode = null,
                Backend = AdapterName
            };
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogWarning(ex, "GraphRAG external query failed; falling back");
            return null;
        }
    }

    private static string RenderBackendArgs(
        GraphRagBackendExecutionContext context,
        string operation,
        bool force,
        string? query,
        string? mode,
        int? maxChunks)
    {
        var template = context.Options.BackendArgs ?? string.Empty;
        return template
            .Replace("{operation}", EscapeArg(operation), StringComparison.Ordinal)
            .Replace("{graphRoot}", EscapeArg(context.GraphRoot), StringComparison.Ordinal)
            .Replace("{workspacePath}", EscapeArg(context.WorkspacePath), StringComparison.Ordinal)
            .Replace("{force}", force ? "true" : "false", StringComparison.Ordinal)
            .Replace("{query}", EscapeArg(query ?? string.Empty), StringComparison.Ordinal)
            .Replace("{mode}", EscapeArg(mode ?? string.Empty), StringComparison.Ordinal)
            .Replace("{maxChunks}", (maxChunks ?? context.Options.DefaultMaxChunks).ToString(), StringComparison.Ordinal);
    }

    private static string EscapeArg(string value)
        => "\"" + value.Replace("\"", "\\\"", StringComparison.Ordinal) + "\"";
}
