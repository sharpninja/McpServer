using System.Text.Json;
using McpServer.Support.Mcp.Ingestion;
using McpServer.Support.Mcp.Models;
using McpServer.Support.Mcp.Options;
using Microsoft.Extensions.Options;
using System.Linq;

namespace McpServer.Support.Mcp.Services;

internal sealed class GraphRagService : IGraphRagService
{
    private const string StatusFileName = "graphrag-status.json";
    private static readonly JsonSerializerOptions s_jsonOptions = new(JsonSerializerDefaults.Web) { WriteIndented = true };

    private readonly GraphRagOptions _options;
    private readonly IngestionOptions _ingestionOptions;
    private readonly WorkspaceContext _workspaceContext;
    private readonly IContextSearchService _contextSearchService;
    private readonly ILogger<GraphRagService> _logger;

    public GraphRagService(
        IOptions<GraphRagOptions> options,
        IOptions<IngestionOptions> ingestionOptions,
        WorkspaceContext workspaceContext,
        IContextSearchService contextSearchService,
        ILogger<GraphRagService> logger)
    {
        _options = options.Value;
        _ingestionOptions = ingestionOptions.Value;
        _workspaceContext = workspaceContext;
        _contextSearchService = contextSearchService;
        _logger = logger;
    }

    public async Task<GraphRagStatusResponse> GetStatusAsync(CancellationToken cancellationToken = default)
    {
        var workspacePath = ResolveWorkspacePath();
        var graphRoot = ResolveGraphRoot(workspacePath);
        var persisted = await TryReadStatusAsync(graphRoot, cancellationToken).ConfigureAwait(false);
        var initialized = Directory.Exists(graphRoot) &&
                          Directory.Exists(Path.Combine(graphRoot, "input")) &&
                          Directory.Exists(Path.Combine(graphRoot, "output")) &&
                          Directory.Exists(Path.Combine(graphRoot, "cache"));

        return new GraphRagStatusResponse
        {
            Enabled = _options.Enabled,
            WorkspacePath = workspacePath,
            GraphRoot = graphRoot,
            IsInitialized = initialized,
            IsIndexed = persisted?.IsIndexed ?? false,
            LastIndexedAtUtc = persisted?.LastIndexedAtUtc,
            LastError = persisted?.LastError,
            Backend = ResolveBackendName()
        };
    }

    public async Task<GraphRagStatusResponse> InitializeAsync(CancellationToken cancellationToken = default)
    {
        var workspacePath = ResolveWorkspacePath();
        var graphRoot = ResolveGraphRoot(workspacePath);
        EnsureGraphDirectories(graphRoot);

        var status = new PersistedGraphRagStatus
        {
            IsIndexed = (await TryReadStatusAsync(graphRoot, cancellationToken).ConfigureAwait(false))?.IsIndexed ?? false,
            LastIndexedAtUtc = (await TryReadStatusAsync(graphRoot, cancellationToken).ConfigureAwait(false))?.LastIndexedAtUtc,
            LastError = null,
            Backend = ResolveBackendName()
        };
        await WriteStatusAsync(graphRoot, status, cancellationToken).ConfigureAwait(false);

        return await GetStatusAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task<GraphRagStatusResponse> IndexAsync(GraphRagIndexRequest? request = null, CancellationToken cancellationToken = default)
    {
        var workspacePath = ResolveWorkspacePath();
        var graphRoot = ResolveGraphRoot(workspacePath);
        EnsureGraphDirectories(graphRoot);

        try
        {
            // Initial implementation keeps indexing lightweight and deterministic:
            // create isolated workspace graph folders and mark the index as ready.
            var status = new PersistedGraphRagStatus
            {
                IsIndexed = true,
                LastIndexedAtUtc = DateTimeOffset.UtcNow,
                LastError = null,
                Backend = ResolveBackendName()
            };

            await WriteStatusAsync(graphRoot, status, cancellationToken).ConfigureAwait(false);
            _logger.LogInformation("GraphRAG index marked ready: Workspace={WorkspacePath}; GraphRoot={GraphRoot}; Force={Force}",
                workspacePath,
                graphRoot,
                request?.Force ?? false);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            var failed = new PersistedGraphRagStatus
            {
                IsIndexed = false,
                LastIndexedAtUtc = null,
                LastError = ex.Message,
                Backend = ResolveBackendName()
            };
            await WriteStatusAsync(graphRoot, failed, cancellationToken).ConfigureAwait(false);
            _logger.LogWarning(ex, "GraphRAG index failed");
        }

        return await GetStatusAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task<GraphRagQueryResponse> QueryAsync(GraphRagQueryRequest request, CancellationToken cancellationToken = default)
    {
        var status = await GetStatusAsync(cancellationToken).ConfigureAwait(false);
        var query = (request.Query ?? string.Empty).Trim();
        var mode = string.IsNullOrWhiteSpace(request.Mode) ? _options.DefaultQueryMode : request.Mode.Trim();
        var maxChunks = Math.Clamp(request.MaxChunks ?? _options.DefaultMaxChunks, 1, 100);
        var fallbackUsed = !status.Enabled || !status.IsIndexed;

        var searchResult = await _contextSearchService.SearchAsync(query, maxChunks, null, cancellationToken).ConfigureAwait(false);
        var chunks = searchResult.Chunks.Select(c => new ContextChunk
        {
            Id = c.ChunkId,
            DocumentId = c.DocumentId,
            Content = c.Content,
            TokenCount = c.TokenCount,
            ChunkIndex = c.ChunkIndex
        }).ToList();

        var citations = chunks
            .Zip(searchResult.SourceKeys.DefaultIfEmpty(string.Empty), (chunk, sourceKey) => new GraphRagCitation
            {
                ChunkId = chunk.Id,
                SourceKey = string.IsNullOrWhiteSpace(sourceKey) ? chunk.DocumentId : sourceKey,
                Snippet = chunk.Content.Length > 240 ? chunk.Content[..240] + "..." : chunk.Content
            })
            .ToList();

        var answer = BuildFallbackAnswer(query, chunks.Count, searchResult.SourceKeys);
        return new GraphRagQueryResponse
        {
            Query = query,
            Mode = mode,
            Answer = answer,
            Chunks = request.IncludeContextChunks ? chunks : [],
            SourceKeys = searchResult.SourceKeys,
            Citations = citations,
            FallbackUsed = fallbackUsed,
            Backend = ResolveBackendName()
        };
    }

    private string ResolveWorkspacePath()
    {
        if (!string.IsNullOrWhiteSpace(_workspaceContext.WorkspacePath))
            return Path.GetFullPath(_workspaceContext.WorkspacePath);
        return Path.GetFullPath(_ingestionOptions.RepoRoot);
    }

    private string ResolveGraphRoot(string workspacePath)
    {
        if (Path.IsPathRooted(_options.RootPath))
            return Path.GetFullPath(_options.RootPath);
        return Path.GetFullPath(Path.Combine(workspacePath, _options.RootPath));
    }

    private static void EnsureGraphDirectories(string graphRoot)
    {
        Directory.CreateDirectory(graphRoot);
        Directory.CreateDirectory(Path.Combine(graphRoot, "input"));
        Directory.CreateDirectory(Path.Combine(graphRoot, "output"));
        Directory.CreateDirectory(Path.Combine(graphRoot, "cache"));
    }

    private static string BuildFallbackAnswer(string query, int chunkCount, IReadOnlyList<string> sourceKeys)
    {
        if (chunkCount == 0)
            return $"No GraphRAG context found for '{query}'.";
        return $"GraphRAG fallback retrieved {chunkCount} context chunk(s) from {sourceKeys.Count} source(s) for '{query}'.";
    }

    private string ResolveBackendName()
        => string.IsNullOrWhiteSpace(_options.BackendCommand) ? "internal-fallback" : "external-command";

    private static string GetStatusFilePath(string graphRoot)
        => Path.Combine(graphRoot, StatusFileName);

    private static async Task<PersistedGraphRagStatus?> TryReadStatusAsync(string graphRoot, CancellationToken cancellationToken)
    {
        var path = GetStatusFilePath(graphRoot);
        if (!File.Exists(path))
            return null;

        await using var stream = File.OpenRead(path);
        return await JsonSerializer.DeserializeAsync<PersistedGraphRagStatus>(stream, s_jsonOptions, cancellationToken).ConfigureAwait(false);
    }

    private static async Task WriteStatusAsync(string graphRoot, PersistedGraphRagStatus status, CancellationToken cancellationToken)
    {
        var path = GetStatusFilePath(graphRoot);
        await using var stream = File.Create(path);
        await JsonSerializer.SerializeAsync(stream, status, s_jsonOptions, cancellationToken).ConfigureAwait(false);
    }

    private sealed class PersistedGraphRagStatus
    {
        public bool IsIndexed { get; set; }
        public DateTimeOffset? LastIndexedAtUtc { get; set; }
        public string? LastError { get; set; }
        public string Backend { get; set; } = "internal-fallback";
    }
}
