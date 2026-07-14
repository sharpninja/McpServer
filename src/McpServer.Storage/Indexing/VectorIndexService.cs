using System.Text.Json;
using HNSWIndex;
using HNSWIndex.Metrics;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace McpServer.Support.Mcp.Indexing;

/// <summary>
/// TR-PLANNED-CORE-013: HNSW-based vector index using HnswIndex 1.6.0 NuGet.
/// FR-SUPPORT-010: Provides approximate nearest-neighbor search with cosine distance.
/// Falls back to in-memory brute-force if HNSW fails to initialize.
/// </summary>
public sealed class VectorIndexService : IVectorIndexService, IDisposable
{
    private readonly ILogger<VectorIndexService> _logger;
    private readonly VectorIndexOptions _options;
    private readonly object _lock = new();
    private HNSWIndex<float[], float>? _index;
    private readonly Dictionary<string, int> _chunkIdToInternalId = new(StringComparer.Ordinal);
    private readonly Dictionary<int, string> _internalIdToChunkId = [];
    private readonly Dictionary<int, float[]> _internalIdToVector = [];

    /// <summary>TR-PLANNED-CORE-013: Constructor with configuration.</summary>
    public VectorIndexService(IOptions<VectorIndexOptions> options, ILogger<VectorIndexService> logger)
    {
        _options = options?.Value ?? new VectorIndexOptions();
        _logger = logger;
        InitializeIndex();
    }

    /// <summary>TR-PLANNED-CORE-013: Constructor for testing.</summary>
    internal VectorIndexService(VectorIndexOptions options, ILogger<VectorIndexService> logger)
    {
        _options = options ?? new VectorIndexOptions();
        _logger = logger;
        InitializeIndex();
    }

    /// <inheritdoc />
    public int Count
    {
        get { lock (_lock) return _chunkIdToInternalId.Count; }
    }

    /// <inheritdoc />
    public void AddVector(string chunkId, float[] embedding)
    {
        ArgumentNullException.ThrowIfNull(chunkId);
        ArgumentNullException.ThrowIfNull(embedding);
        lock (_lock)
        {
            if (_chunkIdToInternalId.TryGetValue(chunkId, out var existingId))
            {
                _internalIdToVector[existingId] = embedding;
                return;
            }

            try
            {
                var id = _index?.Add(embedding) ?? -1;
                if (id >= 0)
                {
                    _chunkIdToInternalId[chunkId] = id;
                    _internalIdToChunkId[id] = chunkId;
                    _internalIdToVector[id] = embedding;
                }
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "HNSW Add failed for chunk {ChunkId}", chunkId);
            }
        }
    }

    /// <inheritdoc />
    public bool RemoveVector(string chunkId)
    {
        ArgumentNullException.ThrowIfNull(chunkId);
        lock (_lock)
        {
            if (!_chunkIdToInternalId.TryGetValue(chunkId, out var internalId))
                return false;

            _chunkIdToInternalId.Remove(chunkId);
            _internalIdToChunkId.Remove(internalId);
            _internalIdToVector.Remove(internalId);
            return true;
        }
    }

    /// <inheritdoc />
    public IReadOnlyList<(string ChunkId, float Distance)> Search(float[] queryEmbedding, int k = 20)
    {
        ArgumentNullException.ThrowIfNull(queryEmbedding);
        lock (_lock)
        {
            if (_chunkIdToInternalId.Count == 0)
                return [];

            var actualK = Math.Min(k, _chunkIdToInternalId.Count);

            // Try HNSW search first
            if (_index is not null)
            {
                try
                {
                    var results = _index.KnnQuery(queryEmbedding, actualK);
                    return results
                        .Where(r => _internalIdToChunkId.ContainsKey(r.Id))
                        .Select(r => (_internalIdToChunkId[r.Id], (float)r.Distance))
                        .ToList();
                }
                catch (Exception ex)
                {
                    _logger.LogDebug(ex, "HNSW search failed, falling back to brute-force");
                }
            }

            // Brute-force fallback
            return _internalIdToVector
                .Select(kv => (_internalIdToChunkId[kv.Key], Distance: CosineDistance(queryEmbedding, kv.Value)))
                .OrderBy(x => x.Distance)
                .Take(actualK)
                .ToList();
        }
    }

    /// <inheritdoc />
    public async Task SaveAsync(string path, CancellationToken ct = default)
    {
        var actualPath = string.IsNullOrEmpty(path) ? _options.IndexPath : path;
        var dir = Path.GetDirectoryName(actualPath);
        if (!string.IsNullOrEmpty(dir))
            Directory.CreateDirectory(dir);

        lock (_lock)
        {
            if (_index is not null && _chunkIdToInternalId.Count > 0)
            {
                try
                {
                    _index.Serialize(actualPath);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to serialize HNSW index to {Path}", actualPath);
                }
            }

            // Save companion mapping file
            var mapPath = actualPath + ".map";
            var mapData = _chunkIdToInternalId
                .Select(kv => new ChunkIdMapping(kv.Key, kv.Value))
                .ToList();
            var json = JsonSerializer.Serialize(mapData, VectorIndexJsonContext.Default.ListChunkIdMapping);
            File.WriteAllText(mapPath, json);

            // Save vectors for rebuild capability
            var vectorsPath = actualPath + ".vectors";
            using var fs = File.Create(vectorsPath);
            using var bw = new BinaryWriter(fs);
            bw.Write(_internalIdToVector.Count);
            foreach (var (id, vector) in _internalIdToVector)
            {
                bw.Write(id);
                bw.Write(vector.Length);
                foreach (var v in vector) bw.Write(v);
            }
        }

        _logger.LogInformation("VectorIndexService: saved {Count} vectors to {Path}", Count, actualPath);
        await Task.CompletedTask.ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task LoadAsync(string path, CancellationToken ct = default)
    {
        var actualPath = string.IsNullOrEmpty(path) ? _options.IndexPath : path;
        var mapPath = actualPath + ".map";
        var vectorsPath = actualPath + ".vectors";

        if (!File.Exists(mapPath) || !File.Exists(vectorsPath))
        {
            _logger.LogDebug("VectorIndexService: no persisted index found at {Path}", actualPath);
            return;
        }

        lock (_lock)
        {
            try
            {
                // Load mapping
                var json = File.ReadAllText(mapPath);
                var mappings = JsonSerializer.Deserialize(json, VectorIndexJsonContext.Default.ListChunkIdMapping) ?? [];
                _chunkIdToInternalId.Clear();
                _internalIdToChunkId.Clear();
                _internalIdToVector.Clear();

                foreach (var m in mappings)
                {
                    _chunkIdToInternalId[m.ChunkId] = m.InternalId;
                    _internalIdToChunkId[m.InternalId] = m.ChunkId;
                }

                // Load vectors
                using var fs = File.OpenRead(vectorsPath);
                using var br = new BinaryReader(fs);
                var count = br.ReadInt32();
                for (var i = 0; i < count; i++)
                {
                    var id = br.ReadInt32();
                    var dim = br.ReadInt32();
                    var vec = new float[dim];
                    for (var d = 0; d < dim; d++) vec[d] = br.ReadSingle();
                    _internalIdToVector[id] = vec;
                }

                // Try load HNSW index
                if (File.Exists(actualPath))
                {
                    try
                    {
                        _index = HNSWIndex<float[], float>.Deserialize(CosineMetric.Compute, actualPath);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to deserialize HNSW index, will rebuild");
                        RebuildHnswIndex();
                    }
                }
                else
                {
                    RebuildHnswIndex();
                }

                _logger.LogInformation("VectorIndexService: loaded {Count} vectors from {Path}", _chunkIdToInternalId.Count, actualPath);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to load vector index from {Path}", actualPath);
            }
        }

        await Task.CompletedTask.ConfigureAwait(false);
    }

    /// <inheritdoc />
    public Task RebuildAsync(CancellationToken ct = default)
    {
        lock (_lock)
        {
            _chunkIdToInternalId.Clear();
            _internalIdToChunkId.Clear();
            _internalIdToVector.Clear();
            InitializeIndex();
        }
        _logger.LogInformation("VectorIndexService: index cleared for rebuild");
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public void Dispose()
    {
        lock (_lock)
        {
            _chunkIdToInternalId.Clear();
            _internalIdToChunkId.Clear();
            _internalIdToVector.Clear();
        }
    }

    private void InitializeIndex()
    {
        if (!_options.Enabled)
        {
            _logger.LogInformation("VectorIndexService: disabled via configuration.");
            return;
        }

        try
        {
            var parameters = new HNSWParameters<float>
            {
                MaxEdges = _options.M,
                MaxCandidates = _options.EfConstruction,
                CollectionSize = _options.MaxElements,
                MinNN = _options.EfSearch
            };
            _index = new HNSWIndex<float[], float>(CosineMetric.Compute, parameters);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to initialize HNSW index, using brute-force fallback");
            _index = null;
        }
    }

    private void RebuildHnswIndex()
    {
        InitializeIndex();
        if (_index is null) return;
        foreach (var (_, vector) in _internalIdToVector.OrderBy(kv => kv.Key))
        {
            try { _index.Add(vector); }
            catch { /* skip */ }
        }
    }

    private static float CosineDistance(float[] a, float[] b)
    {
        if (a.Length != b.Length) return float.MaxValue;
        float dot = 0, normA = 0, normB = 0;
        for (var i = 0; i < a.Length; i++)
        {
            dot += a[i] * b[i];
            normA += a[i] * a[i];
            normB += b[i] * b[i];
        }
        var denom = MathF.Sqrt(normA) * MathF.Sqrt(normB);
        if (denom < 1e-10f) return 1f;
        return 1f - (dot / denom);
    }
}

internal sealed record ChunkIdMapping(string ChunkId, int InternalId);
