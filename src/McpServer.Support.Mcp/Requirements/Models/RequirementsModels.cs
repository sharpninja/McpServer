namespace McpServer.Support.Mcp.Requirements.Models;

/// <summary>FR-MCP-026: Functional Requirement entry parsed from Functional-Requirements.md.</summary>
/// <param name="Id">The FR identifier (e.g. FR-MCP-001).</param>
/// <param name="Title">The requirement title text.</param>
/// <param name="Body">The full body text (may include **Covered by:** lines).</param>
public sealed record FrEntry(string Id, string Title, string Body);

/// <summary>FR-MCP-026: Technical Requirement entry parsed from Technical-Requirements.md.</summary>
/// <param name="Id">The TR identifier (e.g. TR-MCP-ARCH-001).</param>
/// <param name="Title">Optional bold title before em-dash separator (may be empty).</param>
/// <param name="Body">The full body text of the requirement.</param>
public sealed record TrEntry(string Id, string Title, string Body);

/// <summary>FR-MCP-026: Testing Requirement entry parsed from Testing-Requirements.md.</summary>
/// <param name="Id">The TEST identifier (e.g. TEST-MCP-001).</param>
/// <param name="Condition">The test condition text.</param>
public sealed record TestEntry(string Id, string Condition);

/// <summary>FR-MCP-026: FR-to-TR mapping row from TR-per-FR-Mapping.md.</summary>
/// <param name="FrId">The FR identifier.</param>
/// <param name="TrIds">List of associated TR identifiers.</param>
public sealed record FrTrMapping(string FrId, IReadOnlyList<string> TrIds);

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

    /// <summary>All four documents as a ZIP archive.</summary>
    All
}