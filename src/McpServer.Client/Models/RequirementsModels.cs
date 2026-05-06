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

    /// <summary>Owning workspace discriminator.</summary>
    [JsonPropertyName("workspaceId")]
    public string WorkspaceId { get; set; } = string.Empty;
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
}

/// <summary>Request payload for updating a functional requirement entry.</summary>
public sealed class UpdateFrRequest
{
    /// <summary>Requirement title.</summary>
    [JsonPropertyName("title")]
    public string Title { get; set; } = string.Empty;

    /// <summary>Requirement body.</summary>
    [JsonPropertyName("body")]
    public string Body { get; set; } = string.Empty;
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
}

/// <summary>Request payload for updating a technical requirement entry.</summary>
public sealed class UpdateTrRequest
{
    /// <summary>Requirement title.</summary>
    [JsonPropertyName("title")]
    public string? Title { get; set; }

    /// <summary>Requirement body.</summary>
    [JsonPropertyName("body")]
    public string Body { get; set; } = string.Empty;
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
}

/// <summary>Request payload for updating a testing requirement entry.</summary>
public sealed class UpdateTestRequest
{
    /// <summary>Testing condition text.</summary>
    [JsonPropertyName("condition")]
    public string Condition { get; set; } = string.Empty;
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
}

/// <summary>Request payload for bulk requirements ingest from markdown text.</summary>
public sealed class RequirementsIngestRequest
{
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

    /// <summary>Total TR entries parsed from input markdown.</summary>
    [JsonPropertyName("technicalParsed")]
    public int TechnicalParsed { get; set; }

    /// <summary>Total TR entries added.</summary>
    [JsonPropertyName("technicalAdded")]
    public int TechnicalAdded { get; set; }

    /// <summary>Total TR entries updated.</summary>
    [JsonPropertyName("technicalUpdated")]
    public int TechnicalUpdated { get; set; }

    /// <summary>Total TEST entries parsed from input markdown.</summary>
    [JsonPropertyName("testingParsed")]
    public int TestingParsed { get; set; }

    /// <summary>Total TEST entries added.</summary>
    [JsonPropertyName("testingAdded")]
    public int TestingAdded { get; set; }

    /// <summary>Total TEST entries updated.</summary>
    [JsonPropertyName("testingUpdated")]
    public int TestingUpdated { get; set; }

    /// <summary>Total mapping rows parsed from input markdown.</summary>
    [JsonPropertyName("mappingParsed")]
    public int MappingParsed { get; set; }

    /// <summary>Total mapping rows added.</summary>
    [JsonPropertyName("mappingAdded")]
    public int MappingAdded { get; set; }

    /// <summary>Total mapping rows updated.</summary>
    [JsonPropertyName("mappingUpdated")]
    public int MappingUpdated { get; set; }
}
