using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace McpServer.Client.Models;

/// <summary>Memory visibility scope.</summary>
[JsonConverter(typeof(JsonStringEnumConverter<MemoryScope>))]
public enum MemoryScope
{
    /// <summary>Memory visible to every workspace.</summary>
    Global = 0,

    /// <summary>Memory visible only to the active workspace.</summary>
    Workspace = 1,
}

/// <summary>Request to create a memory.</summary>
public sealed class MemoryAddRequest
{
    /// <summary>Optional explicit memory id.</summary>
    [JsonPropertyName("id")]
    public string? Id { get; set; }

    /// <summary>Memory category.</summary>
    [JsonPropertyName("category")]
    public string Category { get; set; } = string.Empty;

    /// <summary>Memory visibility scope.</summary>
    [JsonPropertyName("scope")]
    public MemoryScope Scope { get; set; } = MemoryScope.Workspace;

    /// <summary>Raw memory text.</summary>
    [JsonPropertyName("text")]
    public string Text { get; set; } = string.Empty;

    /// <summary>Optional actor or subsystem name recorded as updater.</summary>
    [JsonPropertyName("updatedBy")]
    public string? UpdatedBy { get; set; }
}

/// <summary>Request to update a memory.</summary>
public sealed class MemoryUpdateRequest
{
    /// <summary>Optional category replacement.</summary>
    [JsonPropertyName("category")]
    public string? Category { get; set; }

    /// <summary>Optional scope replacement.</summary>
    [JsonPropertyName("scope")]
    public MemoryScope? Scope { get; set; }

    /// <summary>Optional raw text replacement.</summary>
    [JsonPropertyName("text")]
    public string? Text { get; set; }

    /// <summary>Optional actor or subsystem name recorded as updater.</summary>
    [JsonPropertyName("updatedBy")]
    public string? UpdatedBy { get; set; }
}

/// <summary>Flattened memory item.</summary>
public sealed class MemoryItem
{
    /// <summary>Stable globally unique memory id.</summary>
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    /// <summary>Normalized category token.</summary>
    [JsonPropertyName("category")]
    public string Category { get; set; } = string.Empty;

    /// <summary>Memory visibility scope.</summary>
    [JsonPropertyName("scope")]
    public MemoryScope Scope { get; set; }

    /// <summary>Workspace owner for Workspace memories; null for Global memories.</summary>
    [JsonPropertyName("workspacePath")]
    public string? WorkspacePath { get; set; }

    /// <summary>Raw memory text.</summary>
    [JsonPropertyName("text")]
    public string Text { get; set; } = string.Empty;

    /// <summary>Monotonic version incremented on update.</summary>
    [JsonPropertyName("version")]
    public int Version { get; set; }

    /// <summary>UTC timestamp when the memory was created.</summary>
    [JsonPropertyName("createdAtUtc")]
    public DateTimeOffset CreatedAtUtc { get; set; }

    /// <summary>UTC timestamp when the memory was last changed.</summary>
    [JsonPropertyName("updatedAtUtc")]
    public DateTimeOffset UpdatedAtUtc { get; set; }

    /// <summary>Optional actor or subsystem that last changed the memory.</summary>
    [JsonPropertyName("updatedBy")]
    public string? UpdatedBy { get; set; }
}

/// <summary>Result of listing memories.</summary>
public sealed class MemoryQueryResult
{
    /// <summary>Visible memory items.</summary>
    [JsonPropertyName("items")]
    public IReadOnlyList<MemoryItem> Items { get; set; } = [];

    /// <summary>Total number of visible memory items.</summary>
    [JsonPropertyName("totalCount")]
    public int TotalCount { get; set; }
}

/// <summary>Failure mode for memory mutations.</summary>
[JsonConverter(typeof(JsonStringEnumConverter<MemoryMutationFailureKind>))]
public enum MemoryMutationFailureKind
{
    /// <summary>No failure classification applies.</summary>
    None = 0,

    /// <summary>The request is invalid.</summary>
    Validation = 1,

    /// <summary>The request conflicts with existing memory state.</summary>
    Conflict = 2,

    /// <summary>The target memory is missing or not visible.</summary>
    NotFound = 3,
}

/// <summary>Request to remember a multi-layer memory.</summary>
public sealed class MemoryRememberRequest
{
    /// <summary>Optional explicit memory id.</summary>
    [JsonPropertyName("id")]
    public string? Id { get; set; }

    /// <summary>Optional title.</summary>
    [JsonPropertyName("title")]
    public string? Title { get; set; }

    /// <summary>Optional summary.</summary>
    [JsonPropertyName("summary")]
    public string? Summary { get; set; }

    /// <summary>Memory content.</summary>
    [JsonPropertyName("content")]
    public string? Content { get; set; }

    /// <summary>Type token.</summary>
    [JsonPropertyName("type")]
    public string? Type { get; set; }

    /// <summary>Optional tags.</summary>
    [JsonPropertyName("tags")]
    public IReadOnlyList<string>? Tags { get; set; }

    /// <summary>Optional confidence in [0,1].</summary>
    [JsonPropertyName("confidence")]
    public double? Confidence { get; set; }

    /// <summary>Optional provenance kind.</summary>
    [JsonPropertyName("sourceKind")]
    public string? SourceKind { get; set; }

    /// <summary>Optional provenance reference.</summary>
    [JsonPropertyName("sourceRef")]
    public string? SourceRef { get; set; }

    /// <summary>Optional scope.</summary>
    [JsonPropertyName("scope")]
    public MemoryScope? Scope { get; set; }

    /// <summary>Optional updater identity.</summary>
    [JsonPropertyName("updatedBy")]
    public string? UpdatedBy { get; set; }
}

/// <summary>Request to recall memories by meaning or keyword.</summary>
public sealed class MemoryRecallRequest
{
    /// <summary>Recall query text.</summary>
    [JsonPropertyName("query")]
    public string? Query { get; set; }

    /// <summary>Optional minimum score.</summary>
    [JsonPropertyName("minScore")]
    public double? MinScore { get; set; }

    /// <summary>Optional result cap.</summary>
    [JsonPropertyName("topN")]
    public int? TopN { get; set; }

    /// <summary>Optional tag filter.</summary>
    [JsonPropertyName("tags")]
    public IReadOnlyList<string>? Tags { get; set; }

    /// <summary>Optional type filter.</summary>
    [JsonPropertyName("type")]
    public string? Type { get; set; }

    /// <summary>Optional scope filter.</summary>
    [JsonPropertyName("scope")]
    public MemoryScope? Scope { get; set; }
}

/// <summary>Request to explore a memory neighborhood.</summary>
public sealed class MemoryExploreRequest
{
    /// <summary>Optional MEMORY-* seed.</summary>
    [JsonPropertyName("seedId")]
    public string? SeedId { get; set; }

    /// <summary>Optional query seed.</summary>
    [JsonPropertyName("query")]
    public string? Query { get; set; }

    /// <summary>Optional hop depth.</summary>
    [JsonPropertyName("depth")]
    public int? Depth { get; set; }

    /// <summary>Optional neighbor cap.</summary>
    [JsonPropertyName("maxNeighbors")]
    public int? MaxNeighbors { get; set; }

    /// <summary>Optional Hebbian override.</summary>
    [JsonPropertyName("hebbianEnabled")]
    public bool? HebbianEnabled { get; set; }
}

/// <summary>Request to plan or apply consolidate/sleep merge.</summary>
public sealed class MemoryConsolidateRequest
{
    /// <summary>When false, apply writes. Default is dry-run.</summary>
    [JsonPropertyName("dryRun")]
    public bool? DryRun { get; set; }

    /// <summary>Optional similarity threshold.</summary>
    [JsonPropertyName("similarityThreshold")]
    public double? SimilarityThreshold { get; set; }

    /// <summary>When true, merged-away rows may be hard-deleted.</summary>
    [JsonPropertyName("allowHardDelete")]
    public bool? AllowHardDelete { get; set; }

    /// <summary>Optional client run id.</summary>
    [JsonPropertyName("runId")]
    public string? RunId { get; set; }
}

/// <summary>Request to promote a session-log or context source into memory.</summary>
public sealed class MemoryPromoteRequest
{
    /// <summary>Source kind token.</summary>
    [JsonPropertyName("sourceKind")]
    public string? SourceKind { get; set; }

    /// <summary>Source reference.</summary>
    [JsonPropertyName("sourceRef")]
    public string? SourceRef { get; set; }

    /// <summary>Optional content override.</summary>
    [JsonPropertyName("content")]
    public string? Content { get; set; }

    /// <summary>Optional summary override.</summary>
    [JsonPropertyName("summary")]
    public string? Summary { get; set; }
}

/// <summary>Request to revert a memory to snapshot N.</summary>
public sealed class MemoryRevertRequest
{
    /// <summary>Snapshot number to restore.</summary>
    [JsonPropertyName("versionNumber")]
    public int VersionNumber { get; set; }
}

/// <summary>
/// Narrow leftover DTO with only status/id/error. Do not use it for list-bearing memory
/// HTTP bodies; those verbs have typed results that keep <c>items</c>/<c>hits</c>/<c>plan</c>.
/// </summary>
public sealed class MemorySurfaceResult
{
    /// <summary>HTTP status code from the memory surface.</summary>
    [JsonPropertyName("statusCode")]
    public int StatusCode { get; set; }

    /// <summary>Created memory id when present.</summary>
    [JsonPropertyName("memoryId")]
    public string? MemoryId { get; set; }

    /// <summary>Optional error text.</summary>
    [JsonPropertyName("error")]
    public string? Error { get; set; }
}

/// <summary>
/// One ranked recall hit. Matches the live <c>/mcpserver/memory/recall</c> JSON shape
/// used by <see cref="MemoryRecallResult"/>.
/// </summary>
public sealed class MemoryRecallHit
{
    /// <summary>MEMORY-* id of the hit.</summary>
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    /// <summary>Hybrid fusion score in [0,1].</summary>
    [JsonPropertyName("score")]
    public double? Score { get; set; }

    /// <summary>Title when persisted.</summary>
    [JsonPropertyName("title")]
    public string? Title { get; set; }

    /// <summary>Content used to open the memory without a second round-trip.</summary>
    [JsonPropertyName("content")]
    public string? Content { get; set; }

    /// <summary>Legacy text field when Content is unset.</summary>
    [JsonPropertyName("text")]
    public string? Text { get; set; }

    /// <summary>Memory type token.</summary>
    [JsonPropertyName("type")]
    public string? Type { get; set; }

    /// <summary>Tags persisted with the memory.</summary>
    [JsonPropertyName("tags")]
    public IReadOnlyList<string>? Tags { get; set; }

    /// <summary>Visibility scope of the hit.</summary>
    [JsonPropertyName("scope")]
    public MemoryScope? Scope { get; set; }

    /// <summary>How the hit was retrieved (bm25, vector, or hybrid).</summary>
    [JsonPropertyName("matchKind")]
    public string? MatchKind { get; set; }

    /// <summary>Workspace id used for ANN scoping.</summary>
    [JsonPropertyName("annWorkspaceId")]
    public string? AnnWorkspaceId { get; set; }
}

/// <summary>
/// Typed recall result. The live HTTP body uses <c>items</c>; plugin/REPL agents look for
/// <c>hits</c>. Both names are bound and kept in sync so neither surface drops ranked memories.
/// </summary>
public sealed class MemoryRecallResult
{
    /// <summary>HTTP status code from the memory surface.</summary>
    [JsonPropertyName("statusCode")]
    public int StatusCode { get; set; }

    /// <summary>Created memory id when present.</summary>
    [JsonPropertyName("memoryId")]
    public string? MemoryId { get; set; }

    /// <summary>Optional error text.</summary>
    [JsonPropertyName("error")]
    public string? Error { get; set; }

    /// <summary>Ranked hits as returned by the live API (<c>items</c>).</summary>
    [JsonPropertyName("items")]
    public IReadOnlyList<MemoryRecallHit>? Items { get; set; }

    /// <summary>Plugin-facing alias for <see cref="Items"/> (<c>hits</c>).</summary>
    [JsonPropertyName("hits")]
    public IReadOnlyList<MemoryRecallHit>? Hits { get; set; }

    /// <summary>Failure classification.</summary>
    [JsonPropertyName("failureKind")]
    public MemoryMutationFailureKind FailureKind { get; set; }

    /// <summary>True when a reranker ran. Production default is false.</summary>
    [JsonPropertyName("rerankApplied")]
    public bool? RerankApplied { get; set; }

    /// <summary>Ranking mode token (for example hybrid).</summary>
    [JsonPropertyName("rankingMode")]
    public string? RankingMode { get; set; }

    /// <summary>Non-empty ranked hits, preferring <see cref="Hits"/> then <see cref="Items"/>.</summary>
    [JsonIgnore]
    public IReadOnlyList<MemoryRecallHit> RankedHits => MemoryResultListSync.FirstNonEmpty(Hits, Items) ?? [];

    /// <summary>
    /// Copies <c>items</c> onto <c>hits</c> (and the reverse) so plugin YAML/JSON keeps
    /// ranked memories under both live-API and agent-facing names.
    /// </summary>
    public void SynchronizeHitsAndItems()
        => MemoryResultListSync.Synchronize(this, static (row, value) => row.Items = value, static (row, value) => row.Hits = value, static row => row.Items, static row => row.Hits);
}

/// <summary>FR-MCP-MEMORY-010: Typed remember result matching live HTTP <c>MemoryRememberResult</c>.</summary>
public sealed class MemoryRememberResult
{
    /// <summary>HTTP status code from the memory surface.</summary>
    [JsonPropertyName("statusCode")]
    public int StatusCode { get; set; }

    /// <summary>Created memory id when present.</summary>
    [JsonPropertyName("memoryId")]
    public string? MemoryId { get; set; }

    /// <summary>Persisted memory row when the write succeeded.</summary>
    [JsonPropertyName("memory")]
    public MemoryItem? Memory { get; set; }

    /// <summary>Optional error text.</summary>
    [JsonPropertyName("error")]
    public string? Error { get; set; }

    /// <summary>Failure classification.</summary>
    [JsonPropertyName("failureKind")]
    public MemoryMutationFailureKind FailureKind { get; set; }
}

/// <summary>FR-MCP-MEMORY-012: One explore neighbor from live <c>/mcpserver/memory/explore</c>.</summary>
public sealed class MemoryExploreNeighbor
{
    /// <summary>MEMORY-* id of the neighbor.</summary>
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    /// <summary>Edge weight in [0,1].</summary>
    [JsonPropertyName("weight")]
    public double Weight { get; set; }

    /// <summary>Edge type token (explicit, related, derived, or co-retrieved).</summary>
    [JsonPropertyName("edgeType")]
    public string EdgeType { get; set; } = string.Empty;

    /// <summary>Hop distance from the seed (1 for a direct neighbor).</summary>
    [JsonPropertyName("depth")]
    public int Depth { get; set; }
}

/// <summary>
/// FR-MCP-MEMORY-012: Typed explore result. Live HTTP uses <c>items</c> for neighbors.
/// Plugin/REPL agents may look for <c>items</c>, <c>neighbors</c>, or <c>hits</c>.
/// </summary>
public sealed class MemoryExploreResult
{
    /// <summary>HTTP status code from the memory surface.</summary>
    [JsonPropertyName("statusCode")]
    public int StatusCode { get; set; }

    /// <summary>Neighbors as returned by the live API (<c>items</c>).</summary>
    [JsonPropertyName("items")]
    public IReadOnlyList<MemoryExploreNeighbor>? Items { get; set; }

    /// <summary>Domain alias for <see cref="Items"/>.</summary>
    [JsonPropertyName("neighbors")]
    public IReadOnlyList<MemoryExploreNeighbor>? Neighbors { get; set; }

    /// <summary>Plugin-facing alias for <see cref="Items"/> (<c>hits</c>).</summary>
    [JsonPropertyName("hits")]
    public IReadOnlyList<MemoryExploreNeighbor>? Hits { get; set; }

    /// <summary>Failure classification.</summary>
    [JsonPropertyName("failureKind")]
    public MemoryMutationFailureKind FailureKind { get; set; }

    /// <summary>Optional error text.</summary>
    [JsonPropertyName("error")]
    public string? Error { get; set; }

    /// <summary>Seed MEMORY-* id used for the walk.</summary>
    [JsonPropertyName("seedId")]
    public string? SeedId { get; set; }

    /// <summary>True when Hebbian strengthening ran.</summary>
    [JsonPropertyName("hebbianApplied")]
    public bool? HebbianApplied { get; set; }

    /// <summary>Non-empty neighbors, preferring items then neighbors then hits.</summary>
    [JsonIgnore]
    public IReadOnlyList<MemoryExploreNeighbor> RankedHits =>
        MemoryResultListSync.FirstNonEmpty(Items, Neighbors, Hits) ?? [];

    /// <summary>Copies the populated neighbor list onto <c>items</c>, <c>neighbors</c>, and <c>hits</c>.</summary>
    public void SynchronizeHitsAndItems()
    {
        var source = MemoryResultListSync.FirstNonEmpty(Items, Neighbors, Hits);
        if (source is null)
            return;

        if (Items is not { Count: > 0 })
            Items = source;
        if (Neighbors is not { Count: > 0 })
            Neighbors = source;
        if (Hits is not { Count: > 0 })
            Hits = source;
    }
}

/// <summary>FR-MCP-MEMORY-013: One planned consolidate merge cluster.</summary>
public sealed class MemoryConsolidatePlanItem
{
    /// <summary>Candidate MEMORY-* ids in the cluster.</summary>
    [JsonPropertyName("candidateIds")]
    public IReadOnlyList<string> CandidateIds { get; set; } = [];

    /// <summary>Similarity score for the cluster.</summary>
    [JsonPropertyName("similarityScore")]
    public double SimilarityScore { get; set; }

    /// <summary>Chosen survivor id when write-mode applied; null for dry-run-only plans.</summary>
    [JsonPropertyName("survivorId")]
    public string? SurvivorId { get; set; }
}

/// <summary>
/// FR-MCP-MEMORY-013: Typed consolidate result. Live HTTP uses <c>plan</c> for the cluster list.
/// <c>items</c> and <c>hits</c> are aliases so plugin-facing results keep that list.
/// </summary>
public sealed class MemoryConsolidateResult
{
    /// <summary>HTTP status code from the memory surface.</summary>
    [JsonPropertyName("statusCode")]
    public int StatusCode { get; set; }

    /// <summary>Planned merge clusters from the live API (<c>plan</c>).</summary>
    [JsonPropertyName("plan")]
    public IReadOnlyList<MemoryConsolidatePlanItem>? Plan { get; set; }

    /// <summary>Alias for <see cref="Plan"/> so list-bearing clients can read <c>items</c>.</summary>
    [JsonPropertyName("items")]
    public IReadOnlyList<MemoryConsolidatePlanItem>? Items { get; set; }

    /// <summary>Plugin-facing alias for <see cref="Plan"/> (<c>hits</c>).</summary>
    [JsonPropertyName("hits")]
    public IReadOnlyList<MemoryConsolidatePlanItem>? Hits { get; set; }

    /// <summary>Chosen survivor id when writes applied.</summary>
    [JsonPropertyName("survivorId")]
    public string? SurvivorId { get; set; }

    /// <summary>Ids merged away when writes applied.</summary>
    [JsonPropertyName("mergedAwayIds")]
    public IReadOnlyList<string>? MergedAwayIds { get; set; }

    /// <summary>Optional client or job run id.</summary>
    [JsonPropertyName("runId")]
    public string? RunId { get; set; }

    /// <summary>True when the run stayed dry-run.</summary>
    [JsonPropertyName("dryRunApplied")]
    public bool? DryRunApplied { get; set; }

    /// <summary>True when a consolidate event was emitted.</summary>
    [JsonPropertyName("eventEmitted")]
    public bool? EventEmitted { get; set; }

    /// <summary>Failure classification.</summary>
    [JsonPropertyName("failureKind")]
    public MemoryMutationFailureKind FailureKind { get; set; }

    /// <summary>Optional error text.</summary>
    [JsonPropertyName("error")]
    public string? Error { get; set; }

    /// <summary>Copies the populated plan list onto <c>plan</c>, <c>items</c>, and <c>hits</c>.</summary>
    public void SynchronizeHitsAndItems()
    {
        var source = MemoryResultListSync.FirstNonEmpty(Plan, Items, Hits);
        if (source is null)
            return;

        if (Plan is not { Count: > 0 })
            Plan = source;
        if (Items is not { Count: > 0 })
            Items = source;
        if (Hits is not { Count: > 0 })
            Hits = source;
    }
}

/// <summary>FR-MCP-MEMORY-015: Typed promote result matching live HTTP <c>MemoryPromoteResult</c>.</summary>
public sealed class MemoryPromoteResult
{
    /// <summary>HTTP status code from the memory surface.</summary>
    [JsonPropertyName("statusCode")]
    public int StatusCode { get; set; }

    /// <summary>Created or updated memory row.</summary>
    [JsonPropertyName("memory")]
    public MemoryItem? Memory { get; set; }

    /// <summary>Source kind token (sessionlog or context).</summary>
    [JsonPropertyName("sourceKind")]
    public string? SourceKind { get; set; }

    /// <summary>Source reference.</summary>
    [JsonPropertyName("sourceRef")]
    public string? SourceRef { get; set; }

    /// <summary>Failure classification.</summary>
    [JsonPropertyName("failureKind")]
    public MemoryMutationFailureKind FailureKind { get; set; }

    /// <summary>Optional error text.</summary>
    [JsonPropertyName("error")]
    public string? Error { get; set; }
}

/// <summary>FR-MCP-MEMORY-014: One version snapshot from live <c>GET .../versions</c>.</summary>
public sealed class MemoryVersionSnapshot
{
    /// <summary>Contiguous positive version number.</summary>
    [JsonPropertyName("versionNumber")]
    public int VersionNumber { get; set; }

    /// <summary>Title at this snapshot.</summary>
    [JsonPropertyName("title")]
    public string? Title { get; set; }

    /// <summary>Content at this snapshot.</summary>
    [JsonPropertyName("content")]
    public string Content { get; set; } = string.Empty;

    /// <summary>When the snapshot was written.</summary>
    [JsonPropertyName("createdAtUtc")]
    public DateTimeOffset CreatedAtUtc { get; set; }
}

/// <summary>
/// FR-MCP-MEMORY-014: Typed versions result. Live HTTP uses <c>items</c> for snapshots.
/// </summary>
public sealed class MemoryVersionListResult
{
    /// <summary>HTTP status code from the memory surface.</summary>
    [JsonPropertyName("statusCode")]
    public int StatusCode { get; set; }

    /// <summary>Version snapshots as returned by the live API (<c>items</c>).</summary>
    [JsonPropertyName("items")]
    public IReadOnlyList<MemoryVersionSnapshot>? Items { get; set; }

    /// <summary>Plugin-facing alias for <see cref="Items"/> (<c>hits</c>).</summary>
    [JsonPropertyName("hits")]
    public IReadOnlyList<MemoryVersionSnapshot>? Hits { get; set; }

    /// <summary>Optional error text.</summary>
    [JsonPropertyName("error")]
    public string? Error { get; set; }

    /// <summary>Copies <c>items</c> onto <c>hits</c> and the reverse.</summary>
    public void SynchronizeHitsAndItems()
    {
        var source = MemoryResultListSync.FirstNonEmpty(Items, Hits);
        if (source is null)
            return;

        if (Items is not { Count: > 0 })
            Items = source;
        if (Hits is not { Count: > 0 })
            Hits = source;
    }
}

/// <summary>FR-MCP-MEMORY-014: Typed revert result matching live HTTP <c>MemoryRevertResult</c>.</summary>
public sealed class MemoryRevertResult
{
    /// <summary>HTTP status code from the memory surface.</summary>
    [JsonPropertyName("statusCode")]
    public int StatusCode { get; set; }

    /// <summary>Restored memory row.</summary>
    [JsonPropertyName("memory")]
    public MemoryItem? Memory { get; set; }

    /// <summary>Failure classification.</summary>
    [JsonPropertyName("failureKind")]
    public MemoryMutationFailureKind FailureKind { get; set; }

    /// <summary>Optional error text.</summary>
    [JsonPropertyName("error")]
    public string? Error { get; set; }
}

/// <summary>Copies the first non-empty list onto empty aliases for plugin-facing memory results.</summary>
public static class MemoryResultListSync
{
    /// <summary>Returns the first list that contains at least one row.</summary>
    public static IReadOnlyList<T>? FirstNonEmpty<T>(params IReadOnlyList<T>?[] lists)
    {
        foreach (var list in lists)
        {
            if (list is { Count: > 0 })
                return list;
        }

        return null;
    }

    /// <summary>Copies a populated list onto empty left/right aliases.</summary>
    public static void Synchronize<TRow, TItem>(
        TRow row,
        Action<TRow, IReadOnlyList<TItem>?> setLeft,
        Action<TRow, IReadOnlyList<TItem>?> setRight,
        Func<TRow, IReadOnlyList<TItem>?> getLeft,
        Func<TRow, IReadOnlyList<TItem>?> getRight)
    {
        var source = FirstNonEmpty(getLeft(row), getRight(row));
        if (source is null)
            return;

        if (getLeft(row) is not { Count: > 0 })
            setLeft(row, source);
        if (getRight(row) is not { Count: > 0 })
            setRight(row, source);
    }
}

/// <summary>Result of creating, updating, or removing a memory.</summary>
public sealed class MemoryMutationResult
{
    /// <summary>True when the mutation succeeded.</summary>
    [JsonPropertyName("success")]
    public bool Success { get; set; }

    /// <summary>Optional error message.</summary>
    [JsonPropertyName("error")]
    public string? Error { get; set; }

    /// <summary>Memory item affected by the mutation.</summary>
    [JsonPropertyName("memory")]
    public MemoryItem? Memory { get; set; }

    /// <summary>Failure classification.</summary>
    [JsonPropertyName("failureKind")]
    public MemoryMutationFailureKind FailureKind { get; set; }
}
