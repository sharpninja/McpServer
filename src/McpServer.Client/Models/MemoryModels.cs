using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace McpServer.Client.Models;

/// <summary>Memory visibility scope.</summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
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
[JsonConverter(typeof(JsonStringEnumConverter))]
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
