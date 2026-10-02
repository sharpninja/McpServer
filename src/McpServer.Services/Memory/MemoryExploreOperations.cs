using McpServer.Support.Mcp.Storage;
using McpServer.Support.Mcp.Storage.Entities;
using Microsoft.EntityFrameworkCore;

namespace McpServer.Support.Mcp.Services;

/// <summary>
/// FR-MCP-MEMORY-012 / TEST-MCP-MEMORY-012: CQRS-owned explore, explicit-edge, and Hebbian
/// operations. Not a public <see cref="IMemoryService"/> facade.
/// </summary>
public sealed class MemoryExploreOperations
{
    private const string CoRetrievedEdgeType = "co-retrieved";
    private const double HebbianIncrement = 0.25;
    private readonly McpDbContext _db;

    /// <summary>Creates operations bound to an already-scoped <see cref="McpDbContext"/>.</summary>
    public MemoryExploreOperations(McpDbContext db)
    {
        _db = db;
    }

    /// <summary>
    /// AC-FR-MCP-MEMORY-012: Walks directed edges from a seed or top recall hit.
    /// Hebbian is off by default; explore never mutates Content.
    /// </summary>
    public async Task<MemoryExploreResult> ExploreAsync(ExploreMemoryQuery query, CancellationToken cancellationToken)
    {
        var depth = query.Depth ?? MemoryExploreLimits.DefaultDepth;
        if (depth <= 0 || depth > MemoryExploreLimits.MaxDepth)
        {
            return new MemoryExploreResult(
                400,
                FailureKind: MemoryMutationFailureKind.Validation,
                Error: depth <= 0
                    ? "depth must be a positive integer."
                    : "depth exceeds the configured maximum.");
        }

        var maxNeighbors = query.MaxNeighbors ?? MemoryExploreLimits.DefaultMaxNeighbors;
        if (maxNeighbors <= 0)
        {
            return new MemoryExploreResult(
                400,
                FailureKind: MemoryMutationFailureKind.Validation,
                Error: "maxNeighbors must be a positive integer.");
        }

        if (maxNeighbors > MemoryExploreLimits.MaxNeighbors)
            maxNeighbors = MemoryExploreLimits.MaxNeighbors;

        var hebbianEnabled = query.HebbianEnabled ?? MemoryExploreLimits.DefaultHebbianEnabled;
        var seedId = NormalizeOptional(query.SeedId);
        if (seedId is null)
        {
            var resolved = await ResolveQuerySeedAsync(query.Query, cancellationToken).ConfigureAwait(false);
            if (resolved.Result is not null)
                return resolved.Result;

            seedId = resolved.SeedId;
            if (seedId is null)
            {
                return new MemoryExploreResult(
                    200,
                    [],
                    HebbianApplied: hebbianEnabled);
            }
        }

        var seed = await _db.Memories
            .AsNoTracking()
            .FirstOrDefaultAsync(memory => memory.Id == seedId, cancellationToken)
            .ConfigureAwait(false);
        if (seed is null)
        {
            return new MemoryExploreResult(
                404,
                FailureKind: MemoryMutationFailureKind.NotFound,
                Error: $"Memory '{seedId}' not found.",
                SeedId: seedId,
                HebbianApplied: hebbianEnabled);
        }

        var visibleIds = await LoadVisibleIdsAsync(cancellationToken).ConfigureAwait(false);
        var edges = await _db.MemoryEdges.AsNoTracking().ToListAsync(cancellationToken).ConfigureAwait(false);
        var neighbors = WalkNeighborhood(seed.Id, depth, hebbianEnabled, visibleIds, edges);
        var truncated = neighbors
            .OrderByDescending(item => item.Weight)
            .ThenBy(item => item.Id, StringComparer.Ordinal)
            .Take(maxNeighbors)
            .ToList();

        return new MemoryExploreResult(
            200,
            truncated,
            SeedId: seed.Id,
            HebbianApplied: hebbianEnabled);
    }

    /// <summary>AC-FR-MCP-MEMORY-012: Creates a directed explicit edge with fail-closed validation.</summary>
    public async Task<MemoryCreateEdgeResult> CreateEdgeAsync(
        MemoryCreateEdgeRequest? request,
        bool readOnlyCaller,
        CancellationToken cancellationToken)
    {
        if (readOnlyCaller)
        {
            return new MemoryCreateEdgeResult(
                403,
                FailureKind: MemoryMutationFailureKind.Validation,
                Error: "Read-only caller cannot create memory edges.");
        }

        if (request is null)
        {
            return new MemoryCreateEdgeResult(
                400,
                FailureKind: MemoryMutationFailureKind.Validation,
                Error: "Request body is required.");
        }

        var fromId = NormalizeOptional(request.FromMemoryId);
        var toId = NormalizeOptional(request.ToMemoryId);
        if (fromId is null || toId is null)
        {
            return new MemoryCreateEdgeResult(
                400,
                FailureKind: MemoryMutationFailureKind.Validation,
                Error: "FromMemoryId and ToMemoryId are required.");
        }

        if (string.Equals(fromId, toId, StringComparison.Ordinal))
        {
            return new MemoryCreateEdgeResult(
                MemoryExploreLimits.SelfLoopStatusCode,
                FailureKind: MemoryMutationFailureKind.Validation,
                Error: "Self-loop edges are not allowed.");
        }

        var edgeType = NormalizeEdgeType(request.EdgeType);
        if (edgeType is null || !MemoryExploreLimits.AllowedEdgeTypes.Contains(edgeType))
        {
            return new MemoryCreateEdgeResult(
                400,
                FailureKind: MemoryMutationFailureKind.Validation,
                Error: "EdgeType is not an allowed memory edge type.");
        }

        var weight = request.Weight ?? MemoryExploreLimits.MaxWeight;
        if (weight < MemoryExploreLimits.MinWeight || weight > MemoryExploreLimits.MaxWeight)
        {
            return new MemoryCreateEdgeResult(
                400,
                FailureKind: MemoryMutationFailureKind.Validation,
                Error: "Weight must be in [0,1].");
        }

        var visibleIds = await LoadVisibleIdsAsync(cancellationToken).ConfigureAwait(false);
        if (!visibleIds.Contains(fromId) || !visibleIds.Contains(toId))
        {
            return new MemoryCreateEdgeResult(
                404,
                FailureKind: MemoryMutationFailureKind.NotFound,
                Error: "Edge endpoints must be visible in the caller Effective set.");
        }

        var exists = await _db.MemoryEdges.AnyAsync(
            edge => edge.FromMemoryId == fromId && edge.ToMemoryId == toId && edge.EdgeType == edgeType,
            cancellationToken).ConfigureAwait(false);
        if (exists)
        {
            return new MemoryCreateEdgeResult(
                MemoryExploreLimits.DuplicateExplicitStatusCode,
                FailureKind: MemoryMutationFailureKind.Conflict,
                Error: "An edge with the same From, To, and EdgeType already exists.");
        }

        var entity = new MemoryEdgeEntity
        {
            FromMemoryId = fromId,
            ToMemoryId = toId,
            EdgeType = edgeType,
            Weight = weight,
        };
        _db.MemoryEdges.Add(entity);
        await _db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return new MemoryCreateEdgeResult(
            201,
            new MemoryEdgeRecord
            {
                FromMemoryId = entity.FromMemoryId,
                ToMemoryId = entity.ToMemoryId,
                EdgeType = entity.EdgeType,
                Weight = entity.Weight,
            });
    }

    /// <summary>
    /// AC-FR-MCP-MEMORY-012-03 / AC-FR-MCP-MEMORY-012-16: Strengthens co-retrieved edges
    /// without touching recall ranking or memory Content. No-op when Hebbian is off.
    /// </summary>
    public async Task<MemoryHebbianResult> RecordHebbianAsync(
        IReadOnlyList<string>? memoryIds,
        bool hebbianEnabled,
        CancellationToken cancellationToken)
    {
        if (!hebbianEnabled)
        {
            return new MemoryHebbianResult(200, [], RankingUnchanged: true);
        }

        var visibleIds = await LoadVisibleIdsAsync(cancellationToken).ConfigureAwait(false);
        var ids = (memoryIds ?? [])
            .Select(NormalizeOptional)
            .Where(id => id is not null && visibleIds.Contains(id))
            .Cast<string>()
            .Distinct(StringComparer.Ordinal)
            .ToList();
        if (ids.Count < 2)
            return new MemoryHebbianResult(200, [], RankingUnchanged: true);

        var created = new List<MemoryEdgeRecord>();
        for (var i = 0; i < ids.Count; i++)
        {
            for (var j = 0; j < ids.Count; j++)
            {
                if (i == j)
                    continue;

                var record = await StrengthenCoRetrievedAsync(ids[i], ids[j], cancellationToken)
                    .ConfigureAwait(false);
                created.Add(record);
            }
        }

        await _db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return new MemoryHebbianResult(201, created, RankingUnchanged: true);
    }

    private async Task<(string? SeedId, MemoryExploreResult? Result)> ResolveQuerySeedAsync(
        string? queryText,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(queryText))
            return (null, null);

        var recall = await new MemorySearchOperations(_db)
            .RecallAsync(
                new RecallMemoryQuery(
                    _db.CurrentWorkspaceId ?? string.Empty,
                    queryText,
                    MinScore: MemoryExploreLimits.QuerySeedMinScore,
                    TopN: 1),
                cancellationToken)
            .ConfigureAwait(false);
        if (recall.StatusCode != 200)
        {
            return (null, new MemoryExploreResult(
                recall.StatusCode,
                FailureKind: recall.FailureKind,
                Error: recall.Error));
        }

        var top = recall.Items?.FirstOrDefault();
        return (top?.Id, null);
    }

    private async Task<HashSet<string>> LoadVisibleIdsAsync(CancellationToken cancellationToken)
    {
        var ids = await _db.Memories
            .AsNoTracking()
            .Select(memory => memory.Id)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
        return ids.ToHashSet(StringComparer.Ordinal);
    }

    private static List<MemoryExploreNeighbor> WalkNeighborhood(
        string seedId,
        int depth,
        bool hebbianEnabled,
        IReadOnlySet<string> visibleIds,
        IReadOnlyList<MemoryEdgeEntity> edges)
    {
        var outgoing = edges
            .GroupBy(edge => edge.FromMemoryId, StringComparer.Ordinal)
            .ToDictionary(group => group.Key, group => group.ToList(), StringComparer.Ordinal);
        var visited = new HashSet<string>(StringComparer.Ordinal) { seedId };
        var frontier = new List<string> { seedId };
        var results = new List<MemoryExploreNeighbor>();

        for (var hop = 1; hop <= depth; hop++)
        {
            var next = new List<string>();
            foreach (var node in frontier)
            {
                if (!outgoing.TryGetValue(node, out var candidates))
                    continue;

                foreach (var edge in candidates)
                {
                    if (string.Equals(edge.FromMemoryId, edge.ToMemoryId, StringComparison.Ordinal))
                        continue;
                    if (!hebbianEnabled
                        && edge.EdgeType.Equals(CoRetrievedEdgeType, StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }

                    if (!visibleIds.Contains(edge.ToMemoryId) || !visited.Add(edge.ToMemoryId))
                        continue;

                    results.Add(new MemoryExploreNeighbor
                    {
                        Id = edge.ToMemoryId,
                        Weight = edge.Weight,
                        EdgeType = edge.EdgeType,
                        Depth = hop,
                    });
                    next.Add(edge.ToMemoryId);
                }
            }

            frontier = next;
        }

        return results;
    }

    private async Task<MemoryEdgeRecord> StrengthenCoRetrievedAsync(
        string fromId,
        string toId,
        CancellationToken cancellationToken)
    {
        var existing = await _db.MemoryEdges.FirstOrDefaultAsync(
            edge => edge.FromMemoryId == fromId
                && edge.ToMemoryId == toId
                && edge.EdgeType == CoRetrievedEdgeType,
            cancellationToken).ConfigureAwait(false);
        if (existing is null)
        {
            existing = new MemoryEdgeEntity
            {
                FromMemoryId = fromId,
                ToMemoryId = toId,
                EdgeType = CoRetrievedEdgeType,
                Weight = HebbianIncrement,
            };
            _db.MemoryEdges.Add(existing);
        }
        else
        {
            existing.Weight = Math.Clamp(existing.Weight + HebbianIncrement, MemoryExploreLimits.MinWeight, MemoryExploreLimits.MaxWeight);
        }

        return new MemoryEdgeRecord
        {
            FromMemoryId = existing.FromMemoryId,
            ToMemoryId = existing.ToMemoryId,
            EdgeType = existing.EdgeType,
            Weight = existing.Weight,
        };
    }

    private static string? NormalizeOptional(string? value)
    {
        var trimmed = value?.Trim();
        return string.IsNullOrEmpty(trimmed) ? null : trimmed;
    }

    private static string? NormalizeEdgeType(string? edgeType)
    {
        var trimmed = NormalizeOptional(edgeType);
        if (trimmed is null)
            return null;

        foreach (var allowed in MemoryExploreLimits.AllowedEdgeTypes)
        {
            if (allowed.Equals(trimmed, StringComparison.OrdinalIgnoreCase))
                return allowed;
        }

        return trimmed;
    }
}
