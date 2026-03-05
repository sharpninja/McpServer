namespace McpServer.Support.Mcp.Models;

/// <summary>
/// TR-PLANNED-013: Represents a source document indexed by the MCP.
/// FR-SUPPORT-010: Normalized entry for context retrieval.
/// </summary>
public sealed record ContextDocument
{
    /// <summary>TR-PLANNED-013: Unique document identifier.</summary>
    public required string Id { get; init; }

    /// <summary>FR-SUPPORT-010: Source type (repo, session-log, external-doc, issue, pr).</summary>
    public required string SourceType { get; init; }

    /// <summary>TR-PLANNED-013: Source path or URL.</summary>
    public required string SourceKey { get; init; }

    /// <summary>FR-SUPPORT-010: Last ingestion timestamp (UTC).</summary>
    public DateTime IngestedAt { get; init; }

    /// <summary>TR-PLANNED-013: Hash for change detection.</summary>
    public required string ContentHash { get; init; }
}
