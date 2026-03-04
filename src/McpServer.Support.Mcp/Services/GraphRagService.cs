using System.Collections.Concurrent;
using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Linq;
using McpServer.Support.Mcp.Ingestion;
using McpServer.Support.Mcp.Models;
using McpServer.Support.Mcp.Options;
using Microsoft.Extensions.Options;

namespace McpServer.Support.Mcp.Services;

internal sealed class GraphRagService : IGraphRagService
{
    private const string StatusFileName = "graphrag-status.json";
    private const string ReadyArtifactFileName = "output/graphrag-index-ready.json";
    private static readonly JsonSerializerOptions s_jsonOptions = new(JsonSerializerDefaults.Web) { WriteIndented = true };
    private static readonly ConcurrentDictionary<string, SemaphoreSlim> s_workspaceIndexLocks = new(StringComparer.OrdinalIgnoreCase);
    private static readonly ConcurrentDictionary<string, string> s_workspaceActiveJobs = new(StringComparer.OrdinalIgnoreCase);

    private readonly GraphRagOptions _options;
    private readonly IngestionOptions _ingestionOptions;
    private readonly WorkspaceContext _workspaceContext;
    private readonly IContextSearchService _contextSearchService;
    private readonly IReadOnlyList<IGraphRagBackendAdapter> _backendAdapters;
    private readonly ILogger<GraphRagService> _logger;

    public GraphRagService(
        IOptions<GraphRagOptions> options,
        IOptions<IngestionOptions> ingestionOptions,
        WorkspaceContext workspaceContext,
        IContextSearchService contextSearchService,
        IEnumerable<IGraphRagBackendAdapter> backendAdapters,
        ILogger<GraphRagService> logger)
    {
        _options = options.Value;
        _ingestionOptions = ingestionOptions.Value;
        _workspaceContext = workspaceContext;
        _contextSearchService = contextSearchService;
        _backendAdapters = backendAdapters.ToList();
        _logger = logger;
    }

    public async Task<GraphRagStatusResponse> GetStatusAsync(CancellationToken cancellationToken = default)
    {
        var workspacePath = ResolveWorkspacePath();
        var graphRoot = ResolveGraphRoot(workspacePath);
        var persisted = await TryReadStatusAsync(graphRoot, cancellationToken).ConfigureAwait(false);
        var initialized = HasInitializedStructure(graphRoot);
        var activeJobId = s_workspaceActiveJobs.TryGetValue(workspacePath, out var currentJobId) ? currentJobId : null;
        var isIndexedByArtifact = IsReadyArtifactPresent(graphRoot);
        var isIndexed = persisted?.IsIndexed == true && isIndexedByArtifact;
        var backendAvailabilityIssue = GetBackendAvailabilityIssue();

        return new GraphRagStatusResponse
        {
            Enabled = _options.Enabled,
            WorkspacePath = workspacePath,
            GraphRoot = graphRoot,
            State = ResolveState(_options.Enabled, initialized, isIndexed, activeJobId, backendAvailabilityIssue ?? persisted?.LastError),
            IsInitialized = initialized,
            IsIndexed = isIndexed,
            LastIndexedAtUtc = persisted?.LastIndexedAtUtc,
            LastSuccessAtUtc = persisted?.LastSuccessAtUtc,
            LastFailureAtUtc = persisted?.LastFailureAtUtc,
            ActiveJobId = activeJobId,
            FailureCode = backendAvailabilityIssue is null ? persisted?.FailureCode : "backend_unavailable",
            LastError = backendAvailabilityIssue ?? persisted?.LastError,
            ArtifactVersion = persisted?.ArtifactVersion ?? _options.ArtifactVersion,
            LastIndexDurationMs = persisted?.LastIndexDurationMs,
            LastIndexedDocumentCount = persisted?.LastIndexedDocumentCount,
            Backend = ResolveBackendName()
        };
    }

    public async Task<GraphRagStatusResponse> InitializeAsync(CancellationToken cancellationToken = default)
    {
        var workspacePath = ResolveWorkspacePath();
        var graphRoot = ResolveGraphRoot(workspacePath);
        EnsureGraphDirectories(graphRoot);

        var existing = await TryReadStatusAsync(graphRoot, cancellationToken).ConfigureAwait(false);
        CleanupStaleArtifacts(graphRoot);
        var status = new PersistedGraphRagStatus
        {
            IsIndexed = existing?.IsIndexed ?? IsReadyArtifactPresent(graphRoot),
            LastIndexedAtUtc = existing?.LastIndexedAtUtc,
            LastSuccessAtUtc = existing?.LastSuccessAtUtc,
            LastFailureAtUtc = existing?.LastFailureAtUtc,
            FailureCode = null,
            LastError = null,
            ArtifactVersion = _options.ArtifactVersion,
            LastIndexDurationMs = existing?.LastIndexDurationMs,
            LastIndexedDocumentCount = existing?.LastIndexedDocumentCount,
            Backend = ResolveBackendName(),
        };
        await WriteStatusAsync(graphRoot, status, cancellationToken).ConfigureAwait(false);

        return await GetStatusAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task<GraphRagStatusResponse> IndexAsync(GraphRagIndexRequest? request = null, CancellationToken cancellationToken = default)
    {
        var workspacePath = ResolveWorkspacePath();
        var graphRoot = ResolveGraphRoot(workspacePath);
        EnsureGraphDirectories(graphRoot);
        CleanupStaleArtifacts(graphRoot);
        var readyArtifactPath = GetReadyArtifactPath(graphRoot);
        var backend = SelectBackend();
        var backendContext = new GraphRagBackendExecutionContext(workspacePath, graphRoot, _options);
        var lockKey = workspacePath;
        var workspaceLock = s_workspaceIndexLocks.GetOrAdd(lockKey, _ => new SemaphoreSlim(1, 1));
        var maxConcurrency = Math.Max(1, _options.MaxConcurrentIndexJobsPerWorkspace);
        if (maxConcurrency <= 1 && !await workspaceLock.WaitAsync(0, cancellationToken).ConfigureAwait(false))
            throw new InvalidOperationException("A GraphRAG index operation is already running for this workspace.");

        var jobId = $"job-{Guid.NewGuid():N}";
        s_workspaceActiveJobs[lockKey] = jobId;
        var stopwatch = Stopwatch.StartNew();
        var documentCount = 0;

        try
        {
            if (request?.Force == true && File.Exists(readyArtifactPath))
                File.Delete(readyArtifactPath);
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            cts.CancelAfter(TimeSpan.FromSeconds(Math.Max(1, _options.IndexTimeoutSeconds)));
            var backendResult = await backend.IndexAsync(backendContext, request, cts.Token).ConfigureAwait(false);
            if (!backendResult.Success)
                throw new InvalidOperationException(backendResult.Error ?? "GraphRAG backend index failed.");

            documentCount = backendResult.DocumentCount;
            await WriteReadyArtifactAsync(readyArtifactPath, documentCount, cancellationToken).ConfigureAwait(false);
            stopwatch.Stop();

            var status = new PersistedGraphRagStatus
            {
                IsIndexed = true,
                LastIndexedAtUtc = DateTimeOffset.UtcNow,
                LastSuccessAtUtc = DateTimeOffset.UtcNow,
                LastFailureAtUtc = null,
                FailureCode = null,
                LastError = null,
                ArtifactVersion = _options.ArtifactVersion,
                LastIndexDurationMs = (long)stopwatch.Elapsed.TotalMilliseconds,
                LastIndexedDocumentCount = documentCount,
                Backend = ResolveBackendName(),
            };

            await WriteStatusAsync(graphRoot, status, cancellationToken).ConfigureAwait(false);
            _logger.LogInformation("GraphRAG index marked ready: Workspace={WorkspacePath}; GraphRoot={GraphRoot}; Force={Force}",
                workspacePath,
                graphRoot,
                request?.Force ?? false);
        }
        catch (OperationCanceledException)
        {
            stopwatch.Stop();
            var canceled = new PersistedGraphRagStatus
            {
                IsIndexed = false,
                LastIndexedAtUtc = null,
                LastSuccessAtUtc = null,
                LastFailureAtUtc = DateTimeOffset.UtcNow,
                FailureCode = "index_canceled",
                LastError = "GraphRAG index was canceled or timed out.",
                ArtifactVersion = _options.ArtifactVersion,
                LastIndexDurationMs = (long)stopwatch.Elapsed.TotalMilliseconds,
                LastIndexedDocumentCount = documentCount,
                Backend = ResolveBackendName()
            };
            await WriteStatusAsync(graphRoot, canceled, cancellationToken).ConfigureAwait(false);
            throw;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            stopwatch.Stop();
            var failed = new PersistedGraphRagStatus
            {
                IsIndexed = false,
                LastIndexedAtUtc = null,
                LastSuccessAtUtc = null,
                LastFailureAtUtc = DateTimeOffset.UtcNow,
                FailureCode = "index_failed",
                LastError = ex.Message,
                ArtifactVersion = _options.ArtifactVersion,
                LastIndexDurationMs = (long)stopwatch.Elapsed.TotalMilliseconds,
                LastIndexedDocumentCount = documentCount,
                Backend = ResolveBackendName(),
            };
            await WriteStatusAsync(graphRoot, failed, cancellationToken).ConfigureAwait(false);
            _logger.LogWarning(ex, "GraphRAG index failed");
        }
        finally
        {
            s_workspaceActiveJobs.TryRemove(lockKey, out _);
            if (maxConcurrency <= 1)
                workspaceLock.Release();
        }

        return await GetStatusAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task<GraphRagQueryResponse> QueryAsync(GraphRagQueryRequest request, CancellationToken cancellationToken = default)
    {
        var status = await GetStatusAsync(cancellationToken).ConfigureAwait(false);
        var query = (request.Query ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(query))
            throw new InvalidOperationException("query is required");

        var mode = string.IsNullOrWhiteSpace(request.Mode) ? _options.DefaultQueryMode : request.Mode.Trim();
        var maxChunks = Math.Clamp(request.MaxChunks ?? _options.DefaultMaxChunks, 1, 100);
        var fallbackReason = GetFallbackReason(status);
        var fallbackUsed = fallbackReason is not null;

        if (!fallbackUsed && IsExternalBackendConfigured())
        {
            var external = await SelectBackend().QueryAsync(
                new GraphRagBackendExecutionContext(status.WorkspacePath, status.GraphRoot, _options),
                request,
                query,
                mode,
                maxChunks,
                cancellationToken).ConfigureAwait(false);
            if (external is not null)
                return external;
            fallbackUsed = true;
            fallbackReason = "external_query_failed";
        }

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

        var answer = BuildFallbackAnswer(query, chunks.Count, searchResult.SourceKeys, mode);
        return new GraphRagQueryResponse
        {
            Query = query,
            Mode = mode,
            Answer = answer,
            Chunks = request.IncludeContextChunks ? chunks : [],
            SourceKeys = searchResult.SourceKeys,
            Citations = citations,
            Entities = BuildEntitiesFromChunks(chunks, request.MaxEntities),
            Relationships = BuildRelationshipsFromSourceKeys(searchResult.SourceKeys, request.MaxRelationships),
            Communities = BuildCommunities(searchResult.SourceKeys, request.CommunityDepth),
            FallbackUsed = fallbackUsed,
            FallbackReason = fallbackReason,
            FailureCode = fallbackUsed ? "query_fallback" : null,
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
        {
            var workspaceHash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(workspacePath))).ToLowerInvariant()[..12];
            return Path.GetFullPath(Path.Combine(_options.RootPath, workspaceHash));
        }
        return Path.GetFullPath(Path.Combine(workspacePath, _options.RootPath));
    }

    private static void EnsureGraphDirectories(string graphRoot)
    {
        Directory.CreateDirectory(graphRoot);
        Directory.CreateDirectory(Path.Combine(graphRoot, "input"));
        Directory.CreateDirectory(Path.Combine(graphRoot, "output"));
        Directory.CreateDirectory(Path.Combine(graphRoot, "cache"));
        Directory.CreateDirectory(Path.Combine(graphRoot, "logs"));
        Directory.CreateDirectory(Path.Combine(graphRoot, "config"));
    }

    private static bool HasInitializedStructure(string graphRoot)
    {
        return Directory.Exists(graphRoot)
               && Directory.Exists(Path.Combine(graphRoot, "input"))
               && Directory.Exists(Path.Combine(graphRoot, "output"))
               && Directory.Exists(Path.Combine(graphRoot, "cache"))
               && Directory.Exists(Path.Combine(graphRoot, "logs"))
               && Directory.Exists(Path.Combine(graphRoot, "config"));
    }

    private static string BuildFallbackAnswer(string query, int chunkCount, IReadOnlyList<string> sourceKeys, string mode)
    {
        if (chunkCount == 0)
            return $"No GraphRAG context found for '{query}'.";
        return $"GraphRAG {mode} fallback retrieved {chunkCount} context chunk(s) from {sourceKeys.Count} source(s) for '{query}'.";
    }

    private static string? GetFallbackReason(GraphRagStatusResponse status)
    {
        if (!status.Enabled)
            return "graphrag_disabled";
        if (string.Equals(status.State, "indexing", StringComparison.OrdinalIgnoreCase))
            return "index_in_progress";
        if (!status.IsInitialized)
            return "graphrag_not_initialized";
        if (!status.IsIndexed)
            return "graphrag_not_indexed";
        return null;
    }

    private static string ResolveState(bool enabled, bool initialized, bool indexed, string? activeJobId, string? lastError)
    {
        if (!enabled)
            return "disabled";
        if (!string.IsNullOrWhiteSpace(activeJobId))
            return "indexing";
        if (!initialized)
            return "uninitialized";
        if (!indexed)
            return string.IsNullOrWhiteSpace(lastError) ? "ready_for_index" : "degraded";
        return "ready";
    }

    private bool IsExternalBackendConfigured() => string.Equals(SelectBackend().AdapterName, "external-command", StringComparison.OrdinalIgnoreCase);

    private string? GetBackendAvailabilityIssue()
    {
        if (!IsExternalBackendConfigured())
            return null;

        if (Path.IsPathRooted(_options.BackendCommand!) && !File.Exists(_options.BackendCommand))
            return $"Configured GraphRAG backend command not found: {_options.BackendCommand}";

        return null;
    }

    private IGraphRagBackendAdapter SelectBackend()
    {
        var selected = _backendAdapters.FirstOrDefault(a => a.CanHandle(_options));
        if (selected is not null)
            return selected;

        return _backendAdapters.First(a => string.Equals(a.AdapterName, "internal-fallback", StringComparison.OrdinalIgnoreCase));
    }

    private static string GetReadyArtifactPath(string graphRoot) => Path.Combine(graphRoot, ReadyArtifactFileName);

    private static bool IsReadyArtifactPresent(string graphRoot) => File.Exists(GetReadyArtifactPath(graphRoot));

    private static async Task WriteReadyArtifactAsync(string readyArtifactPath, int documentCount, CancellationToken cancellationToken)
    {
        var payload = new
        {
            indexedAtUtc = DateTimeOffset.UtcNow,
            documentCount
        };
        var json = JsonSerializer.Serialize(payload, s_jsonOptions);
        await File.WriteAllTextAsync(readyArtifactPath, json, cancellationToken).ConfigureAwait(false);
    }

    private static void CleanupStaleArtifacts(string graphRoot)
    {
        foreach (var folder in new[]
                 {
                     graphRoot,
                     Path.Combine(graphRoot, "output"),
                     Path.Combine(graphRoot, "cache")
                 })
        {
            if (!Directory.Exists(folder))
                continue;

            foreach (var file in Directory.EnumerateFiles(folder, "*.tmp", SearchOption.AllDirectories))
            {
                var age = DateTimeOffset.UtcNow - File.GetLastWriteTimeUtc(file);
                if (age > TimeSpan.FromHours(2))
                    File.Delete(file);
            }

            foreach (var file in Directory.EnumerateFiles(folder, "*.partial", SearchOption.AllDirectories))
            {
                var age = DateTimeOffset.UtcNow - File.GetLastWriteTimeUtc(file);
                if (age > TimeSpan.FromHours(2))
                    File.Delete(file);
            }
        }
    }

    private static IReadOnlyList<string> BuildEntitiesFromChunks(IReadOnlyList<ContextChunk> chunks, int? maxEntities)
    {
        var limit = Math.Clamp(maxEntities ?? 20, 1, 100);
        return chunks
            .SelectMany(static c => c.Content.Split([' ', '\r', '\n', '\t', ',', '.', ';', ':', '(', ')', '[', ']', '{', '}', '"', '\''], StringSplitOptions.RemoveEmptyEntries))
            .Where(static token => token.Length >= 4 && char.IsUpper(token[0]))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Take(limit)
            .ToList();
    }

    private static IReadOnlyList<string> BuildRelationshipsFromSourceKeys(IReadOnlyList<string> sourceKeys, int? maxRelationships)
    {
        var limit = Math.Clamp(maxRelationships ?? 20, 1, 100);
        var relationships = new List<string>(Math.Min(limit, sourceKeys.Count));
        for (var i = 0; i < sourceKeys.Count && relationships.Count < limit; i++)
        {
            var next = i + 1 < sourceKeys.Count ? sourceKeys[i + 1] : sourceKeys[0];
            if (string.IsNullOrWhiteSpace(sourceKeys[i]) || string.IsNullOrWhiteSpace(next))
                continue;
            relationships.Add($"{sourceKeys[i]} -> {next}");
        }
        return relationships;
    }

    private static IReadOnlyList<string> BuildCommunities(IReadOnlyList<string> sourceKeys, int? communityDepth)
    {
        var depth = Math.Clamp(communityDepth ?? 2, 1, 5);
        return sourceKeys
            .GroupBy(static key => key.Split(['/', '\\'], StringSplitOptions.RemoveEmptyEntries).FirstOrDefault() ?? "root", StringComparer.OrdinalIgnoreCase)
            .OrderByDescending(static g => g.Count())
            .Take(depth)
            .Select(static g => $"{g.Key} ({g.Count()})")
            .ToList();
    }

    private string ResolveBackendName()
        => SelectBackend().AdapterName;

    private static string GetStatusFilePath(string graphRoot)
        => Path.Combine(graphRoot, StatusFileName);

    private static async Task<PersistedGraphRagStatus?> TryReadStatusAsync(string graphRoot, CancellationToken cancellationToken)
    {
        var path = GetStatusFilePath(graphRoot);
        if (!File.Exists(path))
            return null;

        try
        {
            await using var stream = File.OpenRead(path);
            return await JsonSerializer.DeserializeAsync<PersistedGraphRagStatus>(stream, s_jsonOptions, cancellationToken).ConfigureAwait(false);
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private static async Task WriteStatusAsync(string graphRoot, PersistedGraphRagStatus status, CancellationToken cancellationToken)
    {
        var path = GetStatusFilePath(graphRoot);
        var tempPath = path + ".tmp";
        await using (var stream = File.Create(tempPath))
        {
            await JsonSerializer.SerializeAsync(stream, status, s_jsonOptions, cancellationToken).ConfigureAwait(false);
        }

        if (File.Exists(path))
            File.Delete(path);
        File.Move(tempPath, path);
    }

    private sealed class PersistedGraphRagStatus
    {
        public bool IsIndexed { get; set; }
        public DateTimeOffset? LastIndexedAtUtc { get; set; }
        public DateTimeOffset? LastSuccessAtUtc { get; set; }
        public DateTimeOffset? LastFailureAtUtc { get; set; }
        public string? FailureCode { get; set; }
        public string? LastError { get; set; }
        public string ArtifactVersion { get; set; } = "v1";
        public long? LastIndexDurationMs { get; set; }
        public int? LastIndexedDocumentCount { get; set; }
        public string Backend { get; set; } = "internal-fallback";
    }
}
