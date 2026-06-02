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
public sealed record FrEntry(
    string Id,
    string Title,
    string Body,
    string WorkspaceId = "",
    string Priority = "medium",
    string Status = "pending",
    string? Notes = null,
    IReadOnlyList<AcceptanceCriterion>? AcceptanceCriteria = null);

/// <summary>FR-MCP-026: Technical Requirement entry parsed from Technical-Requirements.md.</summary>
/// <param name="Id">The TR identifier (e.g. TR-MCP-ARCH-001).</param>
/// <param name="Title">Optional bold title before em-dash separator (may be empty).</param>
/// <param name="Body">The full body text of the requirement.</param>
/// <param name="WorkspaceId">The workspace discriminator that owns the row.</param>
/// <param name="Priority">The requirement priority.</param>
/// <param name="Status">The requirement status.</param>
/// <param name="Notes">Optional operator notes.</param>
/// <param name="AcceptanceCriteria">FR-MCP-REQAC-001: structured acceptance criteria (same shape as TODO criteria).</param>
public sealed record TrEntry(
    string Id,
    string Title,
    string Body,
    string WorkspaceId = "",
    string Priority = "medium",
    string Status = "pending",
    string? Notes = null,
    IReadOnlyList<AcceptanceCriterion>? AcceptanceCriteria = null);

/// <summary>FR-MCP-026: Testing Requirement entry parsed from Testing-Requirements.md.</summary>
/// <param name="Id">The TEST identifier (e.g. TEST-MCP-001).</param>
/// <param name="Condition">The test condition text.</param>
/// <param name="WorkspaceId">The workspace discriminator that owns the row.</param>
/// <param name="Title">Optional test title.</param>
/// <param name="Priority">The requirement priority.</param>
/// <param name="Status">The requirement status.</param>
/// <param name="Notes">Optional operator notes.</param>
/// <param name="AcceptanceCriteria">FR-MCP-REQAC-001: structured acceptance criteria (same shape as TODO criteria).</param>
public sealed record TestEntry(
    string Id,
    string Condition,
    string WorkspaceId = "",
    string Title = "",
    string Priority = "medium",
    string Status = "pending",
    string? Notes = null,
    IReadOnlyList<AcceptanceCriterion>? AcceptanceCriteria = null);

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
