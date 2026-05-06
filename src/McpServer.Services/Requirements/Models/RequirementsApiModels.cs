namespace McpServer.Support.Mcp.Requirements.Models;

/// <summary>Request payload for creating a Functional Requirement entry.</summary>
/// <param name="Id">Requirement identifier (e.g. FR-MCP-040).</param>
/// <param name="Title">Requirement title.</param>
/// <param name="Body">Requirement body text.</param>
public sealed record CreateFrRequest(string Id, string Title, string Body);

/// <summary>Request payload for updating a Functional Requirement entry.</summary>
/// <param name="Title">Requirement title.</param>
/// <param name="Body">Requirement body text.</param>
public sealed record UpdateFrRequest(string Title, string Body);

/// <summary>Request payload for creating a Technical Requirement entry.</summary>
/// <param name="Id">Requirement identifier (e.g. TR-MCP-REQ-002).</param>
/// <param name="Title">Optional title rendered as bold text before the em dash.</param>
/// <param name="Body">Requirement body text.</param>
public sealed record CreateTrRequest(string Id, string? Title, string Body);

/// <summary>Request payload for updating a Technical Requirement entry.</summary>
/// <param name="Title">Optional title rendered as bold text before the em dash.</param>
/// <param name="Body">Requirement body text.</param>
public sealed record UpdateTrRequest(string? Title, string Body);

/// <summary>Request payload for creating a Testing Requirement entry.</summary>
/// <param name="Id">Requirement identifier (e.g. TEST-MCP-039).</param>
/// <param name="Condition">Test condition text.</param>
public sealed record CreateTestRequest(string Id, string Condition);

/// <summary>Request payload for updating a Testing Requirement entry.</summary>
/// <param name="Condition">Test condition text.</param>
public sealed record UpdateTestRequest(string Condition);

/// <summary>Request payload for creating or updating an FR-to-TR/TEST mapping row.</summary>
/// <param name="TrIds">List of TR identifiers mapped to the FR row.</param>
/// <param name="TestIds">List of TEST identifiers mapped to the FR row.</param>
public sealed record UpsertFrTrMappingRequest(IReadOnlyList<string> TrIds, IReadOnlyList<string>? TestIds = null);

/// <summary>
/// Request payload for bulk requirements ingest from Markdown content.
/// Any null or empty field is skipped.
/// </summary>
public sealed class RequirementsIngestRequest
{
    /// <summary>Functional requirements markdown content.</summary>
    public string? FunctionalMarkdown { get; init; }

    /// <summary>Technical requirements markdown content.</summary>
    public string? TechnicalMarkdown { get; init; }

    /// <summary>Testing requirements markdown content.</summary>
    public string? TestingMarkdown { get; init; }

    /// <summary>FR-to-TR mapping markdown content.</summary>
    public string? MappingMarkdown { get; init; }
}

/// <summary>
/// Result payload for bulk requirements ingest.
/// Includes parsed, added, and updated counts per document type.
/// </summary>
public sealed class RequirementsIngestResult
{
    /// <summary>Total FR entries parsed from input markdown.</summary>
    public int FunctionalParsed { get; init; }

    /// <summary>Total FR entries added to the requirements store.</summary>
    public int FunctionalAdded { get; init; }

    /// <summary>Total FR entries updated in the requirements store.</summary>
    public int FunctionalUpdated { get; init; }

    /// <summary>Total TR entries parsed from input markdown.</summary>
    public int TechnicalParsed { get; init; }

    /// <summary>Total TR entries added to the requirements store.</summary>
    public int TechnicalAdded { get; init; }

    /// <summary>Total TR entries updated in the requirements store.</summary>
    public int TechnicalUpdated { get; init; }

    /// <summary>Total TEST entries parsed from input markdown.</summary>
    public int TestingParsed { get; init; }

    /// <summary>Total TEST entries added to the requirements store.</summary>
    public int TestingAdded { get; init; }

    /// <summary>Total TEST entries updated in the requirements store.</summary>
    public int TestingUpdated { get; init; }

    /// <summary>Total mapping rows parsed from input markdown.</summary>
    public int MappingParsed { get; init; }

    /// <summary>Total mapping rows added to the requirements store.</summary>
    public int MappingAdded { get; init; }

    /// <summary>Total mapping rows updated in the requirements store.</summary>
    public int MappingUpdated { get; init; }
}
