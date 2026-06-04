using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace McpServer.Client.Models;

/// <summary>A functional requirement entry.</summary>
public sealed class FrEntry
{
    /// <summary>Requirement identifier.</summary>
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    /// <summary>Requirement title.</summary>
    [JsonPropertyName("title")]
    public string Title { get; set; } = string.Empty;

    /// <summary>Requirement body.</summary>
    [JsonPropertyName("body")]
    public string Body { get; set; } = string.Empty;

    /// <summary>Owning workspace discriminator.</summary>
    [JsonPropertyName("workspaceId")]
    public string WorkspaceId { get; set; } = string.Empty;

    /// <summary>Requirement priority.</summary>
    [JsonPropertyName("priority")]
    public string Priority { get; set; } = "medium";

    /// <summary>Requirement status.</summary>
    [JsonPropertyName("status")]
    public string Status { get; set; } = "pending";

    /// <summary>Optional operator notes.</summary>
    [JsonPropertyName("notes")]
    public string? Notes { get; set; }

    /// <summary>FR-MCP-REQAC-001: structured acceptance criteria (same shape as TODO criteria).</summary>
    [JsonPropertyName("acceptanceCriteria")]
    public IReadOnlyList<AcceptanceCriterion>? AcceptanceCriteria { get; set; }
}

/// <summary>A technical requirement entry.</summary>
public sealed class TrEntry
{
    /// <summary>Requirement identifier.</summary>
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    /// <summary>Requirement title.</summary>
    [JsonPropertyName("title")]
    public string Title { get; set; } = string.Empty;

    /// <summary>Requirement body.</summary>
    [JsonPropertyName("body")]
    public string Body { get; set; } = string.Empty;

    /// <summary>Owning workspace discriminator.</summary>
    [JsonPropertyName("workspaceId")]
    public string WorkspaceId { get; set; } = string.Empty;

    /// <summary>Requirement priority.</summary>
    [JsonPropertyName("priority")]
    public string Priority { get; set; } = "medium";

    /// <summary>Requirement status.</summary>
    [JsonPropertyName("status")]
    public string Status { get; set; } = "pending";

    /// <summary>Optional operator notes.</summary>
    [JsonPropertyName("notes")]
    public string? Notes { get; set; }

    /// <summary>FR-MCP-REQAC-001: structured acceptance criteria (same shape as TODO criteria).</summary>
    [JsonPropertyName("acceptanceCriteria")]
    public IReadOnlyList<AcceptanceCriterion>? AcceptanceCriteria { get; set; }
}

/// <summary>A testing requirement entry.</summary>
public sealed class TestEntry
{
    /// <summary>Requirement identifier.</summary>
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    /// <summary>Testing condition text.</summary>
    [JsonPropertyName("condition")]
    public string Condition { get; set; } = string.Empty;

    /// <summary>Requirement title.</summary>
    [JsonPropertyName("title")]
    public string Title { get; set; } = string.Empty;

    /// <summary>Owning workspace discriminator.</summary>
    [JsonPropertyName("workspaceId")]
    public string WorkspaceId { get; set; } = string.Empty;

    /// <summary>Requirement priority.</summary>
    [JsonPropertyName("priority")]
    public string Priority { get; set; } = "medium";

    /// <summary>Requirement status.</summary>
    [JsonPropertyName("status")]
    public string Status { get; set; } = "pending";

    /// <summary>Optional operator notes.</summary>
    [JsonPropertyName("notes")]
    public string? Notes { get; set; }

    /// <summary>FR-MCP-REQAC-001: structured acceptance criteria (same shape as TODO criteria).</summary>
    [JsonPropertyName("acceptanceCriteria")]
    public IReadOnlyList<AcceptanceCriterion>? AcceptanceCriteria { get; set; }
}

/// <summary>A functional-to-technical requirement mapping row.</summary>
public sealed class FrTrMapping
{
    /// <summary>Functional requirement identifier.</summary>
    [JsonPropertyName("frId")]
    public string FrId { get; set; } = string.Empty;

    /// <summary>Mapped technical requirement identifiers.</summary>
    [JsonPropertyName("trIds")]
    public IReadOnlyList<string> TrIds { get; set; } = [];

    /// <summary>Mapped testing requirement identifiers.</summary>
    [JsonPropertyName("testIds")]
    public IReadOnlyList<string> TestIds { get; set; } = [];

    /// <summary>Owning workspace discriminator.</summary>
    [JsonPropertyName("workspaceId")]
    public string WorkspaceId { get; set; } = string.Empty;
}

/// <summary>Request payload for creating a functional requirement entry.</summary>
public sealed class CreateFrRequest
{
    /// <summary>Requirement identifier.</summary>
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    /// <summary>Requirement title.</summary>
    [JsonPropertyName("title")]
    public string Title { get; set; } = string.Empty;

    /// <summary>Requirement body.</summary>
    [JsonPropertyName("body")]
    public string Body { get; set; } = string.Empty;

    /// <summary>Requirement priority.</summary>
    [JsonPropertyName("priority")]
    public string? Priority { get; set; }

    /// <summary>Requirement status.</summary>
    [JsonPropertyName("status")]
    public string? Status { get; set; }

    /// <summary>Optional operator notes.</summary>
    [JsonPropertyName("notes")]
    public string? Notes { get; set; }

    /// <summary>FR-MCP-REQAC-001: structured acceptance criteria (same shape as TODO criteria).</summary>
    [JsonPropertyName("acceptanceCriteria")]
    public IReadOnlyList<AcceptanceCriterion>? AcceptanceCriteria { get; set; }
}

/// <summary>Request payload for updating a functional requirement entry.</summary>
public sealed class UpdateFrRequest
{
    /// <summary>Requirement title.</summary>
    [JsonPropertyName("title")]
    public string? Title { get; set; }

    /// <summary>Requirement body.</summary>
    [JsonPropertyName("body")]
    public string? Body { get; set; }

    /// <summary>Requirement priority.</summary>
    [JsonPropertyName("priority")]
    public string? Priority { get; set; }

    /// <summary>Requirement status.</summary>
    [JsonPropertyName("status")]
    public string? Status { get; set; }

    /// <summary>Optional operator notes.</summary>
    [JsonPropertyName("notes")]
    public string? Notes { get; set; }

    /// <summary>FR-MCP-REQAC-001: structured acceptance criteria (same shape as TODO criteria).</summary>
    [JsonPropertyName("acceptanceCriteria")]
    public IReadOnlyList<AcceptanceCriterion>? AcceptanceCriteria { get; set; }
}

/// <summary>Request payload for creating a technical requirement entry.</summary>
public sealed class CreateTrRequest
{
    /// <summary>Requirement identifier.</summary>
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    /// <summary>Requirement title.</summary>
    [JsonPropertyName("title")]
    public string? Title { get; set; }

    /// <summary>Requirement body.</summary>
    [JsonPropertyName("body")]
    public string Body { get; set; } = string.Empty;

    /// <summary>Requirement priority.</summary>
    [JsonPropertyName("priority")]
    public string? Priority { get; set; }

    /// <summary>Requirement status.</summary>
    [JsonPropertyName("status")]
    public string? Status { get; set; }

    /// <summary>Optional operator notes.</summary>
    [JsonPropertyName("notes")]
    public string? Notes { get; set; }

    /// <summary>FR-MCP-REQAC-001: structured acceptance criteria (same shape as TODO criteria).</summary>
    [JsonPropertyName("acceptanceCriteria")]
    public IReadOnlyList<AcceptanceCriterion>? AcceptanceCriteria { get; set; }
}

/// <summary>Request payload for updating a technical requirement entry.</summary>
public sealed class UpdateTrRequest
{
    /// <summary>Requirement title.</summary>
    [JsonPropertyName("title")]
    public string? Title { get; set; }

    /// <summary>Requirement body.</summary>
    [JsonPropertyName("body")]
    public string? Body { get; set; }

    /// <summary>Requirement priority.</summary>
    [JsonPropertyName("priority")]
    public string? Priority { get; set; }

    /// <summary>Requirement status.</summary>
    [JsonPropertyName("status")]
    public string? Status { get; set; }

    /// <summary>Optional operator notes.</summary>
    [JsonPropertyName("notes")]
    public string? Notes { get; set; }

    /// <summary>FR-MCP-REQAC-001: structured acceptance criteria (same shape as TODO criteria).</summary>
    [JsonPropertyName("acceptanceCriteria")]
    public IReadOnlyList<AcceptanceCriterion>? AcceptanceCriteria { get; set; }
}

/// <summary>Request payload for creating a testing requirement entry.</summary>
public sealed class CreateTestRequest
{
    /// <summary>Requirement identifier.</summary>
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    /// <summary>Testing condition text.</summary>
    [JsonPropertyName("condition")]
    public string Condition { get; set; } = string.Empty;

    /// <summary>Requirement title.</summary>
    [JsonPropertyName("title")]
    public string? Title { get; set; }

    /// <summary>Requirement priority.</summary>
    [JsonPropertyName("priority")]
    public string? Priority { get; set; }

    /// <summary>Requirement status.</summary>
    [JsonPropertyName("status")]
    public string? Status { get; set; }

    /// <summary>Optional operator notes.</summary>
    [JsonPropertyName("notes")]
    public string? Notes { get; set; }

    /// <summary>FR-MCP-REQAC-001: structured acceptance criteria (same shape as TODO criteria).</summary>
    [JsonPropertyName("acceptanceCriteria")]
    public IReadOnlyList<AcceptanceCriterion>? AcceptanceCriteria { get; set; }
}

/// <summary>Request payload for updating a testing requirement entry.</summary>
public sealed class UpdateTestRequest
{
    /// <summary>Testing condition text.</summary>
    [JsonPropertyName("condition")]
    public string? Condition { get; set; }

    /// <summary>Requirement title.</summary>
    [JsonPropertyName("title")]
    public string? Title { get; set; }

    /// <summary>Requirement priority.</summary>
    [JsonPropertyName("priority")]
    public string? Priority { get; set; }

    /// <summary>Requirement status.</summary>
    [JsonPropertyName("status")]
    public string? Status { get; set; }

    /// <summary>Optional operator notes.</summary>
    [JsonPropertyName("notes")]
    public string? Notes { get; set; }

    /// <summary>FR-MCP-REQAC-001: structured acceptance criteria (same shape as TODO criteria).</summary>
    [JsonPropertyName("acceptanceCriteria")]
    public IReadOnlyList<AcceptanceCriterion>? AcceptanceCriteria { get; set; }
}

/// <summary>Request payload for creating multiple functional requirements atomically.</summary>
public sealed class CreateFrBatchRequest
{
    /// <summary>FR records to create.</summary>
    [JsonPropertyName("records")]
    public IReadOnlyList<CreateFrBatchRecord> Records { get; set; } = [];
}

/// <summary>One functional requirement create record in a batch request.</summary>
public sealed class CreateFrBatchRecord
{
    /// <summary>Requirement identifier.</summary>
    [JsonPropertyName("id")]
    public string? Id { get; set; }

    /// <summary>Requirement title.</summary>
    [JsonPropertyName("title")]
    public string? Title { get; set; }

    /// <summary>Requirement body.</summary>
    [JsonPropertyName("body")]
    public string? Body { get; set; }

    /// <summary>Requirement body alias for YAML commands.</summary>
    [JsonPropertyName("description")]
    public string? Description { get; set; }

    /// <summary>Requirement priority.</summary>
    [JsonPropertyName("priority")]
    public string? Priority { get; set; }

    /// <summary>Requirement status.</summary>
    [JsonPropertyName("status")]
    public string? Status { get; set; }

    /// <summary>Optional operator notes.</summary>
    [JsonPropertyName("notes")]
    public string? Notes { get; set; }

    /// <summary>FR-MCP-REQAC-001: structured acceptance criteria (same shape as TODO criteria).</summary>
    [JsonPropertyName("acceptanceCriteria")]
    public IReadOnlyList<AcceptanceCriterion>? AcceptanceCriteria { get; set; }
}

/// <summary>Request payload for updating multiple functional requirements atomically.</summary>
public sealed class UpdateFrBatchRequest
{
    /// <summary>FR records to update.</summary>
    [JsonPropertyName("records")]
    public IReadOnlyList<UpdateFrBatchRecord> Records { get; set; } = [];
}

/// <summary>One functional requirement update record in a batch request.</summary>
public sealed class UpdateFrBatchRecord
{
    /// <summary>Requirement identifier.</summary>
    [JsonPropertyName("id")]
    public string? Id { get; set; }

    /// <summary>Requirement title. Null preserves the current value.</summary>
    [JsonPropertyName("title")]
    public string? Title { get; set; }

    /// <summary>Requirement body. Null preserves the current value.</summary>
    [JsonPropertyName("body")]
    public string? Body { get; set; }

    /// <summary>Requirement body alias for YAML commands. Null preserves the current value.</summary>
    [JsonPropertyName("description")]
    public string? Description { get; set; }

    /// <summary>Requirement priority. Null preserves the current value.</summary>
    [JsonPropertyName("priority")]
    public string? Priority { get; set; }

    /// <summary>Requirement status. Null preserves the current value.</summary>
    [JsonPropertyName("status")]
    public string? Status { get; set; }

    /// <summary>Optional operator notes. Null preserves the current value.</summary>
    [JsonPropertyName("notes")]
    public string? Notes { get; set; }

    /// <summary>FR-MCP-REQAC-001: structured acceptance criteria. Null preserves the current value.</summary>
    [JsonPropertyName("acceptanceCriteria")]
    public IReadOnlyList<AcceptanceCriterion>? AcceptanceCriteria { get; set; }
}

/// <summary>Request payload for creating multiple technical requirements atomically.</summary>
public sealed class CreateTrBatchRequest
{
    /// <summary>TR records to create.</summary>
    [JsonPropertyName("records")]
    public IReadOnlyList<CreateTrBatchRecord> Records { get; set; } = [];
}

/// <summary>One technical requirement create record in a batch request.</summary>
public sealed class CreateTrBatchRecord
{
    /// <summary>Requirement identifier.</summary>
    [JsonPropertyName("id")]
    public string? Id { get; set; }

    /// <summary>Requirement title.</summary>
    [JsonPropertyName("title")]
    public string? Title { get; set; }

    /// <summary>Requirement body.</summary>
    [JsonPropertyName("body")]
    public string? Body { get; set; }

    /// <summary>Requirement body alias for YAML commands.</summary>
    [JsonPropertyName("description")]
    public string? Description { get; set; }

    /// <summary>Requirement priority.</summary>
    [JsonPropertyName("priority")]
    public string? Priority { get; set; }

    /// <summary>Requirement status.</summary>
    [JsonPropertyName("status")]
    public string? Status { get; set; }

    /// <summary>Optional operator notes.</summary>
    [JsonPropertyName("notes")]
    public string? Notes { get; set; }

    /// <summary>FR-MCP-REQAC-001: structured acceptance criteria (same shape as TODO criteria).</summary>
    [JsonPropertyName("acceptanceCriteria")]
    public IReadOnlyList<AcceptanceCriterion>? AcceptanceCriteria { get; set; }
}

/// <summary>Request payload for updating multiple technical requirements atomically.</summary>
public sealed class UpdateTrBatchRequest
{
    /// <summary>TR records to update.</summary>
    [JsonPropertyName("records")]
    public IReadOnlyList<UpdateTrBatchRecord> Records { get; set; } = [];
}

/// <summary>One technical requirement update record in a batch request.</summary>
public sealed class UpdateTrBatchRecord
{
    /// <summary>Requirement identifier.</summary>
    [JsonPropertyName("id")]
    public string? Id { get; set; }

    /// <summary>Requirement title. Null preserves the current value.</summary>
    [JsonPropertyName("title")]
    public string? Title { get; set; }

    /// <summary>Requirement body. Null preserves the current value.</summary>
    [JsonPropertyName("body")]
    public string? Body { get; set; }

    /// <summary>Requirement body alias for YAML commands. Null preserves the current value.</summary>
    [JsonPropertyName("description")]
    public string? Description { get; set; }

    /// <summary>Requirement priority. Null preserves the current value.</summary>
    [JsonPropertyName("priority")]
    public string? Priority { get; set; }

    /// <summary>Requirement status. Null preserves the current value.</summary>
    [JsonPropertyName("status")]
    public string? Status { get; set; }

    /// <summary>Optional operator notes. Null preserves the current value.</summary>
    [JsonPropertyName("notes")]
    public string? Notes { get; set; }

    /// <summary>FR-MCP-REQAC-001: structured acceptance criteria. Null preserves the current value.</summary>
    [JsonPropertyName("acceptanceCriteria")]
    public IReadOnlyList<AcceptanceCriterion>? AcceptanceCriteria { get; set; }
}

/// <summary>Request payload for creating multiple testing requirements atomically.</summary>
public sealed class CreateTestBatchRequest
{
    /// <summary>TEST records to create.</summary>
    [JsonPropertyName("records")]
    public IReadOnlyList<CreateTestBatchRecord> Records { get; set; } = [];
}

/// <summary>One testing requirement create record in a batch request.</summary>
public sealed class CreateTestBatchRecord
{
    /// <summary>Requirement identifier.</summary>
    [JsonPropertyName("id")]
    public string? Id { get; set; }

    /// <summary>Testing condition text.</summary>
    [JsonPropertyName("condition")]
    public string? Condition { get; set; }

    /// <summary>Testing condition alias for YAML commands.</summary>
    [JsonPropertyName("description")]
    public string? Description { get; set; }

    /// <summary>Requirement title.</summary>
    [JsonPropertyName("title")]
    public string? Title { get; set; }

    /// <summary>Requirement priority.</summary>
    [JsonPropertyName("priority")]
    public string? Priority { get; set; }

    /// <summary>Requirement status.</summary>
    [JsonPropertyName("status")]
    public string? Status { get; set; }

    /// <summary>Optional operator notes.</summary>
    [JsonPropertyName("notes")]
    public string? Notes { get; set; }

    /// <summary>FR-MCP-REQAC-001: structured acceptance criteria (same shape as TODO criteria).</summary>
    [JsonPropertyName("acceptanceCriteria")]
    public IReadOnlyList<AcceptanceCriterion>? AcceptanceCriteria { get; set; }
}

/// <summary>Request payload for updating multiple testing requirements atomically.</summary>
public sealed class UpdateTestBatchRequest
{
    /// <summary>TEST records to update.</summary>
    [JsonPropertyName("records")]
    public IReadOnlyList<UpdateTestBatchRecord> Records { get; set; } = [];
}

/// <summary>One testing requirement update record in a batch request.</summary>
public sealed class UpdateTestBatchRecord
{
    /// <summary>Requirement identifier.</summary>
    [JsonPropertyName("id")]
    public string? Id { get; set; }

    /// <summary>Testing condition text. Null preserves the current value.</summary>
    [JsonPropertyName("condition")]
    public string? Condition { get; set; }

    /// <summary>Testing condition alias for YAML commands. Null preserves the current value.</summary>
    [JsonPropertyName("description")]
    public string? Description { get; set; }

    /// <summary>Requirement title. Null preserves the current value.</summary>
    [JsonPropertyName("title")]
    public string? Title { get; set; }

    /// <summary>Requirement priority. Null preserves the current value.</summary>
    [JsonPropertyName("priority")]
    public string? Priority { get; set; }

    /// <summary>Requirement status. Null preserves the current value.</summary>
    [JsonPropertyName("status")]
    public string? Status { get; set; }

    /// <summary>Optional operator notes. Null preserves the current value.</summary>
    [JsonPropertyName("notes")]
    public string? Notes { get; set; }

    /// <summary>FR-MCP-REQAC-001: structured acceptance criteria. Null preserves the current value.</summary>
    [JsonPropertyName("acceptanceCriteria")]
    public IReadOnlyList<AcceptanceCriterion>? AcceptanceCriteria { get; set; }
}

/// <summary>Request payload for creating mixed FR/TR/TEST requirements atomically.</summary>
public sealed class CreateRequirementsBatchRequest
{
    /// <summary>Mixed records to create.</summary>
    [JsonPropertyName("records")]
    public IReadOnlyList<CreateRequirementBatchRecord> Records { get; set; } = [];
}

/// <summary>One mixed requirement create record in a batch request.</summary>
public sealed class CreateRequirementBatchRecord
{
    /// <summary>Requirement kind: fr, tr, or test.</summary>
    [JsonPropertyName("kind")]
    public string? Kind { get; set; }

    /// <summary>Requirement identifier.</summary>
    [JsonPropertyName("id")]
    public string? Id { get; set; }

    /// <summary>Requirement title.</summary>
    [JsonPropertyName("title")]
    public string? Title { get; set; }

    /// <summary>FR/TR body text.</summary>
    [JsonPropertyName("body")]
    public string? Body { get; set; }

    /// <summary>TEST condition text.</summary>
    [JsonPropertyName("condition")]
    public string? Condition { get; set; }

    /// <summary>Body/condition alias for YAML commands.</summary>
    [JsonPropertyName("description")]
    public string? Description { get; set; }

    /// <summary>Requirement priority.</summary>
    [JsonPropertyName("priority")]
    public string? Priority { get; set; }

    /// <summary>Requirement status.</summary>
    [JsonPropertyName("status")]
    public string? Status { get; set; }

    /// <summary>Optional operator notes.</summary>
    [JsonPropertyName("notes")]
    public string? Notes { get; set; }

    /// <summary>FR-MCP-REQAC-001: structured acceptance criteria (same shape as TODO criteria).</summary>
    [JsonPropertyName("acceptanceCriteria")]
    public IReadOnlyList<AcceptanceCriterion>? AcceptanceCriteria { get; set; }
}

/// <summary>Request payload for updating mixed FR/TR/TEST requirements atomically.</summary>
public sealed class UpdateRequirementsBatchRequest
{
    /// <summary>Mixed records to update.</summary>
    [JsonPropertyName("records")]
    public IReadOnlyList<UpdateRequirementBatchRecord> Records { get; set; } = [];
}

/// <summary>One mixed requirement update record in a batch request.</summary>
public sealed class UpdateRequirementBatchRecord
{
    /// <summary>Requirement kind: fr, tr, or test.</summary>
    [JsonPropertyName("kind")]
    public string? Kind { get; set; }

    /// <summary>Requirement identifier.</summary>
    [JsonPropertyName("id")]
    public string? Id { get; set; }

    /// <summary>Requirement title. Null preserves the current value.</summary>
    [JsonPropertyName("title")]
    public string? Title { get; set; }

    /// <summary>FR/TR body text. Null preserves the current value.</summary>
    [JsonPropertyName("body")]
    public string? Body { get; set; }

    /// <summary>TEST condition text. Null preserves the current value.</summary>
    [JsonPropertyName("condition")]
    public string? Condition { get; set; }

    /// <summary>Body/condition alias for YAML commands. Null preserves the current value.</summary>
    [JsonPropertyName("description")]
    public string? Description { get; set; }

    /// <summary>Requirement priority. Null preserves the current value.</summary>
    [JsonPropertyName("priority")]
    public string? Priority { get; set; }

    /// <summary>Requirement status. Null preserves the current value.</summary>
    [JsonPropertyName("status")]
    public string? Status { get; set; }

    /// <summary>Optional operator notes. Null preserves the current value.</summary>
    [JsonPropertyName("notes")]
    public string? Notes { get; set; }

    /// <summary>FR-MCP-REQAC-001: structured acceptance criteria. Null preserves the current value.</summary>
    [JsonPropertyName("acceptanceCriteria")]
    public IReadOnlyList<AcceptanceCriterion>? AcceptanceCriteria { get; set; }
}

/// <summary>Structured response returned by requirements batch endpoints.</summary>
public sealed class RequirementsBatchResult
{
    /// <summary>Whether the batch operation completed successfully.</summary>
    [JsonPropertyName("success")]
    public bool Success { get; set; }

    /// <summary>Batch operation name: create or update.</summary>
    [JsonPropertyName("operation")]
    public string Operation { get; set; } = string.Empty;

    /// <summary>Requirement kind for per-kind batches, or null for mixed batches.</summary>
    [JsonPropertyName("kind")]
    public string? Kind { get; set; }

    /// <summary>Total number of applied records.</summary>
    [JsonPropertyName("total")]
    public int Total { get; set; }

    /// <summary>Applied records returned by the server.</summary>
    [JsonPropertyName("items")]
    public IReadOnlyList<RequirementsBatchItem> Items { get; set; } = [];

    /// <summary>Validation or persistence errors.</summary>
    [JsonPropertyName("errors")]
    public IReadOnlyList<RequirementsBatchError> Errors { get; set; } = [];
}

/// <summary>One applied item in a requirements batch response.</summary>
public sealed class RequirementsBatchItem
{
    /// <summary>Requirement kind: fr, tr, or test.</summary>
    [JsonPropertyName("kind")]
    public string Kind { get; set; } = string.Empty;

    /// <summary>Requirement identifier.</summary>
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    /// <summary>Functional requirement payload when kind is fr.</summary>
    [JsonPropertyName("fr")]
    public FrEntry? Fr { get; set; }

    /// <summary>Technical requirement payload when kind is tr.</summary>
    [JsonPropertyName("tr")]
    public TrEntry? Tr { get; set; }

    /// <summary>Testing requirement payload when kind is test.</summary>
    [JsonPropertyName("test")]
    public TestEntry? Test { get; set; }
}

/// <summary>One validation or persistence error in a requirements batch response.</summary>
public sealed class RequirementsBatchError
{
    /// <summary>Zero-based record index, or -1 when the error applies to the whole batch.</summary>
    [JsonPropertyName("index")]
    public int Index { get; set; } = -1;

    /// <summary>Requirement kind when known.</summary>
    [JsonPropertyName("kind")]
    public string? Kind { get; set; }

    /// <summary>Requirement identifier when known.</summary>
    [JsonPropertyName("id")]
    public string? Id { get; set; }

    /// <summary>Error message.</summary>
    [JsonPropertyName("error")]
    public string Error { get; set; } = string.Empty;
}

/// <summary>Request payload for creating or updating a mapping row.</summary>
public sealed class UpsertFrTrMappingRequest
{
    /// <summary>Mapped technical requirement identifiers.</summary>
    [JsonPropertyName("trIds")]
    public IReadOnlyList<string> TrIds { get; set; } = [];

    /// <summary>Mapped testing requirement identifiers.</summary>
    [JsonPropertyName("testIds")]
    public IReadOnlyList<string> TestIds { get; set; } = [];
}

/// <summary>Result of a mutation operation.</summary>
public sealed class RequirementsMutationResult
{
    /// <summary>Whether the operation succeeded.</summary>
    [JsonPropertyName("success")]
    public bool Success { get; set; }

    /// <summary>Error message (when available).</summary>
    [JsonPropertyName("error")]
    public string? Error { get; set; }
}

/// <summary>Binary output returned by requirements document generation.</summary>
public sealed class RequirementsGeneratedDocument
{
    /// <summary>Generated document content.</summary>
    public byte[] Content { get; set; } = [];

    /// <summary>Document media type.</summary>
    public string? ContentType { get; set; }

    /// <summary>Workspace export metadata when a multi-document export writes files directly to disk.</summary>
    public RequirementsDocumentExportResult? ExportResult { get; set; }
}

/// <summary>Result payload returned after requirements documents are exported to the workspace.</summary>
public sealed class RequirementsDocumentExportResult
{
    /// <summary>Whether the export succeeded.</summary>
    [JsonPropertyName("success")]
    public bool Success { get; set; }

    /// <summary>Generated document format.</summary>
    [JsonPropertyName("format")]
    public string Format { get; set; } = string.Empty;

    /// <summary>Generated document selector.</summary>
    [JsonPropertyName("docType")]
    public string DocType { get; set; } = string.Empty;

    /// <summary>UTC timestamp used for manifests and exported file modified times.</summary>
    [JsonPropertyName("generatedAtUtc")]
    public DateTimeOffset GeneratedAtUtc { get; set; }

    /// <summary>Absolute workspace output root where the files were written.</summary>
    [JsonPropertyName("outputRoot")]
    public string OutputRoot { get; set; } = string.Empty;

    /// <summary>Files written by the export operation.</summary>
    [JsonPropertyName("files")]
    public IReadOnlyList<RequirementsDocumentExportFile> Files { get; set; } = [];
}

/// <summary>Metadata for one requirements document written during workspace export.</summary>
public sealed class RequirementsDocumentExportFile
{
    /// <summary>Path relative to the export output root.</summary>
    [JsonPropertyName("relativePath")]
    public string RelativePath { get; set; } = string.Empty;

    /// <summary>Absolute path written on disk.</summary>
    [JsonPropertyName("fullPath")]
    public string FullPath { get; set; } = string.Empty;

    /// <summary>Content type for the written file.</summary>
    [JsonPropertyName("contentType")]
    public string ContentType { get; set; } = string.Empty;

    /// <summary>UTC modified time assigned to the written file.</summary>
    [JsonPropertyName("lastModifiedUtc")]
    public DateTimeOffset LastModifiedUtc { get; set; }
}

/// <summary>Request payload for bulk requirements ingest from markdown text.</summary>
public sealed class RequirementsIngestRequest
{
    /// <summary>Requested source format: auto, canonical, or wiki.</summary>
    [JsonPropertyName("sourceFormat")]
    public string? SourceFormat { get; set; }

    /// <summary>Preferred wiki platform when Azure and GitHub timestamp checks disagree.</summary>
    [JsonPropertyName("preferredWikiFormat")]
    public string? PreferredWikiFormat { get; set; }

    /// <summary>Path-keyed document map used for wiki import.</summary>
    [JsonPropertyName("documents")]
    public IReadOnlyDictionary<string, RequirementsIngestDocument>? Documents { get; set; }

    /// <summary>Functional requirements markdown content.</summary>
    [JsonPropertyName("functionalMarkdown")]
    public string? FunctionalMarkdown { get; set; }

    /// <summary>Technical requirements markdown content.</summary>
    [JsonPropertyName("technicalMarkdown")]
    public string? TechnicalMarkdown { get; set; }

    /// <summary>Testing requirements markdown content.</summary>
    [JsonPropertyName("testingMarkdown")]
    public string? TestingMarkdown { get; set; }

    /// <summary>FR-to-TR mapping markdown content.</summary>
    [JsonPropertyName("mappingMarkdown")]
    public string? MappingMarkdown { get; set; }
}

/// <summary>Path-keyed document payload used for requirements wiki imports.</summary>
public sealed class RequirementsIngestDocument
{
    /// <summary>UTF-8 text content for a wiki or canonical document.</summary>
    [JsonPropertyName("content")]
    public string? Content { get; set; }

    /// <summary>Base64-encoded UTF-8 content for binary-safe REPL and plugin transport.</summary>
    [JsonPropertyName("contentBase64")]
    public string? ContentBase64 { get; set; }

    /// <summary>Optional file or ZIP entry modified timestamp used for wiki source selection.</summary>
    [JsonPropertyName("lastModifiedUtc")]
    public DateTimeOffset? LastModifiedUtc { get; set; }
}

/// <summary>Result of bulk requirements ingest.</summary>
public sealed class RequirementsIngestResult
{
    /// <summary>Total FR entries parsed from input markdown.</summary>
    [JsonPropertyName("functionalParsed")]
    public int FunctionalParsed { get; set; }

    /// <summary>Total FR entries added.</summary>
    [JsonPropertyName("functionalAdded")]
    public int FunctionalAdded { get; set; }

    /// <summary>Total FR entries updated.</summary>
    [JsonPropertyName("functionalUpdated")]
    public int FunctionalUpdated { get; set; }

    /// <summary>Total FR entries deleted.</summary>
    [JsonPropertyName("functionalDeleted")]
    public int FunctionalDeleted { get; set; }

    /// <summary>Total FR entries ignored because they already matched.</summary>
    [JsonPropertyName("functionalIgnored")]
    public int FunctionalIgnored { get; set; }

    /// <summary>Total TR entries parsed from input markdown.</summary>
    [JsonPropertyName("technicalParsed")]
    public int TechnicalParsed { get; set; }

    /// <summary>Total TR entries added.</summary>
    [JsonPropertyName("technicalAdded")]
    public int TechnicalAdded { get; set; }

    /// <summary>Total TR entries updated.</summary>
    [JsonPropertyName("technicalUpdated")]
    public int TechnicalUpdated { get; set; }

    /// <summary>Total TR entries deleted.</summary>
    [JsonPropertyName("technicalDeleted")]
    public int TechnicalDeleted { get; set; }

    /// <summary>Total TR entries ignored because they already matched.</summary>
    [JsonPropertyName("technicalIgnored")]
    public int TechnicalIgnored { get; set; }

    /// <summary>Total TEST entries parsed from input markdown.</summary>
    [JsonPropertyName("testingParsed")]
    public int TestingParsed { get; set; }

    /// <summary>Total TEST entries added.</summary>
    [JsonPropertyName("testingAdded")]
    public int TestingAdded { get; set; }

    /// <summary>Total TEST entries updated.</summary>
    [JsonPropertyName("testingUpdated")]
    public int TestingUpdated { get; set; }

    /// <summary>Total TEST entries deleted.</summary>
    [JsonPropertyName("testingDeleted")]
    public int TestingDeleted { get; set; }

    /// <summary>Total TEST entries ignored because they already matched.</summary>
    [JsonPropertyName("testingIgnored")]
    public int TestingIgnored { get; set; }

    /// <summary>Total mapping rows parsed from input markdown.</summary>
    [JsonPropertyName("mappingParsed")]
    public int MappingParsed { get; set; }

    /// <summary>Total mapping rows added.</summary>
    [JsonPropertyName("mappingAdded")]
    public int MappingAdded { get; set; }

    /// <summary>Total mapping rows updated.</summary>
    [JsonPropertyName("mappingUpdated")]
    public int MappingUpdated { get; set; }

    /// <summary>Total mapping rows deleted.</summary>
    [JsonPropertyName("mappingDeleted")]
    public int MappingDeleted { get; set; }

    /// <summary>Total mapping rows ignored because they already matched.</summary>
    [JsonPropertyName("mappingIgnored")]
    public int MappingIgnored { get; set; }

    /// <summary>Selected wiki platform for a wiki import.</summary>
    [JsonPropertyName("selectedWikiFormat")]
    public string? SelectedWikiFormat { get; set; }

    /// <summary>Reason the selected wiki platform was chosen.</summary>
    [JsonPropertyName("selectedWikiReason")]
    public string? SelectedWikiReason { get; set; }

    /// <summary>Manifest timestamp for the selected wiki platform.</summary>
    [JsonPropertyName("selectedManifestGeneratedAtUtc")]
    public DateTimeOffset? SelectedManifestGeneratedAtUtc { get; set; }

    /// <summary>Latest file modified timestamp for the selected wiki platform.</summary>
    [JsonPropertyName("selectedLatestFileModifiedUtc")]
    public DateTimeOffset? SelectedLatestFileModifiedUtc { get; set; }

    /// <summary>Non-fatal ingest warnings.</summary>
    [JsonPropertyName("warnings")]
    public IReadOnlyList<string> Warnings { get; set; } = [];
}
