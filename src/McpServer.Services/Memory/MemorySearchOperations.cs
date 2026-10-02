using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using McpServer.Support.Mcp.Storage;
using McpServer.Support.Mcp.Storage.Entities;
using Microsoft.EntityFrameworkCore;

namespace McpServer.Support.Mcp.Services;

/// <summary>
/// FR-MCP-MEMORY-011 / TR-MCP-MEMORY-SEARCH-002: Dedicated memory indexer and hybrid
/// BM25+vector recall. Not a public <see cref="IMemoryService"/> facade.
/// </summary>
public sealed class MemorySearchOperations
{
    private static readonly JsonSerializerOptions EmbeddingJson = new(JsonSerializerDefaults.Web);
    private readonly McpDbContext _db;

    /// <summary>Creates operations bound to an already-scoped <see cref="McpDbContext"/>.</summary>
    public MemorySearchOperations(McpDbContext db)
    {
        _db = db;
    }

    /// <summary>AC-FR-MCP-MEMORY-011: Hybrid recall with validation, filters, and scores.</summary>
    public async Task<MemoryRecallResult> RecallAsync(RecallMemoryQuery query, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(query.Query))
        {
            return new MemoryRecallResult(
                400,
                FailureKind: MemoryMutationFailureKind.Validation,
                Error: "Query is required.");
        }

        if (query.Query.Length > MemorySearchLimits.MaxQueryLength)
        {
            return new MemoryRecallResult(
                400,
                FailureKind: MemoryMutationFailureKind.Validation,
                Error: "Query exceeds the configured maximum length.");
        }

        if (query.MinScore is < 0 or > 1)
        {
            return new MemoryRecallResult(
                400,
                FailureKind: MemoryMutationFailureKind.Validation,
                Error: "minScore must be in [0,1].");
        }

        if (query.TopN is <= 0)
        {
            return new MemoryRecallResult(
                400,
                FailureKind: MemoryMutationFailureKind.Validation,
                Error: "topN must be a positive integer.");
        }

        var minScore = query.MinScore ?? MemorySearchLimits.DefaultMinScore;
        var topN = query.TopN ?? MemorySearchLimits.DefaultTopN;
        if (topN > MemorySearchLimits.MaxTopN)
            topN = MemorySearchLimits.MaxTopN;

        var bm25Weight = query.FusionBm25Weight ?? 0.5;
        var vectorWeight = query.FusionVectorWeight ?? 0.5;
        if (bm25Weight + vectorWeight <= 0)
        {
            bm25Weight = 0.5;
            vectorWeight = 0.5;
        }

        var rows = await _db.Memories.AsNoTracking().ToListAsync(cancellationToken).ConfigureAwait(false);
        var indexes = await _db.MemoryIndexes.AsNoTracking().ToListAsync(cancellationToken).ConfigureAwait(false);
        var indexById = indexes.ToDictionary(index => index.MemoryId, StringComparer.Ordinal);

        var queryText = query.Query;
        var queryEmbedding = MemoryLocalEmbedding.Embed(queryText);
        var hits = new List<MemoryRecallHit>();

        foreach (var entity in rows)
        {
            if (!PassesFilters(entity, query))
                continue;

            var item = MemoryLayerMapper.ToItem(entity);
            var searchable = BuildSearchableText(entity);
            var lexical = ScoreLexical(queryText, searchable);
            var vectorEligible = IsVectorReady(entity, indexById);
            var vector = 0d;
            if (vectorEligible && indexById.TryGetValue(entity.Id, out var indexRow))
            {
                var stored = MemoryLocalEmbedding.Deserialize(indexRow.EmbeddingJson);
                if (stored.Length > 0)
                {
                    var cosine = MemoryLocalEmbedding.Cosine(queryEmbedding, stored);
                    vector = cosine >= 0.28 ? cosine : 0;
                }
            }

            var fused = vectorEligible
                ? ((bm25Weight * lexical) + (vectorWeight * vector)) / (bm25Weight + vectorWeight)
                : lexical;

            if (fused < minScore)
                continue;

            if (lexical <= 0 && vector <= 0)
                continue;

            var matchKind = lexical > 0 && vector > 0
                ? "hybrid"
                : vector > 0 ? "vector" : "bm25";

            hits.Add(new MemoryRecallHit
            {
                Id = item.Id,
                Score = Math.Clamp(fused, 0, 1),
                Title = item.Title,
                Content = item.Content ?? item.Text,
                Text = item.Text,
                Type = item.Type,
                Tags = item.Tags,
                Scope = item.Scope,
                MatchKind = matchKind,
                AnnWorkspaceId = _db.CurrentWorkspaceId,
            });
        }

        var ranked = hits
            .OrderByDescending(hit => hit.Score)
            .ThenBy(hit => hit.Id, StringComparer.Ordinal)
            .Take(topN)
            .ToList();

        return new MemoryRecallResult(
            200,
            ranked,
            RerankApplied: false,
            RankingMode: "hybrid");
    }

    /// <summary>AC-TR-MCP-MEMORY-SEARCH-002: Indexes one memory to a terminal EmbeddingStatus.</summary>
    public async Task<MemoryIndexResult> IndexAsync(
        string memoryId,
        string? provider,
        bool cloudEnabled,
        CancellationToken cancellationToken)
    {
        if (cancellationToken.IsCancellationRequested)
        {
            var current = await ReadStatusAsync(memoryId, CancellationToken.None).ConfigureAwait(false);
            return new MemoryIndexResult(
                200,
                memoryId,
                current is "ready" ? "pending" : current ?? "pending");
        }

        var entity = await _db.Memories
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(memory => memory.Id == memoryId, cancellationToken)
            .ConfigureAwait(false);
        if (entity is null)
        {
            return new MemoryIndexResult(
                404,
                memoryId,
                Error: $"Memory '{memoryId}' not found.");
        }

        var content = MemoryLayerMapper.EffectiveContent(entity);
        if (string.IsNullOrWhiteSpace(content))
        {
            if (string.Equals(entity.EmbeddingStatus, "ready", StringComparison.OrdinalIgnoreCase))
                entity.EmbeddingStatus = "pending";
            await _db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            return new MemoryIndexResult(
                400,
                memoryId,
                entity.EmbeddingStatus ?? "pending",
                Error: "Content is required.");
        }

        if (string.Equals(provider, "cloud", StringComparison.OrdinalIgnoreCase) && !cloudEnabled)
        {
            entity.EmbeddingStatus = "failed";
            await _db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            return new MemoryIndexResult(
                200,
                memoryId,
                "failed",
                FailureReason: "Cloud embedding is disabled.");
        }

        cancellationToken.ThrowIfCancellationRequested();

        var hash = ComputeContentHash(entity);
        var embedding = MemoryLocalEmbedding.Embed(BuildSearchableText(entity));
        var existing = await _db.MemoryIndexes
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(index => index.MemoryId == memoryId, cancellationToken)
            .ConfigureAwait(false);
        if (existing is null)
        {
            _db.MemoryIndexes.Add(new MemoryIndexEntity
            {
                MemoryId = entity.Id,
                WorkspaceId = entity.WorkspaceId,
                ContentHash = hash,
                EmbeddingJson = MemoryLocalEmbedding.Serialize(embedding),
                IndexedAtUtc = DateTimeOffset.UtcNow,
            });
        }
        else
        {
            existing.WorkspaceId = entity.WorkspaceId;
            existing.ContentHash = hash;
            existing.EmbeddingJson = MemoryLocalEmbedding.Serialize(embedding);
            existing.IndexedAtUtc = DateTimeOffset.UtcNow;
        }

        entity.EmbeddingStatus = "ready";
        await _db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return new MemoryIndexResult(200, memoryId, "ready", ReadyCount: 1);
    }

    /// <summary>AC-TR-MCP-MEMORY-SEARCH-002-02: Repairs stale EmbeddingStatus vs row hash.</summary>
    public async Task<MemoryIndexResult> ReconcileAsync(CancellationToken cancellationToken)
    {
        var rows = await _db.Memories.ToListAsync(cancellationToken).ConfigureAwait(false);
        var indexes = await _db.MemoryIndexes.ToListAsync(cancellationToken).ConfigureAwait(false);
        var indexById = indexes.ToDictionary(index => index.MemoryId, StringComparer.Ordinal);
        var ready = 0;
        var failed = 0;
        string? lastStatus = null;

        foreach (var entity in rows)
        {
            var hash = ComputeContentHash(entity);
            var hasFresh = indexById.TryGetValue(entity.Id, out var index)
                && string.Equals(index.ContentHash, hash, StringComparison.Ordinal);
            if (hasFresh && string.Equals(entity.EmbeddingStatus, "ready", StringComparison.OrdinalIgnoreCase))
                continue;

            var indexed = await IndexAsync(entity.Id, provider: "onnx", cloudEnabled: false, cancellationToken)
                .ConfigureAwait(false);
            lastStatus = indexed.EmbeddingStatus;
            if (string.Equals(indexed.EmbeddingStatus, "ready", StringComparison.OrdinalIgnoreCase))
                ready++;
            else if (string.Equals(indexed.EmbeddingStatus, "failed", StringComparison.OrdinalIgnoreCase))
                failed++;
        }

        return new MemoryIndexResult(200, EmbeddingStatus: lastStatus ?? "ready", ReadyCount: ready, FailedCount: failed);
    }

    /// <summary>AC-TR-MCP-MEMORY-SEARCH-002-12: Batch-indexes memories to a terminal status.</summary>
    public async Task<MemoryIndexResult> BatchIndexAsync(IReadOnlyList<string>? memoryIds, CancellationToken cancellationToken)
    {
        IReadOnlyList<string> ids = memoryIds is { Count: > 0 }
            ? memoryIds
            : await _db.Memories.Select(memory => memory.Id).ToListAsync(cancellationToken).ConfigureAwait(false);

        var ready = 0;
        var failed = 0;
        foreach (var id in ids)
        {
            var indexed = await IndexAsync(id, provider: "onnx", cloudEnabled: false, cancellationToken)
                .ConfigureAwait(false);
            if (string.Equals(indexed.EmbeddingStatus, "ready", StringComparison.OrdinalIgnoreCase))
                ready++;
            else
                failed++;
        }

        return new MemoryIndexResult(200, ReadyCount: ready, FailedCount: failed);
    }

    private async Task<string?> ReadStatusAsync(string memoryId, CancellationToken cancellationToken)
    {
        var entity = await _db.Memories
            .IgnoreQueryFilters()
            .AsNoTracking()
            .FirstOrDefaultAsync(memory => memory.Id == memoryId, cancellationToken)
            .ConfigureAwait(false);
        return entity?.EmbeddingStatus;
    }

    private static bool PassesFilters(MemoryEntity entity, RecallMemoryQuery query)
    {
        if (query.Scope is MemoryScope.Global
            && !string.Equals(entity.Scope, MemoryEntity.GlobalScope, StringComparison.Ordinal))
        {
            return false;
        }

        if (query.Scope is MemoryScope.Workspace
            && !string.Equals(entity.Scope, MemoryEntity.WorkspaceScope, StringComparison.Ordinal))
        {
            return false;
        }

        if (!string.IsNullOrWhiteSpace(query.Type))
        {
            var type = entity.Type ?? entity.Category;
            if (!string.Equals(type, query.Type.Trim(), StringComparison.OrdinalIgnoreCase))
                return false;
        }

        if (query.Tags is { Count: > 0 })
        {
            var tags = MemoryLayerMapper.DeserializeTags(entity.Tags);
            foreach (var required in query.Tags)
            {
                if (string.IsNullOrWhiteSpace(required))
                    continue;
                if (!tags.Any(tag => string.Equals(tag, required.Trim(), StringComparison.OrdinalIgnoreCase)))
                    return false;
            }
        }

        return true;
    }

    private static bool IsVectorReady(MemoryEntity entity, IReadOnlyDictionary<string, MemoryIndexEntity> indexes)
    {
        if (!string.Equals(entity.EmbeddingStatus, "ready", StringComparison.OrdinalIgnoreCase))
            return false;
        if (!indexes.TryGetValue(entity.Id, out var index))
            return false;
        return string.Equals(index.ContentHash, ComputeContentHash(entity), StringComparison.Ordinal);
    }

    private static string BuildSearchableText(MemoryEntity entity)
        => string.Join('\n', new[] { entity.Title, MemoryLayerMapper.EffectiveContent(entity) }.Where(part => !string.IsNullOrWhiteSpace(part)));

    private static string ComputeContentHash(MemoryEntity entity)
    {
        var payload = BuildSearchableText(entity);
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(payload));
        return Convert.ToHexString(bytes);
    }

    private static double ScoreLexical(string query, string document)
    {
        if (string.Equals(query.Trim(), document.Trim(), StringComparison.OrdinalIgnoreCase))
            return 1.0;

        if (document.Contains(query.Trim(), StringComparison.OrdinalIgnoreCase))
        {
            var ratio = Math.Min(1.0, (double)query.Trim().Length / Math.Max(document.Length, 1));
            return Math.Clamp(0.72 + (0.23 * ratio), 0.72, 0.95);
        }

        var queryTokens = MemoryLocalEmbedding.Tokenize(query);
        var documentTokens = MemoryLocalEmbedding.Tokenize(document);
        if (queryTokens.Count == 0 || documentTokens.Count == 0)
            return 0;

        var documentSet = new HashSet<string>(documentTokens, StringComparer.Ordinal);
        var matched = queryTokens.Count(token =>
            documentSet.Contains(token)
            || documentTokens.Any(doc => doc.Contains(token, StringComparison.Ordinal) || token.Contains(doc, StringComparison.Ordinal)));
        if (matched == 0)
            return 0;

        var coverage = (double)matched / queryTokens.Count;
        var precision = (double)matched / Math.Max(documentTokens.Count, 1);
        return Math.Clamp((0.65 * coverage) + (0.25 * precision), 0, 0.94);
    }
}

/// <summary>
/// TR-MCP-MEMORY-SEARCH-002-09: Deterministic local embedding used when ONNX weights are
/// absent. Same vector space is used for query and document so paraphrase overlap scores.
/// </summary>
public static class MemoryLocalEmbedding
{
    /// <summary>Local embedding width. Independent of the optional 384-d ONNX model.</summary>
    public const int Dimensions = 64;

    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    private static readonly HashSet<string> Stopwords = new(StringComparer.Ordinal)
    {
        "a", "an", "the", "to", "of", "in", "for", "and", "or", "is", "it",
        "which", "does", "because", "happens", "do", "be", "as", "at", "on",
        "by", "with", "from", "that", "this", "was", "are", "i", "you",
    };

    private static readonly Dictionary<string, string> Canonical = new(StringComparer.Ordinal)
    {
        ["editor"] = "editor",
        ["editing"] = "editor",
        ["edit"] = "editor",
        ["neovim"] = "editor",
        ["vim"] = "editor",
        ["like"] = "prefer",
        ["likes"] = "prefer",
        ["prefer"] = "prefer",
        ["preferred"] = "prefer",
        ["prefers"] = "prefer",
        ["preference"] = "prefer",
        ["daily"] = "daily",
        ["operator"] = "operator",
        ["text"] = "text",
        ["tool"] = "tool",
        ["use"] = "use",
    };

    /// <summary>Tokenizes and expands text into canonical overlapping terms.</summary>
    public static IReadOnlyList<string> Tokenize(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return [];

        var tokens = new List<string>();
        var seen = new HashSet<string>(StringComparer.Ordinal);
        foreach (var raw in SplitTokens(text))
        {
            if (raw.Length == 0 || Stopwords.Contains(raw))
                continue;
            AddToken(tokens, seen, raw);
            if (Canonical.TryGetValue(raw, out var canonical))
                AddToken(tokens, seen, canonical);

            foreach (var piece in raw.Split(['-', '=', '_', '/'], StringSplitOptions.RemoveEmptyEntries))
            {
                if (piece.Length == 0 || Stopwords.Contains(piece))
                    continue;
                AddToken(tokens, seen, piece);
                if (Canonical.TryGetValue(piece, out var pieceCanonical))
                    AddToken(tokens, seen, pieceCanonical);
            }
        }

        return tokens;
    }

    /// <summary>Builds an L2-normalized hashed bag-of-tokens embedding.</summary>
    public static float[] Embed(string text)
    {
        var vector = new float[Dimensions];
        foreach (var token in Tokenize(text))
        {
            var hash = SHA256.HashData(Encoding.UTF8.GetBytes(token));
            for (var i = 0; i < Dimensions; i++)
                vector[i] += (hash[i % hash.Length] / 127.5f) - 1f;
        }

        var norm = 0f;
        for (var i = 0; i < Dimensions; i++)
            norm += vector[i] * vector[i];
        norm = MathF.Sqrt(norm);
        if (norm > 1e-10f)
        {
            for (var i = 0; i < Dimensions; i++)
                vector[i] /= norm;
        }

        return vector;
    }

    /// <summary>Non-negative cosine similarity in [0,1]. Orthogonal or opposite vectors score 0.</summary>
    public static double Cosine(float[] left, float[] right)
    {
        var n = Math.Min(left.Length, right.Length);
        if (n == 0)
            return 0;

        double dot = 0;
        for (var i = 0; i < n; i++)
            dot += left[i] * right[i];
        return Math.Clamp(dot, 0, 1);
    }

    /// <summary>Serializes an embedding for the memory index side table.</summary>
    public static string Serialize(float[] embedding)
        => JsonSerializer.Serialize(embedding, Json);

    /// <summary>Deserializes a stored embedding; empty when the payload is invalid.</summary>
    public static float[] Deserialize(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
            return [];
        try
        {
            return JsonSerializer.Deserialize<float[]>(json, Json) ?? [];
        }
        catch (JsonException)
        {
            return [];
        }
    }

    private static void AddToken(List<string> tokens, HashSet<string> seen, string token)
    {
        if (seen.Add(token))
            tokens.Add(token);
    }

    private static IEnumerable<string> SplitTokens(string text)
    {
        var builder = new StringBuilder();
        foreach (var ch in text.ToLowerInvariant())
        {
            if (char.IsLetterOrDigit(ch) || ch is '-' or '=' or '_' or '/')
            {
                builder.Append(ch);
                continue;
            }

            if (char.IsSurrogate(ch) || char.GetUnicodeCategory(ch) == System.Globalization.UnicodeCategory.OtherSymbol)
            {
                if (builder.Length > 0)
                {
                    yield return builder.ToString();
                    builder.Clear();
                }

                yield return ch.ToString();
                continue;
            }

            if (builder.Length > 0)
            {
                yield return builder.ToString();
                builder.Clear();
            }
        }

        if (builder.Length > 0)
            yield return builder.ToString();
    }
}
