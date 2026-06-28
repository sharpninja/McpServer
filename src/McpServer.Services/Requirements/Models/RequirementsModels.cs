using McpServer.Support.Mcp.Models;

namespace McpServer.Support.Mcp.Requirements.Models;

/// <summary>FR-MCP-026: Functional Requirement entry parsed from Functional-Requirements.md.</summary>
/// <param name="Id">The FR identifier (e.g. FR-MCP-001).</param>
/// <param name="Title">The requirement title text.</param>
/// <param name="Body">The full body text (may include **Covered by:** lines).</param>
/// <param name="WorkspaceId">The workspace discriminator that owns the row.</param>
/// <param name="Priority">The requirement priority.</param>
/// <param name="Status">The requirement status.</param>
/// <param name="Notes">Optional operator notes.</param>
/// <param name="AcceptanceCriteria">FR-MCP-REQAC-001: structured acceptance criteria (same shape as TODO criteria).</param>
/// <param name="ScopeStartLayerKey">FR-MCP-REQSCOPE-002: first requirement layer where this FR applies.</param>
/// <param name="ScopeEndLayerKey">FR-MCP-REQSCOPE-002: optional last requirement layer where this FR applies.</param>
public sealed record FrEntry(
    string Id,
    string Title,
    string Body,
    string WorkspaceId = "",
    string Priority = "medium",
    string Status = "pending",
    string? Notes = null,
    IReadOnlyList<AcceptanceCriterion>? AcceptanceCriteria = null,
    string ScopeStartLayerKey = RequirementScopeLayerDefaults.DefaultLayerKey,
    string? ScopeEndLayerKey = null);

/// <summary>FR-MCP-026: Technical Requirement entry parsed from Technical-Requirements.md.</summary>
/// <param name="Id">The TR identifier (e.g. TR-MCP-ARCH-001).</param>
/// <param name="Title">Optional bold title before em-dash separator (may be empty).</param>
/// <param name="Body">The full body text of the requirement.</param>
/// <param name="WorkspaceId">The workspace discriminator that owns the row.</param>
/// <param name="Priority">The requirement priority.</param>
/// <param name="Status">The requirement status.</param>
/// <param name="Notes">Optional operator notes.</param>
/// <param name="AcceptanceCriteria">FR-MCP-REQAC-001: structured acceptance criteria (same shape as TODO criteria).</param>
/// <param name="ScopeStartLayerKey">FR-MCP-REQSCOPE-002: first requirement layer where this TR applies.</param>
/// <param name="ScopeEndLayerKey">FR-MCP-REQSCOPE-002: optional last requirement layer where this TR applies.</param>
public sealed record TrEntry(
    string Id,
    string Title,
    string Body,
    string WorkspaceId = "",
    string Priority = "medium",
    string Status = "pending",
    string? Notes = null,
    IReadOnlyList<AcceptanceCriterion>? AcceptanceCriteria = null,
    string ScopeStartLayerKey = RequirementScopeLayerDefaults.DefaultLayerKey,
    string? ScopeEndLayerKey = null);

/// <summary>FR-MCP-026: Testing Requirement entry parsed from Testing-Requirements.md.</summary>
/// <param name="Id">The TEST identifier (e.g. TEST-MCP-001).</param>
/// <param name="Condition">The test condition text.</param>
/// <param name="WorkspaceId">The workspace discriminator that owns the row.</param>
/// <param name="Title">Optional test title.</param>
/// <param name="Priority">The requirement priority.</param>
/// <param name="Status">The requirement status.</param>
/// <param name="Notes">Optional operator notes.</param>
/// <param name="AcceptanceCriteria">FR-MCP-REQAC-001: structured acceptance criteria (same shape as TODO criteria).</param>
/// <param name="ScopeStartLayerKey">FR-MCP-REQSCOPE-002: first requirement layer where this TEST applies.</param>
/// <param name="ScopeEndLayerKey">FR-MCP-REQSCOPE-002: optional last requirement layer where this TEST applies.</param>
public sealed record TestEntry(
    string Id,
    string Condition,
    string WorkspaceId = "",
    string Title = "",
    string Priority = "medium",
    string Status = "pending",
    string? Notes = null,
    IReadOnlyList<AcceptanceCriterion>? AcceptanceCriteria = null,
    string ScopeStartLayerKey = RequirementScopeLayerDefaults.DefaultLayerKey,
    string? ScopeEndLayerKey = null);

/// <summary>
/// FR-MCP-026: Grouped FR/TR/TEST requirement entries used by atomic batch mutations.
/// </summary>
/// <param name="Functional">Functional requirement entries in the batch.</param>
/// <param name="Technical">Technical requirement entries in the batch.</param>
/// <param name="Testing">Testing requirement entries in the batch.</param>
public sealed record RequirementsBatchEntries(
    IReadOnlyList<FrEntry> Functional,
    IReadOnlyList<TrEntry> Technical,
    IReadOnlyList<TestEntry> Testing)
{
    /// <summary>An empty requirements batch.</summary>
    public static RequirementsBatchEntries Empty { get; } = new([], [], []);

    /// <summary>Total number of entries across all requirement kinds.</summary>
    public int Count => Functional.Count + Technical.Count + Testing.Count;
}

/// <summary>Shared constants for requirement scope layers.</summary>
public static class RequirementScopeLayerDefaults
{
    /// <summary>Default layer key applied to legacy requirements and workspaces.</summary>
    public const string DefaultLayerKey = "layer-1";
}

/// <summary>FR-MCP-REQSCOPE-001: ordered workspace requirement scope layer.</summary>
public sealed record RequirementScopeLayerEntry(
    string Key,
    int Order,
    string Name,
    string? Description = null,
    string? ScopeEndLayerKey = null,
    string WorkspaceId = "",
    DateTimeOffset? CreatedAtUtc = null,
    DateTimeOffset? UpdatedAtUtc = null);

/// <summary>FR-MCP-REQSCOPE-001: layer update request allowing mutable metadata and layer sunset.</summary>
public sealed class RequirementScopeLayerUpdateRequest
{
    /// <summary>Layer key to update.</summary>
    public RequirementScopeLayerUpdateRequest(string key)
    {
        Key = key;
    }

    /// <summary>Layer key. Attempts to change it are rejected by the service.</summary>
    public string Key { get; set; }

    /// <summary>Optional immutable order value. Non-null mismatches are rejected.</summary>
    public int? Order { get; set; }

    /// <summary>Optional new layer name.</summary>
    public string? Name { get; set; }

    /// <summary>Optional new layer description.</summary>
    public string? Description { get; set; }

    /// <summary>Optional last layer where requirements starting in this layer apply.</summary>
    public string? ScopeEndLayerKey { get; set; }
}

/// <summary>FR-MCP-REQSCOPE-003: effective requirements resolved for a workspace layer.</summary>
public sealed record EffectiveRequirementsResult(
    RequirementScopeLayerEntry CurrentLayer,
    IReadOnlyList<FrEntry> Functional,
    IReadOnlyList<TrEntry> Technical,
    IReadOnlyList<TestEntry> Testing,
    IReadOnlyList<FrTrMapping> Mappings);

/// <summary>FR-MCP-026: FR-to-TR mapping row from TR-per-FR-Mapping.md.</summary>
public sealed record FrTrMapping
{
    /// <summary>Initializes a two-column legacy FR-to-TR mapping row.</summary>
    public FrTrMapping(string frId, IReadOnlyList<string> trIds)
        : this(frId, trIds, [], string.Empty)
    {
    }

    /// <summary>Initializes a TEST-aware FR mapping row.</summary>
    public FrTrMapping(
        string frId,
        IReadOnlyList<string> trIds,
        IReadOnlyList<string> testIds,
        string workspaceId = "")
    {
        FrId = frId;
        TrIds = trIds;
        TestIds = testIds;
        WorkspaceId = workspaceId;
    }

    /// <summary>The FR identifier.</summary>
    public string FrId { get; init; }

    /// <summary>List of associated TR identifiers.</summary>
    public IReadOnlyList<string> TrIds { get; init; }

    /// <summary>List of associated TEST identifiers.</summary>
    public IReadOnlyList<string> TestIds { get; init; }

    /// <summary>The workspace discriminator that owns the row.</summary>
    public string WorkspaceId { get; init; }
}

/// <summary>FR-MCP-026: Enumeration of requirements document types for generation.</summary>
public enum RequirementsDocType
{
    /// <summary>Functional Requirements document.</summary>
    Functional,

    /// <summary>Technical Requirements document.</summary>
    Technical,

    /// <summary>Testing Requirements document.</summary>
    Testing,

    /// <summary>TR-per-FR Mapping document.</summary>
    Mapping,

    /// <summary>Requirements Matrix document.</summary>
    Matrix,

    /// <summary>All requirements documents as a workspace export.</summary>
    All
}
