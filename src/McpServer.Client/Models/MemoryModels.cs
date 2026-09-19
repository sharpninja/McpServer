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

/// <summary>Shared HTTP result for additive memory verbs.</summary>
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
