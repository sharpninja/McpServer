using McpServer.Support.Mcp.Models;

namespace McpServer.Support.Mcp.Requirements.Models;

/// <summary>Request payload for creating a Functional Requirement entry.</summary>
/// <param name="Id">Requirement identifier (e.g. FR-MCP-040).</param>
/// <param name="Title">Requirement title.</param>
/// <param name="Body">Requirement body text.</param>
/// <param name="Priority">Optional requirement priority.</param>
/// <param name="Status">Optional requirement status.</param>
/// <param name="Notes">Optional requirement notes.</param>
/// <param name="AcceptanceCriteria">FR-MCP-REQAC-001: structured acceptance criteria.</param>
public sealed record CreateFrRequest(string Id, string Title, string Body, string? Priority = null, string? Status = null, string? Notes = null, IReadOnlyList<AcceptanceCriterion>? AcceptanceCriteria = null);

/// <summary>Request payload for updating a Functional Requirement entry.</summary>
/// <param name="Title">Requirement title.</param>
/// <param name="Body">Requirement body text.</param>
/// <param name="Priority">Optional requirement priority.</param>
/// <param name="Status">Optional requirement status.</param>
/// <param name="Notes">Optional requirement notes.</param>
/// <param name="AcceptanceCriteria">FR-MCP-REQAC-001: structured acceptance criteria. Null preserves existing.</param>
public sealed record UpdateFrRequest(string? Title = null, string? Body = null, string? Priority = null, string? Status = null, string? Notes = null, IReadOnlyList<AcceptanceCriterion>? AcceptanceCriteria = null);

/// <summary>Request payload for creating a Technical Requirement entry.</summary>
/// <param name="Id">Requirement identifier (e.g. TR-MCP-REQ-002).</param>
/// <param name="Title">Optional title rendered as bold text before the em dash.</param>
/// <param name="Body">Requirement body text.</param>
/// <param name="Priority">Optional requirement priority.</param>
/// <param name="Status">Optional requirement status.</param>
/// <param name="Notes">Optional requirement notes.</param>
/// <param name="AcceptanceCriteria">FR-MCP-REQAC-001: structured acceptance criteria.</param>
public sealed record CreateTrRequest(string Id, string? Title, string Body, string? Priority = null, string? Status = null, string? Notes = null, IReadOnlyList<AcceptanceCriterion>? AcceptanceCriteria = null);

/// <summary>Request payload for updating a Technical Requirement entry.</summary>
/// <param name="Title">Optional title rendered as bold text before the em dash.</param>
/// <param name="Body">Requirement body text.</param>
/// <param name="Priority">Optional requirement priority.</param>
/// <param name="Status">Optional requirement status.</param>
/// <param name="Notes">Optional requirement notes.</param>
/// <param name="AcceptanceCriteria">FR-MCP-REQAC-001: structured acceptance criteria. Null preserves existing.</param>
public sealed record UpdateTrRequest(string? Title = null, string? Body = null, string? Priority = null, string? Status = null, string? Notes = null, IReadOnlyList<AcceptanceCriterion>? AcceptanceCriteria = null);

/// <summary>Request payload for creating a Testing Requirement entry.</summary>
/// <param name="Id">Requirement identifier (e.g. TEST-MCP-039).</param>
/// <param name="Condition">Test condition text.</param>
/// <param name="Title">Optional test title.</param>
/// <param name="Priority">Optional requirement priority.</param>
/// <param name="Status">Optional requirement status.</param>
/// <param name="Notes">Optional requirement notes.</param>
/// <param name="AcceptanceCriteria">FR-MCP-REQAC-001: structured acceptance criteria.</param>
public sealed record CreateTestRequest(string Id, string Condition, string? Title = null, string? Priority = null, string? Status = null, string? Notes = null, IReadOnlyList<AcceptanceCriterion>? AcceptanceCriteria = null);

/// <summary>Request payload for updating a Testing Requirement entry.</summary>
/// <param name="Condition">Test condition text.</param>
/// <param name="Title">Optional test title.</param>
/// <param name="Priority">Optional requirement priority.</param>
/// <param name="Status">Optional requirement status.</param>
/// <param name="Notes">Optional requirement notes.</param>
/// <param name="AcceptanceCriteria">FR-MCP-REQAC-001: structured acceptance criteria. Null preserves existing.</param>
public sealed record UpdateTestRequest(string? Condition = null, string? Title = null, string? Priority = null, string? Status = null, string? Notes = null, IReadOnlyList<AcceptanceCriterion>? AcceptanceCriteria = null);

/// <summary>Request payload for creating multiple Functional Requirement entries atomically.</summary>
public sealed class CreateFrBatchRequest
{
    /// <summary>FR records to create.</summary>
    public IReadOnlyList<CreateFrBatchRecord> Records { get; init; } = [];
}

/// <summary>One Functional Requirement create record in a batch payload.</summary>
public sealed class CreateFrBatchRecord
{
    /// <summary>Requirement identifier (e.g. FR-MCP-040).</summary>
    public string? Id { get; init; }

    /// <summary>Requirement title.</summary>
    public string? Title { get; init; }

    /// <summary>Requirement body text.</summary>
    public string? Body { get; init; }

    /// <summary>Requirement body alias used by REPL and plugin YAML commands.</summary>
    public string? Description { get; init; }

    /// <summary>Optional requirement priority.</summary>
    public string? Priority { get; init; }

    /// <summary>Optional requirement status.</summary>
    public string? Status { get; init; }

    /// <summary>Optional requirement notes.</summary>
    public string? Notes { get; init; }

    /// <summary>FR-MCP-REQAC-001: structured acceptance criteria (same shape as TODO criteria).</summary>
    public IReadOnlyList<AcceptanceCriterion>? AcceptanceCriteria { get; init; }
}

/// <summary>Request payload for updating multiple Functional Requirement entries atomically.</summary>
public sealed class UpdateFrBatchRequest
{
    /// <summary>FR records to update.</summary>
    public IReadOnlyList<UpdateFrBatchRecord> Records { get; init; } = [];
}

/// <summary>One Functional Requirement update record in a batch payload.</summary>
public sealed class UpdateFrBatchRecord
{
    /// <summary>Requirement identifier (e.g. FR-MCP-040).</summary>
    public string? Id { get; init; }

    /// <summary>Requirement title. Null preserves the current value.</summary>
    public string? Title { get; init; }

    /// <summary>Requirement body text. Null preserves the current value.</summary>
    public string? Body { get; init; }

    /// <summary>Requirement body alias used by REPL and plugin YAML commands. Null preserves the current value.</summary>
    public string? Description { get; init; }

    /// <summary>Optional requirement priority. Null preserves the current value.</summary>
    public string? Priority { get; init; }

    /// <summary>Optional requirement status. Null preserves the current value.</summary>
    public string? Status { get; init; }

    /// <summary>Optional requirement notes. Null preserves the current value.</summary>
    public string? Notes { get; init; }

    /// <summary>FR-MCP-REQAC-001: structured acceptance criteria. Null preserves the current value.</summary>
    public IReadOnlyList<AcceptanceCriterion>? AcceptanceCriteria { get; init; }
}

/// <summary>Request payload for creating multiple Technical Requirement entries atomically.</summary>
public sealed class CreateTrBatchRequest
{
    /// <summary>TR records to create.</summary>
    public IReadOnlyList<CreateTrBatchRecord> Records { get; init; } = [];
}

/// <summary>One Technical Requirement create record in a batch payload.</summary>
public sealed class CreateTrBatchRecord
{
    /// <summary>Requirement identifier (e.g. TR-MCP-REQ-002).</summary>
    public string? Id { get; init; }

    /// <summary>Optional title rendered before the body.</summary>
    public string? Title { get; init; }

    /// <summary>Requirement body text.</summary>
    public string? Body { get; init; }

    /// <summary>Requirement body alias used by REPL and plugin YAML commands.</summary>
    public string? Description { get; init; }

    /// <summary>Optional requirement priority.</summary>
    public string? Priority { get; init; }

    /// <summary>Optional requirement status.</summary>
    public string? Status { get; init; }

    /// <summary>Optional requirement notes.</summary>
    public string? Notes { get; init; }

    /// <summary>FR-MCP-REQAC-001: structured acceptance criteria (same shape as TODO criteria).</summary>
    public IReadOnlyList<AcceptanceCriterion>? AcceptanceCriteria { get; init; }
}

/// <summary>Request payload for updating multiple Technical Requirement entries atomically.</summary>
public sealed class UpdateTrBatchRequest
{
    /// <summary>TR records to update.</summary>
    public IReadOnlyList<UpdateTrBatchRecord> Records { get; init; } = [];
}

/// <summary>One Technical Requirement update record in a batch payload.</summary>
public sealed class UpdateTrBatchRecord
{
    /// <summary>Requirement identifier (e.g. TR-MCP-REQ-002).</summary>
    public string? Id { get; init; }

    /// <summary>Optional title rendered before the body. Null preserves the current value.</summary>
    public string? Title { get; init; }

    /// <summary>Requirement body text. Null preserves the current value.</summary>
    public string? Body { get; init; }

    /// <summary>Requirement body alias used by REPL and plugin YAML commands. Null preserves the current value.</summary>
    public string? Description { get; init; }

    /// <summary>Optional requirement priority. Null preserves the current value.</summary>
    public string? Priority { get; init; }

    /// <summary>Optional requirement status. Null preserves the current value.</summary>
    public string? Status { get; init; }

    /// <summary>Optional requirement notes. Null preserves the current value.</summary>
    public string? Notes { get; init; }

    /// <summary>FR-MCP-REQAC-001: structured acceptance criteria. Null preserves the current value.</summary>
    public IReadOnlyList<AcceptanceCriterion>? AcceptanceCriteria { get; init; }
}

/// <summary>Request payload for creating multiple Testing Requirement entries atomically.</summary>
public sealed class CreateTestBatchRequest
{
    /// <summary>TEST records to create.</summary>
    public IReadOnlyList<CreateTestBatchRecord> Records { get; init; } = [];
}

/// <summary>One Testing Requirement create record in a batch payload.</summary>
public sealed class CreateTestBatchRecord
{
    /// <summary>Requirement identifier (e.g. TEST-MCP-039).</summary>
    public string? Id { get; init; }

    /// <summary>Test condition text.</summary>
    public string? Condition { get; init; }

    /// <summary>Test condition alias used by REPL and plugin YAML commands.</summary>
    public string? Description { get; init; }

    /// <summary>Optional test title.</summary>
    public string? Title { get; init; }

    /// <summary>Optional requirement priority.</summary>
    public string? Priority { get; init; }

    /// <summary>Optional requirement status.</summary>
    public string? Status { get; init; }

    /// <summary>Optional requirement notes.</summary>
    public string? Notes { get; init; }

    /// <summary>FR-MCP-REQAC-001: structured acceptance criteria (same shape as TODO criteria).</summary>
    public IReadOnlyList<AcceptanceCriterion>? AcceptanceCriteria { get; init; }
}

/// <summary>Request payload for updating multiple Testing Requirement entries atomically.</summary>
public sealed class UpdateTestBatchRequest
{
    /// <summary>TEST records to update.</summary>
    public IReadOnlyList<UpdateTestBatchRecord> Records { get; init; } = [];
}

/// <summary>One Testing Requirement update record in a batch payload.</summary>
public sealed class UpdateTestBatchRecord
{
    /// <summary>Requirement identifier (e.g. TEST-MCP-039).</summary>
    public string? Id { get; init; }

    /// <summary>Test condition text. Null preserves the current value.</summary>
    public string? Condition { get; init; }

    /// <summary>Test condition alias used by REPL and plugin YAML commands. Null preserves the current value.</summary>
    public string? Description { get; init; }

    /// <summary>Optional test title. Null preserves the current value.</summary>
    public string? Title { get; init; }

    /// <summary>Optional requirement priority. Null preserves the current value.</summary>
    public string? Priority { get; init; }

    /// <summary>Optional requirement status. Null preserves the current value.</summary>
    public string? Status { get; init; }

    /// <summary>Optional requirement notes. Null preserves the current value.</summary>
    public string? Notes { get; init; }

    /// <summary>FR-MCP-REQAC-001: structured acceptance criteria. Null preserves the current value.</summary>
    public IReadOnlyList<AcceptanceCriterion>? AcceptanceCriteria { get; init; }
}

/// <summary>Request payload for creating mixed FR/TR/TEST entries atomically.</summary>
public sealed class CreateRequirementsBatchRequest
{
    /// <summary>Mixed requirement records to create.</summary>
    public IReadOnlyList<CreateRequirementBatchRecord> Records { get; init; } = [];
}

/// <summary>One mixed requirement create record in a batch payload.</summary>
public sealed class CreateRequirementBatchRecord
{
    /// <summary>Requirement kind: fr, tr, or test.</summary>
    public string? Kind { get; init; }

    /// <summary>Requirement identifier.</summary>
    public string? Id { get; init; }

    /// <summary>Requirement title.</summary>
    public string? Title { get; init; }

    /// <summary>FR/TR body text.</summary>
    public string? Body { get; init; }

    /// <summary>TEST condition text.</summary>
    public string? Condition { get; init; }

    /// <summary>Body/condition alias used by REPL and plugin YAML commands.</summary>
    public string? Description { get; init; }

    /// <summary>Optional requirement priority.</summary>
    public string? Priority { get; init; }

    /// <summary>Optional requirement status.</summary>
    public string? Status { get; init; }

    /// <summary>Optional requirement notes.</summary>
    public string? Notes { get; init; }

    /// <summary>FR-MCP-REQAC-001: structured acceptance criteria (same shape as TODO criteria).</summary>
    public IReadOnlyList<AcceptanceCriterion>? AcceptanceCriteria { get; init; }
}

/// <summary>Request payload for updating mixed FR/TR/TEST entries atomically.</summary>
public sealed class UpdateRequirementsBatchRequest
{
    /// <summary>Mixed requirement records to update.</summary>
    public IReadOnlyList<UpdateRequirementBatchRecord> Records { get; init; } = [];
}

/// <summary>One mixed requirement update record in a batch payload.</summary>
public sealed class UpdateRequirementBatchRecord
{
    /// <summary>Requirement kind: fr, tr, or test.</summary>
    public string? Kind { get; init; }

    /// <summary>Requirement identifier.</summary>
    public string? Id { get; init; }

    /// <summary>Requirement title. Null preserves the current value.</summary>
    public string? Title { get; init; }

    /// <summary>FR/TR body text. Null preserves the current value.</summary>
    public string? Body { get; init; }

    /// <summary>TEST condition text. Null preserves the current value.</summary>
    public string? Condition { get; init; }

    /// <summary>Body/condition alias used by REPL and plugin YAML commands. Null preserves the current value.</summary>
    public string? Description { get; init; }

    /// <summary>Optional requirement priority. Null preserves the current value.</summary>
    public string? Priority { get; init; }

    /// <summary>Optional requirement status. Null preserves the current value.</summary>
    public string? Status { get; init; }

    /// <summary>Optional requirement notes. Null preserves the current value.</summary>
    public string? Notes { get; init; }

    /// <summary>FR-MCP-REQAC-001: structured acceptance criteria. Null preserves the current value.</summary>
    public IReadOnlyList<AcceptanceCriterion>? AcceptanceCriteria { get; init; }
}

/// <summary>Structured response returned by requirements batch create and update endpoints.</summary>
public sealed class RequirementsBatchResult
{
    /// <summary>Whether the batch operation completed successfully.</summary>
    public bool Success { get; init; }

    /// <summary>Batch operation name: create or update.</summary>
    public string Operation { get; init; } = string.Empty;

    /// <summary>Requirement kind for per-kind batches, or null for mixed batches.</summary>
    public string? Kind { get; init; }

    /// <summary>Total number of records applied.</summary>
    public int Total { get; init; }

    /// <summary>Applied records returned by the server.</summary>
    public IReadOnlyList<RequirementsBatchItem> Items { get; init; } = [];

    /// <summary>Validation or persistence errors. Successful batches return an empty collection.</summary>
    public IReadOnlyList<RequirementsBatchError> Errors { get; init; } = [];
}

/// <summary>One applied item in a requirements batch response.</summary>
public sealed class RequirementsBatchItem
{
    /// <summary>Requirement kind: fr, tr, or test.</summary>
    public string Kind { get; init; } = string.Empty;

    /// <summary>Requirement identifier.</summary>
    public string Id { get; init; } = string.Empty;

    /// <summary>Functional requirement payload when <see cref="Kind"/> is fr.</summary>
    public FrEntry? Fr { get; init; }

    /// <summary>Technical requirement payload when <see cref="Kind"/> is tr.</summary>
    public TrEntry? Tr { get; init; }

    /// <summary>Testing requirement payload when <see cref="Kind"/> is test.</summary>
    public TestEntry? Test { get; init; }
}

/// <summary>One validation or persistence error in a requirements batch response.</summary>
public sealed class RequirementsBatchError
{
    /// <summary>Zero-based record index, or -1 when the error applies to the whole batch.</summary>
    public int Index { get; init; } = -1;

    /// <summary>Requirement kind when known.</summary>
    public string? Kind { get; init; }

    /// <summary>Requirement identifier when known.</summary>
    public string? Id { get; init; }

    /// <summary>Error message.</summary>
    public string Error { get; init; } = string.Empty;
}

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

/// <summary>FR-MCP-REQAC-002: request payload for copying acceptance criteria from a TODO onto a requirement.</summary>
/// <param name="TodoId">The source TODO identifier whose acceptance criteria will be copied verbatim.</param>
public sealed record CopyAcceptanceCriteriaFromTodoRequest(string TodoId);
