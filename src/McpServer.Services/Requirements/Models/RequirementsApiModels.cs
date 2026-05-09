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
    /// <summary>Requested source format: auto, canonical, or wiki.</summary>
    public string? SourceFormat { get; init; }

    /// <summary>Preferred wiki platform to use when Azure and GitHub timestamps disagree.</summary>
    public string? PreferredWikiFormat { get; init; }

    /// <summary>Path-keyed document content map for wiki imports.</summary>
    public IReadOnlyDictionary<string, RequirementsIngestDocument>? Documents { get; init; }

    /// <summary>Functional requirements markdown content.</summary>
    public string? FunctionalMarkdown { get; init; }

    /// <summary>Technical requirements markdown content.</summary>
    public string? TechnicalMarkdown { get; init; }

    /// <summary>Testing requirements markdown content.</summary>
    public string? TestingMarkdown { get; init; }

    /// <summary>FR-to-TR mapping markdown content.</summary>
    public string? MappingMarkdown { get; init; }
}

/// <summary>Path-keyed document payload used for requirements wiki imports.</summary>
public sealed class RequirementsIngestDocument
{
    /// <summary>UTF-8 text content for a wiki or canonical document.</summary>
    public string? Content { get; init; }

    /// <summary>Base64-encoded UTF-8 content for binary-safe REPL and plugin transport.</summary>
    public string? ContentBase64 { get; init; }

    /// <summary>Optional file or ZIP entry modified time used for wiki source selection.</summary>
    public DateTimeOffset? LastModifiedUtc { get; init; }
}

/// <summary>Result payload returned after requirements documents are exported to the workspace.</summary>
public sealed class RequirementsDocumentExportResult
{
    /// <summary>Whether the export succeeded.</summary>
    public bool Success { get; init; }

    /// <summary>Generated document format.</summary>
    public string Format { get; init; } = string.Empty;

    /// <summary>Generated document selector.</summary>
    public string DocType { get; init; } = string.Empty;

    /// <summary>UTC timestamp used for manifests and exported file modified times.</summary>
    public DateTimeOffset GeneratedAtUtc { get; init; }

    /// <summary>Absolute workspace output root where the files were written.</summary>
    public string OutputRoot { get; init; } = string.Empty;

    /// <summary>Files written by the export operation.</summary>
    public IReadOnlyList<RequirementsDocumentExportFile> Files { get; init; } = [];
}

/// <summary>Metadata for one requirements document written during workspace export.</summary>
public sealed class RequirementsDocumentExportFile
{
    /// <summary>Path relative to the export output root.</summary>
    public string RelativePath { get; init; } = string.Empty;

    /// <summary>Absolute path written on disk.</summary>
    public string FullPath { get; init; } = string.Empty;

    /// <summary>Content type for the written file.</summary>
    public string ContentType { get; init; } = string.Empty;

    /// <summary>UTC modified time assigned to the written file.</summary>
    public DateTimeOffset LastModifiedUtc { get; init; }
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

    /// <summary>Total FR entries deleted from the requirements store.</summary>
    public int FunctionalDeleted { get; init; }

    /// <summary>Total FR entries already matching imported content.</summary>
    public int FunctionalIgnored { get; init; }

    /// <summary>Total TR entries parsed from input markdown.</summary>
    public int TechnicalParsed { get; init; }

    /// <summary>Total TR entries added to the requirements store.</summary>
    public int TechnicalAdded { get; init; }

    /// <summary>Total TR entries updated in the requirements store.</summary>
    public int TechnicalUpdated { get; init; }

    /// <summary>Total TR entries deleted from the requirements store.</summary>
    public int TechnicalDeleted { get; init; }

    /// <summary>Total TR entries already matching imported content.</summary>
    public int TechnicalIgnored { get; init; }

    /// <summary>Total TEST entries parsed from input markdown.</summary>
    public int TestingParsed { get; init; }

    /// <summary>Total TEST entries added to the requirements store.</summary>
    public int TestingAdded { get; init; }

    /// <summary>Total TEST entries updated in the requirements store.</summary>
    public int TestingUpdated { get; init; }

    /// <summary>Total TEST entries deleted from the requirements store.</summary>
    public int TestingDeleted { get; init; }

    /// <summary>Total TEST entries already matching imported content.</summary>
    public int TestingIgnored { get; init; }

    /// <summary>Total mapping rows parsed from input markdown.</summary>
    public int MappingParsed { get; init; }

    /// <summary>Total mapping rows added to the requirements store.</summary>
    public int MappingAdded { get; init; }

    /// <summary>Total mapping rows updated in the requirements store.</summary>
    public int MappingUpdated { get; init; }

    /// <summary>Total mapping rows deleted from the requirements store.</summary>
    public int MappingDeleted { get; init; }

    /// <summary>Total mapping rows already matching imported content.</summary>
    public int MappingIgnored { get; init; }

    /// <summary>Selected wiki platform when a wiki document set was imported.</summary>
    public string? SelectedWikiFormat { get; init; }

    /// <summary>Reason the selected wiki platform was chosen.</summary>
    public string? SelectedWikiReason { get; init; }

    /// <summary>Manifest timestamp for the selected wiki platform.</summary>
    public DateTimeOffset? SelectedManifestGeneratedAtUtc { get; init; }

    /// <summary>Latest file modified timestamp for the selected wiki platform.</summary>
    public DateTimeOffset? SelectedLatestFileModifiedUtc { get; init; }

    /// <summary>Non-fatal warnings produced while selecting or ingesting wiki documents.</summary>
    public IReadOnlyList<string> Warnings { get; init; } = [];
}
