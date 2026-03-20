using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace McpServer.Client.Models;

/// <summary>A flattened TODO item.</summary>
public sealed class TodoFlatItem
{
    /// <summary>Unique item identifier (e.g. MVP-APP-001).</summary>
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    /// <summary>Item title.</summary>
    [JsonPropertyName("title")]
    public string Title { get; set; } = string.Empty;

    /// <summary>Section key (e.g. mvp-app).</summary>
    [JsonPropertyName("section")]
    public string Section { get; set; } = string.Empty;

    /// <summary>Priority: high, medium, or low.</summary>
    [JsonPropertyName("priority")]
    public string Priority { get; set; } = string.Empty;

    /// <summary>Whether the item is complete.</summary>
    [JsonPropertyName("done")]
    public bool Done { get; set; }

    /// <summary>Time estimate.</summary>
    [JsonPropertyName("estimate")]
    public string? Estimate { get; set; }

    /// <summary>Free-text note.</summary>
    [JsonPropertyName("note")]
    public string? Note { get; set; }

    /// <summary>Description lines.</summary>
    [JsonPropertyName("description")]
    public IReadOnlyList<string>? Description { get; set; }

    /// <summary>Technical detail lines.</summary>
    [JsonPropertyName("technicalDetails")]
    public IReadOnlyList<string>? TechnicalDetails { get; set; }

    /// <summary>Implementation task checklist.</summary>
    [JsonPropertyName("implementationTasks")]
    public IReadOnlyList<TodoFlatTask>? ImplementationTasks { get; set; }

    /// <summary>Date completed (ISO 8601).</summary>
    [JsonPropertyName("completedDate")]
    public string? CompletedDate { get; set; }

    /// <summary>Summary written on completion.</summary>
    [JsonPropertyName("doneSummary")]
    public string? DoneSummary { get; set; }

    /// <summary>Remaining work description.</summary>
    [JsonPropertyName("remaining")]
    public string? Remaining { get; set; }

    /// <summary>Priority note / justification.</summary>
    [JsonPropertyName("priorityNote")]
    public string? PriorityNote { get; set; }

    /// <summary>External reference link.</summary>
    [JsonPropertyName("reference")]
    public string? Reference { get; set; }

    /// <summary>Code-review phase label for remediation items.</summary>
    [JsonPropertyName("phase")]
    public string? Phase { get; set; }

    /// <summary>IDs of items this depends on.</summary>
    [JsonPropertyName("dependsOn")]
    public IReadOnlyList<string>? DependsOn { get; set; }

    /// <summary>Associated functional requirement IDs.</summary>
    [JsonPropertyName("functionalRequirements")]
    public IReadOnlyList<string>? FunctionalRequirements { get; set; }

    /// <summary>Associated technical requirement IDs.</summary>
    [JsonPropertyName("technicalRequirements")]
    public IReadOnlyList<string>? TechnicalRequirements { get; set; }
}

/// <summary>A sub-task within a TODO item.</summary>
public sealed class TodoFlatTask
{
    /// <summary>Task description.</summary>
    [JsonPropertyName("task")]
    public string Task { get; set; } = string.Empty;

    /// <summary>Whether the sub-task is done.</summary>
    [JsonPropertyName("done")]
    public bool Done { get; set; }
}

/// <summary>Request to create a TODO item.</summary>
public sealed class TodoCreateRequest
{
    /// <summary>Unique item identifier.</summary>
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    /// <summary>Item title.</summary>
    [JsonPropertyName("title")]
    public string Title { get; set; } = string.Empty;

    /// <summary>Section key.</summary>
    [JsonPropertyName("section")]
    public string Section { get; set; } = string.Empty;

    /// <summary>Priority: high, medium, or low.</summary>
    [JsonPropertyName("priority")]
    public string Priority { get; set; } = string.Empty;

    /// <summary>Time estimate.</summary>
    [JsonPropertyName("estimate")]
    public string? Estimate { get; set; }

    /// <summary>Description lines.</summary>
    [JsonPropertyName("description")]
    public IReadOnlyList<string>? Description { get; set; }

    /// <summary>Technical detail lines.</summary>
    [JsonPropertyName("technicalDetails")]
    public IReadOnlyList<string>? TechnicalDetails { get; set; }

    /// <summary>Implementation task checklist.</summary>
    [JsonPropertyName("implementationTasks")]
    public IReadOnlyList<TodoFlatTask>? ImplementationTasks { get; set; }

    /// <summary>Free-text note.</summary>
    [JsonPropertyName("note")]
    public string? Note { get; set; }

    /// <summary>Remaining work description.</summary>
    [JsonPropertyName("remaining")]
    public string? Remaining { get; set; }

    /// <summary>Code-review phase label.</summary>
    [JsonPropertyName("phase")]
    public string? Phase { get; set; }

    /// <summary>IDs of items this depends on.</summary>
    [JsonPropertyName("dependsOn")]
    public IReadOnlyList<string>? DependsOn { get; set; }

    /// <summary>Associated functional requirement IDs.</summary>
    [JsonPropertyName("functionalRequirements")]
    public IReadOnlyList<string>? FunctionalRequirements { get; set; }

    /// <summary>Associated technical requirement IDs.</summary>
    [JsonPropertyName("technicalRequirements")]
    public IReadOnlyList<string>? TechnicalRequirements { get; set; }
}

/// <summary>Request to update a TODO item.</summary>
public sealed class TodoUpdateRequest
{
    /// <summary>Updated title.</summary>
    [JsonPropertyName("title")]
    public string? Title { get; set; }

    /// <summary>Updated priority.</summary>
    [JsonPropertyName("priority")]
    public string? Priority { get; set; }

    /// <summary>Updated section.</summary>
    [JsonPropertyName("section")]
    public string? Section { get; set; }

    /// <summary>Mark done or not-done.</summary>
    [JsonPropertyName("done")]
    public bool? Done { get; set; }

    /// <summary>Updated estimate.</summary>
    [JsonPropertyName("estimate")]
    public string? Estimate { get; set; }

    /// <summary>Updated description lines.</summary>
    [JsonPropertyName("description")]
    public IReadOnlyList<string>? Description { get; set; }

    /// <summary>Updated technical details.</summary>
    [JsonPropertyName("technicalDetails")]
    public IReadOnlyList<string>? TechnicalDetails { get; set; }

    /// <summary>Updated implementation tasks.</summary>
    [JsonPropertyName("implementationTasks")]
    public IReadOnlyList<TodoFlatTask>? ImplementationTasks { get; set; }

    /// <summary>Updated note.</summary>
    [JsonPropertyName("note")]
    public string? Note { get; set; }

    /// <summary>Completion date.</summary>
    [JsonPropertyName("completedDate")]
    public string? CompletedDate { get; set; }

    /// <summary>Completion summary.</summary>
    [JsonPropertyName("doneSummary")]
    public string? DoneSummary { get; set; }

    /// <summary>Remaining work.</summary>
    [JsonPropertyName("remaining")]
    public string? Remaining { get; set; }

    /// <summary>Updated reference text.</summary>
    [JsonPropertyName("reference")]
    public string? Reference { get; set; }

    /// <summary>Updated code-review phase label.</summary>
    [JsonPropertyName("phase")]
    public string? Phase { get; set; }

    /// <summary>Dependency IDs.</summary>
    [JsonPropertyName("dependsOn")]
    public IReadOnlyList<string>? DependsOn { get; set; }

    /// <summary>Associated functional requirement IDs.</summary>
    [JsonPropertyName("functionalRequirements")]
    public IReadOnlyList<string>? FunctionalRequirements { get; set; }

    /// <summary>Associated technical requirement IDs.</summary>
    [JsonPropertyName("technicalRequirements")]
    public IReadOnlyList<string>? TechnicalRequirements { get; set; }
}

/// <summary>Result of a TODO query.</summary>
public sealed class TodoQueryResult
{
    /// <summary>Matching items.</summary>
    [JsonPropertyName("items")]
    public IReadOnlyList<TodoFlatItem> Items { get; set; } = [];

    /// <summary>Total count of matching items.</summary>
    [JsonPropertyName("totalCount")]
    public int TotalCount { get; set; }
}

/// <summary>Result of a TODO mutation (create/update/delete).</summary>
public sealed class TodoMutationResult
{
    /// <summary>Whether the operation succeeded.</summary>
    [JsonPropertyName("success")]
    public bool Success { get; set; }

    /// <summary>Error message on failure.</summary>
    [JsonPropertyName("error")]
    public string? Error { get; set; }

    /// <summary>The affected item (on success).</summary>
    [JsonPropertyName("item")]
    public TodoFlatItem? Item { get; set; }

    /// <summary>Structured failure classification for partial or failed mutations.</summary>
    [JsonPropertyName("failureKind")]
    public TodoMutationFailureKind FailureKind { get; set; }
}

/// <summary>Classifies the failure mode of a TODO mutation.</summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum TodoMutationFailureKind
{
    /// <summary>No failure classification applies.</summary>
    None = 0,

    /// <summary>The request content was invalid.</summary>
    Validation = 1,

    /// <summary>The request conflicted with existing state.</summary>
    Conflict = 2,

    /// <summary>The target TODO item was not found.</summary>
    NotFound = 3,

    /// <summary>The authoritative database mutation succeeded but TODO.yaml projection failed.</summary>
    ProjectionFailed = 4,

    /// <summary>An external dependency failed after the local state changed.</summary>
    ExternalSyncFailed = 5,
}

/// <summary>Result of querying TODO audit history.</summary>
public sealed class TodoAuditQueryResult
{
    /// <summary>Audit entries ordered by TODO version.</summary>
    [JsonPropertyName("entries")]
    public IReadOnlyList<TodoAuditEntry> Entries { get; set; } = [];

    /// <summary>Total matching audit entry count before pagination.</summary>
    [JsonPropertyName("totalCount")]
    public int TotalCount { get; set; }
}

/// <summary>Append-only audit entry for a TODO item.</summary>
public sealed class TodoAuditEntry
{
    /// <summary>Monotonic audit row identifier.</summary>
    [JsonPropertyName("auditId")]
    public long AuditId { get; set; }

    /// <summary>TODO item identifier.</summary>
    [JsonPropertyName("todoId")]
    public string TodoId { get; set; } = string.Empty;

    /// <summary>Monotonic version for this TODO id.</summary>
    [JsonPropertyName("version")]
    public int Version { get; set; }

    /// <summary>Recorded action.</summary>
    [JsonPropertyName("action")]
    public string Action { get; set; } = string.Empty;

    /// <summary>UTC timestamp when the history row was recorded.</summary>
    [JsonPropertyName("recordedAtUtc")]
    public string RecordedAtUtc { get; set; } = string.Empty;

    /// <summary>Post-mutation snapshot.</summary>
    [JsonPropertyName("snapshot")]
    public TodoFlatItem? Snapshot { get; set; }

    /// <summary>Pre-mutation snapshot.</summary>
    [JsonPropertyName("previousSnapshot")]
    public TodoFlatItem? PreviousSnapshot { get; set; }

    /// <summary>Origin of the mutation or backfill operation.</summary>
    [JsonPropertyName("source")]
    public string? Source { get; set; }
}

/// <summary>Result of a requirements analysis.</summary>
public sealed class RequirementsAnalysisResult
{
    /// <summary>Whether the analysis succeeded.</summary>
    [JsonPropertyName("success")]
    public bool Success { get; set; }

    /// <summary>Discovered functional requirement IDs.</summary>
    [JsonPropertyName("functionalRequirements")]
    public IReadOnlyList<string>? FunctionalRequirements { get; set; }

    /// <summary>Discovered technical requirement IDs.</summary>
    [JsonPropertyName("technicalRequirements")]
    public IReadOnlyList<string>? TechnicalRequirements { get; set; }

    /// <summary>Error message on failure.</summary>
    [JsonPropertyName("error")]
    public string? Error { get; set; }

    /// <summary>Raw Copilot response.</summary>
    [JsonPropertyName("copilotResponse")]
    public string? CopilotResponse { get; set; }
}
